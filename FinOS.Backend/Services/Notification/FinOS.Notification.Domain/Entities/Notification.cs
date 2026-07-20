using FinOS.Notification.Domain.Enums;

namespace FinOS.Notification.Domain.Entities;

/// <summary>
/// A single notification instance addressed to a specific user.
/// Supports scheduling, delivery tracking, read/action status, and expiration.
/// </summary>
public class Notification
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public int NotificationTypeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional deep-link URI that navigates the user to the relevant screen when tapped.
    /// </summary>
    public string? DeepLink { get; set; }

    /// <summary>
    /// The domain entity type this notification refers to (e.g., "Transaction", "Budget", "Goal").
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// The identifier of the domain entity this notification refers to.
    /// Stored as string to accommodate various ID formats (long, GUID, etc.).
    /// </summary>
    public string? EntityId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public bool IsActionTaken { get; set; } = false;

    public DateTime? ActionTakenAt { get; set; }

    /// <summary>
    /// If set, the notification will not be delivered until this time.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Timestamp when the notification was actually sent to the delivery provider.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// The channel through which this notification is (to be) delivered.
    /// Stored as string in DB for readability; mapped via EF Core conversion.
    /// </summary>
    public DeliveryChannel DeliveryChannel { get; set; } = DeliveryChannel.InApp;

    /// <summary>
    /// Current delivery lifecycle status.
    /// Stored as string in DB for readability; mapped via EF Core conversion.
    /// </summary>
    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;

    /// <summary>
    /// Optional expiration time; notifications past this time should be auto-archived.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public NotificationType? NotificationType { get; set; }
}
