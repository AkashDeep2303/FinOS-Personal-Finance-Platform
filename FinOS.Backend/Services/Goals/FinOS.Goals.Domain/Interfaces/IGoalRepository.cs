using FinOS.Common.Interfaces;
using FinOS.Goals.Domain.Entities;

namespace FinOS.Goals.Domain.Interfaces;

public interface IGoalRepository : IRepository<Goal>
{
    Task<List<Goal>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Goal?> GetWithContributionsAsync(long goalId, CancellationToken ct = default);
    Task SoftDeleteAsync(long goalId, CancellationToken ct = default);
}
