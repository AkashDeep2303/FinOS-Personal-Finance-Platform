using FinOS.Analytics.Application.Queries;
using FinOS.Analytics.Domain.Interfaces;
using FinOS.Analytics.Domain.Results;
using Xunit;

namespace FinOS.Common.Tests;

public sealed class CashFlowAnalyticsTests
{
    [Fact]
    public async Task Handler_ComputesRatiosVolatilityAndZeroActivityMonths()
    {
        var repository = new StubRepository(
        [
            new MonthlyCashFlowResult
            {
                YearMonth = 202601, Income = 100, TotalExpenses = 50,
                EssentialExpenses = 20, LifestyleExpenses = 10,
                EmiPayments = 5, Investments = 10, OtherExpenses = 5
            },
            new MonthlyCashFlowResult
            {
                YearMonth = 202602, Income = 200, TotalExpenses = 100,
                EssentialExpenses = 50, LifestyleExpenses = 20,
                EmiPayments = 5, Investments = 20, OtherExpenses = 5
            }
        ]);
        var handler = new GetCashFlowAnalyticsHandler(repository);

        var result = await handler.Handle(
            new GetCashFlowAnalyticsQuery(7, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1)),
            CancellationToken.None);

        Assert.Equal(3, result.Series.Count);
        Assert.Equal(202603, result.Series[2].YearMonth);
        Assert.Equal(0, result.Series[2].Income);
        Assert.Equal(300, result.Metrics.Income);
        Assert.Equal(150, result.Metrics.Expenses);
        Assert.Equal(50, result.Metrics.AverageSurplus);
        Assert.Equal(50, result.Metrics.SavingsRatePct);
        Assert.Equal(3.33m, result.Metrics.EmiRatioPct);
        Assert.Equal(26.67m, result.Metrics.FixedCostRatioPct);
        Assert.Equal(10, result.Metrics.InvestmentRatePct);
        Assert.Equal(81.65m, result.Metrics.IncomeVolatilityPct);
        Assert.Equal(81.65m, result.Metrics.ExpenseVolatilityPct);
    }

    private sealed class StubRepository(IReadOnlyList<MonthlyCashFlowResult> rows)
        : ICashFlowClassificationRepository
    {
        public Task<CashFlowClassificationResult> GetForMonthAsync(
            long userId, DateTime monthStart, DateTime monthEnd, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CashFlowClassificationResult());

        public Task<IReadOnlyList<MonthlyCashFlowResult>> GetHistoryAsync(
            long userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult(rows);
    }
}
