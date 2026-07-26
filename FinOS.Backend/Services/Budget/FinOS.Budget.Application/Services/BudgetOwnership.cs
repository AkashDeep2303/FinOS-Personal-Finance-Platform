using FinOS.Budget.Domain.Entities;
using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Exceptions;

namespace FinOS.Budget.Application.Services;

internal static class BudgetOwnership
{
    internal static async Task<Domain.Entities.Budget> GetOwnedAsync(
        IBudgetRepository repository, long budgetId, long userId, CancellationToken cancellationToken,
        bool includeCategories = false)
    {
        var budget = includeCategories
            ? await repository.GetWithCategoriesAsync(budgetId, cancellationToken)
            : await repository.GetByIdAsync(budgetId, cancellationToken);
        if (budget is null || budget.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Budget), budgetId);
        return budget;
    }

    internal static async Task<SavingsRule> GetOwnedRuleAsync(
        ISavingsRuleRepository repository, long ruleId, long userId, CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(ruleId, cancellationToken);
        if (rule is null || rule.UserId != userId)
            throw new NotFoundException(nameof(SavingsRule), ruleId);
        return rule;
    }
}
