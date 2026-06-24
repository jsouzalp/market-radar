using MarketRadar.Application.Contracts.Services;

namespace MarketRadar.Application.Services;

public sealed class SymbolNormalizer : ISymbolNormalizer
{
    public string Normalize(string providerSymbol)
        => providerSymbol.Trim().ToUpperInvariant();
}
