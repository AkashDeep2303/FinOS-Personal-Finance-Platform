using FinOS.Analytics.Domain.Enums;

namespace FinOS.Analytics.Application.Services;

public interface IScoreCalculationService
{
    int CalculateSavingsRateScore(decimal savingsRatePct);
    int CalculateDebtToIncomeScore(decimal debtToIncomeRatio);
    int CalculateEmergencyFundScore(decimal emergencyFundMonths);
    int CalculateInvestmentScore(decimal investmentToIncomeRatio);
    int CalculateGoalProgressScore(decimal avgGoalProgressPct);
    int CalculateOverallScore(int savingsRateScore, int debtToIncomeScore, int emergencyFundScore, int investmentScore, int goalProgressScore);
    ScoreGrade DetermineGrade(int overallScore);
    List<string> GenerateRecommendations(decimal savingsRatePct, decimal debtToIncomeRatio, decimal emergencyFundMonths, decimal investmentToIncomeRatio, decimal avgGoalProgressPct);
}

public class ScoreCalculationService : IScoreCalculationService
{
    // Savings rate: 20%+ = 200, linearly scaled
    public int CalculateSavingsRateScore(decimal savingsRatePct)
    {
        if (savingsRatePct >= 20m) return 200;
        if (savingsRatePct <= 0) return 0;
        return (int)Math.Round(savingsRatePct / 20m * 200);
    }

    // Debt-to-income: <30% = 200, >60% = 0
    public int CalculateDebtToIncomeScore(decimal debtToIncomeRatio)
    {
        if (debtToIncomeRatio <= 0.3m) return 200;
        if (debtToIncomeRatio >= 0.6m) return 0;
        return (int)Math.Round((0.6m - debtToIncomeRatio) / 0.3m * 200);
    }

    // Emergency fund: 6+ months = 200
    public int CalculateEmergencyFundScore(decimal emergencyFundMonths)
    {
        if (emergencyFundMonths >= 6m) return 200;
        if (emergencyFundMonths <= 0) return 0;
        return (int)Math.Round(emergencyFundMonths / 6m * 200);
    }

    // Investment-to-income: 30%+ = 200
    public int CalculateInvestmentScore(decimal investmentToIncomeRatio)
    {
        if (investmentToIncomeRatio >= 0.3m) return 200;
        if (investmentToIncomeRatio <= 0) return 0;
        return (int)Math.Round(investmentToIncomeRatio / 0.3m * 200);
    }

    // Goal progress: based on average goal progress
    public int CalculateGoalProgressScore(decimal avgGoalProgressPct)
    {
        if (avgGoalProgressPct >= 100m) return 200;
        if (avgGoalProgressPct <= 0) return 0;
        return (int)Math.Round(avgGoalProgressPct / 100m * 200);
    }

    // Total: 5 sub-scores each 0-200 = 0-1000
    public int CalculateOverallScore(int savingsRateScore, int debtToIncomeScore, int emergencyFundScore, int investmentScore, int goalProgressScore)
        => savingsRateScore + debtToIncomeScore + emergencyFundScore + investmentScore + goalProgressScore;

    // Grade mapping: 900+=A+, 800+=A, 700+=B, 600+=C, 400+=D, else E
    public ScoreGrade DetermineGrade(int overallScore) => overallScore switch
    {
        >= 900 => ScoreGrade.APlus,
        >= 800 => ScoreGrade.A,
        >= 700 => ScoreGrade.B,
        >= 600 => ScoreGrade.C,
        >= 400 => ScoreGrade.D,
        _ => ScoreGrade.E
    };

    public List<string> GenerateRecommendations(decimal savingsRatePct, decimal debtToIncomeRatio, decimal emergencyFundMonths, decimal investmentToIncomeRatio, decimal avgGoalProgressPct)
    {
        var recommendations = new List<string>();

        if (savingsRatePct < 20m)
            recommendations.Add($"Your savings rate is {savingsRatePct:F1}%. Aim for at least 20% to build a strong financial cushion.");

        if (debtToIncomeRatio > 0.3m)
            recommendations.Add($"Your debt-to-income ratio is {debtToIncomeRatio:P1}. Try to reduce it below 30% by prioritizing debt repayment.");

        if (emergencyFundMonths < 6m)
            recommendations.Add($"You have {emergencyFundMonths:F1} months of emergency fund. Build it up to at least 6 months of expenses.");

        if (investmentToIncomeRatio < 0.3m)
            recommendations.Add($"Your investment-to-income ratio is {investmentToIncomeRatio:P1}. Consider investing at least 30% of your income for long-term wealth.");

        if (avgGoalProgressPct < 50m)
            recommendations.Add($"Your average goal progress is {avgGoalProgressPct:F1}%. Consider increasing contributions to stay on track.");

        if (!recommendations.Any())
            recommendations.Add("Excellent financial health! Keep up the good work and review your plan periodically.");

        return recommendations;
    }
}
