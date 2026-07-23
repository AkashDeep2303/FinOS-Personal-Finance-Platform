using FinOS.Investment.Application.Commands;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public record GetSIPListQuery(long UserId, bool? IsActive = null) : IRequest<List<SIPDto>>;

public class GetSIPListQueryHandler : IRequestHandler<GetSIPListQuery, List<SIPDto>>
{
    private readonly ISIPRepository _repo;
    public GetSIPListQueryHandler(ISIPRepository repo) => _repo = repo;
    public async Task<List<SIPDto>> Handle(GetSIPListQuery q, CancellationToken ct)
    {
        var items = await _repo.GetByUserIdAsync(q.UserId, ct);
        if (q.IsActive.HasValue) items = items.Where(x => x.IsActive == q.IsActive.Value).ToList();
        return items.Select(SIPMapper.Map).ToList();
    }
}
