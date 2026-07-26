using FinOS.Loan.Application.Queries;
using FluentValidation;

namespace FinOS.Loan.Application.Validators;

public class LoanStrategyValidator : AbstractValidator<CompareLoanStrategyQuery>
{
    public LoanStrategyValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Request.LoanId).GreaterThan(0);
        RuleFor(x => x.Request.SurplusAmount).GreaterThan(0);
        RuleFor(x => x.Request.SplitPrepaymentAmount)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(x => x.Request.SurplusAmount);
        RuleFor(x => x.Request.ExpectedAnnualInvestmentReturn).InclusiveBetween(0, 0.50m);
        RuleFor(x => x.Request.InvestmentHorizonMonths).InclusiveBetween(1, 600);
    }
}
