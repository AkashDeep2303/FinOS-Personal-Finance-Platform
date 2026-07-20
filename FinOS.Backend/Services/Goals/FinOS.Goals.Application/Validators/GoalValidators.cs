using FinOS.Goals.Application.Commands;
using FluentValidation;

namespace FinOS.Goals.Application.Validators;

public class CreateGoalCommandValidator : AbstractValidator<CreateGoalCommand>
{
    public CreateGoalCommandValidator()
    {
        RuleFor(x => x.Dto.UserId).GreaterThan(0).WithMessage("UserId is required");
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200).WithMessage("Name is required and must be under 200 characters");
        RuleFor(x => x.Dto.Category).NotEmpty().WithMessage("Category is required");
        RuleFor(x => x.Dto.TargetAmount).GreaterThan(0).WithMessage("Target amount must be greater than 0");
        RuleFor(x => x.Dto.MonthlyContribution).GreaterThanOrEqualTo(0).WithMessage("Monthly contribution cannot be negative");
        RuleFor(x => x.Dto.StartDate).NotEmpty().WithMessage("Start date is required");
    }
}

public class UpdateGoalCommandValidator : AbstractValidator<UpdateGoalCommand>
{
    public UpdateGoalCommandValidator()
    {
        RuleFor(x => x.Dto.Id).GreaterThan(0).WithMessage("Goal Id is required");
        When(x => x.Dto.TargetAmount.HasValue, () =>
        {
            RuleFor(x => x.Dto.TargetAmount!.Value).GreaterThan(0).WithMessage("Target amount must be greater than 0");
        });
        When(x => x.Dto.MonthlyContribution.HasValue, () =>
        {
            RuleFor(x => x.Dto.MonthlyContribution!.Value).GreaterThanOrEqualTo(0).WithMessage("Monthly contribution cannot be negative");
        });
    }
}

public class AddGoalContributionCommandValidator : AbstractValidator<AddGoalContributionCommand>
{
    public AddGoalContributionCommandValidator()
    {
        RuleFor(x => x.Dto.GoalId).GreaterThan(0).WithMessage("GoalId is required");
        RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("Contribution amount must be greater than 0");
        RuleFor(x => x.Dto.ContributionDate).NotEmpty().WithMessage("Contribution date is required");
    }
}

public class PauseGoalCommandValidator : AbstractValidator<PauseGoalCommand>
{
    public PauseGoalCommandValidator()
    {
        RuleFor(x => x.GoalId).GreaterThan(0).WithMessage("GoalId is required");
    }
}

public class ResumeGoalCommandValidator : AbstractValidator<ResumeGoalCommand>
{
    public ResumeGoalCommandValidator()
    {
        RuleFor(x => x.GoalId).GreaterThan(0).WithMessage("GoalId is required");
    }
}
