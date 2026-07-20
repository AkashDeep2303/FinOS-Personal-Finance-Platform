using FinOS.Common.Interfaces;
using FinOS.Analytics.Domain.Entities;

namespace FinOS.Analytics.Domain.Interfaces;

public interface IMonthlyAggregateRepository : IRepository<MonthlyAggregate>
{
    Task<List<MonthlyAggregate>> GetByUserAsync(long userId, int months, CancellationToken ct = default);
    Task<MonthlyAggregate?> GetByUserAndMonthAsync(long userId, int yearMonth, CancellationToken ct = default);
}
