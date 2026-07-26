namespace FinOS.Loan.Application.DTOs;

public record LoanRateHistoryDto(long Id, decimal PreviousRate, decimal NewRate, DateTime EffectiveDate, string? Reason);
public record AddLoanRateChangeRequest(decimal NewRate, DateTime EffectiveDate, string? Reason);
public record LoanPaymentAnalysisDto(
    int ScheduledPayments, int PaidPayments, int UpcomingPayments, int LatePayments,
    decimal ScheduledPrincipal, decimal PrincipalPaid, decimal ScheduledInterest,
    decimal InterestPaid, decimal LateFeesPaid, decimal RemainingInterest);
