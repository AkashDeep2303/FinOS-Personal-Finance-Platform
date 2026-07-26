using FinOS.Common.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Domain.Enums;
using FinOS.Goals.Domain.Interfaces;
using MediatR;
using FinOS.Goals.Application.Services;

namespace FinOS.Goals.Application.Commands;

public record PauseGoalCommand(long UserId, long GoalId) : IRequest<GoalDto>;

public class PauseGoalCommandHandler : IRequestHandler<PauseGoalCommand, GoalDto>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PauseGoalCommandHandler(IGoalRepository goalRepository, IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoalDto> Handle(PauseGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = await GoalOwnership.GetOwnedAsync(
            _goalRepository, request.GoalId, request.UserId, cancellationToken);

        if (goal.Status != GoalStatus.Active)
            throw new DomainException("GOAL_NOT_ACTIVE", "Only active goals can be paused.");

        goal.Status = GoalStatus.Paused;
        goal.IsAutoContribute = false;
        goal.UpdatedAt = DateTime.UtcNow;

        await _goalRepository.UpdateAsync(goal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new GoalDto(
            goal.Id, goal.UserId, goal.GoalTemplateId, goal.Name, goal.Description,
            goal.Category, goal.TargetAmount, goal.CurrentAmount, goal.MonthlyContribution,
            goal.StartDate, goal.TargetDate, goal.CompletedDate, goal.Priority, goal.Status,
            goal.LinkedAccountIds, goal.Icon, goal.Color, goal.IsAutoContribute,
            goal.ProjectedDate, goal.CreatedAt, goal.UpdatedAt
        );
    }
}
