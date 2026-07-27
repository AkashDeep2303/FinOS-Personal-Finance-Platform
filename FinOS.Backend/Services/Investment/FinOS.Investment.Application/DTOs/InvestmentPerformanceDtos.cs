namespace FinOS.Investment.Application.DTOs;

public record InvestmentPerformanceDto(
    long PortfolioId, decimal InvestedValue, decimal CurrentValue,
    decimal UnrealizedGain, decimal RealizedGain, decimal DividendIncome,
    decimal Charges, decimal AbsoluteReturnPct, bool RealizedGainComplete,
    IReadOnlyList<ContributionTrendDto> ContributionTrend,
    IReadOnlyList<PortfolioValuePointDto> ValueHistory);

public record ContributionTrendDto(int YearMonth, decimal Contributions, decimal Withdrawals, decimal Income);
public record PortfolioValuePointDto(DateTime Date, decimal InvestedValue, decimal CurrentValue, decimal UnrealizedGain);
