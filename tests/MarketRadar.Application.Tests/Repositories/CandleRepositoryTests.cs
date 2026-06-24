using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Application.Tests.Helpers;
using MarketRadar.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace MarketRadar.Application.Tests.Repositories;

public class CandleRepositoryTests
{
    [Fact]
    public async Task AddOrUpdate_NewCandles_ArePersisted()
    {
        using var fx    = new AppTestFixture();
        using var scope = fx.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICandleRepository>();

        await repo.AddOrUpdateAsync(MakeCandles(5), CancellationToken.None);

        var count = await repo.CountAsync("XAUUSD", "M1", CancellationToken.None);
        count.Should().Be(5);
    }

    [Fact]
    public async Task AddOrUpdate_DuplicateKey_Upserts()
    {
        using var fx     = new AppTestFixture();
        var baseTime = DateTime.UtcNow.AddMinutes(-10);

        using (var scope = fx.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
            await repo.AddOrUpdateAsync([MakeCandle(baseTime, close: 100m)], CancellationToken.None);
        }

        using (var scope = fx.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
            await repo.AddOrUpdateAsync([MakeCandle(baseTime, close: 105m)], CancellationToken.None);
        }

        using (var scope = fx.CreateScope())
        {
            var repo   = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
            var result = await repo.GetRecentAsync("XAUUSD", "M1", 1, CancellationToken.None);
            result.Should().HaveCount(1);
            result.First().ClosePrice.Should().Be(105m);
        }
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsMaxN_AscendingOrder()
    {
        using var fx    = new AppTestFixture();
        using var scope = fx.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICandleRepository>();

        await repo.AddOrUpdateAsync(MakeCandles(200), CancellationToken.None);

        var result = await repo.GetRecentAsync("XAUUSD", "M1", 120, CancellationToken.None);

        result.Should().HaveCount(120);
        result.Should().BeInAscendingOrder(c => c.OpenTime);
    }

    [Fact]
    public async Task CountAsync_IsIsolatedBySymbol()
    {
        using var fx    = new AppTestFixture();
        using var scope = fx.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICandleRepository>();

        await repo.AddOrUpdateAsync(MakeCandles(10, "XAUUSD"), CancellationToken.None);
        await repo.AddOrUpdateAsync(MakeCandles(5,  "EURUSD"), CancellationToken.None);

        var count = await repo.CountAsync("XAUUSD", "M1", CancellationToken.None);
        count.Should().Be(10);
    }

    private static MarketCandle MakeCandle(DateTime openTime, decimal close) =>
        new()
        {
            Symbol     = "XAUUSD",
            Timeframe  = "M1",
            OpenTime   = openTime,
            OpenPrice  = close,
            HighPrice  = close + 1m,
            LowPrice   = close - 1m,
            ClosePrice = close,
            Volume     = 100m
        };

    private static IReadOnlyList<MarketCandle> MakeCandles(int count, string symbol = "XAUUSD") =>
        Enumerable.Range(0, count)
            .Select(i => new MarketCandle
            {
                Symbol     = symbol,
                Timeframe  = "M1",
                OpenTime   = DateTime.UtcNow.AddMinutes(-(count - i) - 10),
                OpenPrice  = 100m + i,
                HighPrice  = 101m + i,
                LowPrice   =  99m + i,
                ClosePrice = 100m + i,
                Volume     = 100m
            })
            .ToList()
            .AsReadOnly();
}
