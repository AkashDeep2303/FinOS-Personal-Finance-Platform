using FinOS.Common.Exceptions;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Commands;

public record SaveTargetAllocationCommand(long UserId, AllocationAnalysisRequest Request)
    : IRequest<IReadOnlyList<TargetAllocationInput>>;

public class SaveTargetAllocationCommandHandler : IRequestHandler<SaveTargetAllocationCommand, IReadOnlyList<TargetAllocationInput>>
{
    private readonly IPortfolioRepository _portfolios;
    private readonly ITargetAllocationRepository _targets;
    public SaveTargetAllocationCommandHandler(IPortfolioRepository portfolios, ITargetAllocationRepository targets)
        => (_portfolios, _targets) = (portfolios, targets);

    public async Task<IReadOnlyList<TargetAllocationInput>> Handle(SaveTargetAllocationCommand command, CancellationToken ct)
    {
        var portfolio = await _portfolios.GetByIdAsync(command.Request.PortfolioId, ct)
            ?? throw new NotFoundException("Portfolio", command.Request.PortfolioId);
        if (portfolio.UserId != command.UserId) throw new UnauthorizedAccessException();
        var targets = command.Request.Targets.ToDictionary(x => x.AssetClass.Trim(), x => x.TargetPct, StringComparer.OrdinalIgnoreCase);
        await _targets.ReplaceAsync(portfolio.Id, targets, ct);
        return targets.Select(x => new TargetAllocationInput(x.Key, x.Value)).ToList();
    }
}
