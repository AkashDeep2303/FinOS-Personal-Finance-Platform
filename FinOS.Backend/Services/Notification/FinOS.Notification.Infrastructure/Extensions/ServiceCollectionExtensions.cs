using FinOS.Common.Extensions;
using FinOS.Notification.Domain.Interfaces;
using FinOS.Notification.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.Notification.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // Repositories
        services.AddFinOSDataAccess();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationSubscriptionRepository>();
        services.AddScoped<INotificationTypeRepository, NotificationTypeRepository>();

        return services;
    }
}
