using FinOS.Notification.Domain.Entities;

namespace FinOS.Notification.Domain.Interfaces;

/// <summary>
/// Repository for NotificationType reference/lookup data.
/// Does NOT extend the generic IRepository{T} because NotificationType uses int keys.
/// </summary>
public interface INotificationTypeRepository
{
    Task<NotificationType?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<NotificationType>> GetAllAsync(CancellationToken ct = default);
    Task<List<NotificationType>> GetEnabledAsync(CancellationToken ct = default);
    Task<NotificationType> AddAsync(NotificationType entity, CancellationToken ct = default);
    Task UpdateAsync(NotificationType entity, CancellationToken ct = default);
    Task RemoveAsync(NotificationType entity, CancellationToken ct = default);
}
