using System.Text;
using System.Text.Json;
using FinOS.EventBus.Events;
using FinOS.EventBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FinOS.EventBus.RabbitMQ;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly Dictionary<string, List<Type>> _handlers = new();
    private readonly Dictionary<string, Type> _eventTypes = new();

    public RabbitMQEventBus(
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventBus> logger,
        string hostName = "localhost",
        string exchangeName = "finos_event_bus",
        string queueName = "finos_queue")
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _exchangeName = exchangeName;
        _queueName = queueName;

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_exchangeName, "direct", durable: true);
        _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IntegrationEvent
    {
        var eventName = typeof(TEvent).Name;
        var message = JsonSerializer.Serialize(@event, @event.GetType());
        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: eventName,
            basicProperties: null,
            body: body);

        _logger.LogInformation("Published event {EventName}: {Message}", eventName, message);
        await Task.CompletedTask;
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = typeof(TEvent).Name;

        if (!_handlers.ContainsKey(eventName))
        {
            _handlers.Add(eventName, new List<Type>());
            _channel.QueueBind(_queueName, _exchangeName, eventName);
        }

        _handlers[eventName].Add(typeof(THandler));
        _eventTypes[eventName] = typeof(TEvent);

        StartConsuming();
    }

    public void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = typeof(TEvent).Name;
        if (_handlers.ContainsKey(eventName))
        {
            _handlers[eventName].Remove(typeof(THandler));
            if (_handlers[eventName].Count == 0)
            {
                _handlers.Remove(eventName);
                _channel.QueueUnbind(_queueName, _exchangeName, eventName);
            }
        }
    }

    private void StartConsuming()
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var eventName = ea.RoutingKey;
            var message = Encoding.UTF8.GetString(ea.Body.Span);

            if (_handlers.ContainsKey(eventName))
            {
                foreach (var handlerType in _handlers[eventName])
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);

                    if (_eventTypes.TryGetValue(eventName, out var eventType))
                    {
                        var @event = JsonSerializer.Deserialize(message, eventType) as IntegrationEvent;
                        var handlerMethod = handlerType.GetMethod("HandleAsync");
                        if (handlerMethod != null && @event != null)
                        {
                            var task = (Task?)handlerMethod.Invoke(handler, new object[] { @event, CancellationToken.None });
                            if (task != null) await task;
                        }
                    }
                }
            }

            _channel.BasicAck(ea.DeliveryTag, false);
            await Task.CompletedTask;
        };

        _channel.BasicConsume(_queueName, autoAck: false, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
