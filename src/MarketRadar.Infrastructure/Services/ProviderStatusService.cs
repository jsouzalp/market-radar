using System.Collections.Concurrent;
using MarketRadar.Application.Contracts.Services;

namespace MarketRadar.Infrastructure.Services;

public sealed class ProviderStatusService : IProviderStatusService
{
    private readonly ConcurrentDictionary<string, bool> _closed = new(StringComparer.OrdinalIgnoreCase);

    public void SetMarketClosed(string symbol) => _closed[symbol] = true;
    public void SetMarketOpen(string symbol)   => _closed[symbol] = false;
    public bool IsMarketClosed(string symbol)  => _closed.TryGetValue(symbol, out var v) && v;
}
