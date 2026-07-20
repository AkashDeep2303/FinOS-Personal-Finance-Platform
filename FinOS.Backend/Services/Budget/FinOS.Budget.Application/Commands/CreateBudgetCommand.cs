using System.Text.Json;
using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Domain.Entities;
using FinOS.Budget.Domain.Enums;
using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Budget.Application.Commands;

public class CreateBudgetCommand : IRequest<BudgetDto>
{
    public CreateBudgetRequest Request { get; set; }

    public CreateBudgetCommand(CreateBudgetRequest request)
    {
        Request = request;
    }
}

public class CreateBudgetCommandHandler : IRequestHandler<CreateBudgetCommand, BudgetDto>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBudgetCommandHandler(IBudgetRepository budgetRepository, IUnitOfWork unitOfWork)
    {
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BudgetDto> Handle(CreateBudgetCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var budget = new Domain.Entities.Budget
        {
            UserId = req.UserId,
            Name = req.Name,
            PeriodType = req.PeriodType,
            StartDate = req.StartDate,
            EndDate = req.EndDate ?? CalculateEndDate(req.StartDate, req.PeriodType),
            TotalBudgetAmount = req.TotalBudgetAmount,
            Currency = req.Currency,
            RolloverEnabled = req.RolloverEnabled,
            AlertThresholdPct = req.AlertThresholdPct,
            IsTemplate = req.IsTemplate,
            IsActive = true
        };

        // Build categories JSON for the stored procedure
        var categoryDtos = req.Categories.Select(c => new
        {
            CategoryId = c.CategoryId,
            CustomLabel = c.CustomLabel,
            AllocatedAmount = c.AllocatedAmount,
            AlertThresholdPct = c.AlertThresholdPct
        }).ToList();
        var categoriesJson = JsonSerializer.Serialize(categoryDtos);

        await _budgetRepository.CreateAsync(budget, categoriesJson, ct);

        return MapToDto(budget);
    }

    private static DateTime CalculateEndDate(DateTime startDate, PeriodType periodType) => periodType switch
    {
        PeriodType.Weekly => startDate.AddDays(7),
        PeriodType.Monthly => startDate.AddMonths(1).AddDays(-1),
        PeriodType.Quarterly => startDate.AddMonths(3).AddDays(-1),
        PeriodType.Yearly => startDate.AddYears(1).AddDays(-1),
        _ => startDate.AddMonths(1).AddDays(-1)
    };

    private static BudgetDto MapToDto(Domain.Entities.Budget budget)
    {
        var totalSpent = budget.Categories?.Sum(c => c.SpentAmount) ?? 0;
        return new BudgetDto
        {
            Id = budget.Id,
            UserId = budget.UserId,
            Name = budget.Name,
            PeriodType = budget.PeriodType,
            PeriodTypeDisplay = budget.PeriodType.ToString(),
            StartDate = budget.StartDate,
            EndDate = budget.EndDate,
            TotalBudgetAmount = budget.TotalBudgetAmount,
            TotalSpentAmount = totalSpent,
            RemainingAmount = budget.TotalBudgetAmount - totalSpent,
            SpentPercentage = budget.TotalBudgetAmount > 0
                ? Math.Round(totalSpent / budget.TotalBudgetAmount * 100, 2)
                : 0,
            Currency = budget.Currency,
            RolloverEnabled = budget.RolloverEnabled,
            AlertThresholdPct = budget.AlertThresholdPct,
            IsTemplate = budget.IsTemplate,
            IsActive = budget.IsActive,
            CreatedAt = budget.CreatedAt,
            Categories = budget.Categories?.Select(c => new BudgetCategoryDto
            {
                Id = c.Id,
                BudgetId = c.BudgetId,
                CategoryId = c.CategoryId,
                CustomLabel = c.CustomLabel,
                AllocatedAmount = c.AllocatedAmount,
                SpentAmount = c.SpentAmount,
                AlertThresholdPct = c.AlertThresholdPct,
                SortOrder = c.SortOrder
            }).ToList() ?? new List<BudgetCategoryDto>()
        };
    }
}
