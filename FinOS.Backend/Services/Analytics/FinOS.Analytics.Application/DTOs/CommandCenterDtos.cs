namespace FinOS.Analytics.Application.DTOs;

public record CommandCenterDto(
    DateTime AsOfUtc,
    CommandCenterMetricsDto Metrics,
    MoneyFlowDto MoneyFlow,
    AssetsAndLiabilitiesDto BalanceSheet,
    FinancialHealthSummaryDto FinancialHealth,
    IReadOnlyList<FinancialInsightDto> Insights,
    DataCompletenessDto DataCompleteness);

public record CommandCenterMetricsDto(
    decimal NetWorth,
    decimal? NetWorthChange,
    decimal? NetWorthChangePct,
    decimal CashAvailable,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal MonthlySurplus,
    decimal SavingsRatePct,
    int? FinancialHealthScore);

public record MoneyFlowDto(
    decimal Income,
    decimal TotalExpenses,
    decimal? EssentialExpenses,
    decimal? LifestyleExpenses,
    decimal? EmiPayments,
    decimal? Investments,
    decimal OtherExpenses,
    decimal Savings,
    decimal FreeCash);

public record AssetsAndLiabilitiesDto(
    decimal TotalAssets,
    decimal TotalLiabilities,
    IReadOnlyList<BreakdownItemDto> Assets,
    IReadOnlyList<BreakdownItemDto> Liabilities);

public record BreakdownItemDto(string Name, decimal Amount);

public record FinancialHealthSummaryDto(
    int? OverallScore,
    string? Grade,
    decimal? SavingsRatePct,
    decimal? DebtToIncomeRatio,
    decimal? EmergencyFundMonths);

public record FinancialInsightDto(
    string Code,
    string Severity,
    string Area,
    string Title,
    string Explanation,
    string Calculation,
    string ActionLabel,
    string ActionRoute);

public record DataCompletenessDto(
    int Score,
    IReadOnlyList<string> Available,
    IReadOnlyList<string> Missing);
