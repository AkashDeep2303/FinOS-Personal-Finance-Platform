using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Interfaces;

public interface ISIPRepository : IRepository<Domain.Entities.SIP>
{
    Task<List<Domain.Entities.SIP>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<List<Domain.Entities.SIP>> GetActiveSIPsAsync(CancellationToken ct = default);
    Task<List<Domain.Entities.SIP>> GetDueSIPsAsync(DateTime asOfDate, CancellationToken ct = default);
}
