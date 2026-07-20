using FinOS.Common.Interfaces;

namespace FinOS.Notification.Domain.Entities;

/// <summary>
/// Per-user, per-notification-type preference controlling which delivery channels are active
/// and optional quiet-hours during which notifications are suppressed.
/// </summary>
public class NotificationPreference : IAuditableEntity
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public int NotificationTypeId { get; set; }

    public bool EmailEnabled { get; set; } = true;

    public bool PushEnabled { get; set; } = true;

    public bool SmsEnabled { get; set; } = false;

    public bool InAppEnabled { get; set; } = true;

    /// <summary>
    /// Start of quiet-hours window. Stored as TimeSpan (time-of-day).
    /// Null means quiet-hours are disabled.
    /// </summary>
    public TimeSpan? QuietHoursStart { get; set; }

    /// <summary>
    /// End of quiet-hours window. Stored as TimeSpan (time-of-day).
    /// Null means quiet-hours are disabled.
    /// </summary>
    public TimeSpan? QuietHoursEnd { get; set; }

    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public NotificationType? NotificationType { get; set; }
}
