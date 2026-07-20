using FinOS.Common.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Domain.Enums;
using FinOS.Goals.Domain.Interfaces;
using MediatR;

namespace FinOS.Goals.Application.Commands;

public record ResumeGoalCommand(long GoalId) : IRequest<GoalDto>;

public class ResumeGoalCommandHandler : IRequestHandler<ResumeGoalCommand, GoalDto>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResumeGoalCommandHandler(IGoalRepository goalRepository, IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoalDto> Handle(ResumeGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = await _goalRepository.GetByIdAsync(request.GoalId, cancellationToken)
            ?? throw new NotFoundException("Goal", request.GoalId);

        if (goal.Status != GoalStatus.Paused)
            throw new DomainException("GOAL_NOT_PAUSED", "Only paused goals can be resumed.");

        goal.Status = GoalStatus.Active;
        goal.UpdatedAt = DateTime.UtcNow;

        // Recalculate projected date
        if (goal.MonthlyContribution > 0 && goal.TargetAmount > goal.CurrentAmount)
        {
            var remaining = goal.TargetAmount - goal.CurrentAmount;
            var months = (int)Math.Ceiling(remaining / goal.MonthlyContribution);
            goal.ProjectedDate = DateTime.UtcNow.AddMonths(months);
        }

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
