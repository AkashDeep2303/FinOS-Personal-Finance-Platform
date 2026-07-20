using FinOS.Notification.Domain.Enums;

namespace FinOS.Notification.Application.DTOs;

/// <summary>
/// Read-model DTO for a notification type definition.
/// </summary>
public record NotificationTypeDto(
    int Id,
    string Name,
    string? Description,
    NotificationCategory Category,
    bool IsEnabled
);
