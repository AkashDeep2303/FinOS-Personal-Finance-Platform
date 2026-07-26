using FinOS.Common.Exceptions;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public record GetTargetAllocationQuery(long UserId, long PortfolioId) : IRequest<IReadOnlyList<TargetAllocationInput>>;

public class GetTargetAllocationQueryHandler : IRequestHandler<GetTargetAllocationQuery, IReadOnlyList<TargetAllocationInput>>
{
    private readonly IPortfolioRepository _portfolios;
    private readonly ITargetAllocationRepository _targets;
    public GetTargetAllocationQueryHandler(IPortfolioRepository portfolios, ITargetAllocationRepository targets)
        => (_portfolios, _targets) = (portfolios, targets);

    public async Task<IReadOnlyList<TargetAllocationInput>> Handle(GetTargetAllocationQuery query, CancellationToken ct)
    {
        var portfolio = await _portfolios.GetByIdAsync(query.PortfolioId, ct)
            ?? throw new NotFoundException("Portfolio", query.PortfolioId);
        if (portfolio.UserId != query.UserId) throw new UnauthorizedAccessException();
        return (await _targets.GetAsync(query.PortfolioId, ct))
            .Select(x => new TargetAllocationInput(x.Key, x.Value)).ToList();
    }
}
