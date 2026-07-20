using FinOS.Common.Extensions;
using FinOS.Identity.Application.Interfaces;
using FinOS.Identity.Application.Services;
using FinOS.Identity.Domain.Interfaces;
using FinOS.Identity.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.Identity.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddFinOSDataAccess();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        
        // Register application services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        
        return services;
    }
}
