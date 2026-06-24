namespace MarketRadar.Application.Contracts.Services;

public interface IMarketMonitoringAppService
{
    Task ExecuteMonitoringCycleAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken);
}
