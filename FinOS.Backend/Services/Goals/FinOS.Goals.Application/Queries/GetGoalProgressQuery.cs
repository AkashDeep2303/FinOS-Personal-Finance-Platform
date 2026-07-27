using FinOS.Common.Exceptions;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Domain.Enums;
using FinOS.Goals.Domain.Interfaces;
using MediatR;

namespace FinOS.Goals.Application.Queries;

public record GetGoalProgressQuery(long UserId, long GoalId) : IRequest<GoalProgressDto>;

public class GetGoalProgressQueryHandler : IRequestHandler<GetGoalProgressQuery, GoalProgressDto>
{
    private readonly IGoalRepository _goalRepository;

    public GetGoalProgressQueryHandler(IGoalRepository goalRepository)
    {
        _goalRepository = goalRepository;
    }

    public async Task<GoalProgressDto> Handle(GetGoalProgressQuery request, CancellationToken cancellationToken)
    {
        var goal = await _goalRepository.GetWithContributionsAsync(request.GoalId, cancellationToken)
            ?? throw new NotFoundException("Goal", request.GoalId);
        if (goal.UserId != request.UserId)
            throw new NotFoundException("Goal", request.GoalId);

        var progressPct = goal.TargetAmount > 0
            ? Math.Min(100m, Math.Round(goal.CurrentAmount / goal.TargetAmount * 100, 2))
            : 0m;

        var remainingAmount = Math.Max(0, goal.TargetAmount - goal.CurrentAmount);
        var remainingMonths = goal.MonthlyContribution > 0
            ? (int)Math.Ceiling(remainingAmount / goal.MonthlyContribution)
            : 0;

        var projectedCompletion = goal.Status == GoalStatus.Completed
            ? goal.CompletedDate
            : (goal.MonthlyContribution > 0 ? DateTime.UtcNow.AddMonths(remainingMonths) : goal.ProjectedDate);

        var isOnTrack = goal.TargetDate.HasValue && projectedCompletion.HasValue
            ? projectedCompletion.Value <= goal.TargetDate.Value
            : true;

        return new GoalProgressDto(
            goal.Id,
            goal.Name,
            goal.TargetAmount,
            goal.CurrentAmount,
            progressPct,
            remainingAmount,
            goal.MonthlyContribution,
            remainingMonths,
            projectedCompletion,
            isOnTrack
        );
    }
}
