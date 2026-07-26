using System.Text.Json;
using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Application.Queries;
using FinOS.Analytics.Domain.Entities;
using FinOS.Analytics.Domain.Interfaces;
using FinOS.Common.Exceptions;
using MediatR;

namespace FinOS.Analytics.Application.Commands;

public record SaveScenarioCommand(long UserId, SaveScenarioRequest Request) : IRequest<SavedScenarioDto>;
public record DeleteScenarioCommand(long UserId, long Id) : IRequest;

public class SaveScenarioCommandHandler(IScenarioRepository repository, IMediator mediator)
    : IRequestHandler<SaveScenarioCommand, SavedScenarioDto>
{
    public async Task<SavedScenarioDto> Handle(SaveScenarioCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(new CalculateScenarioQuery(command.Request.Scenario), ct);
        var entity = await repository.AddAsync(new Scenario
        {
            UserId = command.UserId,
            Name = command.Request.Name.Trim(),
            ScenarioType = result.ScenarioType,
            Verdict = result.Verdict,
            InputJson = JsonSerializer.Serialize(command.Request.Scenario),
            ResultJson = JsonSerializer.Serialize(result),
            CreatedAt = DateTime.UtcNow
        }, ct);
        return new(entity.Id, entity.Name, entity.ScenarioType, entity.Verdict,
            command.Request.Scenario, result, entity.CreatedAt);
    }
}

public class DeleteScenarioCommandHandler(IScenarioRepository repository) : IRequestHandler<DeleteScenarioCommand>
{
    public async Task Handle(DeleteScenarioCommand command, CancellationToken ct)
    {
        if (!await repository.SoftDeleteAsync(command.Id, command.UserId, ct))
            throw new NotFoundException("Scenario", command.Id);
    }
}
