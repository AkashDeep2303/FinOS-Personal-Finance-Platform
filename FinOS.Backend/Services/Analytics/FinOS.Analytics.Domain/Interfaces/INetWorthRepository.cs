using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Analytics.Domain.Entities;

namespace FinOS.Analytics.Domain.Interfaces;

public interface INetWorthRepository : IRepository<NetWorthSnapshot>
{
    Task<List<NetWorthSnapshot>> GetByUserAsync(long userId, int months, CancellationToken ct = default);
    Task<NetWorthSnapshot?> GetLatestByUserAsync(long userId, CancellationToken ct = default);
}
