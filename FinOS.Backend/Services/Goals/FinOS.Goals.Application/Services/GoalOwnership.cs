using FinOS.Common.Exceptions;
using FinOS.Goals.Domain.Entities;
using FinOS.Goals.Domain.Interfaces;

namespace FinOS.Goals.Application.Services;

internal static class GoalOwnership
{
    internal static async Task<Goal> GetOwnedAsync(
        IGoalRepository repository, long goalId, long userId, CancellationToken cancellationToken)
    {
        var goal = await repository.GetByIdAsync(goalId, cancellationToken);
        if (goal is null || goal.UserId != userId)
            throw new NotFoundException("Goal", goalId);
        return goal;
    }
}
