using FinOS.Common.Interfaces;
using FinOS.Budget.Domain.Entities;

namespace FinOS.Budget.Domain.Interfaces;

public interface IBudgetCategoryRepository : IRepository<BudgetCategory>
{
    Task<List<BudgetCategory>> GetByBudgetIdAsync(long budgetId, CancellationToken ct = default);
    Task<List<BudgetCategory>> GetByCategoryIdAsync(long categoryId, CancellationToken ct = default);
    Task UpdateSpentAmountAsync(long budgetCategoryId, decimal spentAmount, CancellationToken ct = default);
    Task<long> CreateAsync(BudgetCategory entity, CancellationToken ct = default);
    new Task UpdateAsync(BudgetCategory entity, CancellationToken ct = default);
    Task UpdateBudgetSpentAsync(long? budgetCategoryId = null, long? budgetId = null, CancellationToken ct = default);
}
