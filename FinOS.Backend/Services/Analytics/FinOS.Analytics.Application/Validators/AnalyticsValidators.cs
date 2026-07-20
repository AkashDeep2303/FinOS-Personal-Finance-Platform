using FinOS.Analytics.Application.Commands;
using FluentValidation;

namespace FinOS.Analytics.Application.Validators;

public class CalculateNetWorthCommandValidator : AbstractValidator<CalculateNetWorthCommand>
{
    public CalculateNetWorthCommandValidator()
    {
        RuleFor(x => x.Dto.UserId).GreaterThan(0);
        RuleFor(x => x.Dto.CashAndBank).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.InvestmentValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.RealEstateValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.GoldValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.OtherAssets).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.LoanOutstanding).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.CreditCardOutstanding).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.OtherLiabilities).GreaterThanOrEqualTo(0);
    }
}

public class CalculateFinancialScoreCommandValidator : AbstractValidator<CalculateFinancialScoreCommand>
{
    public CalculateFinancialScoreCommandValidator()
    {
        RuleFor(x => x.Dto.UserId).GreaterThan(0);
        RuleFor(x => x.Dto.MonthlyIncome).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.MonthlyExpenses).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.TotalDebt).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.TotalInvestments).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.EmergencyFundBalance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.AverageGoalProgressPct).InclusiveBetween(0, 100);
    }
}

public class GenerateMonthlyAggregatesCommandValidator : AbstractValidator<GenerateMonthlyAggregatesCommand>
{
    public GenerateMonthlyAggregatesCommandValidator()
    {
        RuleFor(x => x.Dto.UserId).GreaterThan(0);
        RuleFor(x => x.Dto.YearMonth).InclusiveBetween(200001, 210012);
    }
}
