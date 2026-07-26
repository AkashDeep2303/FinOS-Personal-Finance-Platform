using FinOS.Analytics.Application.DTOs;
using FinOS.Common.Helpers;
using MediatR;

namespace FinOS.Analytics.Application.Queries;

public record CalculateFinancialToolQuery(CalculatorRequest Request) : IRequest<CalculatorResultDto>;

public class CalculateFinancialToolQueryHandler : IRequestHandler<CalculateFinancialToolQuery, CalculatorResultDto>
{
    public Task<CalculatorResultDto> Handle(CalculateFinancialToolQuery query, CancellationToken ct)
    {
        var x = query.Request;
        var rate = x.AnnualRate / 100m;
        decimal primary;
        decimal secondary;
        decimal contributed;
        var resultUnit = "INR";
        string formula;

        switch (x.Calculator.Trim().ToLowerInvariant())
        {
            case "emi":
                primary = FinancialCalculator.CalculateEMI(x.Principal, rate, x.Months);
                secondary = FinancialCalculator.TotalInterest(x.Principal, rate, x.Months);
                contributed = x.Principal;
                formula = "Reducing-balance EMI using monthly compounding.";
                break;
            case "sip":
                primary = FinancialCalculator.FutureValueWithMonthlyContributions(0, x.MonthlyAmount, rate, x.Months);
                contributed = x.MonthlyAmount * x.Months;
                secondary = primary - contributed;
                formula = "Future value of month-end contributions.";
                break;
            case "lumpsum":
                primary = FinancialCalculator.CompoundInterestFutureValue(x.Principal, rate, x.Months / 12m, 12);
                contributed = x.Principal;
                secondary = primary - contributed;
                formula = "Compound future value with monthly compounding.";
                break;
            case "goal":
                primary = FinancialCalculator.RequiredMonthlyContribution(x.TargetAmount, x.CurrentAmount, rate, x.Months);
                contributed = x.CurrentAmount;
                secondary = Math.Max(0, x.TargetAmount - x.CurrentAmount);
                formula = "Monthly contribution required for a future target.";
                break;
            case "inflation":
                primary = FinancialCalculator.InflationAdjustedValue(x.Principal, rate, (int)Math.Ceiling(x.Months / 12m));
                contributed = x.Principal;
                secondary = primary - contributed;
                formula = "Present amount compounded by the inflation assumption.";
                break;
            case "fd":
                primary = FinancialCalculator.CompoundInterestFutureValue(x.Principal, rate, x.Months / 12m, 4);
                contributed = x.Principal;
                secondary = primary - contributed;
                formula = "Fixed-deposit maturity using quarterly compounding; tax is not deducted.";
                break;
            case "rd":
                primary = FinancialCalculator.FutureValueWithMonthlyContributions(0, x.MonthlyAmount, rate, x.Months);
                contributed = x.MonthlyAmount * x.Months;
                secondary = primary - contributed;
                formula = "Recurring-deposit estimate using month-end deposits and monthly compounding; tax is not deducted.";
                break;
            case "cagr":
                primary = FinancialCalculator.CompoundAnnualGrowthRate(x.Principal, x.EndingAmount, x.Months / 12m) * 100m;
                contributed = x.Principal;
                secondary = x.EndingAmount - x.Principal;
                resultUnit = "PERCENT";
                formula = "Annualized growth rate from beginning value, ending value, and elapsed time.";
                break;
            case "emergencyfund":
                primary = x.MonthlyAmount * x.Months;
                secondary = Math.Max(0, primary - x.Principal);
                contributed = x.Principal;
                formula = "Target coverage months multiplied by essential monthly expenses.";
                break;
            case "creditcard":
                var payoff = FinancialCalculator.CreditCardPayoff(x.Principal, rate, x.MonthlyAmount);
                primary = payoff.Months;
                secondary = payoff.TotalInterest;
                contributed = x.Principal;
                resultUnit = "MONTHS";
                formula = "Fixed monthly payment after monthly interest until the balance reaches zero.";
                break;
            case "refinance":
                var newRate = x.EndingAmount / 100m;
                var existingEmi = FinancialCalculator.CalculateEMI(x.Principal, rate, x.Months);
                primary = FinancialCalculator.CalculateEMI(x.Principal, newRate, x.Months);
                var existingCost = existingEmi * x.Months;
                var newCost = primary * x.Months + x.MonthlyAmount;
                secondary = existingCost - newCost;
                contributed = x.MonthlyAmount;
                formula = "Existing remaining payments minus new remaining payments and refinance fees.";
                break;
            default:
                throw new ArgumentException("Unsupported calculator.");
        }

        var assumptions = new Dictionary<string, decimal>
        {
            ["annualRatePct"] = x.AnnualRate,
            ["months"] = x.Months
        };
        if (x.Calculator.Equals("refinance", StringComparison.OrdinalIgnoreCase))
        {
            assumptions["newAnnualRatePct"] = x.EndingAmount;
            assumptions["refinanceFees"] = x.MonthlyAmount;
        }
        if (x.Calculator.Equals("emergencyfund", StringComparison.OrdinalIgnoreCase))
            assumptions["coverageMonths"] = x.Months;

        return Task.FromResult(new CalculatorResultDto(
            x.Calculator, FinancialCalculator.RoundMoney(primary), FinancialCalculator.RoundMoney(secondary),
            FinancialCalculator.RoundMoney(contributed), resultUnit, formula, assumptions));
    }
}

public record CalculateXirrQuery(XirrRequest Request) : IRequest<CalculatorResultDto>;

public class CalculateXirrQueryHandler : IRequestHandler<CalculateXirrQuery, CalculatorResultDto>
{
    public Task<CalculatorResultDto> Handle(CalculateXirrQuery query, CancellationToken ct)
    {
        var flows = query.Request.CashFlows
            .Select(x => (x.Date.Date, x.Amount))
            .OrderBy(x => x.Date)
            .ToList();
        var rate = FinancialCalculator.ExtendedInternalRateOfReturn(flows);
        var invested = Math.Abs(flows.Where(x => x.Amount < 0).Sum(x => x.Amount));
        var netCashFlow = flows.Sum(x => x.Amount);
        var durationDays = (flows[^1].Date - flows[0].Date).Days;

        return Task.FromResult(new CalculatorResultDto(
            "xirr", Math.Round(rate * 100m, 2, MidpointRounding.ToEven),
            FinancialCalculator.RoundMoney(netCashFlow), FinancialCalculator.RoundMoney(invested),
            "PERCENT", "Annualized return from the exact dates and amounts of irregular cash flows.",
            new Dictionary<string, decimal>
            {
                ["cashFlowCount"] = flows.Count,
                ["durationDays"] = durationDays
            }));
    }
}

public record CalculateScenarioQuery(ScenarioRequest Request) : IRequest<ScenarioResultDto>;

public class CalculateScenarioQueryHandler : IRequestHandler<CalculateScenarioQuery, ScenarioResultDto>
{
    public Task<ScenarioResultDto> Handle(CalculateScenarioQuery query, CancellationToken ct)
    {
        var x = query.Request;
        var income = Math.Max(0, x.MonthlyIncome + x.MonthlyIncomeChange);
        var expenses = Math.Max(0, x.MonthlyExpenses + x.MonthlyExpenseChange);
        var debt = Math.Max(0, x.MonthlyDebtPayments + x.NewMonthlyDebtPayment);
        var currentSurplus = x.MonthlyIncome - x.MonthlyExpenses - x.MonthlyDebtPayments;
        var scenarioSurplus = income - expenses - debt;
        var currentRate = FinancialCalculator.SavingsRate(x.MonthlyIncome, x.MonthlyExpenses + x.MonthlyDebtPayments) * 100m;
        var scenarioRate = FinancialCalculator.SavingsRate(income, expenses + debt) * 100m;
        var currentDti = FinancialCalculator.DebtToIncomeRatio(x.MonthlyDebtPayments, x.MonthlyIncome) * 100m;
        var scenarioDti = FinancialCalculator.DebtToIncomeRatio(debt, income) * 100m;
        var currentEmergency = FinancialCalculator.EmergencyFundCoverage(x.LiquidAssets, x.MonthlyExpenses);
        var remainingLiquid = Math.Max(0, x.LiquidAssets - x.OneTimeCost);
        var scenarioEmergency = FinancialCalculator.EmergencyFundCoverage(remainingLiquid, expenses);
        var scenarioNetWorth = x.CurrentNetWorth - x.OneTimeCost + scenarioSurplus * x.HorizonMonths;
        var reasons = new List<string>();
        if (scenarioSurplus < 0) reasons.Add("The scenario creates a monthly cash-flow deficit.");
        if (scenarioDti > 40) reasons.Add("Debt-to-income exceeds the 40% scenario threshold.");
        if (scenarioEmergency < 3) reasons.Add("Emergency-fund coverage falls below three months.");
        var verdict = scenarioSurplus < 0 || scenarioDti > 50 || scenarioEmergency < 1 ? "High Risk"
            : scenarioDti > 40 || scenarioEmergency < 3 ? "Conditional"
            : scenarioRate < 10 ? "Manageable" : "Comfortable";
        if (reasons.Count == 0) reasons.Add("The supplied assumptions remain within the configured scenario thresholds.");

        return Task.FromResult(new ScenarioResultDto(
            x.ScenarioType, x.CurrentNetWorth, FinancialCalculator.RoundMoney(scenarioNetWorth),
            FinancialCalculator.RoundMoney(currentSurplus), FinancialCalculator.RoundMoney(scenarioSurplus),
            Math.Round(currentRate, 2), Math.Round(scenarioRate, 2), Math.Round(currentDti, 2), Math.Round(scenarioDti, 2),
            currentEmergency, scenarioEmergency, verdict, reasons,
            new Dictionary<string, decimal> { ["horizonMonths"] = x.HorizonMonths, ["oneTimeCost"] = x.OneTimeCost }));
    }
}
