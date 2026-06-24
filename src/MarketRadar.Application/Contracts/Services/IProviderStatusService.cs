namespace MarketRadar.Application.Contracts.Services;

public interface IProviderStatusService
{
    void SetMarketClosed(string symbol);
    void SetMarketOpen(string symbol);
    bool IsMarketClosed(string symbol);
}
