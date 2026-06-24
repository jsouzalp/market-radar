using MarketRadar.Application.Contracts.Services;

namespace MarketRadar.Application.Services;

public sealed class TimeframeNormalizer : ITimeframeNormalizer
{
    public string Normalize(string providerTimeframe)
        => providerTimeframe.Trim().ToUpperInvariant();
}
