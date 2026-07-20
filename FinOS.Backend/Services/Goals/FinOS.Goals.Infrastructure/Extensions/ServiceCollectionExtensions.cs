using FinOS.Common.Extensions;
using FinOS.Goals.Domain.Interfaces;
using FinOS.Goals.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.Goals.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories
        services.AddFinOSDataAccess();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IGoalContributionRepository, GoalContributionRepository>();
        services.AddScoped<IGoalTemplateRepository, GoalTemplateRepository>();

        return services;
    }
}
