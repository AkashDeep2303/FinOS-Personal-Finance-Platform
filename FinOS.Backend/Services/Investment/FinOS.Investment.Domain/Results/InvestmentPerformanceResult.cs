namespace FinOS.Investment.Domain.Results;

public record InvestmentPerformanceResult(
    decimal InvestedValue,
    decimal CurrentValue,
    decimal UnrealizedGain,
    decimal RealizedGain,
    decimal DividendIncome,
    decimal Charges,
    int SellTransactionCount,
    int ValuedSellTransactionCount);

public record ContributionTrendResult(int YearMonth, decimal Contributions, decimal Withdrawals, decimal Income);
public record PortfolioValueSnapshotResult(
    DateTime SnapshotDate, decimal InvestedValue, decimal CurrentValue, decimal UnrealizedGain);
