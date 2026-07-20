namespace FinOS.Notification.Application.DTOs;

/// <summary>
/// DTO representing the count of unread notifications for a user.
/// </summary>
public record UnreadCountDto(
    long UserId,
    int UnreadCount
);
