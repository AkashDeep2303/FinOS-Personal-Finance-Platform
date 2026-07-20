using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Enums;

namespace FinOS.CoreFinance.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Entities.Category>
{
    Task<List<Entities.Category>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<List<Entities.Category>> GetByUserIdAndTypeAsync(long userId, CategoryType type, CancellationToken ct = default);
    Task<List<Entities.Category>> GetSystemCategoriesAsync(CancellationToken ct = default);
}
