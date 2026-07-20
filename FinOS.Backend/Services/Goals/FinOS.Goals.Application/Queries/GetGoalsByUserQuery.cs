using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Domain.Interfaces;
using MediatR;

namespace FinOS.Goals.Application.Queries;

public record GetGoalsByUserQuery(long UserId) : IRequest<List<GoalDto>>;

public class GetGoalsByUserQueryHandler : IRequestHandler<GetGoalsByUserQuery, List<GoalDto>>
{
    private readonly IGoalRepository _goalRepository;

    public GetGoalsByUserQueryHandler(IGoalRepository goalRepository)
    {
        _goalRepository = goalRepository;
    }

    public async Task<List<GoalDto>> Handle(GetGoalsByUserQuery request, CancellationToken cancellationToken)
    {
        var goals = await _goalRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return goals.Select(g => new GoalDto(
            g.Id, g.UserId, g.GoalTemplateId, g.Name, g.Description,
            g.Category, g.TargetAmount, g.CurrentAmount, g.MonthlyContribution,
            g.StartDate, g.TargetDate, g.CompletedDate, g.Priority, g.Status,
            g.LinkedAccountIds, g.Icon, g.Color, g.IsAutoContribute,
            g.ProjectedDate, g.CreatedAt, g.UpdatedAt
        )).ToList();
    }
}
