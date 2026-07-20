using FinOS.Budget.Domain.Entities;
using FinOS.Budget.Domain.Enums;
using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Budget.Application.Commands;

public class CheckBudgetAlertsCommand : IRequest<List<BudgetAlert>>
{
    public long BudgetId { get; set; }

    public CheckBudgetAlertsCommand(long budgetId)
    {
        BudgetId = budgetId;
    }
}

public class CheckBudgetAlertsCommandHandler : IRequestHandler<CheckBudgetAlertsCommand, List<BudgetAlert>>
{
    private readonly IBudgetRepository _budgetRepository;

    public CheckBudgetAlertsCommandHandler(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<List<BudgetAlert>> Handle(CheckBudgetAlertsCommand command, CancellationToken ct)
    {
        var budget = await _budgetRepository.GetWithCategoriesAsync(command.BudgetId, ct);
        if (budget == null) return new List<BudgetAlert>();

        // Call the stored procedure to check and create alerts
        await _budgetRepository.CheckBudgetAlertsAsync(budget.UserId, ct: ct);

        // Reload to get the newly created alerts
        budget = await _budgetRepository.GetWithCategoriesAsync(command.BudgetId, ct);
        if (budget == null) return new List<BudgetAlert>();

        var newAlerts = new List<BudgetAlert>();

        foreach (var category in budget.Categories)
        {
            var spentPct = category.AllocatedAmount > 0
                ? category.SpentAmount / category.AllocatedAmount * 100
                : 0;

            if (spentPct >= 100)
            {
                newAlerts.Add(new BudgetAlert
                {
                    BudgetCategoryId = category.Id,
                    AlertType = AlertType.Overspent,
                    ThresholdPercentage = spentPct,
                    Message = $"Category budget exceeded! Spent {spentPct:F1}% of allocated {category.AllocatedAmount:C}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (spentPct >= category.AlertThresholdPct)
            {
                newAlerts.Add(new BudgetAlert
                {
                    BudgetCategoryId = category.Id,
                    AlertType = AlertType.Threshold,
                    ThresholdPercentage = spentPct,
                    Message = $"Category spending at {spentPct:F1}% - threshold is {category.AlertThresholdPct}%",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        var totalSpentPct = budget.TotalBudgetAmount > 0
            ? budget.Categories.Sum(c => c.SpentAmount) / budget.TotalBudgetAmount * 100
            : 0;

        if (budget.EndDate <= DateTime.UtcNow && totalSpentPct < 100)
        {
            newAlerts.Add(new BudgetAlert
            {
                AlertType = AlertType.NearEnd,
                ThresholdPercentage = totalSpentPct,
                Message = $"Budget period ending. Used {totalSpentPct:F1}% of total budget.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return newAlerts;
    }
}
