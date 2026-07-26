using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;
using FinOS.Loan.Application.Services;

namespace FinOS.Loan.Application.Queries;

public class GetEMIScheduleQuery : IRequest<List<EMIScheduleDto>>
{
    public long UserId { get; set; }
    public long LoanId { get; set; }

    public GetEMIScheduleQuery(long userId, long loanId) { UserId = userId; LoanId = loanId; }
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
        var loan = LoanOwnership.EnsureOwned(
            await _loanRepository.GetWithScheduleAsync(query.LoanId, ct),
            query.LoanId, query.UserId);

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
