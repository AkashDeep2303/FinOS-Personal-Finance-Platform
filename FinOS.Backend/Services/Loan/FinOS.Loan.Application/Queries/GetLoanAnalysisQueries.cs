using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;
using FinOS.Loan.Application.Services;

namespace FinOS.Loan.Application.Queries;

public record GetLoanRateHistoryQuery(long UserId, long LoanId) : IRequest<List<LoanRateHistoryDto>>;
public record GetLoanPaymentAnalysisQuery(long UserId, long LoanId) : IRequest<LoanPaymentAnalysisDto>;

public class GetLoanRateHistoryQueryHandler : IRequestHandler<GetLoanRateHistoryQuery, List<LoanRateHistoryDto>>
{
    private readonly ILoanRepository _repository;
    public GetLoanRateHistoryQueryHandler(ILoanRepository repository) => _repository = repository;
    public async Task<List<LoanRateHistoryDto>> Handle(GetLoanRateHistoryQuery query, CancellationToken ct)
    {
        await LoanOwnership.GetOwnedAsync(_repository, query.LoanId, query.UserId, ct);
        return (await _repository.GetRateHistoryAsync(query.LoanId, ct))
            .Select(x => new LoanRateHistoryDto(x.Id, x.PreviousRate, x.NewRate, x.EffectiveDate, x.Reason)).ToList();
    }
}

public class GetLoanPaymentAnalysisQueryHandler : IRequestHandler<GetLoanPaymentAnalysisQuery, LoanPaymentAnalysisDto>
{
    private readonly ILoanRepository _repository;
    public GetLoanPaymentAnalysisQueryHandler(ILoanRepository repository) => _repository = repository;
    public async Task<LoanPaymentAnalysisDto> Handle(GetLoanPaymentAnalysisQuery query, CancellationToken ct)
    {
        await LoanOwnership.GetOwnedAsync(_repository, query.LoanId, query.UserId, ct);
        var x = await _repository.GetPaymentAnalysisAsync(query.LoanId, ct);
        return new(x.ScheduledPayments, x.PaidPayments, x.UpcomingPayments, x.LatePayments,
            x.ScheduledPrincipal, x.PrincipalPaid, x.ScheduledInterest, x.InterestPaid, x.LateFeesPaid, x.RemainingInterest);
    }
}
