using System.Text.Json;
using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;

namespace FinOS.Analytics.Application.Queries;

public record GetSavedScenariosQuery(long UserId) : IRequest<IReadOnlyList<SavedScenarioDto>>;

public class GetSavedScenariosQueryHandler(IScenarioRepository repository)
    : IRequestHandler<GetSavedScenariosQuery, IReadOnlyList<SavedScenarioDto>>
{
    public async Task<IReadOnlyList<SavedScenarioDto>> Handle(GetSavedScenariosQuery query, CancellationToken ct) =>
        (await repository.GetByUserAsync(query.UserId, ct)).Select(x => new SavedScenarioDto(
            x.Id, x.Name, x.ScenarioType, x.Verdict,
            JsonSerializer.Deserialize<ScenarioRequest>(x.InputJson)!,
            JsonSerializer.Deserialize<ScenarioResultDto>(x.ResultJson)!,
            x.CreatedAt)).ToList();
}
