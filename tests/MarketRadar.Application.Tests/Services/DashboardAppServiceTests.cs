using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Application.Contracts.Services;
using MarketRadar.Application.Tests.Helpers;
using MarketRadar.Application.ViewModels.Dashboard;
using MarketRadar.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace MarketRadar.Application.Tests.Services;

public class DashboardAppServiceTests
{
    [Fact]
    public async Task GetDashboard_WhenMarketClosed_ReturnsMarketClosedStatus()
    {
        using var fx    = new AppTestFixture();
        using var scope = fx.CreateScope();
        var svc            = scope.ServiceProvider.GetRequiredService<IDashboardAppService>();
        var providerStatus = scope.ServiceProvider.GetRequiredService<IProviderStatusService>();

        providerStatus.SetMarketClosed("XAUUSD");

        var vm = await svc.GetDashboardAsync("XAUUSD", "M1", CancellationToken.None);

        vm.Status.Should().Be(DashboardStatus.MarketClosed);
        vm.CandleCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboard_WhenNoCandles_ReturnsWaitingForEnoughData()
    {
        using var fx    = new AppTestFixture();
        using var scope = fx.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDashboardAppService>();
        // DB is empty; providerStatus defaults to "open" (not closed)

        var vm = await svc.GetDashboardAsync("XAUUSD", "M1", CancellationToken.None);

        vm.Status.Should().Be(DashboardStatus.WaitingForEnoughData);
        vm.CandleCount.Should().Be(0);
        vm.MinimumRequiredCandles.Should().Be(120);
    }

    [Fact]
    public async Task GetDashboard_WithSufficientCandles_ReturnsOnline()
    {
        using var fx = new AppTestFixture();

        // Seed 120 uptrend candles directly via repository
        using (var scope = fx.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
            await repo.AddOrUpdateAsync(MakeUptrendCandles(120), CancellationToken.None);
        }

        using (var scope = fx.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IDashboardAppService>();
            var vm  = await svc.GetDashboardAsync("XAUUSD", "M1", CancellationToken.None);

            vm.Status.Should().Be(DashboardStatus.Online);
            vm.TrendLine.Should().NotBeNull();
            vm.TrendLine!.Slope.Should().BeGreaterThan(0m, "uptrend candles produce a positive slope");
            vm.CurrentPrice.Should().BeGreaterThan(0m);
        }
    }

    [Fact]
    public async Task GetDashboard_AfterAlertGenerated_LastAlertIsPopulated()
    {
        using var fx = new AppTestFixture(mockScenario: "TrendBreakDown");

        // Run one monitoring cycle to generate an alert
        using (var scope = fx.CreateScope())
        {
            var monSvc = scope.ServiceProvider.GetRequiredService<IMarketMonitoringAppService>();
            await monSvc.ExecuteMonitoringCycleAsync("XAUUSD", "M1", CancellationToken.None);
        }

        using (var scope = fx.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IDashboardAppService>();
            var vm  = await svc.GetDashboardAsync("XAUUSD", "M1", CancellationToken.None);

            vm.Status.Should().Be(DashboardStatus.Online);
            vm.LastAlert.Should().NotBeNull("a TrendBreakDown alert was generated in the monitoring cycle");
            vm.LastAlert!.AlertType.Should().Be("TrendBreakDown");
        }
    }

    private static IReadOnlyList<MarketCandle> MakeUptrendCandles(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new MarketCandle
            {
                Symbol     = "XAUUSD",
                Timeframe  = "M1",
                OpenTime   = DateTime.UtcNow.AddMinutes(-(count - i)),
                OpenPrice  = 1800m + i * 0.5m,
                HighPrice  = 1800m + i * 0.5m + 0.1m,
                LowPrice   = 1800m + i * 0.5m - 0.1m,
                ClosePrice = 1800m + i * 0.5m,
                Volume     = 100m
            })
            .ToList()
            .AsReadOnly();
}
