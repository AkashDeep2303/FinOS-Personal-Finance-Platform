using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Domain.Interfaces;
using MediatR;
using FinOS.Budget.Application.Services;

namespace FinOS.Budget.Application.Queries;

public class GetBudgetAlertsQuery : IRequest<List<BudgetAlertDto>>
{
    public long UserId { get; set; }
    public long BudgetId { get; set; }
    public bool? UnreadOnly { get; set; }

    public GetBudgetAlertsQuery(long userId, long budgetId, bool? unreadOnly = null)
    {
        UserId = userId;
        BudgetId = budgetId;
        UnreadOnly = unreadOnly;
    }
}

public class GetBudgetAlertsQueryHandler : IRequestHandler<GetBudgetAlertsQuery, List<BudgetAlertDto>>
{
    private readonly IBudgetRepository _budgetRepository;

    public GetBudgetAlertsQueryHandler(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<List<BudgetAlertDto>> Handle(GetBudgetAlertsQuery query, CancellationToken ct)
    {
        var budget = await BudgetOwnership.GetOwnedAsync(
            _budgetRepository, query.BudgetId, query.UserId, ct, includeCategories: true);

        var alerts = budget.Categories
            .SelectMany(c => c.Alerts)
            .Where(a => query.UnreadOnly != true || !a.IsRead)
            .Select(a => new BudgetAlertDto
            {
                Id = a.Id,
                BudgetCategoryId = a.BudgetCategoryId,
                CategoryName = a.BudgetCategory?.CustomLabel ?? a.BudgetCategoryId.ToString(),
                AlertType = a.AlertType,
                AlertTypeDisplay = a.AlertType.ToString(),
                ThresholdPercentage = a.ThresholdPercentage,
                Message = a.Message,
                IsRead = a.IsRead,
                CreatedAt = a.CreatedAt
            })
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        return alerts;
    }
}
