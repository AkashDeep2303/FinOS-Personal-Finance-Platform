using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Investment.Domain.Results;

namespace FinOS.Investment.Infrastructure.Repositories;

public class InvestmentAnalyticsRepository : IInvestmentAnalyticsRepository
{
    private readonly IConnectionFactory _connectionFactory;
    public InvestmentAnalyticsRepository(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<InvestmentPerformanceResult> GetPerformanceAsync(long portfolioId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<InvestmentPerformanceResult>(new CommandDefinition(
            """
            SELECT ISNULL((SELECT SUM(InvestedAmount) FROM Investment.Holdings WHERE PortfolioId=@PortfolioId AND DeletedAt IS NULL),0) InvestedValue,
                   ISNULL((SELECT SUM(CurrentValue) FROM Investment.Holdings WHERE PortfolioId=@PortfolioId AND DeletedAt IS NULL),0) CurrentValue,
                   ISNULL((SELECT SUM(CurrentValue-InvestedAmount) FROM Investment.Holdings WHERE PortfolioId=@PortfolioId AND DeletedAt IS NULL),0) UnrealizedGain,
                   ISNULL(SUM(CASE WHEN t.TransactionType=N'Sell' THEN t.RealizedGain ELSE 0 END),0) RealizedGain,
                   ISNULL(SUM(CASE WHEN t.TransactionType=N'Dividend' THEN t.TotalAmount ELSE 0 END),0) DividendIncome,
                   ISNULL(SUM(t.Charges+t.STT+t.StampDuty),0) Charges,
                   ISNULL(SUM(CASE WHEN t.TransactionType=N'Sell' THEN 1 ELSE 0 END),0) SellTransactionCount,
                   ISNULL(SUM(CASE WHEN t.TransactionType=N'Sell' AND t.RealizedGain IS NOT NULL THEN 1 ELSE 0 END),0) ValuedSellTransactionCount
            FROM Investment.Transactions t
            INNER JOIN Investment.Holdings h ON h.Id=t.HoldingId
            WHERE h.PortfolioId=@PortfolioId
            """, new { PortfolioId = portfolioId }, cancellationToken: ct));
    }

    public async Task<List<ContributionTrendResult>> GetContributionTrendAsync(long portfolioId, int months, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ContributionTrendResult>(new CommandDefinition(
            """
            SELECT YEAR(t.TransactionDate)*100+MONTH(t.TransactionDate) YearMonth,
                   SUM(CASE WHEN t.TransactionType IN (N'Buy',N'SIP') THEN t.TotalAmount ELSE 0 END) Contributions,
                   SUM(CASE WHEN t.TransactionType=N'Sell' THEN t.TotalAmount ELSE 0 END) Withdrawals,
                   SUM(CASE WHEN t.TransactionType=N'Dividend' THEN t.TotalAmount ELSE 0 END) Income
            FROM Investment.Transactions t INNER JOIN Investment.Holdings h ON h.Id=t.HoldingId
            WHERE h.PortfolioId=@PortfolioId AND t.TransactionDate>=DATEADD(MONTH,-@Months,CAST(GETUTCDATE() AS DATE))
            GROUP BY YEAR(t.TransactionDate),MONTH(t.TransactionDate) ORDER BY YearMonth
            """, new { PortfolioId = portfolioId, Months = months }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<List<PortfolioValueSnapshotResult>> GetValueHistoryAsync(long portfolioId, int months, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<PortfolioValueSnapshotResult>(new CommandDefinition(
            """
            SELECT SnapshotDate, InvestedValue, CurrentValue, UnrealizedGain
            FROM Investment.PortfolioValueSnapshots
            WHERE PortfolioId=@PortfolioId AND SnapshotDate>=DATEADD(MONTH,-@Months,CAST(GETUTCDATE() AS DATE))
            ORDER BY SnapshotDate
            """, new { PortfolioId = portfolioId, Months = months }, cancellationToken: ct));
        return rows.ToList();
    }
}
