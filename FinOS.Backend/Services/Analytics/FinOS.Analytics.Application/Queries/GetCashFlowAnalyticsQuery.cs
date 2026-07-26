using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;

namespace FinOS.Analytics.Application.Queries;

public sealed record GetCashFlowAnalyticsQuery(long UserId, DateTime StartDate, DateTime EndDate)
    : IRequest<CashFlowAnalyticsDto>;

public sealed class GetCashFlowAnalyticsHandler(ICashFlowClassificationRepository repository)
    : IRequestHandler<GetCashFlowAnalyticsQuery, CashFlowAnalyticsDto>
{
    public async Task<CashFlowAnalyticsDto> Handle(GetCashFlowAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var raw = await repository.GetHistoryAsync(request.UserId, request.StartDate, request.EndDate, cancellationToken);
        var byMonth = raw.ToDictionary(x => x.YearMonth);
        var rows = new List<MonthlyCashFlowDto>();
        for (var month = new DateTime(request.StartDate.Year, request.StartDate.Month, 1);
             month < request.EndDate; month = month.AddMonths(1))
        {
            var key = month.Year * 100 + month.Month;
            byMonth.TryGetValue(key, out var item);
            var income = item?.Income ?? 0;
            var expenses = item?.TotalExpenses ?? 0;
            rows.Add(new MonthlyCashFlowDto(
                key, income, expenses, income - expenses,
                item?.EssentialExpenses ?? 0, item?.LifestyleExpenses ?? 0,
                item?.EmiPayments ?? 0, item?.Investments ?? 0, item?.OtherExpenses ?? 0));
        }

        var incomeTotal = rows.Sum(x => x.Income);
        var expenseTotal = rows.Sum(x => x.Expenses);
        var surplusTotal = incomeTotal - expenseTotal;
        var divisor = rows.Count == 0 ? 1 : rows.Count;
        decimal Ratio(decimal numerator) => incomeTotal == 0 ? 0 : Math.Round(numerator / incomeTotal * 100, 2);
        var metrics = new CashFlowMetricsDto(
            incomeTotal, expenseTotal, rows.LastOrDefault()?.Surplus ?? 0,
            Math.Round(surplusTotal / divisor, 2), Ratio(surplusTotal), Ratio(expenseTotal),
            Ratio(rows.Sum(x => x.EmiPayments)),
            Ratio(rows.Sum(x => x.EssentialExpenses + x.EmiPayments)),
            Ratio(rows.Sum(x => x.LifestyleExpenses)),
            Ratio(rows.Sum(x => x.Investments)),
            Volatility(rows.Select(x => x.Income)),
            Volatility(rows.Select(x => x.Expenses)));
        return new CashFlowAnalyticsDto(request.StartDate.Date, request.EndDate.AddDays(-1).Date, metrics, rows);
    }

    private static decimal Volatility(IEnumerable<decimal> values)
    {
        var items = values.ToArray();
        if (items.Length == 0) return 0;
        var mean = items.Average();
        if (mean == 0) return 0;
        var variance = items.Sum(value => (value - mean) * (value - mean)) / items.Length;
        return Math.Round((decimal)Math.Sqrt((double)variance) / Math.Abs(mean) * 100, 2);
    }
}
