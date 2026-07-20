using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;

namespace FinOS.Loan.Application.Queries;

public class GetUpcomingEMIsQuery : IRequest<List<EMIScheduleDto>>
{
    public long LoanId { get; set; }
    public int Count { get; set; } = 3;

    public GetUpcomingEMIsQuery(long loanId, int count = 3)
    {
        LoanId = loanId;
        Count = count;
    }
}

public class GetUpcomingEMIsQueryHandler : IRequestHandler<GetUpcomingEMIsQuery, List<EMIScheduleDto>>
{
    private readonly IEMIScheduleRepository _emiScheduleRepository;

    public GetUpcomingEMIsQueryHandler(IEMIScheduleRepository emiScheduleRepository)
    {
        _emiScheduleRepository = emiScheduleRepository;
    }

    public async Task<List<EMIScheduleDto>> Handle(GetUpcomingEMIsQuery query, CancellationToken ct)
    {
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
