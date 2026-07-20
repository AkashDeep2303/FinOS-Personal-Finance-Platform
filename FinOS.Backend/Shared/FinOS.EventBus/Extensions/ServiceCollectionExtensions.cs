using FinOS.EventBus.Implementations;
using FinOS.EventBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.EventBus.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds in-memory event bus for local development (no external dependencies).
    /// </summary>
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        return services;
    }
}
