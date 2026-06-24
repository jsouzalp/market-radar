using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Application.Contracts.Services;
using MarketRadar.Application.Tests.Helpers;
using MarketRadar.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace MarketRadar.Application.Tests.Services;

public class MarketMonitoringAppServiceTests
{
    [Fact]
    public async Task TrendBreakDown_GeneratesAndDispatchesAlert()
    {
        using var fx    = new AppTestFixture(mockScenario: "TrendBreakDown");
        using var scope = fx.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IMarketMonitoringAppService>();

        await svc.ExecuteMonitoringCycleAsync("XAUUSD", "M1", CancellationToken.None);

        fx.Dispatcher.Dispatched.Should().HaveCount(1);
        fx.Dispatcher.Dispatched[0].AlertType.Should().Be(AlertType.TrendBreakDown);
        fx.Dispatcher.Dispatched[0].Score.Should().BeGreaterThanOrEqualTo(70);

        // Alert also persisted to DB
        using var readScope = fx.CreateScope();
        var alertRepo  = readScope.ServiceProvider.GetRequiredService<IAlertRepository>();
        var dbAlerts   = await alertRepo.GetRecentAsync("XAUUSD", 1, CancellationToken.None);
        dbAlerts.Should().HaveCount(1);
    }

    [Fact]
    public async Task EmptyResponse_SetsMarketClosed_NoAlert()
    {
        using var fx    = new AppTestFixture(mockScenario: "EmptyResponse");
        using var scope = fx.CreateScope();
        var svc            = scope.ServiceProvider.GetRequiredService<IMarketMonitoringAppService>();
        var providerStatus = scope.ServiceProvider.GetRequiredService<IProviderStatusService>();

        await svc.ExecuteMonitoringCycleAsync("XAUUSD", "M1", CancellationToken.None);

        providerStatus.IsMarketClosed("XAUUSD").Should().BeTrue();
        fx.Dispatcher.Dispatched.Should().BeEmpty();
    }

    [Fact]
    public async Task InsufficientCandles_QualityGateFails_NoAlert()
    {
        // minimumRequiredCandles=200 > RegressionWindowCandles=120 so quality always fails
        using var fx    = new AppTestFixture(mockScenario: "TrendBreakDown", minimumRequiredCandles: 200);
        using var scope = fx.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IMarketMonitoringAppService>();

        await svc.ExecuteMonitoringCycleAsync("XAUUSD", "M1", CancellationToken.None);

        fx.Dispatcher.Dispatched.Should().BeEmpty();
    }

    [Fact]
    public async Task Cooldown_BlocksAfterMaxConsecutive()
    {
        using var fx  = new AppTestFixture(mockScenario: "TrendBreakDown", maxConsecutiveAlerts: 2);
        var ct = CancellationToken.None;

        for (int i = 0; i < 3; i++)
        {
            using var scope = fx.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IMarketMonitoringAppService>();
            await svc.ExecuteMonitoringCycleAsync("XAUUSD", "M1", ct);
        }

        // Cycle 1 and 2 dispatch; cycle 3 is blocked by cooldown (max=2)
        fx.Dispatcher.Dispatched.Should().HaveCount(2);
    }

    [Fact]
    public async Task MarketClosed_Scenario_NoCandlesPersisted()
    {
        using var fx    = new AppTestFixture(mockScenario: "MarketClosed");
        using var scope = fx.CreateScope();
        var svc      = scope.ServiceProvider.GetRequiredService<IMarketMonitoringAppService>();
        var candleRepo = scope.ServiceProvider.GetRequiredService<ICandleRepository>();

        await svc.ExecuteMonitoringCycleAsync("XAUUSD", "M1", CancellationToken.None);

        var count = await candleRepo.CountAsync("XAUUSD", "M1", CancellationToken.None);
        count.Should().Be(0);
        fx.Dispatcher.Dispatched.Should().BeEmpty();
    }
}
