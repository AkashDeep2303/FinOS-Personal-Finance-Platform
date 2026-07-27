using FinOS.Common.Exceptions;
using FinOS.Common.Helpers;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;
using FinOS.Loan.Application.Services;

namespace FinOS.Loan.Application.Queries;

public record CompareLoanStrategyQuery(long UserId, CompareLoanStrategyRequest Request)
    : IRequest<LoanStrategyComparisonDto>;

public class CompareLoanStrategyQueryHandler
    : IRequestHandler<CompareLoanStrategyQuery, LoanStrategyComparisonDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ILoanPrepaymentRepository _prepaymentRepository;

    public CompareLoanStrategyQueryHandler(
        ILoanRepository loanRepository,
        ILoanPrepaymentRepository prepaymentRepository)
    {
        _loanRepository = loanRepository;
        _prepaymentRepository = prepaymentRepository;
    }

    public async Task<LoanStrategyComparisonDto> Handle(CompareLoanStrategyQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var loan = LoanOwnership.EnsureOwned(
            await _loanRepository.GetWithScheduleAsync(request.LoanId, ct),
            request.LoanId, query.UserId);
        if (!loan.IsPrepaymentAllowed)
            throw new DomainException("PREPAYMENT_NOT_ALLOWED", "Prepayment is not allowed for this loan.");

        var splitPrepayment = Math.Min(request.SplitPrepaymentAmount, request.SurplusAmount);
        var allPrepay = await BuildOption("Prepay", request.SurplusAmount, 0, request, ct);
        var allInvest = await BuildOption("Invest", 0, request.SurplusAmount, request, ct);
        var split = await BuildOption("Split", splitPrepayment, request.SurplusAmount - splitPrepayment, request, ct);

        return new LoanStrategyComparisonDto(
            loan.Id, request.SurplusAmount, request.ExpectedAnnualInvestmentReturn,
            request.InvestmentHorizonMonths, new[] { allPrepay, allInvest, split },
            "Investment returns are assumptions and are not guaranteed. Taxes, fees, market volatility and liquidity needs may change actual outcomes.");
    }

    private async Task<LoanStrategyOptionDto> BuildOption(
        string name, decimal prepayment, decimal investment,
        CompareLoanStrategyRequest request, CancellationToken ct)
    {
        decimal interestSaved = 0;
        decimal penalty = 0;
        var tenureSaved = 0;
        if (prepayment > 0)
        {
            var simulation = await _prepaymentRepository.SimulatePrepaymentAsync(
                request.LoanId, prepayment, "ReduceTenure", DateTime.UtcNow, ct);
            interestSaved = simulation.InterestSaved;
            penalty = simulation.PenaltyEstimate;
            tenureSaved = simulation.TenureSaved;
        }

        var futureValue = investment == 0 ? 0 : FinancialCalculator.CompoundInterestFutureValue(
            investment, request.ExpectedAnnualInvestmentReturn,
            request.InvestmentHorizonMonths / 12m, 12);
        futureValue = FinancialCalculator.RoundMoney(futureValue);
        var gain = FinancialCalculator.RoundMoney(futureValue - investment);
        var netBenefit = FinancialCalculator.RoundMoney(interestSaved + gain - penalty);
        var risk = investment == 0 ? "Low" :
            request.ExpectedAnnualInvestmentReturn <= 0.08m ? "Moderate" : "High";

        return new LoanStrategyOptionDto(
            name, prepayment, investment, interestSaved, tenureSaved,
            futureValue, gain, penalty, investment, netBenefit, risk);
    }
}
