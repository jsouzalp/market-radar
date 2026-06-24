using MarketRadar.Application.ViewModels.Dashboard;

namespace MarketRadar.Application.Contracts.Services;

public interface IDashboardAppService
{
    Task<DashboardViewModel> GetDashboardAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken);
}
