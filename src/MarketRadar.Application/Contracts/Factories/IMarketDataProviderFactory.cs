using MarketRadar.Application.Contracts.Providers;

namespace MarketRadar.Application.Contracts.Factories;

public interface IMarketDataProviderFactory
{
    IMarketDataProvider Create();
}
