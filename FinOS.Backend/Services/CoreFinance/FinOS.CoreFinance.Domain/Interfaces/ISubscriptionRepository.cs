using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Enums;

namespace FinOS.CoreFinance.Domain.Interfaces;

public interface ISubscriptionRepository : IRepository<Entities.DetectedSubscription>
{
    Task<List<Entities.DetectedSubscription>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<List<Entities.DetectedSubscription>> DetectSubscriptionsAsync(long userId, CancellationToken ct = default);
}
