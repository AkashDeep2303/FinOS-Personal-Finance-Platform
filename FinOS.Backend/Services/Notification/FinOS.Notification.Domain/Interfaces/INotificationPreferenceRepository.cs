using FinOS.Common.Interfaces;
using FinOS.Notification.Domain.Entities;

namespace FinOS.Notification.Domain.Interfaces;

/// <summary>
/// Repository for per-user notification preferences.
/// </summary>
public interface INotificationPreferenceRepository : IRepository<NotificationPreference>
{
    /// <summary>
    /// Returns all preferences for a user, including the associated NotificationType.
    /// </summary>
    Task<List<NotificationPreference>> GetByUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the preference for a specific user + notification type, or null.
    /// </summary>
    Task<NotificationPreference?> GetByUserAndTypeAsync(
        long userId,
        int notificationTypeId,
        CancellationToken ct = default);
}
