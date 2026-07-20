namespace FinOS.Notification.Application.DTOs;

/// <summary>
/// Read-model DTO for a user's notification preference.
/// </summary>
public record NotificationPreferenceDto(
    long Id,
    long UserId,
    int NotificationTypeId,
    string NotificationTypeName,
    bool EmailEnabled,
    bool PushEnabled,
    bool SmsEnabled,
    bool InAppEnabled,
    string? QuietHoursStart,
    string? QuietHoursEnd
);

/// <summary>
/// Request DTO for creating/updating a notification preference.
/// All boolean fields are optional (nullable); only supplied values are applied.
/// Quiet-hours values are ISO 8601 time-of-day strings (e.g., "22:00", "07:30").
/// </summary>
public record UpdateNotificationPreferenceDto(
    long UserId,
    int NotificationTypeId,
    bool? EmailEnabled = null,
    bool? PushEnabled = null,
    bool? SmsEnabled = null,
    bool? InAppEnabled = null,
    string? QuietHoursStart = null,
    string? QuietHoursEnd = null
);
