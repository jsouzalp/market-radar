using MarketRadar.Application.Contracts.Providers;
using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Application.Contracts.Services;
using MarketRadar.Application.Extensions;
using MarketRadar.Application.Settings;
using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Settings;
using MarketRadar.Infrastructure.Data;
using MarketRadar.Infrastructure.Providers.Mock;
using MarketRadar.Infrastructure.Repositories;
using MarketRadar.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarketRadar.Application.Tests.Helpers;

internal sealed class NullAlertNotificationService : IAlertNotificationService
{
    public event Action<MarketAlert>? AlertReceived { add { } remove { } }
    public void Notify(MarketAlert alert) { }
}

internal sealed class AppTestFixture : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ServiceProvider  _provider;

    public CapturingAlertDispatcher Dispatcher { get; } = new();

    public AppTestFixture(
        string mockScenario          = "TrendBreakDown",
        int    maxConsecutiveAlerts  = 5,
        int    minCyclesToReset      = 3,
        int    minimumRequiredCandles = 120)
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();

        var services = new ServiceCollection();

        // EF Core — shared open connection keeps the in-memory DB alive across scopes
        services.AddDbContext<MarketRadarDbContext>(o => o.UseSqlite(_conn));

        // Repositories (scoped — one DbContext per scope)
        services.AddScoped<ICandleRepository, CandleRepository>();
        services.AddScoped<IAlertRepository,  AlertRepository>();

        // Domain + application services (pure, stateless)
        services.AddApplicationServices();

        // Infrastructure singletons
        services.AddSingleton<IProviderStatusService, ProviderStatusService>();
        services.AddSingleton<IAlertCooldownService,  AlertCooldownService>();

        // Test doubles
        services.AddSingleton<IAlertDispatcher>(Dispatcher);
        services.AddSingleton<IAlertNotificationService, NullAlertNotificationService>();

        // MockMarketDataProvider — UseOnlyClosedCandles=false so candles near UtcNow pass validation
        services.AddSingleton<IMarketDataProvider>(_ =>
            new MockMarketDataProvider(
                new MarketDataProviderSettings
                {
                    ActiveProvider        = "Mock",
                    MockScenario          = mockScenario,
                    UseOnlyClosedCandles  = false,
                    RequestOverlapMinutes = 5
                },
                NullLogger<MockMarketDataProvider>.Instance));

        // Settings
        services.Configure<MarketMonitorSettings>(s =>
        {
            s.HistoryDays = 1;
            s.Symbols     = [new MarketMonitorSymbolSettings { Code = "XAUUSD", Enabled = true, Timeframe = "M1" }];
        });
        services.Configure<MarketDataProviderSettings>(s =>
        {
            s.MockScenario          = mockScenario;
            s.UseOnlyClosedCandles  = false;
            s.RequestOverlapMinutes = 5;
        });
        services.Configure<TrendAnalysisSettings>(s =>
        {
            s.RegressionWindowCandles      = 120;
            s.RequiredBreakCandles         = 3;
            s.DeviationMultiplier          = 1.5m;
            s.MinimumSlope                 = 0.001m;
            s.MinimumBreakScore            = 70;
            s.RecentHighLowWindowCandles   = 20;
            s.FalseBreakoutLookbackCandles = 5;
        });
        services.Configure<MovingAverageSettings>(s => s.Periods = [9, 21, 50]);
        services.Configure<MarketDataQualitySettings>(s =>
        {
            s.MinimumRequiredCandles    = minimumRequiredCandles;
            s.StaleDataToleranceMinutes = 5;
        });
        services.Configure<AlertCooldownSettings>(s =>
        {
            s.MaxConsecutiveAlerts = maxConsecutiveAlerts;
            s.MinCyclesToReset     = minCyclesToReset;
        });

        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        _provider = services.BuildServiceProvider();

        // Create schema in the in-memory database
        using var initScope = _provider.CreateScope();
        initScope.ServiceProvider
                 .GetRequiredService<MarketRadarDbContext>()
                 .Database.EnsureCreated();
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    public void Dispose()
    {
        _provider.Dispose();
        _conn.Dispose();
    }
}
