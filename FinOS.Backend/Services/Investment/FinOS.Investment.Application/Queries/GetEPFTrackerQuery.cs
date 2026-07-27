using FinOS.Investment.Application.Commands;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public record GetEPFTrackerQuery(long UserId) : IRequest<EPFTrackerDto?>;

public class GetEPFTrackerQueryHandler : IRequestHandler<GetEPFTrackerQuery, EPFTrackerDto?>
{
    private readonly IEPFAccountRepository _repo;
    public GetEPFTrackerQueryHandler(IEPFAccountRepository repo) => _repo = repo;
    public async Task<EPFTrackerDto?> Handle(GetEPFTrackerQuery q, CancellationToken ct)
    {
        var account = (await _repo.GetByUserIdAsync(q.UserId, ct)).FirstOrDefault(x => x.IsActive);
        return account is null ? null : EPFMapper.Map(account);
    }
}
