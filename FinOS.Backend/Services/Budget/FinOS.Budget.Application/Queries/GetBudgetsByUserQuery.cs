using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Domain.Interfaces;
using MediatR;

namespace FinOS.Budget.Application.Queries;

public class GetBudgetsByUserQuery : IRequest<List<BudgetListDto>>
{
    public long UserId { get; set; }
    public bool? IsActive { get; set; }

    public GetBudgetsByUserQuery(long userId, bool? isActive = null)
    {
        UserId = userId;
        IsActive = isActive;
    }
}

public class GetBudgetsByUserQueryHandler : IRequestHandler<GetBudgetsByUserQuery, List<BudgetListDto>>
{
    private readonly IBudgetRepository _budgetRepository;

    public GetBudgetsByUserQueryHandler(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<List<BudgetListDto>> Handle(GetBudgetsByUserQuery query, CancellationToken ct)
    {
        var budgets = query.IsActive == true
            ? await _budgetRepository.GetActiveByUserIdAsync(query.UserId, ct)
            : await _budgetRepository.GetByUserIdAsync(query.UserId, ct);

        return budgets.Select(b => new BudgetListDto
        {
            Id = b.Id,
            Name = b.Name,
            PeriodType = b.PeriodType,
            PeriodTypeDisplay = b.PeriodType.ToString(),
            StartDate = b.StartDate,
            EndDate = b.EndDate,
            TotalBudgetAmount = b.TotalBudgetAmount,
            TotalSpentAmount = b.Categories?.Sum(c => c.SpentAmount) ?? 0,
            SpentPercentage = b.TotalBudgetAmount > 0
                ? Math.Round((b.Categories?.Sum(c => c.SpentAmount) ?? 0) / b.TotalBudgetAmount * 100, 2)
                : 0,
            Currency = b.Currency,
            IsActive = b.IsActive,
            CategoryCount = b.Categories?.Count ?? 0
        }).ToList();
    }
}
