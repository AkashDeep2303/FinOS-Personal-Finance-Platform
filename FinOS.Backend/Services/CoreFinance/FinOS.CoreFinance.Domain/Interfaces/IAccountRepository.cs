using FinOS.Common.Interfaces;

namespace FinOS.CoreFinance.Domain.Interfaces;

public interface IAccountRepository : IRepository<Entities.Account>
{
    Task<List<Entities.Account>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<List<Entities.Account>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(long userId, long accountId, CancellationToken ct = default);
}
