using FinOS.Analytics.Domain.Entities;

namespace FinOS.Analytics.Domain.Interfaces;

public interface IScenarioRepository
{
    Task<Scenario> AddAsync(Scenario scenario, CancellationToken ct = default);
    Task<IReadOnlyList<Scenario>> GetByUserAsync(long userId, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(long id, long userId, CancellationToken ct = default);
}
