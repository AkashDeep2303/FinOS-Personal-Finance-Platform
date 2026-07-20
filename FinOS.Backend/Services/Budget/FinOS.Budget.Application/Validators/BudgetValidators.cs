using FinOS.Budget.Application.DTOs;
using FluentValidation;

namespace FinOS.Budget.Application.Validators;

public class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.TotalBudgetAmount).GreaterThan(0);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
        RuleFor(x => x.AlertThresholdPct).InclusiveBetween(1, 100);
        RuleForEach(x => x.Categories).SetValidator(new CreateBudgetCategoryRequestValidator());
    }
}

public class CreateBudgetCategoryRequestValidator : AbstractValidator<CreateBudgetCategoryRequest>
{
    public CreateBudgetCategoryRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.AllocatedAmount).GreaterThan(0);
        RuleFor(x => x.AlertThresholdPct).InclusiveBetween(1, 100);
    }
}

public class UpdateBudgetRequestValidator : AbstractValidator<UpdateBudgetRequest>
{
    public UpdateBudgetRequestValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name!).NotEmpty().MaximumLength(200));
        When(x => x.TotalBudgetAmount.HasValue, () => RuleFor(x => x.TotalBudgetAmount!.Value).GreaterThan(0));
        When(x => x.Currency is not null, () => RuleFor(x => x.Currency!).NotEmpty().MaximumLength(3));
        When(x => x.AlertThresholdPct.HasValue, () => RuleFor(x => x.AlertThresholdPct!.Value).InclusiveBetween(1, 100));
    }
}

public class CreateSavingsRuleRequestValidator : AbstractValidator<CreateSavingsRuleRequest>
{
    public CreateSavingsRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public class UpdateSavingsRuleRequestValidator : AbstractValidator<UpdateSavingsRuleRequest>
{
    public UpdateSavingsRuleRequestValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name!).NotEmpty().MaximumLength(200));
    }
}
