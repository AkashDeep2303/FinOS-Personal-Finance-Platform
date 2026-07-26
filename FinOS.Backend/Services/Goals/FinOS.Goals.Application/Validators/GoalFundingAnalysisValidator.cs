using FinOS.Goals.Application.Queries;
using FluentValidation;

namespace FinOS.Goals.Application.Validators;

public class GoalFundingAnalysisValidator : AbstractValidator<GetGoalFundingAnalysisQuery>
{
    public GoalFundingAnalysisValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.AvailableMonthlySurplus).GreaterThanOrEqualTo(0);
    }
}
