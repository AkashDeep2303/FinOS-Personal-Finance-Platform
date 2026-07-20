using FinOS.Investment.Application.DTOs;
using FluentValidation;

namespace FinOS.Investment.Application.Validators;

public class CreatePortfolioRequestValidator : AbstractValidator<CreatePortfolioRequest>
{
    public CreatePortfolioRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
    }
}

public class CreateHoldingRequestValidator : AbstractValidator<CreateHoldingRequest>
{
    public CreateHoldingRequestValidator()
    {
        RuleFor(x => x.PortfolioId).GreaterThan(0);
        RuleFor(x => x.InvestmentTypeId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.AvgPurchasePrice).GreaterThan(0);
        RuleFor(x => x.CurrentPrice).GreaterThan(0);
    }
}

public class RecordTransactionRequestValidator : AbstractValidator<RecordTransactionRequest>
{
    public RecordTransactionRequestValidator()
    {
        RuleFor(x => x.HoldingId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PricePerUnit).GreaterThan(0);
        RuleFor(x => x.TransactionDate).NotEmpty();
    }
}

public class CreateSIPRequestValidator : AbstractValidator<CreateSIPRequest>
{
    public CreateSIPRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.HoldingId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.DayOfMonth).InclusiveBetween(1, 31);
        RuleFor(x => x.StartDate).NotEmpty();
    }
}

public class CreateEPFAccountRequestValidator : AbstractValidator<CreateEPFAccountRequest>
{
    public CreateEPFAccountRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.MonthlySalary).GreaterThan(0);
        RuleFor(x => x.StartDate).NotEmpty();
    }
}
