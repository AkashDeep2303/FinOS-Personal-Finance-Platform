using FinOS.Analytics.Application.Queries;
using FluentValidation;

namespace FinOS.Analytics.Application.Validators;

public class RetirementProjectionValidator : AbstractValidator<CalculateRetirementProjectionQuery>
{
    public RetirementProjectionValidator()
    {
        RuleFor(x => x.Request.CurrentAge).InclusiveBetween(18, 79);
        RuleFor(x => x.Request.RetirementAge).GreaterThan(x => x.Request.CurrentAge).LessThanOrEqualTo(80);
        RuleFor(x => x.Request.LifeExpectancy).GreaterThan(x => x.Request.RetirementAge).LessThanOrEqualTo(120);
        RuleFor(x => x.Request.CurrentRetirementCorpus).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.MonthlyRetirementContribution).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.CurrentMonthlyExpense).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.DesiredRetirementExpense).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.AnnualInflationRate).InclusiveBetween(0, 0.25m);
        RuleFor(x => x.Request.AnnualPreRetirementReturn).InclusiveBetween(0, 0.50m);
        RuleFor(x => x.Request.AnnualPostRetirementReturn).InclusiveBetween(0, 0.30m);
        RuleFor(x => x.Request)
            .Must(x => x.CurrentMonthlyExpense > 0 || x.DesiredRetirementExpense > 0)
            .WithMessage("A current or desired retirement expense is required.");
    }
}
