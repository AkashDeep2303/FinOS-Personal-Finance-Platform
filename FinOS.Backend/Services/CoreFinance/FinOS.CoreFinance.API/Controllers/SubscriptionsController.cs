using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Application.Queries;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.CoreFinance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionsController(
        IMediator mediator,
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    private long GetUserId() => long.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value ?? "0");

    [HttpGet]
    public async Task<ApiResponse<List<SubscriptionDto>>> GetSubscriptions()
    {
        var subscriptions = await _subscriptionRepository.GetByUserIdAsync(GetUserId());
        var dtos = subscriptions.Select(s => new SubscriptionDto
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
        return ApiResponse<List<SubscriptionDto>>.Ok(dtos);
    }

    [HttpPost("detect")]
    public async Task<ApiResponse<List<SubscriptionDto>>> DetectSubscriptions()
    {
        var result = await _mediator.Send(new DetectSubscriptionsQuery { UserId = GetUserId() });
        return ApiResponse<List<SubscriptionDto>>.Ok(result, "Subscriptions detected successfully");
    }

    [HttpPut("{id:long}/confirm")]
    public async Task<ApiResponse<SubscriptionDto>> ConfirmSubscription(long id, [FromBody] ConfirmSubscriptionRequest request)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(id);
        if (subscription == null || subscription.UserId != GetUserId())
            return ApiResponse<SubscriptionDto>.Fail("Subscription not found");

        subscription.IsConfirmed = request.IsConfirmed;
        if (request.CategoryId.HasValue)
            subscription.CategoryId = request.CategoryId;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<SubscriptionDto>.Ok(new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            MerchantName = subscription.MerchantName,
            CategoryId = subscription.CategoryId,
            CategoryName = subscription.Category?.Name,
            Amount = subscription.Amount,
            Currency = subscription.Currency,
            Frequency = subscription.Frequency.ToString(),
            NextExpectedDate = subscription.NextExpectedDate,
            LastTransactionDate = subscription.LastTransactionDate,
            LastTransactionId = subscription.LastTransactionId,
            DetectionConfidence = subscription.DetectionConfidence,
            TransactionCount = subscription.TransactionCount,
            IsConfirmed = subscription.IsConfirmed,
            IsActive = subscription.IsActive,
            CreatedAt = subscription.CreatedAt
        }, "Subscription confirmed");
    }
}
