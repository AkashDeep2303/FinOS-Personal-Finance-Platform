namespace FinOS.Analytics.Application.DTOs;

public sealed record CashFlowAnalyticsDto(
    DateTime StartDate,
    DateTime EndDate,
    CashFlowMetricsDto Metrics,
    IReadOnlyList<MonthlyCashFlowDto> Series);

public sealed record CashFlowMetricsDto(
    decimal Income,
    decimal Expenses,
    decimal MonthlySurplus,
    decimal AverageSurplus,
    decimal SavingsRatePct,
    decimal ExpenseRatioPct,
    decimal EmiRatioPct,
    decimal FixedCostRatioPct,
    decimal LifestyleCostRatioPct,
    decimal InvestmentRatePct,
    decimal IncomeVolatilityPct,
    decimal ExpenseVolatilityPct);

public sealed record MonthlyCashFlowDto(
    int YearMonth,
    decimal Income,
    decimal Expenses,
    decimal Surplus,
    decimal EssentialExpenses,
    decimal LifestyleExpenses,
    decimal EmiPayments,
    decimal Investments,
    decimal OtherExpenses);
