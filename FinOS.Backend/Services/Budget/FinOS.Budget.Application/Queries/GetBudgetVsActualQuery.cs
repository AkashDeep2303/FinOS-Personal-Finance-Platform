using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Exceptions;
using MediatR;

namespace FinOS.Budget.Application.Queries;

public class GetBudgetVsActualQuery : IRequest<BudgetVsActualDto>
{
    public long BudgetId { get; set; }

    public GetBudgetVsActualQuery(long budgetId)
    {
        BudgetId = budgetId;
    }
}

public class GetBudgetVsActualQueryHandler : IRequestHandler<GetBudgetVsActualQuery, BudgetVsActualDto>
{
    private readonly IBudgetRepository _budgetRepository;

    public GetBudgetVsActualQueryHandler(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<BudgetVsActualDto> Handle(GetBudgetVsActualQuery query, CancellationToken ct)
    {
        var budget = await _budgetRepository.GetWithCategoriesAsync(query.BudgetId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Budget), query.BudgetId);

        var totalSpent = budget.Categories.Sum(c => c.SpentAmount);

        return new BudgetVsActualDto
        {
            BudgetId = budget.Id,
            BudgetName = budget.Name,
            TotalBudget = budget.TotalBudgetAmount,
            TotalSpent = totalSpent,
            TotalRemaining = budget.TotalBudgetAmount - totalSpent,
            OverallSpentPct = budget.TotalBudgetAmount > 0
                ? Math.Round(totalSpent / budget.TotalBudgetAmount * 100, 2)
                : 0,
            Categories = budget.Categories.Select(c => new CategoryVsActualDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryId.ToString(),
                CustomLabel = c.CustomLabel,
                Allocated = c.AllocatedAmount,
                Spent = c.SpentAmount,
                Remaining = c.AllocatedAmount - c.SpentAmount,
                SpentPct = c.AllocatedAmount > 0
                    ? Math.Round(c.SpentAmount / c.AllocatedAmount * 100, 2)
                    : 0
            }).ToList()
        };
    }
}
