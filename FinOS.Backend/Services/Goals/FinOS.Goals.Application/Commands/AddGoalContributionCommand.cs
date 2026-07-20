using FinOS.Common.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Domain.Entities;
using FinOS.Goals.Domain.Enums;
using FinOS.Goals.Domain.Interfaces;
using MediatR;

namespace FinOS.Goals.Application.Commands;

public record AddGoalContributionCommand(AddGoalContributionDto Dto) : IRequest<GoalContributionDto>;

public class AddGoalContributionCommandHandler : IRequestHandler<AddGoalContributionCommand, GoalContributionDto>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IGoalContributionRepository _contributionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddGoalContributionCommandHandler(
        IGoalRepository goalRepository,
        IGoalContributionRepository contributionRepository,
        IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _contributionRepository = contributionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoalContributionDto> Handle(AddGoalContributionCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var goal = await _goalRepository.GetByIdAsync(dto.GoalId, cancellationToken)
            ?? throw new NotFoundException("Goal", dto.GoalId);

        if (goal.Status != GoalStatus.Active)
            throw new DomainException("GOAL_NOT_ACTIVE", "Cannot add contribution to a non-active goal.");

        var contribution = new GoalContribution
        {
            GoalId = dto.GoalId,
            Amount = dto.Amount,
            ContributionDate = dto.ContributionDate,
            Source = dto.Source,
            SourceAccountId = dto.SourceAccountId,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await _contributionRepository.AddAsync(contribution, cancellationToken);

        // Update goal current amount
        goal.CurrentAmount += dto.Amount;
        goal.UpdatedAt = DateTime.UtcNow;

        // Auto-complete goal when target reached
        if (goal.CurrentAmount >= goal.TargetAmount)
        {
            goal.Status = GoalStatus.Completed;
            goal.CompletedDate = DateTime.UtcNow;
            goal.CurrentAmount = goal.TargetAmount; // Cap at target
        }

        await _goalRepository.UpdateAsync(goal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new GoalContributionDto(
            contribution.Id, contribution.GoalId, contribution.Amount,
            contribution.ContributionDate, contribution.Source,
            contribution.SourceAccountId, contribution.Notes, contribution.CreatedAt
        );
    }
}
