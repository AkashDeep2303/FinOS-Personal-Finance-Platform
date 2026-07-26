using FinOS.Common.Extensions;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Investment.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.Investment.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories
        services.AddFinOSDataAccess();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IHoldingRepository, HoldingRepository>();
        services.AddScoped<ISIPRepository, SIPRepository>();
        services.AddScoped<IEPFAccountRepository, EPFAccountRepository>();
        services.AddScoped<IGoldPriceRepository, GoldPriceRepository>();
        services.AddScoped<IInvestmentTypeRepository, InvestmentTypeRepository>();
        services.AddScoped<ITargetAllocationRepository, TargetAllocationRepository>();
        services.AddScoped<IInvestmentAnalyticsRepository, InvestmentAnalyticsRepository>();

        return services;
    }
}
