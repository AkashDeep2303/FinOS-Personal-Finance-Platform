using FinOS.Common.Interfaces;

namespace FinOS.CoreFinance.Domain.Interfaces;

public interface ITagRepository : IRepository<Entities.Tag>
{
    Task<List<Entities.Tag>> GetByUserIdAsync(long userId, CancellationToken ct = default);
}
