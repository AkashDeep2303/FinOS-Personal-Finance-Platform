using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Entities;

namespace FinOS.CoreFinance.Application.Services;

public interface ISubscriptionDetectionService
{
    Task<List<DetectedSubscription>> DetectAsync(long userId, CancellationToken ct = default);
}

public class SubscriptionDetectionService : ISubscriptionDetectionService
{
    private readonly FinOS.CoreFinance.Domain.Interfaces.ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionDetectionService(
        FinOS.CoreFinance.Domain.Interfaces.ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<DetectedSubscription>> DetectAsync(long userId, CancellationToken ct = default)
    {
        var existingSubscriptions = await _subscriptionRepository.GetByUserIdAsync(userId, ct);
        var existingMerchants = existingSubscriptions
            .Where(s => s.IsActive)
            .Select(s => s.MerchantName.ToLowerInvariant())
            .ToHashSet();

        var detected = await _subscriptionRepository.DetectSubscriptionsAsync(userId, ct);

        var newSubscriptions = detected
            .Where(d => !existingMerchants.Contains(d.MerchantName.ToLowerInvariant()))
            .ToList();

        return newSubscriptions;
    }
}
