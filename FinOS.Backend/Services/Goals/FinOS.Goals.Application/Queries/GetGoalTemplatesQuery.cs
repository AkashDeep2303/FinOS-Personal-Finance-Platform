using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Domain.Interfaces;
using MediatR;

namespace FinOS.Goals.Application.Queries;

public record GetGoalTemplatesQuery : IRequest<List<GoalTemplateDto>>;

public class GetGoalTemplatesQueryHandler : IRequestHandler<GetGoalTemplatesQuery, List<GoalTemplateDto>>
{
    private readonly IGoalTemplateRepository _templateRepository;

    public GetGoalTemplatesQueryHandler(IGoalTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<List<GoalTemplateDto>> Handle(GetGoalTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _templateRepository.ListAsync(null, cancellationToken);

        return templates.Select(t => new GoalTemplateDto(
            t.Id, t.Name, t.Description, t.Category,
            t.SuggestedAmount, t.SuggestedMonths, t.Icon, t.Color, t.SortOrder
        )).OrderBy(t => t.SortOrder).ToList();
    }
}
