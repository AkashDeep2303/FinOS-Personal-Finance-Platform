using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;

namespace FinOS.Loan.Application.Queries;

public class GetEMIScheduleQuery : IRequest<List<EMIScheduleDto>>
{
    public long LoanId { get; set; }

    public GetEMIScheduleQuery(long loanId) { LoanId = loanId; }
}

public class GetEMIScheduleQueryHandler : IRequestHandler<GetEMIScheduleQuery, List<EMIScheduleDto>>
{
    private readonly ILoanRepository _loanRepository;

    public GetEMIScheduleQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<List<EMIScheduleDto>> Handle(GetEMIScheduleQuery query, CancellationToken ct)
    {
        var loan = await _loanRepository.GetWithScheduleAsync(query.LoanId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Loan), query.LoanId);

        return loan.EMISchedule.OrderBy(e => e.EMINumber).Select(e => new EMIScheduleDto
        {
            Id = e.Id, LoanId = e.LoanId, EMINumber = e.EMINumber,
            EMIDate = e.EMIDate, EMIAmount = e.EMIAmount,
            PrincipalComponent = e.PrincipalComponent, InterestComponent = e.InterestComponent,
            OutstandingBefore = e.OutstandingBefore, OutstandingAfter = e.OutstandingAfter,
            IsPaid = e.IsPaid, PaidDate = e.PaidDate, PaidAmount = e.PaidAmount, LateFee = e.LateFee
        }).ToList();
    }
}
