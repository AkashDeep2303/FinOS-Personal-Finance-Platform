using FinOS.Budget.Domain.Entities;
using FinOS.Budget.Domain.Enums;
using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Budget.Application.Commands;

public class UpdateBudgetSpentCommand : IRequest<Unit>
{
    public long BudgetId { get; set; }
    public List<CategorySpentUpdate> Updates { get; set; }

    public UpdateBudgetSpentCommand(long budgetId, List<CategorySpentUpdate> updates)
    {
        BudgetId = budgetId;
        Updates = updates;
    }
}

public class CategorySpentUpdate
{
    public long CategoryId { get; set; }
    public decimal SpentAmount { get; set; }
}

public class UpdateBudgetSpentCommandHandler : IRequestHandler<UpdateBudgetSpentCommand, Unit>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IBudgetCategoryRepository _budgetCategoryRepository;

    public UpdateBudgetSpentCommandHandler(
        IBudgetRepository budgetRepository,
        IBudgetCategoryRepository budgetCategoryRepository)
    {
        _budgetRepository = budgetRepository;
        _budgetCategoryRepository = budgetCategoryRepository;
    }

    public async Task<Unit> Handle(UpdateBudgetSpentCommand command, CancellationToken ct)
    {
        var budget = await _budgetRepository.GetWithCategoriesAsync(command.BudgetId, ct);
        if (budget == null) return Unit.Value;

        foreach (var update in command.Updates)
        {
            var category = budget.Categories.FirstOrDefault(c => c.CategoryId == update.CategoryId);
            if (category != null)
            {
                category.SpentAmount = update.SpentAmount;
                await _budgetCategoryRepository.UpdateAsync(category, ct);
            }
        }

        return Unit.Value;
    }
}
