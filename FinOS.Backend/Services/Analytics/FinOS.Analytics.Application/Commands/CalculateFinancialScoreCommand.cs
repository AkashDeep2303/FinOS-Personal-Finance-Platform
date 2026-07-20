using FinOS.Common.Interfaces;
using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Application.Services;
using FinOS.Analytics.Domain.Entities;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace FinOS.Analytics.Application.Commands;

public record CalculateFinancialScoreCommand(CalculateFinancialScoreDto Dto) : IRequest<FinancialScoreDto>;

public class CalculateFinancialScoreCommandHandler : IRequestHandler<CalculateFinancialScoreCommand, FinancialScoreDto>
{
    private readonly IFinancialScoreRepository _scoreRepository;
    private readonly IScoreCalculationService _scoreService;
    private readonly IUnitOfWork _unitOfWork;

    public CalculateFinancialScoreCommandHandler(
        IFinancialScoreRepository scoreRepository,
        IScoreCalculationService scoreService,
        IUnitOfWork unitOfWork)
    {
        _scoreRepository = scoreRepository;
        _scoreService = scoreService;
        _unitOfWork = unitOfWork;
    }

    public async Task<FinancialScoreDto> Handle(CalculateFinancialScoreCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var monthlySavings = dto.MonthlyIncome - dto.MonthlyExpenses;
        var savingsRatePct = dto.MonthlyIncome > 0 ? Math.Round(monthlySavings / dto.MonthlyIncome * 100, 2) : 0;
        var debtToIncomeRatio = dto.MonthlyIncome > 0 ? dto.TotalDebt / (dto.MonthlyIncome * 12) : 0;
        var emergencyFundMonths = dto.MonthlyExpenses > 0 ? dto.EmergencyFundBalance / dto.MonthlyExpenses : 0;
        var investmentToIncomeRatio = dto.MonthlyIncome > 0 ? dto.TotalInvestments / (dto.MonthlyIncome * 12) : 0;

        var savingsRateScore = _scoreService.CalculateSavingsRateScore(savingsRatePct);
        var debtToIncomeScore = _scoreService.CalculateDebtToIncomeScore(debtToIncomeRatio);
        var emergencyFundScore = _scoreService.CalculateEmergencyFundScore(emergencyFundMonths);
        var investmentScore = _scoreService.CalculateInvestmentScore(investmentToIncomeRatio);
        var goalProgressScore = _scoreService.CalculateGoalProgressScore(dto.AverageGoalProgressPct);

        var overallScore = _scoreService.CalculateOverallScore(savingsRateScore, debtToIncomeScore, emergencyFundScore, investmentScore, goalProgressScore);
        var grade = _scoreService.DetermineGrade(overallScore);

        var recommendations = _scoreService.GenerateRecommendations(savingsRatePct, debtToIncomeRatio, emergencyFundMonths, investmentToIncomeRatio, dto.AverageGoalProgressPct);

        var score = new FinancialScore
        {
            UserId = dto.UserId,
            ScoreDate = DateTime.UtcNow,
            OverallScore = overallScore,
            ScoreGrade = grade,
            SavingsRateScore = savingsRateScore,
            DebtToIncomeScore = debtToIncomeScore,
            EmergencyFundScore = emergencyFundScore,
            InvestmentScore = investmentScore,
            GoalProgressScore = goalProgressScore,
            SavingsRatePct = savingsRatePct,
            DebtToIncomeRatio = debtToIncomeRatio,
            EmergencyFundMonths = emergencyFundMonths,
            InvestmentToIncomeRatio = investmentToIncomeRatio,
            MonthlyIncome = dto.MonthlyIncome,
            MonthlyExpenses = dto.MonthlyExpenses,
            MonthlySavings = monthlySavings,
            TotalDebt = dto.TotalDebt,
            TotalInvestments = dto.TotalInvestments,
            Recommendations = JsonSerializer.Serialize(recommendations),
            CreatedAt = DateTime.UtcNow
        };

        await _scoreRepository.AddAsync(score, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new FinancialScoreDto(
            score.Id, score.UserId, score.ScoreDate, score.OverallScore, score.ScoreGrade,
            score.SavingsRateScore, score.DebtToIncomeScore, score.EmergencyFundScore,
            score.InvestmentScore, score.GoalProgressScore, score.SavingsRatePct,
            score.DebtToIncomeRatio, score.EmergencyFundMonths, score.InvestmentToIncomeRatio,
            score.MonthlyIncome, score.MonthlyExpenses, score.MonthlySavings,
            score.TotalDebt, score.TotalInvestments, score.Recommendations, score.CreatedAt
        );
    }
}
