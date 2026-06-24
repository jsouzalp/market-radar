namespace MarketRadar.Application.ViewModels.Dashboard;

public enum DashboardStatus
{
    Loading              = 0,
    Online               = 1,
    WaitingForEnoughData = 2,
    ProviderUnavailable  = 3,
    StaleData            = 4,
    Error                = 5,
    MarketClosed         = 6
}
