using FinOS.Common.Interfaces;
using FinOS.Budget.Domain.Entities;

namespace FinOS.Budget.Domain.Interfaces;

public interface IBudgetRepository : IRepository<Domain.Entities.Budget>
{
    Task<List<Domain.Entities.Budget>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Domain.Entities.Budget?> GetWithCategoriesAsync(long budgetId, CancellationToken ct = default);
    Task<List<Domain.Entities.Budget>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default);
    Task<long> CreateAsync(Domain.Entities.Budget budget, string? categoriesJson = null, CancellationToken ct = default);
    Task UpdateAsync(Domain.Entities.Budget budget, CancellationToken ct = default);
    Task ReplaceCategoriesAsync(long budgetId, List<BudgetCategory> categories, CancellationToken ct = default);
    Task SoftDeleteAsync(long budgetId, CancellationToken ct = default);
    Task CheckBudgetAlertsAsync(long userId, long? categoryId = null, decimal? amount = null, CancellationToken ct = default);
}
