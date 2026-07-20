using FinOS.Common.Interfaces;
using FinOS.Budget.Domain.Entities;

namespace FinOS.Budget.Domain.Interfaces;

public interface ISavingsRuleRepository : IRepository<SavingsRule>
{
    Task<List<SavingsRule>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<List<SavingsRule>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default);
    Task<long> CreateAsync(SavingsRule entity, CancellationToken ct = default);
    Task UpdateAsync(SavingsRule entity, CancellationToken ct = default);
}
