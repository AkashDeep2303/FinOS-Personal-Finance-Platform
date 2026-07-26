using Dapper;
using FinOS.Analytics.Domain.Interfaces;
using FinOS.Analytics.Domain.Results;
using FinOS.Common.Interfaces;

namespace FinOS.Analytics.Infrastructure.Repositories;

public sealed class CashFlowClassificationRepository(IConnectionFactory connectionFactory)
    : ICashFlowClassificationRepository
{
    public async Task<CashFlowClassificationResult> GetForMonthAsync(
        long userId, DateTime monthStart, DateTime monthEnd, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                COALESCE(SUM(CASE WHEN c.CashFlowClassification = N'Essential' THEN t.Amount ELSE 0 END), 0) AS EssentialExpenses,
                COALESCE(SUM(CASE WHEN c.CashFlowClassification = N'Lifestyle' THEN t.Amount ELSE 0 END), 0) AS LifestyleExpenses,
                COALESCE(SUM(CASE WHEN c.CashFlowClassification = N'EMI' THEN t.Amount ELSE 0 END), 0) AS EmiPayments,
                COALESCE(SUM(CASE WHEN c.CashFlowClassification = N'Investment' THEN t.Amount ELSE 0 END), 0) AS Investments,
                COALESCE(SUM(CASE WHEN c.CashFlowClassification IS NULL OR c.CashFlowClassification = N'Other' THEN t.Amount ELSE 0 END), 0) AS OtherExpenses
            FROM Core.Transactions t
            LEFT JOIN Core.Categories c ON c.Id = t.CategoryId
            WHERE t.UserId = @UserId AND t.Type = N'Expense' AND t.DeletedAt IS NULL
              AND t.TransactionDate >= @MonthStart AND t.TransactionDate < @MonthEnd;
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<CashFlowClassificationResult>(
            new CommandDefinition(sql, new { UserId = userId, MonthStart = monthStart, MonthEnd = monthEnd },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<MonthlyCashFlowResult>> GetHistoryAsync(
        long userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT YEAR(t.TransactionDate) * 100 + MONTH(t.TransactionDate) AS YearMonth,
                COALESCE(SUM(CASE WHEN t.Type = N'Income' THEN t.Amount ELSE 0 END), 0) AS Income,
                COALESCE(SUM(CASE WHEN t.Type = N'Expense' THEN t.Amount ELSE 0 END), 0) AS TotalExpenses,
                COALESCE(SUM(CASE WHEN t.Type = N'Expense' AND c.CashFlowClassification = N'Essential' THEN t.Amount ELSE 0 END), 0) AS EssentialExpenses,
                COALESCE(SUM(CASE WHEN t.Type = N'Expense' AND c.CashFlowClassification = N'Lifestyle' THEN t.Amount ELSE 0 END), 0) AS LifestyleExpenses,
                COALESCE(SUM(CASE WHEN t.Type = N'Expense' AND c.CashFlowClassification = N'EMI' THEN t.Amount ELSE 0 END), 0) AS EmiPayments,
                COALESCE(SUM(CASE WHEN t.Type = N'Expense' AND c.CashFlowClassification = N'Investment' THEN t.Amount ELSE 0 END), 0) AS Investments,
                COALESCE(SUM(CASE WHEN t.Type = N'Expense' AND (c.CashFlowClassification IS NULL OR c.CashFlowClassification = N'Other') THEN t.Amount ELSE 0 END), 0) AS OtherExpenses
            FROM Core.Transactions t
            LEFT JOIN Core.Categories c ON c.Id = t.CategoryId
            WHERE t.UserId = @UserId AND t.DeletedAt IS NULL
              AND t.TransactionDate >= @StartDate AND t.TransactionDate < @EndDate
              AND t.Type IN (N'Income', N'Expense')
            GROUP BY YEAR(t.TransactionDate), MONTH(t.TransactionDate)
            ORDER BY YearMonth;
            """;
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<MonthlyCashFlowResult>(
            new CommandDefinition(sql, new { UserId = userId, StartDate = startDate, EndDate = endDate },
                cancellationToken: cancellationToken))).AsList();
    }
}
