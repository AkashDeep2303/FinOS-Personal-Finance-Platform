using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;
using FinOS.Loan.Application.Services;

namespace FinOS.Loan.Application.Queries;

public class GetUpcomingEMIsQuery : IRequest<List<EMIScheduleDto>>
{
    public long UserId { get; set; }
    public long LoanId { get; set; }
    public int Count { get; set; } = 3;

    public GetUpcomingEMIsQuery(long userId, long loanId, int count = 3)
    {
        UserId = userId;
        LoanId = loanId;
        Count = count;
    }
}

public class GetUpcomingEMIsQueryHandler : IRequestHandler<GetUpcomingEMIsQuery, List<EMIScheduleDto>>
{
    private readonly IEMIScheduleRepository _emiScheduleRepository;
    private readonly ILoanRepository _loanRepository;

    public GetUpcomingEMIsQueryHandler(IEMIScheduleRepository emiScheduleRepository, ILoanRepository loanRepository)
    {
        _emiScheduleRepository = emiScheduleRepository;
        _loanRepository = loanRepository;
    }

    public async Task<List<EMIScheduleDto>> Handle(GetUpcomingEMIsQuery query, CancellationToken ct)
    {
        await LoanOwnership.GetOwnedAsync(
            _loanRepository, query.LoanId, query.UserId, ct);
        var emis = await _emiScheduleRepository.GetUpcomingEMIsAsync(query.LoanId, query.Count, ct);

        return emis.Select(e => new EMIScheduleDto
        {
            Id = e.Id, LoanId = e.LoanId, EMINumber = e.EMINumber,
            EMIDate = e.EMIDate, EMIAmount = e.EMIAmount,
            PrincipalComponent = e.PrincipalComponent, InterestComponent = e.InterestComponent,
            OutstandingBefore = e.OutstandingBefore, OutstandingAfter = e.OutstandingAfter,
            IsPaid = e.IsPaid, LateFee = e.LateFee
        }).ToList();
    }
}
