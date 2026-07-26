namespace FinOS.Analytics.Application.DTOs;

public record RetirementProjectionRequest(
    int CurrentAge,
    int RetirementAge,
    int LifeExpectancy,
    decimal CurrentRetirementCorpus,
    decimal MonthlyRetirementContribution,
    decimal CurrentMonthlyExpense,
    decimal DesiredRetirementExpense,
    decimal AnnualInflationRate,
    decimal AnnualPreRetirementReturn,
    decimal AnnualPostRetirementReturn);

public record RetirementProjectionDto(
    int YearsToRetirement,
    int RetirementYears,
    decimal FirstMonthRetirementExpense,
    decimal TargetRetirementCorpus,
    decimal ProjectedRetirementCorpus,
    decimal RetirementGap,
    decimal RequiredMonthlyContribution,
    int RetirementReadinessScore,
    string Status,
    IReadOnlyList<string> Assumptions);
