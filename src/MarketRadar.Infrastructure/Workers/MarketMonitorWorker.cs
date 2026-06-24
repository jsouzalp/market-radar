using MarketRadar.Application.Contracts.Services;
using MarketRadar.Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketRadar.Infrastructure.Workers;

public sealed class MarketMonitorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MarketMonitorSettings _monitorSettings;
    private readonly MarketDataProviderSettings _providerSettings;
    private readonly ILogger<MarketMonitorWorker> _logger;

    public MarketMonitorWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<MarketMonitorSettings> monitorSettings,
        IOptions<MarketDataProviderSettings> providerSettings,
        ILogger<MarketMonitorWorker> logger)
    {
        _scopeFactory     = scopeFactory;
        _monitorSettings  = monitorSettings.Value;
        _providerSettings = providerSettings.Value;
        _logger           = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketMonitorWorker started. Provider={Provider} PollingInterval={Interval}s",
            _providerSettings.ActiveProvider, _providerSettings.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var symbol in _monitorSettings.Symbols.Where(s => s.Enabled))
            {
                try
                {
                    using var scope       = _scopeFactory.CreateScope();
                    var monitoringService = scope.ServiceProvider
                        .GetRequiredService<IMarketMonitoringAppService>();

                    await monitoringService.ExecuteMonitoringCycleAsync(
                        symbol.Code, symbol.Timeframe, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error in monitoring cycle for {Symbol}/{Timeframe}.",
                        symbol.Code, symbol.Timeframe);
                }
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_providerSettings.PollingIntervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation("MarketMonitorWorker stopped.");
    }
}
