using MarketRadar.Application.Contracts.Providers;
using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Application.Contracts.Services;
using MarketRadar.Application.Settings;
using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Services;
using MarketRadar.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketRadar.Application.Services;

public sealed class MarketMonitoringAppService : IMarketMonitoringAppService
{
    private readonly IMarketDataProvider _provider;
    private readonly IMarketCandleNormalizer _normalizer;
    private readonly ICandleValidator _candleValidator;
    private readonly ICandleRepository _candleRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IAlertDispatcher _alertDispatcher;
    private readonly ITrendBreakAnalysisService _trendBreakService;
    private readonly IMarketDataQualityService _qualityService;
    private readonly IProviderStatusService _providerStatus;
    private readonly IAlertCooldownService _cooldownService;
    private readonly MarketMonitorSettings _monitorSettings;
    private readonly MarketDataProviderSettings _providerSettings;
    private readonly TrendAnalysisSettings _trendSettings;
    private readonly MovingAverageSettings _maSettings;
    private readonly MarketDataQualitySettings _qualitySettings;
    private readonly AlertCooldownSettings _cooldownSettings;
    private readonly ILogger<MarketMonitoringAppService> _logger;

    public MarketMonitoringAppService(
        IMarketDataProvider provider,
        IMarketCandleNormalizer normalizer,
        ICandleValidator candleValidator,
        ICandleRepository candleRepository,
        IAlertRepository alertRepository,
        IAlertDispatcher alertDispatcher,
        ITrendBreakAnalysisService trendBreakService,
        IMarketDataQualityService qualityService,
        IProviderStatusService providerStatus,
        IAlertCooldownService cooldownService,
        IOptions<MarketMonitorSettings> monitorSettings,
        IOptions<MarketDataProviderSettings> providerSettings,
        IOptions<TrendAnalysisSettings> trendSettings,
        IOptions<MovingAverageSettings> maSettings,
        IOptions<MarketDataQualitySettings> qualitySettings,
        IOptions<AlertCooldownSettings> cooldownSettings,
        ILogger<MarketMonitoringAppService> logger)
    {
        _provider          = provider;
        _normalizer        = normalizer;
        _candleValidator   = candleValidator;
        _candleRepository  = candleRepository;
        _alertRepository   = alertRepository;
        _alertDispatcher   = alertDispatcher;
        _trendBreakService = trendBreakService;
        _qualityService    = qualityService;
        _providerStatus    = providerStatus;
        _cooldownService   = cooldownService;
        _monitorSettings   = monitorSettings.Value;
        _providerSettings  = providerSettings.Value;
        _trendSettings     = trendSettings.Value;
        _maSettings        = maSettings.Value;
        _qualitySettings   = qualitySettings.Value;
        _cooldownSettings  = cooldownSettings.Value;
        _logger            = logger;
    }

    public async Task ExecuteMonitoringCycleAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken)
    {
        // 1. Determine fetch range
        var lastCandles = await _candleRepository.GetRecentAsync(symbol, timeframe, 1, cancellationToken);
        DateTime from;
        if (lastCandles.Count > 0)
        {
            from = lastCandles.Max(c => c.OpenTime)
                              .AddMinutes(-_providerSettings.RequestOverlapMinutes);
        }
        else
        {
            from = DateTime.UtcNow.AddDays(-_monitorSettings.HistoryDays);
        }
        var to = DateTime.UtcNow;

        // 2. Fetch from provider
        var rawCandles = await _provider.GetLatestCandlesAsync(symbol, timeframe, from, to, cancellationToken);
        if (rawCandles.Count == 0)
        {
            _providerStatus.SetMarketClosed(symbol);
            _logger.LogInformation("MarketClosed: provider returned no candles for {Symbol}/{Timeframe}.", symbol, timeframe);
            return;
        }
        _providerStatus.SetMarketOpen(symbol);

        // 3. Normalize (symbol, timeframe, UTC, sort ascending)
        var normalized = _normalizer.Normalize(rawCandles, symbol, timeframe);

        // 4. Filter forming candle
        IReadOnlyCollection<MarketCandle> filtered = normalized;
        if (_providerSettings.UseOnlyClosedCandles)
        {
            var now = DateTime.UtcNow;
            filtered = normalized.Where(c => c.OpenTime.AddMinutes(1) <= now).ToList().AsReadOnly();
        }

        // 5. Validate — keep only valid candles
        var valid = filtered.Where(c => _candleValidator.Validate(c).IsValid).ToList().AsReadOnly();
        if (valid.Count == 0)
        {
            _logger.LogWarning("No valid candles after validation for {Symbol}/{Timeframe}.", symbol, timeframe);
            return;
        }

        // 6. Persist
        await _candleRepository.AddOrUpdateAsync(valid, cancellationToken);

        // 7. Load analysis window
        var recentCandles = await _candleRepository.GetRecentAsync(
            symbol, timeframe, _trendSettings.RegressionWindowCandles, cancellationToken);
        var recentList = recentCandles.ToList();

        // 8. Quality check
        var qualityResult = _qualityService.Evaluate(recentList, _qualitySettings);
        if (!qualityResult.CanAnalyze)
        {
            _logger.LogDebug("Quality check: {Status} for {Symbol}/{Timeframe} ({Count} candles).",
                qualityResult.Status, symbol, timeframe, recentList.Count);
            return;
        }

        // 9. Analyze
        var analysis = _trendBreakService.Analyze(recentList, _trendSettings, _maSettings);

        // 10. Cooldown evaluation (called every cycle to track quiet cycles too)
        var alertType = analysis.AlertType?.ToString() ?? "None";
        var cooldown  = _cooldownService.Evaluate(symbol, alertType, analysis.HasAlert, _cooldownSettings);

        if (!analysis.HasAlert)
            return;

        if (!cooldown.CanDispatch)
        {
            _logger.LogInformation(
                "Alert blocked by cooldown: {AlertType} {Symbol} ({Count}/{Max})",
                alertType, symbol, cooldown.ConsecutiveCount, cooldown.MaxConsecutiveAlerts);
            return;
        }

        var alert = new MarketAlert
        {
            Symbol    = symbol,
            Timeframe = timeframe,
            AlertType = analysis.AlertType!.Value,
            Severity  = analysis.Severity!.Value,
            Score     = analysis.Score,
            Message   = analysis.Message,
            CreatedAt = DateTime.UtcNow
        };

        await _alertRepository.AddAsync(alert, cancellationToken);
        await _alertDispatcher.DispatchAsync(alert, cancellationToken);

        _logger.LogInformation(
            "Alert generated: {AlertType} {Symbol}/{Timeframe} Score={Score}",
            alert.AlertType, alert.Symbol, alert.Timeframe, alert.Score);
    }
}
