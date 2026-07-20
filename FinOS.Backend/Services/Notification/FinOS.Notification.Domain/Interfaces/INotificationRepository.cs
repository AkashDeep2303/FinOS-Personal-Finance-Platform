using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using NotificationEntity = FinOS.Notification.Domain.Entities.Notification;

namespace FinOS.Notification.Domain.Interfaces;

/// <summary>
/// Repository for Notification aggregate with specialized query methods.
/// </summary>
public interface INotificationRepository : IRepository<NotificationEntity>
{
    /// <summary>
    /// Returns all unread notifications for a given user, newest first.
    /// </summary>
    Task<List<NotificationEntity>> GetUnreadByUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Paginated, filtered listing of notifications for a user.
    /// </summary>
    Task<PagedResult<NotificationEntity>> GetPagedByUserAsync(
        long userId,
        bool? isRead,
        int? notificationTypeId,
        PagedQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Count of unread notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all pending scheduled notifications that are due for delivery.
    /// </summary>
    Task<List<NotificationEntity>> GetScheduledAsync(CancellationToken ct = default);

    /// <summary>
    /// Marks a single notification as read.
    /// </summary>
    Task MarkAsReadAsync(long notificationId, CancellationToken ct = default);

    /// <summary>
    /// Marks all unread notifications for a user as read.
    /// </summary>
    Task MarkAllAsReadAsync(long userId, CancellationToken ct = default);
}
