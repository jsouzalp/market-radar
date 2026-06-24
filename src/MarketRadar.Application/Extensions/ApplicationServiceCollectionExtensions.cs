using MarketRadar.Application.Contracts.Services;
using MarketRadar.Application.Services;
using MarketRadar.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketRadar.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Domain services — pure functions, no state → singleton
        services.AddSingleton<ITrendLineService,          TrendLineService>();
        services.AddSingleton<IMovingAverageService,      MovingAverageService>();
        services.AddSingleton<ITrendScoreService,         TrendScoreService>();
        services.AddSingleton<ITrendBreakAnalysisService, TrendBreakAnalysisService>();
        services.AddSingleton<ICandleValidator,           CandleValidator>();
        services.AddSingleton<IMarketDataQualityService,  MarketDataQualityService>();

        // Application normalizers — stateless → singleton
        services.AddSingleton<ISymbolNormalizer,    SymbolNormalizer>();
        services.AddSingleton<ITimeframeNormalizer, TimeframeNormalizer>();
        services.AddScoped<IMarketCandleNormalizer, MarketCandleNormalizer>();

        // Application orchestration services — scoped (depend on scoped repositories)
        services.AddScoped<IMarketMonitoringAppService, MarketMonitoringAppService>();
        services.AddScoped<IDashboardAppService,        DashboardAppService>();

        return services;
    }
}
