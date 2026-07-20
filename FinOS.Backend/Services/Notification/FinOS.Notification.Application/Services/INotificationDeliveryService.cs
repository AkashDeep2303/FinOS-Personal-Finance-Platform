using NotificationEntity = FinOS.Notification.Domain.Entities.Notification;

namespace FinOS.Notification.Application.Services;

/// <summary>
/// Abstraction for notification delivery across channels.
/// In production, implementations would route to SendGrid (email),
/// Firebase Cloud Messaging (push), Twilio (SMS), or SignalR (in-app).
/// </summary>
public interface INotificationDeliveryService
{
    /// <summary>
    /// Delivers the notification through its configured channel.
    /// Mutates the notification's DeliveryStatus and SentAt upon success/failure.
    /// </summary>
    Task DeliverAsync(NotificationEntity notification, CancellationToken ct = default);
}
