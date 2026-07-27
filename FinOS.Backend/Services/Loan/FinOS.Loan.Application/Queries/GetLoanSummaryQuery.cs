using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;
using FinOS.Loan.Application.Services;

namespace FinOS.Loan.Application.Queries;

public class GetLoanSummaryQuery : IRequest<LoanSummaryDto>
{
    public long UserId { get; set; }
    public long LoanId { get; set; }

    public GetLoanSummaryQuery(long userId, long loanId) { UserId = userId; LoanId = loanId; }
}

public class GetLoanSummaryQueryHandler : IRequestHandler<GetLoanSummaryQuery, LoanSummaryDto>
{
    private readonly ILoanRepository _loanRepository;

    public GetLoanSummaryQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<LoanSummaryDto> Handle(GetLoanSummaryQuery query, CancellationToken ct)
    {
        var loan = LoanOwnership.EnsureOwned(
            await _loanRepository.GetWithPrepaymentsAsync(query.LoanId, ct),
            query.LoanId, query.UserId);

        var paidPct = loan.PrincipalAmount > 0
            ? Math.Round((loan.PrincipalAmount - loan.OutstandingPrincipal) / loan.PrincipalAmount * 100, 2)
            : 0;

        var interestSaved = loan.Prepayments.Sum(p => p.InterestSaved);

        return new LoanSummaryDto
        {
            LoanId = loan.Id,
            LenderName = loan.LenderName,
            PrincipalAmount = loan.PrincipalAmount,
            OutstandingPrincipal = loan.OutstandingPrincipal,
            TotalPaid = loan.TotalPaid,
            TotalInterestPaid = loan.TotalInterestPaid,
            TotalPrepaid = loan.TotalPrepaid,
            InterestSaved = interestSaved,
            PaidPercentage = paidPct,
            RemainingTenureMonths = loan.RemainingTenureMonths,
            NextEMIDate = loan.NextEMIDate
        };
    }
}
