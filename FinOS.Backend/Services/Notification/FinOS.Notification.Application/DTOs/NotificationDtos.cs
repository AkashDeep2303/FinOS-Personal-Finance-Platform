using FinOS.Notification.Domain.Enums;

namespace FinOS.Notification.Application.DTOs;

/// <summary>
/// Read-model DTO returned to consumers for a single notification.
/// </summary>
public record NotificationDto(
    long Id,
    long UserId,
    int NotificationTypeId,
    string NotificationTypeName,
    string Title,
    string Message,
    string? DeepLink,
    string? EntityType,
    string? EntityId,
    bool IsRead,
    DateTime? ReadAt,
    bool IsActionTaken,
    DateTime? ActionTakenAt,
    DeliveryChannel DeliveryChannel,
    DeliveryStatus DeliveryStatus,
    DateTime? ScheduledAt,
    DateTime? SentAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);

/// <summary>
/// Request DTO for creating a new notification.
/// </summary>
public record CreateNotificationDto(
    long UserId,
    int NotificationTypeId,
    string Title,
    string Message,
    string? DeepLink = null,
    string? EntityType = null,
    string? EntityId = null,
    DeliveryChannel DeliveryChannel = DeliveryChannel.InApp,
    DateTime? ScheduledAt = null,
    DateTime? ExpiresAt = null
);

/// <summary>
/// Request DTO for marking one or all notifications as read.
/// </summary>
public record MarkReadDto(
    long UserId,
    long? NotificationId
);
