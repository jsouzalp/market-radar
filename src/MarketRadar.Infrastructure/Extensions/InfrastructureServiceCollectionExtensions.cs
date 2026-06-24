using MarketRadar.Application.Contracts.Factories;
using MarketRadar.Application.Contracts.Providers;
using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Application.Contracts.Services;
using MarketRadar.Application.Settings;
using MarketRadar.Domain.Settings;
using MarketRadar.Infrastructure.Data;
using MarketRadar.Infrastructure.Providers;
using MarketRadar.Infrastructure.Providers.Mock;
using MarketRadar.Infrastructure.Repositories;
using MarketRadar.Infrastructure.Services;
using MarketRadar.Infrastructure.Settings;
using MarketRadar.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketRadar.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Application-layer settings (monitoring + provider)
        services.Configure<MarketMonitorSettings>(
            configuration.GetSection("MarketMonitor"));
        services.Configure<MarketDataProviderSettings>(
            configuration.GetSection("MarketDataProvider"));

        // Infrastructure-only settings
        services.Configure<AlertSettings>(
            configuration.GetSection("Alerts"));

        // Domain settings
        services.Configure<TrendAnalysisSettings>(
            configuration.GetSection("TrendAnalysis"));
        services.Configure<MovingAverageSettings>(
            configuration.GetSection("MovingAverages"));
        services.Configure<MarketDataQualitySettings>(
            configuration.GetSection("MarketDataQuality"));
        services.Configure<AlertCooldownSettings>(
            configuration.GetSection("AlertCooldown"));

        // SQLite / EF Core
        var connStr = configuration.GetConnectionString("MarketRadar")
                      ?? "Data Source=marketradar.db";
        services.AddDbContext<MarketRadarDbContext>(o => o.UseSqlite(connStr));

        // Repositories (scoped — one DbContext per scope)
        services.AddScoped<ICandleRepository, CandleRepository>();
        services.AddScoped<IAlertRepository,  AlertRepository>();

        // Provider factory (singleton; Create() called once at first resolution)
        services.AddSingleton<IMarketDataProviderFactory>(sp =>
        {
            var settings   = sp.GetRequiredService<IOptions<MarketDataProviderSettings>>().Value;
            var mockLogger = sp.GetRequiredService<ILogger<MockMarketDataProvider>>();
            return new MarketDataProviderFactory(settings, mockLogger);
        });
        services.AddSingleton<IMarketDataProvider>(
            sp => sp.GetRequiredService<IMarketDataProviderFactory>().Create());

        // Alert notification bus (singleton — Blazor components subscribe to AlertReceived)
        services.AddSingleton<IAlertNotificationService, AlertNotificationService>();

        // Provider status tracker (singleton — worker signals open/closed; dashboard reads it)
        services.AddSingleton<IProviderStatusService, ProviderStatusService>();

        // Alert cooldown (singleton — tracks consecutive alerts per symbol+type in memory)
        services.AddSingleton<IAlertCooldownService, AlertCooldownService>();

        // Alert dispatcher (scoped — used inside monitoring scope)
        services.AddScoped<IAlertDispatcher, AlertDispatcher>();

        // Background worker
        services.AddHostedService<MarketMonitorWorker>();

        return services;
    }
}
