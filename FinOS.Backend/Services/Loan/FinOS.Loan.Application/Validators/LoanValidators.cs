using FinOS.Loan.Application.DTOs;
using FluentValidation;

namespace FinOS.Loan.Application.Validators;

public class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.LoanTypeId).GreaterThan(0);
        RuleFor(x => x.LenderName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrincipalAmount).GreaterThan(0);
        RuleFor(x => x.InterestRate).GreaterThan(0);
        RuleFor(x => x.TenureMonths).GreaterThan(0);
        RuleFor(x => x.EMIDayOfMonth).InclusiveBetween(1, 31);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
    }
}

public class RecordEMIPaymentRequestValidator : AbstractValidator<RecordEMIPaymentRequest>
{
    public RecordEMIPaymentRequestValidator()
    {
        RuleFor(x => x.LoanId).GreaterThan(0);
        RuleFor(x => x.EMINumber).GreaterThan(0);
        RuleFor(x => x.PaidAmount).GreaterThan(0);
        RuleFor(x => x.PaidDate).NotEmpty();
    }
}

public class SimulatePrepaymentRequestValidator : AbstractValidator<SimulatePrepaymentRequest>
{
    public SimulatePrepaymentRequestValidator()
    {
        RuleFor(x => x.LoanId).GreaterThan(0);
        RuleFor(x => x.PrepaymentAmount).GreaterThan(0);
        RuleFor(x => x.PrepaymentDate).NotEmpty();
    }
}

public class ExecutePrepaymentRequestValidator : AbstractValidator<ExecutePrepaymentRequest>
{
    public ExecutePrepaymentRequestValidator()
    {
        RuleFor(x => x.LoanId).GreaterThan(0);
        RuleFor(x => x.PrepaymentAmount).GreaterThan(0);
        RuleFor(x => x.PrepaymentDate).NotEmpty();
    }
}
