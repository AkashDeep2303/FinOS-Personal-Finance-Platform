using FinOS.Common.Interfaces;
using FinOS.Goals.Domain.Entities;

namespace FinOS.Goals.Domain.Interfaces;

public interface IGoalContributionRepository : IRepository<GoalContribution>
{
    Task<List<GoalContribution>> GetByGoalIdAsync(long goalId, CancellationToken ct = default);
}
