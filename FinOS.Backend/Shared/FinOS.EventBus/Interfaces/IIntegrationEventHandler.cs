using FinOS.EventBus.Events;

namespace FinOS.EventBus.Interfaces;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
