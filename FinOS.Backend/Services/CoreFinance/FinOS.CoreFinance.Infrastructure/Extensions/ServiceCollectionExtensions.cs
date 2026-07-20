using FinOS.Common.Extensions;
using FinOS.CoreFinance.Application.Services;
using FinOS.CoreFinance.Domain.Interfaces;
using FinOS.CoreFinance.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.CoreFinance.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddFinOSDataAccess();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IRecurringScheduleRepository, RecurringScheduleRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        
        // Register application services
        services.AddScoped<IBalanceUpdateService, BalanceUpdateService>();
        services.AddScoped<ISubscriptionDetectionService, SubscriptionDetectionService>();
        
        return services;
    }
}
