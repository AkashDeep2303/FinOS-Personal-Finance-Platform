using FinOS.Common.Interfaces;
using FinOS.Loan.Domain.Interfaces;
using FinOS.Loan.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.Loan.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // Register Dapper-based repositories
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IEMIScheduleRepository, EMIScheduleRepository>();
        services.AddScoped<ILoanPrepaymentRepository, LoanPrepaymentRepository>();

        // IUnitOfWork is registered by FinOS.Common.Extensions.ServiceCollectionExtensions.AddFinOSDataAccess()
        // in the API host project. No need to register it here.

        return services;
    }
}
