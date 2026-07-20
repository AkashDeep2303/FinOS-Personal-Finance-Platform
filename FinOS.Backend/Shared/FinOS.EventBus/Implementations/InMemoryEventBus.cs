using FinOS.EventBus.Events;
using FinOS.EventBus.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinOS.EventBus.Implementations;

/// <summary>
/// Simple in-memory event bus implementation for local development.
/// Events are published to registered handlers synchronously.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly Dictionary<Type, List<Type>> _handlerMap = new();

    public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IntegrationEvent
    {
        var eventType = typeof(TEvent);
        _logger.LogInformation("Publishing event: {EventType}", eventType.Name);

        if (!_handlerMap.TryGetValue(eventType, out var handlers))
        {
            _logger.LogWarning("No handlers registered for event: {EventType}", eventType.Name);
            return;
        }

        var tasks = new List<Task>();

        foreach (var handlerType in handlers)
        {
            try
            {
                var handler = _serviceProvider.GetService(handlerType);
                if (handler == null)
                {
                    _logger.LogWarning("Handler not found in service provider: {HandlerType}", handlerType.Name);
                    continue;
                }

                var handleMethod = handlerType.GetMethod("Handle",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    null, new[] { eventType, typeof(CancellationToken) }, null);

                if (handleMethod == null)
                {
                    _logger.LogWarning("Handle method not found on handler: {HandlerType}", handlerType.Name);
                    continue;
                }

                var task = (Task?)handleMethod.Invoke(handler, new object[] { @event, ct });
                if (task != null)
                {
                    tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event to handler {HandlerType}", handlerType.Name);
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }

        _logger.LogInformation("Event published successfully: {EventType}", eventType.Name);
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventType = typeof(TEvent);
        var handlerType = typeof(THandler);

        if (!_handlerMap.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Type>();
            _handlerMap[eventType] = handlers;
        }

        if (!handlers.Contains(handlerType))
        {
            handlers.Add(handlerType);
            _logger.LogInformation("Subscribed handler {HandlerType} to event {EventType}", handlerType.Name, eventType.Name);
        }
    }

    public void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventType = typeof(TEvent);
        var handlerType = typeof(THandler);

        if (_handlerMap.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handlerType);
            _logger.LogInformation("Unsubscribed handler {HandlerType} from event {EventType}", handlerType.Name, eventType.Name);
        }
    }
}
