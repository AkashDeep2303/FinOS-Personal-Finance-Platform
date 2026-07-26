using FinOS.Common.Extensions;
using FinOS.Analytics.Application.Services;
using FinOS.Analytics.Domain.Interfaces;
using FinOS.Analytics.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.Analytics.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories
        services.AddFinOSDataAccess();
        services.AddScoped<INetWorthRepository, NetWorthRepository>();
        services.AddScoped<IMonthlyAggregateRepository, MonthlyAggregateRepository>();
        services.AddScoped<IFinancialScoreRepository, FinancialScoreRepository>();
        services.AddScoped<IScenarioRepository, ScenarioRepository>();
        services.AddScoped<ICashFlowClassificationRepository, CashFlowClassificationRepository>();

        // Register application services
        services.AddScoped<IScoreCalculationService, ScoreCalculationService>();

        return services;
    }
}
