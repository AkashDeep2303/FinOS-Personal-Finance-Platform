using FinOS.Common.Interfaces;

namespace FinOS.CoreFinance.Domain.Interfaces;

public interface IRecurringScheduleRepository : IRepository<Entities.RecurringSchedule>
{
    Task<List<Entities.RecurringSchedule>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<List<Entities.RecurringSchedule>> GetDueSchedulesAsync(CancellationToken ct = default);
}
