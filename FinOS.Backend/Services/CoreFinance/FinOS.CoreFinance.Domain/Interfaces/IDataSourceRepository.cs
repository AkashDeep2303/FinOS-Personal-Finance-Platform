using FinOS.CoreFinance.Domain.Entities;

namespace FinOS.CoreFinance.Domain.Interfaces;

public interface IDataSourceRepository
{
    Task<IReadOnlyList<DataSource>> GetAsync(long userId, CancellationToken cancellationToken = default);
    Task<DataSource> AddAsync(DataSource source, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default);
}
