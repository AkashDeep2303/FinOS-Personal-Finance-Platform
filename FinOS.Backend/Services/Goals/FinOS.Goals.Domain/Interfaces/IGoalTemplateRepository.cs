using FinOS.Common.Interfaces;
using FinOS.Goals.Domain.Entities;

namespace FinOS.Goals.Domain.Interfaces;

public interface IGoalTemplateRepository : IRepository<GoalTemplate>
{
    Task<List<GoalTemplate>> ListAsync(string? category = null, CancellationToken ct = default);
}
