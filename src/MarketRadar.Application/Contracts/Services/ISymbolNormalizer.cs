namespace MarketRadar.Application.Contracts.Services;

public interface ISymbolNormalizer
{
    string Normalize(string providerSymbol);
}
