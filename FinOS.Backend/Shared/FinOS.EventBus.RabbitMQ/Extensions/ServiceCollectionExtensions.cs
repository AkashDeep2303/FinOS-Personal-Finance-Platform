using FinOS.EventBus.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinOS.EventBus.RabbitMQ.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEventBus>(sp =>
        {
            var serviceProvider = sp.GetRequiredService<IServiceProvider>();
            var logger = sp.GetRequiredService<ILogger<RabbitMQEventBus>>();
            var hostName = configuration["EventBus:RabbitMQ:HostName"] ?? "localhost";
            var exchangeName = configuration["EventBus:RabbitMQ:ExchangeName"] ?? "finos_event_bus";
            var queueName = configuration["EventBus:RabbitMQ:QueueName"] ?? "finos_queue";
            return new RabbitMQEventBus(serviceProvider, logger, hostName, exchangeName, queueName);
        });

        return services;
    }
}
