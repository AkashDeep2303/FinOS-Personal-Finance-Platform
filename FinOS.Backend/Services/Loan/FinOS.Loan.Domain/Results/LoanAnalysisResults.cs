namespace FinOS.Loan.Domain.Results;

public record LoanRateHistoryResult(
    long Id, long LoanId, decimal PreviousRate, decimal NewRate,
    DateTime EffectiveDate, string? Reason, DateTime CreatedAt);

public record LoanPaymentAnalysisResult(
    int ScheduledPayments,
    int PaidPayments,
    int UpcomingPayments,
    int LatePayments,
    decimal ScheduledPrincipal,
    decimal PrincipalPaid,
    decimal ScheduledInterest,
    decimal InterestPaid,
    decimal LateFeesPaid,
    decimal RemainingInterest);
