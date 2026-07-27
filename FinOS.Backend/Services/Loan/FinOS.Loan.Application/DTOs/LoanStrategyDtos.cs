namespace FinOS.Loan.Application.DTOs;

public record CompareLoanStrategyRequest(
    long LoanId,
    decimal SurplusAmount,
    decimal SplitPrepaymentAmount,
    decimal ExpectedAnnualInvestmentReturn,
    int InvestmentHorizonMonths);

public record LoanStrategyComparisonDto(
    long LoanId,
    decimal SurplusAmount,
    decimal AssumedAnnualInvestmentReturn,
    int InvestmentHorizonMonths,
    IReadOnlyList<LoanStrategyOptionDto> Options,
    string Disclaimer);

public record LoanStrategyOptionDto(
    string Strategy,
    decimal PrepaymentAmount,
    decimal InvestmentAmount,
    decimal InterestSaved,
    int TenureReductionMonths,
    decimal InvestmentFutureValue,
    decimal EstimatedInvestmentGain,
    decimal PenaltyEstimate,
    decimal LiquidityRemaining,
    decimal ProjectedNetBenefit,
    string RiskIndicator);
