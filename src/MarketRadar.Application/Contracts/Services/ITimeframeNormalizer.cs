namespace MarketRadar.Application.Contracts.Services;

public interface ITimeframeNormalizer
{
    string Normalize(string providerTimeframe);
}
