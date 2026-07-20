using FinOS.Common.Interfaces;
using FinOS.Analytics.Domain.Entities;

namespace FinOS.Analytics.Domain.Interfaces;

public interface IFinancialScoreRepository : IRepository<FinancialScore>
{
    Task<List<FinancialScore>> GetHistoryByUserAsync(long userId, int months, CancellationToken ct = default);
    Task<FinancialScore?> GetLatestByUserAsync(long userId, CancellationToken ct = default);
}
