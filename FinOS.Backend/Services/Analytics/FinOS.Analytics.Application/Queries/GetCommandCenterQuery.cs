using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;

namespace FinOS.Analytics.Application.Queries;

public record GetCommandCenterQuery(long UserId) : IRequest<CommandCenterDto>;

public class GetCommandCenterQueryHandler : IRequestHandler<GetCommandCenterQuery, CommandCenterDto>
{
    private readonly INetWorthRepository _netWorthRepository;
    private readonly IMonthlyAggregateRepository _aggregateRepository;
    private readonly IFinancialScoreRepository _scoreRepository;
    private readonly ICashFlowClassificationRepository _cashFlowRepository;

    public GetCommandCenterQueryHandler(
        INetWorthRepository netWorthRepository,
        IMonthlyAggregateRepository aggregateRepository,
        IFinancialScoreRepository scoreRepository,
        ICashFlowClassificationRepository cashFlowRepository)
    {
        _netWorthRepository = netWorthRepository;
        _aggregateRepository = aggregateRepository;
        _scoreRepository = scoreRepository;
        _cashFlowRepository = cashFlowRepository;
    }

    public async Task<CommandCenterDto> Handle(GetCommandCenterQuery request, CancellationToken ct)
    {
        var yearMonth = int.Parse(DateTime.UtcNow.ToString("yyyyMM"));
        var netWorthTask = _netWorthRepository.GetLatestByUserAsync(request.UserId, ct);
        var aggregateTask = _aggregateRepository.GetByUserAndMonthAsync(request.UserId, yearMonth, ct);
        var scoreTask = _scoreRepository.GetLatestByUserAsync(request.UserId, ct);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var cashFlowTask = _cashFlowRepository.GetForMonthAsync(request.UserId, monthStart, monthStart.AddMonths(1), ct);
        await Task.WhenAll(netWorthTask, aggregateTask, scoreTask, cashFlowTask);

        var netWorth = await netWorthTask;
        var aggregate = await aggregateTask;
        var score = await scoreTask;
        var cashFlow = await cashFlowTask;

        var monthlyIncome = aggregate?.TotalIncome ?? 0;
        var monthlyExpenses = aggregate?.TotalExpense ?? 0;
        var monthlySurplus = aggregate?.TotalSavings ?? monthlyIncome - monthlyExpenses;
        var savingsRate = aggregate?.SavingsRate ?? (monthlyIncome == 0 ? 0 : Math.Round(monthlySurplus / monthlyIncome * 100, 2));

        var assets = new List<BreakdownItemDto>
        {
            new("Cash & bank", netWorth?.CashAndBank ?? 0),
            new("Investments", netWorth?.InvestmentValue ?? 0),
            new("Property", netWorth?.RealEstateValue ?? 0),
            new("Gold", netWorth?.GoldValue ?? 0),
            new("Other assets", netWorth?.OtherAssets ?? 0)
        }.Where(x => x.Amount != 0).ToList();

        var liabilities = new List<BreakdownItemDto>
        {
            new("Loans", netWorth?.LoanOutstanding ?? 0),
            new("Credit cards", netWorth?.CreditCardOutstanding ?? 0),
            new("Other liabilities", netWorth?.OtherLiabilities ?? 0)
        }.Where(x => x.Amount != 0).ToList();

        var insights = BuildInsights(
            savingsRate, monthlyIncome, monthlyExpenses, monthlySurplus,
            score?.DebtToIncomeRatio, score?.EmergencyFundMonths,
            cashFlow, netWorth?.ChangeFromPrevious, netWorth?.CashAndBank ?? 0);
        var available = new List<string>();
        if (netWorth is not null) available.Add("Net worth and balance sheet");
        if (aggregate is not null) available.Add("Current-month income and expenses");
        if (score is not null) available.Add("Financial health");

        var missing = new List<string>();
        if (netWorth is null) missing.Add("Net worth snapshot");
        if (aggregate is null) missing.Add("Current-month aggregate");
        if (score is null) missing.Add("Financial health score");
        available.Add("Cash-flow expense classification");
        var completeness = (int)Math.Round(available.Count * 100m / (available.Count + missing.Count));

        return new CommandCenterDto(
            DateTime.UtcNow,
            new CommandCenterMetricsDto(
                netWorth?.NetWorth ?? 0, netWorth?.ChangeFromPrevious, netWorth?.ChangePctFromPrevious,
                netWorth?.CashAndBank ?? 0, monthlyIncome, monthlyExpenses, monthlySurplus,
                savingsRate, score?.OverallScore),
            new MoneyFlowDto(
                monthlyIncome, monthlyExpenses, cashFlow.EssentialExpenses, cashFlow.LifestyleExpenses,
                cashFlow.EmiPayments, cashFlow.Investments, cashFlow.OtherExpenses,
                monthlySurplus, monthlySurplus),
            new AssetsAndLiabilitiesDto(netWorth?.TotalAssets ?? 0, netWorth?.TotalLiabilities ?? 0, assets, liabilities),
            new FinancialHealthSummaryDto(score?.OverallScore, score?.ScoreGrade.ToString(), score?.SavingsRatePct, score?.DebtToIncomeRatio, score?.EmergencyFundMonths),
            insights,
            new DataCompletenessDto(completeness, available, missing));
    }

    private static IReadOnlyList<FinancialInsightDto> BuildInsights(
        decimal savingsRate, decimal income, decimal expenses, decimal surplus,
        decimal? dti, decimal? emergencyFundMonths,
        FinOS.Analytics.Domain.Results.CashFlowClassificationResult cashFlow,
        decimal? netWorthChange, decimal cashAvailable)
    {
        var insights = new List<FinancialInsightDto>();
        if (surplus < 0)
            insights.Add(new("NEGATIVE_SURPLUS", "high", "Cash flow", "Expenses exceeded income",
                "Recorded expenses are higher than recorded income this month.",
                $"{income:N2} - {expenses:N2} = {surplus:N2}",
                "Review cash flow", "/cash-flow"));
        if (income > 0 && savingsRate < 20)
            insights.Add(new("LOW_SAVINGS_RATE", "warning", "Cash flow", "Savings rate needs attention",
                "Less than 20% of recorded monthly income is currently retained.",
                $"({income:N2} - {expenses:N2}) / {income:N2} = {savingsRate:N2}%",
                "Review spending", "/cash-flow"));
        if (income > 0 && cashFlow.LifestyleExpenses / income > 0.30m)
            insights.Add(new("HIGH_LIFESTYLE_RATIO", "warning", "Cash flow", "Lifestyle spending is elevated",
                "Lifestyle-classified expenses exceed 30% of recorded income this month.",
                $"{cashFlow.LifestyleExpenses:N2} / {income:N2} = {cashFlow.LifestyleExpenses / income:P1}",
                "Review categories", "/cash-flow"));
        if (expenses > 0 && cashFlow.OtherExpenses / expenses > 0.20m)
            insights.Add(new("LOW_CLASSIFICATION_QUALITY", "warning", "Data quality", "A large share of spending is unclassified",
                "More than 20% of recorded expenses are in Other or have no category classification.",
                $"{cashFlow.OtherExpenses:N2} / {expenses:N2} = {cashFlow.OtherExpenses / expenses:P1}",
                "Classify categories", "/categories"));
        if (dti is > 0.40m)
            insights.Add(new("HIGH_DTI", "high", "Debt", "Debt ratio is elevated",
                "Recorded debt obligations are high relative to income.",
                $"Debt-to-income ratio = {dti.Value:P1}", "Review debt", "/loans"));
        if (emergencyFundMonths is < 3m)
            insights.Add(new("LOW_EMERGENCY_FUND", "warning", "Protection", "Emergency fund coverage is low",
                "Recorded liquid reserves cover fewer than three months of expenses.",
                $"Emergency fund coverage = {emergencyFundMonths.Value:N1} months", "Review financial health", "/financial-health"));
        if (netWorthChange is < 0)
            insights.Add(new("NET_WORTH_DECLINE", "warning", "Net worth", "Net worth decreased",
                "The latest recorded net-worth snapshot is below the previous snapshot.",
                $"Latest snapshot change = {netWorthChange.Value:N2}", "Understand the change", "/net-worth"));
        if (expenses > 0 && cashAvailable > expenses * 12)
            insights.Add(new("EXCESS_LIQUID_CASH", "info", "Wealth", "Liquid cash is high relative to spending",
                "Recorded cash and bank balances exceed twelve months of current recorded expenses. This is an opportunity for review, not an instruction to invest.",
                $"{cashAvailable:N2} / {expenses:N2} = {cashAvailable / expenses:N1} months",
                "Review allocation", "/investments"));
        return insights;
    }
}
