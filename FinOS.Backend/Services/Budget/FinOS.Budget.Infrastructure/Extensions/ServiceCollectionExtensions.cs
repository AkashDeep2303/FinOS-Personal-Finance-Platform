using FinOS.Budget.Domain.Interfaces;
using FinOS.Budget.Infrastructure.Repositories;
using FinOS.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.Budget.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // Register IConnectionFactory and IUnitOfWork from FinOS.Common
        // Reads connection string from IConfiguration → ConnectionStrings:DefaultConnection
        services.AddFinOSDataAccess();

        // Register Dapper repositories
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IBudgetCategoryRepository, BudgetCategoryRepository>();
        services.AddScoped<ISavingsRuleRepository, SavingsRuleRepository>();

        return services;
    }
}
