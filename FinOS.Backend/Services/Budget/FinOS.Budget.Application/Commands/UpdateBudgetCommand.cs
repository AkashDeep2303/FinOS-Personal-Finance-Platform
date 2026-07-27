using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Domain.Entities;
using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using MediatR;
using FinOS.Budget.Application.Services;

namespace FinOS.Budget.Application.Commands;

public class UpdateBudgetCommand : IRequest<BudgetDto>
{
    public long UserId { get; set; }
    public long BudgetId { get; set; }
    public UpdateBudgetRequest Request { get; set; }

    public UpdateBudgetCommand(long userId, long budgetId, UpdateBudgetRequest request)
    {
        UserId = userId;
        BudgetId = budgetId;
        Request = request;
    }
}

public class UpdateBudgetCommandHandler : IRequestHandler<UpdateBudgetCommand, BudgetDto>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBudgetCommandHandler(
        IBudgetRepository budgetRepository,
        IUnitOfWork unitOfWork)
    {
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BudgetDto> Handle(UpdateBudgetCommand command, CancellationToken ct)
    {
        var budget = await BudgetOwnership.GetOwnedAsync(
            _budgetRepository, command.BudgetId, command.UserId, ct, includeCategories: true);

        var req = command.Request;
        if (req.Name is not null) budget.Name = req.Name;
        if (req.PeriodType.HasValue) budget.PeriodType = req.PeriodType.Value;
        if (req.StartDate.HasValue) budget.StartDate = req.StartDate.Value;
        if (req.EndDate.HasValue) budget.EndDate = req.EndDate.Value;
        if (req.TotalBudgetAmount.HasValue) budget.TotalBudgetAmount = req.TotalBudgetAmount.Value;
        if (req.Currency is not null) budget.Currency = req.Currency;
        if (req.RolloverEnabled.HasValue) budget.RolloverEnabled = req.RolloverEnabled.Value;
        if (req.AlertThresholdPct.HasValue) budget.AlertThresholdPct = req.AlertThresholdPct.Value;
        if (req.IsActive.HasValue) budget.IsActive = req.IsActive.Value;

        // Update budget header
        await _budgetRepository.UpdateAsync(budget, ct);

        // Replace categories if provided
        if (req.Categories is not null)
        {
            var newCategories = req.Categories.Select(catReq => new Domain.Entities.BudgetCategory
            {
                BudgetId = budget.Id,
                CategoryId = catReq.CategoryId,
                CustomLabel = catReq.CustomLabel,
                AllocatedAmount = catReq.AllocatedAmount,
                SpentAmount = 0,
                AlertThresholdPct = catReq.AlertThresholdPct,
                SortOrder = catReq.SortOrder
            }).ToList();

            await _budgetRepository.ReplaceCategoriesAsync(budget.Id, command.UserId, newCategories, ct);

            // Reload budget with new categories
            budget = await _budgetRepository.GetWithCategoriesAsync(command.BudgetId, ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.Budget), command.BudgetId);
        }

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
            SpentPercentage = budget.TotalBudgetAmount > 0 ? Math.Round(totalSpent / budget.TotalBudgetAmount * 100, 2) : 0,
            Currency = budget.Currency,
            RolloverEnabled = budget.RolloverEnabled,
            AlertThresholdPct = budget.AlertThresholdPct,
            IsTemplate = budget.IsTemplate,
            IsActive = budget.IsActive,
            CreatedAt = budget.CreatedAt,
            Categories = budget.Categories?.Select(c => new BudgetCategoryDto
            {
                Id = c.Id, BudgetId = c.BudgetId, CategoryId = c.CategoryId,
                CustomLabel = c.CustomLabel, AllocatedAmount = c.AllocatedAmount,
                SpentAmount = c.SpentAmount, AlertThresholdPct = c.AlertThresholdPct, SortOrder = c.SortOrder
            }).ToList() ?? new List<BudgetCategoryDto>()
        };
    }
}
