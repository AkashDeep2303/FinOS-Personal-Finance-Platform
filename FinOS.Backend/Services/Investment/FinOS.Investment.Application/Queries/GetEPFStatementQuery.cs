using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Common.Exceptions;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public class GetEPFStatementQuery : IRequest<List<EPFContributionDto>>
{
    public long EPFAccountId { get; set; }
    public long UserId { get; set; }

    public GetEPFStatementQuery(long epfAccountId, long userId)
    {
        EPFAccountId = epfAccountId;
        UserId = userId;
    }
}

public class GetEPFStatementQueryHandler : IRequestHandler<GetEPFStatementQuery, List<EPFContributionDto>>
{
    private readonly IEPFAccountRepository _epfRepository;

    public GetEPFStatementQueryHandler(IEPFAccountRepository epfRepository)
    {
        _epfRepository = epfRepository;
    }

    public async Task<List<EPFContributionDto>> Handle(GetEPFStatementQuery query, CancellationToken ct)
    {
        var account = await _epfRepository.GetWithContributionsAsync(query.EPFAccountId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.EPFAccount), query.EPFAccountId);
        if (account.UserId != query.UserId) throw new UnauthorizedAccessException();

        return account.Contributions
            .OrderBy(c => c.Month)
            .Select(c => new EPFContributionDto
            {
                Id = c.Id, Month = c.Month,
                EmployeeContribution = c.EmployeeContribution,
                EmployerContribution = c.EmployerContribution,
                EPSContribution = c.EPSContribution,
                InterestEarned = c.InterestEarned,
                OpeningBalance = c.OpeningBalance,
                ClosingBalance = c.ClosingBalance
            }).ToList();
    }
}
