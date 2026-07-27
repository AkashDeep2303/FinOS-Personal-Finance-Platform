using FinOS.Investment.Application.Queries;
using FinOS.Investment.Application.Commands;
using FluentValidation;

namespace FinOS.Investment.Application.Validators;

public class AllocationAnalysisValidator : AbstractValidator<AnalyzeAllocationQuery>
{
    public AllocationAnalysisValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Request.PortfolioId).GreaterThan(0);
        RuleFor(x => x.Request.Targets).NotEmpty();
        RuleForEach(x => x.Request.Targets).ChildRules(target =>
        {
            target.RuleFor(x => x.AssetClass).NotEmpty().MaximumLength(30);
            target.RuleFor(x => x.TargetPct).InclusiveBetween(0, 100);
        });
        RuleFor(x => x.Request.Targets)
            .Must(targets => Math.Abs(targets.Sum(x => x.TargetPct) - 100m) < 0.01m)
            .WithMessage("Target allocation percentages must total 100%.");
    }
}

public class SaveTargetAllocationValidator : AbstractValidator<SaveTargetAllocationCommand>
{
    public SaveTargetAllocationValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Request.PortfolioId).GreaterThan(0);
        RuleFor(x => x.Request.Targets).NotEmpty()
            .Must(targets => Math.Abs(targets.Sum(x => x.TargetPct) - 100m) < 0.01m)
            .WithMessage("Target allocation percentages must total 100%.");
        RuleForEach(x => x.Request.Targets).ChildRules(target =>
        {
            target.RuleFor(x => x.AssetClass).NotEmpty().MaximumLength(30);
            target.RuleFor(x => x.TargetPct).InclusiveBetween(0, 100);
        });
    }
}
