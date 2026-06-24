using MarketRadar.Application.Contracts.Factories;
using MarketRadar.Application.Contracts.Providers;
using MarketRadar.Application.Settings;
using MarketRadar.Infrastructure.Providers.Mock;
using Microsoft.Extensions.Logging;

namespace MarketRadar.Infrastructure.Providers;

public sealed class MarketDataProviderFactory : IMarketDataProviderFactory
{
    private readonly MarketDataProviderSettings _settings;
    private readonly ILogger<MockMarketDataProvider> _mockLogger;

    public MarketDataProviderFactory(
        MarketDataProviderSettings settings,
        ILogger<MockMarketDataProvider> mockLogger)
    {
        _settings   = settings;
        _mockLogger = mockLogger;
    }

    public IMarketDataProvider Create() => _settings.ActiveProvider switch
    {
        "Mock" => new MockMarketDataProvider(_settings, _mockLogger),
        _      => throw new NotSupportedException(
                      $"Provider '{_settings.ActiveProvider}' is not implemented.")
    };
}
