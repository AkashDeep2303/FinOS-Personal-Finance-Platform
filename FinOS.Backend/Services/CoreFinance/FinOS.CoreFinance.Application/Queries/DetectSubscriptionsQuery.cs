using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Application.Services;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public class DetectSubscriptionsQuery : IRequest<List<SubscriptionDto>>
{
    public long UserId { get; set; }
}

public class DetectSubscriptionsQueryHandler : IRequestHandler<DetectSubscriptionsQuery, List<SubscriptionDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionDetectionService _detectionService;

    public DetectSubscriptionsQueryHandler(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionDetectionService detectionService)
    {
        _subscriptionRepository = subscriptionRepository;
        _detectionService = detectionService;
    }

    public async Task<List<SubscriptionDto>> Handle(DetectSubscriptionsQuery query, CancellationToken ct)
    {
        var detected = await _detectionService.DetectAsync(query.UserId, ct);

        return detected.Select(s => new SubscriptionDto
        {
            Id = s.Id,
            UserId = s.UserId,
            MerchantName = s.MerchantName,
            CategoryId = s.CategoryId,
            CategoryName = s.Category?.Name,
            Amount = s.Amount,
            Currency = s.Currency,
            Frequency = s.Frequency.ToString(),
            NextExpectedDate = s.NextExpectedDate,
            LastTransactionDate = s.LastTransactionDate,
            LastTransactionId = s.LastTransactionId,
            DetectionConfidence = s.DetectionConfidence,
            TransactionCount = s.TransactionCount,
            IsConfirmed = s.IsConfirmed,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        }).ToList();
    }
}
