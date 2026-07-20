using FinOS.Common.Interfaces;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Domain.Entities;
using FinOS.Goals.Domain.Enums;
using FinOS.Goals.Domain.Interfaces;
using MediatR;

namespace FinOS.Goals.Application.Commands;

public record CreateGoalCommand(CreateGoalDto Dto) : IRequest<GoalDto>;

public class CreateGoalCommandHandler : IRequestHandler<CreateGoalCommand, GoalDto>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGoalCommandHandler(IGoalRepository goalRepository, IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoalDto> Handle(CreateGoalCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var goal = new Goal
        {
            UserId = dto.UserId,
            GoalTemplateId = dto.GoalTemplateId,
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            TargetAmount = dto.TargetAmount,
            CurrentAmount = 0,
            MonthlyContribution = dto.MonthlyContribution,
            StartDate = dto.StartDate,
            TargetDate = dto.TargetDate,
            Priority = dto.Priority,
            Status = GoalStatus.Active,
            LinkedAccountIds = dto.LinkedAccountIds,
            Icon = dto.Icon,
            Color = dto.Color,
            IsAutoContribute = dto.IsAutoContribute,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Calculate projected date
        if (dto.MonthlyContribution > 0 && dto.TargetAmount > 0)
        {
            var months = (int)Math.Ceiling(dto.TargetAmount / dto.MonthlyContribution);
            goal.ProjectedDate = dto.StartDate.AddMonths(months);
        }

        await _goalRepository.AddAsync(goal, cancellationToken);
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
