using FinOS.Analytics.Application.Queries;
using FluentValidation;

namespace FinOS.Analytics.Application.Validators;

public class FinancialToolValidator : AbstractValidator<CalculateFinancialToolQuery>
{
    private static readonly string[] Supported = ["emi", "sip", "lumpsum", "goal", "inflation", "fd", "rd", "cagr", "emergencyfund", "creditcard", "refinance"];
    public FinancialToolValidator()
    {
        RuleFor(x => x.Request.Calculator).Must(x => Supported.Contains(x.Trim().ToLowerInvariant()));
        RuleFor(x => x.Request.Principal).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.MonthlyAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.AnnualRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Request.Months).InclusiveBetween(1, 1200);
        RuleFor(x => x.Request.TargetAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.CurrentAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.EndingAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request).Must(x => !x.Calculator.Equals("emi", StringComparison.OrdinalIgnoreCase) || x.Principal > 0)
            .WithMessage("EMI principal must be positive.");
        RuleFor(x => x.Request).Must(x => !x.Calculator.Equals("fd", StringComparison.OrdinalIgnoreCase) || x.Principal > 0)
            .WithMessage("FD principal must be positive.");
        RuleFor(x => x.Request).Must(x => !x.Calculator.Equals("rd", StringComparison.OrdinalIgnoreCase) || x.MonthlyAmount > 0)
            .WithMessage("RD monthly deposit must be positive.");
        RuleFor(x => x.Request).Must(x => !x.Calculator.Equals("cagr", StringComparison.OrdinalIgnoreCase) ||
            (x.Principal > 0 && x.EndingAmount >= 0 && x.Months >= 12))
            .WithMessage("CAGR requires a positive beginning value and at least 12 months.");
        RuleFor(x => x.Request).Must(x => !x.Calculator.Equals("emergencyfund", StringComparison.OrdinalIgnoreCase) ||
            (x.MonthlyAmount > 0 && x.Months is >= 1 and <= 24))
            .WithMessage("Emergency fund requires positive essential monthly expenses and 1-24 coverage months.");
        RuleFor(x => x.Request).Must(x => !x.Calculator.Equals("creditcard", StringComparison.OrdinalIgnoreCase) ||
            (x.Principal > 0 && x.MonthlyAmount > 0))
            .WithMessage("Credit-card payoff requires a positive balance and monthly payment.");
        RuleFor(x => x.Request).Must(x => !x.Calculator.Equals("refinance", StringComparison.OrdinalIgnoreCase) ||
            (x.Principal > 0 && x.EndingAmount >= 0 && x.EndingAmount <= 100))
            .WithMessage("Refinance requires a positive balance and a valid new annual rate.");
    }
}

public class ScenarioValidator : AbstractValidator<CalculateScenarioQuery>
{
    public ScenarioValidator()
    {
        RuleFor(x => x.Request.ScenarioType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Request.CurrentNetWorth).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.MonthlyIncome).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.MonthlyExpenses).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.MonthlyDebtPayments).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.LiquidAssets).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.OneTimeCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.NewMonthlyDebtPayment).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.HorizonMonths).InclusiveBetween(1, 600);
    }
}

public class XirrValidator : AbstractValidator<CalculateXirrQuery>
{
    public XirrValidator()
    {
        RuleFor(x => x.Request.CashFlows).NotNull().Must(x => x is { Count: >= 2 and <= 200 })
            .WithMessage("XIRR requires between 2 and 200 dated cash flows.");
        RuleForEach(x => x.Request.CashFlows).ChildRules(flow =>
        {
            flow.RuleFor(x => x.Date).NotEmpty();
        });
        RuleFor(x => x.Request.CashFlows).Must(x =>
                x is not null && x.Any(flow => flow.Amount < 0) && x.Any(flow => flow.Amount > 0))
            .WithMessage("XIRR requires at least one investment and one redemption/current value.");
        RuleFor(x => x.Request.CashFlows).Must(x =>
                x is not null && x.Select(flow => flow.Date.Date).Distinct().Count() >= 2)
            .WithMessage("XIRR cash flows must span at least two different dates.");
    }
}

public class SaveScenarioValidator : AbstractValidator<FinOS.Analytics.Application.Commands.SaveScenarioCommand>
{
    public SaveScenarioValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => new CalculateScenarioQuery(x.Request.Scenario)).SetValidator(new ScenarioValidator());
    }
}

public class FinancialYearReviewValidator : AbstractValidator<GetFinancialYearReviewQuery>
{
    public FinancialYearReviewValidator() =>
        RuleFor(x => x.StartYear).InclusiveBetween(2000, DateTime.UtcNow.Year);
}
