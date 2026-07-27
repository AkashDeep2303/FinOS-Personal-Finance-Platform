using FinOS.Loan.Application.Commands;
using FluentValidation;

namespace FinOS.Loan.Application.Validators;

public class LoanRateChangeValidator : AbstractValidator<AddLoanRateChangeCommand>
{
    public LoanRateChangeValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.LoanId).GreaterThan(0);
        RuleFor(x => x.Request.NewRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Request.EffectiveDate).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(1));
        RuleFor(x => x.Request.Reason).MaximumLength(250);
    }
}
