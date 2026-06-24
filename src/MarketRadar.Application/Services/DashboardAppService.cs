using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Application.Contracts.Services;
using MarketRadar.Application.Settings;
using MarketRadar.Application.ViewModels.Alerts;
using MarketRadar.Application.ViewModels.Charts;
using MarketRadar.Application.ViewModels.Dashboard;
using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Results;
using MarketRadar.Domain.Services;
using MarketRadar.Domain.Settings;
using Microsoft.Extensions.Options;

namespace MarketRadar.Application.Services;

public sealed class DashboardAppService : IDashboardAppService
{
    private readonly ICandleRepository _candleRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly ITrendBreakAnalysisService _trendBreakService;
    private readonly IMarketDataQualityService _qualityService;
    private readonly IProviderStatusService _providerStatus;
    private readonly IAlertCooldownService _cooldownService;
    private readonly TrendAnalysisSettings _trendSettings;
    private readonly MovingAverageSettings _maSettings;
    private readonly MarketDataQualitySettings _qualitySettings;
    private readonly AlertCooldownSettings _cooldownSettings;

    public DashboardAppService(
        ICandleRepository candleRepository,
        IAlertRepository alertRepository,
        ITrendBreakAnalysisService trendBreakService,
        IMarketDataQualityService qualityService,
        IProviderStatusService providerStatus,
        IAlertCooldownService cooldownService,
        IOptions<TrendAnalysisSettings> trendSettings,
        IOptions<MovingAverageSettings> maSettings,
        IOptions<MarketDataQualitySettings> qualitySettings,
        IOptions<AlertCooldownSettings> cooldownSettings)
    {
        _candleRepository  = candleRepository;
        _alertRepository   = alertRepository;
        _trendBreakService = trendBreakService;
        _qualityService    = qualityService;
        _providerStatus    = providerStatus;
        _cooldownService   = cooldownService;
        _trendSettings     = trendSettings.Value;
        _maSettings        = maSettings.Value;
        _qualitySettings   = qualitySettings.Value;
        _cooldownSettings  = cooldownSettings.Value;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken)
    {
        // MarketClosed takes priority — provider explicitly returned empty
        if (_providerStatus.IsMarketClosed(symbol))
        {
            var count = await _candleRepository.CountAsync(symbol, timeframe, cancellationToken);
            return new DashboardViewModel
            {
                Symbol                 = symbol,
                Timeframe              = timeframe,
                Status                 = DashboardStatus.MarketClosed,
                CandleCount            = count,
                MinimumRequiredCandles = _qualitySettings.MinimumRequiredCandles
            };
        }

        var recentCandles = await _candleRepository.GetRecentAsync(
            symbol, timeframe, _trendSettings.RegressionWindowCandles, cancellationToken);

        var candleList    = recentCandles.ToList();
        var qualityResult = _qualityService.Evaluate(candleList, _qualitySettings);

        if (!qualityResult.CanAnalyze)
        {
            var count = await _candleRepository.CountAsync(symbol, timeframe, cancellationToken);
            return new DashboardViewModel
            {
                Symbol                 = symbol,
                Timeframe              = timeframe,
                Status                 = MapQualityStatus(qualityResult.Status),
                CandleCount            = count,
                MinimumRequiredCandles = _qualitySettings.MinimumRequiredCandles
            };
        }

        var analysis     = _trendBreakService.Analyze(candleList, _trendSettings, _maSettings);
        var recentAlerts = await _alertRepository.GetRecentAsync(symbol, 10, cancellationToken);
        var last         = candleList[^1];
        var prev         = candleList[^2];

        var absVar = last.ClosePrice - prev.ClosePrice;
        var pctVar = prev.ClosePrice != 0m
            ? absVar / prev.ClosePrice * 100m
            : 0m;

        var chartPoints = BuildChartPoints(candleList, analysis.TrendLine);

        // Cooldown state for dashboard display
        var alertType     = analysis.AlertType?.ToString() ?? "None";
        var cooldownState = _cooldownService.GetCurrentState(symbol, alertType, _cooldownSettings);

        return new DashboardViewModel
        {
            Symbol                  = symbol,
            Timeframe               = timeframe,
            Status                  = DashboardStatus.Online,
            CurrentPrice            = last.ClosePrice,
            PreviousClosePrice      = prev.ClosePrice,
            AbsoluteVariation       = absVar,
            PercentageVariation     = pctVar,
            TrendLine               = MapTrendLine(analysis.TrendLine),
            MovingAverages          = analysis.MovingAverages.Select(MapMovingAverage).ToList().AsReadOnly(),
            CurrentAnalysis         = MapTrendBreak(analysis),
            LastAlert               = recentAlerts.Count > 0 ? MapAlert(recentAlerts.First()) : null,
            RecentAlerts            = recentAlerts.Select(MapAlert).ToList().AsReadOnly(),
            ChartPoints             = chartPoints,
            LastUpdatedAt           = last.OpenTime,
            CooldownConsecutiveCount = cooldownState.ConsecutiveCount,
            CooldownMaxCount        = cooldownState.MaxConsecutiveAlerts,
            IsCooldownActive        = cooldownState.IsBlocked
        };
    }

    private static IReadOnlyCollection<ChartPointViewModel> BuildChartPoints(
        IReadOnlyList<MarketCandle> candles,
        TrendLineResult trendLine)
    {
        var ema9History  = ComputeEmaHistory(candles, 9);
        var ema21History = ComputeEmaHistory(candles, 21);
        var ema50History = ComputeEmaHistory(candles, 50);

        var points = new List<ChartPointViewModel>(candles.Count);
        for (int i = 0; i < candles.Count; i++)
        {
            points.Add(new ChartPointViewModel
            {
                Time           = candles[i].OpenTime,
                ClosePrice     = candles[i].ClosePrice,
                TrendLinePrice = trendLine.Intercept + trendLine.Slope * i,
                Ema9           = ema9History[i],
                Ema21          = ema21History[i],
                Ema50          = ema50History[i]
            });
        }

        return points.AsReadOnly();
    }

    private static decimal?[] ComputeEmaHistory(IReadOnlyList<MarketCandle> candles, int period)
    {
        var result = new decimal?[candles.Count];
        if (candles.Count < period) return result;

        decimal sma  = candles.Take(period).Average(c => c.ClosePrice);
        decimal mult = 2m / (period + 1);
        decimal ema  = sma;
        result[period - 1] = sma;

        for (int i = period; i < candles.Count; i++)
        {
            ema       = (candles[i].ClosePrice - ema) * mult + ema;
            result[i] = ema;
        }

        return result;
    }

    private static TrendLineViewModel MapTrendLine(TrendLineResult tl) =>
        new()
        {
            Slope                     = tl.Slope,
            Intercept                 = tl.Intercept,
            CurrentTrendPrice         = tl.CurrentTrendPrice,
            CurrentClosePrice         = tl.CurrentClosePrice,
            DistanceFromTrendLine     = tl.DistanceFromTrendLine,
            ResidualStandardDeviation = tl.ResidualStandardDeviation,
            Direction                 = tl.Direction.ToString()
        };

    private static MovingAverageViewModel MapMovingAverage(MovingAverageResult ma) =>
        new()
        {
            Period              = ma.Period,
            CurrentValue        = ma.CurrentValue,
            PreviousValue       = ma.PreviousValue,
            Direction           = ma.Direction.ToString(),
            CurrentPriceIsAbove = ma.CurrentPriceIsAbove
        };

    private static TrendBreakViewModel MapTrendBreak(TrendBreakAnalysisResult r) =>
        new()
        {
            Symbol    = r.Symbol,
            Timeframe = r.Timeframe,
            Status    = r.Status.ToString(),
            HasAlert  = r.HasAlert,
            AlertType = r.AlertType?.ToString(),
            Severity  = r.Severity?.ToString(),
            Score     = r.Score,
            Message   = r.Message
        };

    private static MarketAlertViewModel MapAlert(MarketAlert a) =>
        new()
        {
            Id          = a.Id,
            Symbol      = a.Symbol,
            Timeframe   = a.Timeframe,
            AlertType   = a.AlertType.ToString(),
            Severity    = a.Severity.ToString(),
            Score       = a.Score,
            Message     = a.Message,
            CreatedAt   = a.CreatedAt,
            TimeDisplay = a.CreatedAt.ToString("HH:mm:ss")
        };

    private static DashboardStatus MapQualityStatus(MarketDataQualityStatus status) =>
        status switch
        {
            MarketDataQualityStatus.WaitingForEnoughData => DashboardStatus.WaitingForEnoughData,
            MarketDataQualityStatus.ProviderUnavailable  => DashboardStatus.ProviderUnavailable,
            MarketDataQualityStatus.StaleData            => DashboardStatus.StaleData,
            MarketDataQualityStatus.MarketClosed         => DashboardStatus.MarketClosed,
            _                                            => DashboardStatus.Error
        };
}
