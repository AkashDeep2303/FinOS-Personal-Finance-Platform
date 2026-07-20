using NotificationEntity = FinOS.Notification.Domain.Entities.Notification;
using FinOS.Notification.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinOS.Notification.Application.Services;

/// <summary>
/// Default delivery service that logs delivery for each channel.
/// Replace with real implementations for production:
///   - Email: SendGrid / SMTP client
///   - Push: Firebase Cloud Messaging / APNS
///   - SMS: Twilio / MessageBird
///   - In-App: SignalR / WebSocket hub
/// </summary>
public class NotificationDeliveryService : INotificationDeliveryService
{
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(ILogger<NotificationDeliveryService> logger)
    {
        _logger = logger;
    }

    public async Task DeliverAsync(NotificationEntity notification, CancellationToken ct = default)
    {
        var channelName = notification.DeliveryChannel switch
        {
            DeliveryChannel.Email => "Email",
            DeliveryChannel.Push => "Push",
            DeliveryChannel.SMS => "SMS",
            _ => "In-App"
        };

        _logger.LogInformation(
            "Delivering notification {NotificationId} to user {UserId} via {Channel}: {Title}",
            notification.Id, notification.UserId, channelName, notification.Title);

        try
        {
            // ── Channel routing (placeholder) ──────────────────────────
            // In production, each branch would call the respective provider:
            //
            // switch (notification.DeliveryChannel)
            // {
            //     case DeliveryChannel.Email:
            //         await _emailSender.SendAsync(notification.UserId, notification.Title, notification.Message, ct);
            //         break;
            //     case DeliveryChannel.Push:
            //         await _pushSender.SendAsync(notification.UserId, notification.Title, notification.Message, ct);
            //         break;
            //     case DeliveryChannel.SMS:
            //         await _smsSender.SendAsync(notification.UserId, notification.Message, ct);
            //         break;
            //     case DeliveryChannel.InApp:
            //         await _hubContext.Clients.User(notification.UserId.ToString())
            //             .SendAsync("Notification", notification, ct);
            //         break;
            // }

            notification.DeliveryStatus = DeliveryStatus.Sent;
            notification.SentAt = DateTime.UtcNow;

            // Simulate async I/O
            await Task.Delay(1, ct);

            notification.DeliveryStatus = DeliveryStatus.Delivered;

            _logger.LogInformation(
                "Notification {NotificationId} delivered successfully via {Channel}",
                notification.Id, channelName);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Delivery of notification {NotificationId} was cancelled", notification.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to deliver notification {NotificationId} via {Channel}",
                notification.Id, channelName);
            notification.DeliveryStatus = DeliveryStatus.Failed;
            throw;
        }
    }
}
