using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Application.Tests.Helpers;
using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace MarketRadar.Application.Tests.Repositories;

public class AlertRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsAlert()
    {
        using var fx    = new AppTestFixture();
        using var scope = fx.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();

        await repo.AddAsync(MakeAlert("XAUUSD"), CancellationToken.None);

        var result = await repo.GetRecentAsync("XAUUSD", 1, CancellationToken.None);
        result.Should().HaveCount(1);
        result.First().Symbol.Should().Be("XAUUSD");
        result.First().AlertType.Should().Be(AlertType.TrendBreakDown);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsDescendingOrder()
    {
        using var fx    = new AppTestFixture();
        using var scope = fx.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();

        // Add three alerts with explicit CreatedAt overridden post-insert by inspecting order
        await repo.AddAsync(MakeAlert("XAUUSD", score: 80m), CancellationToken.None);
        await Task.Delay(5); // ensure different CreatedAt timestamps
        await repo.AddAsync(MakeAlert("XAUUSD", score: 85m), CancellationToken.None);
        await Task.Delay(5);
        await repo.AddAsync(MakeAlert("XAUUSD", score: 90m), CancellationToken.None);

        var result = await repo.GetRecentAsync("XAUUSD", 3, CancellationToken.None);

        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(a => a.CreatedAt);
    }

    private static MarketAlert MakeAlert(string symbol, decimal score = 90m) =>
        new()
        {
            Symbol    = symbol,
            Timeframe = "M1",
            AlertType = AlertType.TrendBreakDown,
            Severity  = AlertSeverity.Warning,
            Score     = score,
            Message   = "Test alert"
        };
}
