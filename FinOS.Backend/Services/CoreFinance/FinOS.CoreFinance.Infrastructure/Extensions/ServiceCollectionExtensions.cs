using FinOS.Common.Extensions;
using FinOS.CoreFinance.Application.Services;
using FinOS.CoreFinance.Domain.Interfaces;
using FinOS.CoreFinance.Infrastructure.Repositories;
using FinOS.CoreFinance.Infrastructure.Storage;
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
        services.AddScoped<ITaxRepository, TaxRepository>();
        services.AddScoped<IInsuranceRepository, InsuranceRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IDataCenterRepository, DataCenterRepository>();
        services.AddScoped<IFinancialDocumentRepository, FinancialDocumentRepository>();
        services.AddSingleton<IFinancialDocumentStorage, LocalFinancialDocumentStorage>();
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        
        // Register application services
        services.AddScoped<IBalanceUpdateService, BalanceUpdateService>();
        services.AddScoped<ISubscriptionDetectionService, SubscriptionDetectionService>();
        
        return services;
    }
}
