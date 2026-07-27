using FinOS.Common.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Domain.Interfaces;
using MediatR;
using FinOS.Goals.Application.Services;

namespace FinOS.Goals.Application.Commands;

public record UpdateGoalCommand(long UserId, UpdateGoalDto Dto) : IRequest<GoalDto>;

public class UpdateGoalCommandHandler : IRequestHandler<UpdateGoalCommand, GoalDto>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGoalCommandHandler(IGoalRepository goalRepository, IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoalDto> Handle(UpdateGoalCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var goal = await GoalOwnership.GetOwnedAsync(
            _goalRepository, dto.Id, request.UserId, cancellationToken);

        if (dto.Name is not null) goal.Name = dto.Name;
        if (dto.Description is not null) goal.Description = dto.Description;
        if (dto.Category is not null) goal.Category = dto.Category;
        if (dto.TargetAmount.HasValue) goal.TargetAmount = dto.TargetAmount.Value;
        if (dto.MonthlyContribution.HasValue) goal.MonthlyContribution = dto.MonthlyContribution.Value;
        if (dto.TargetDate.HasValue) goal.TargetDate = dto.TargetDate;
        if (dto.Priority.HasValue) goal.Priority = dto.Priority.Value;
        if (dto.LinkedAccountIds is not null) goal.LinkedAccountIds = dto.LinkedAccountIds;
        if (dto.Icon is not null) goal.Icon = dto.Icon;
        if (dto.Color is not null) goal.Color = dto.Color;
        if (dto.IsAutoContribute.HasValue) goal.IsAutoContribute = dto.IsAutoContribute.Value;

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
