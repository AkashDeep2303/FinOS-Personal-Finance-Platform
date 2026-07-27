using FinOS.Investment.Domain.Results;

namespace FinOS.Investment.Domain.Interfaces;

public interface IInvestmentAnalyticsRepository
{
    Task<InvestmentPerformanceResult> GetPerformanceAsync(long portfolioId, CancellationToken ct = default);
    Task<List<ContributionTrendResult>> GetContributionTrendAsync(long portfolioId, int months, CancellationToken ct = default);
    Task<List<PortfolioValueSnapshotResult>> GetValueHistoryAsync(long portfolioId, int months, CancellationToken ct = default);
}
