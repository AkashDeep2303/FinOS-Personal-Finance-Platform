using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Interfaces;

public interface ISIPRepository : IRepository<Domain.Entities.SIP>
{
    Task<List<Domain.Entities.SIP>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<List<Domain.Entities.SIP>> GetActiveSIPsAsync(CancellationToken ct = default);
    Task<List<Domain.Entities.SIP>> GetDueSIPsAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<long> CreateAsync(long userId, string name, long? holdingId, decimal amount, string frequency, int dayOfMonth, DateTime startDate, DateTime? endDate, long sourceAccountId, CancellationToken ct = default);
    Task UpdateAsync(long id, long userId, string name, long? holdingId, decimal amount, string frequency, int dayOfMonth, DateTime startDate, DateTime? endDate, long sourceAccountId, CancellationToken ct = default);
    Task SetStatusAsync(long id, long userId, bool isActive, CancellationToken ct = default);
    Task DeleteAsync(long id, long userId, CancellationToken ct = default);
}
