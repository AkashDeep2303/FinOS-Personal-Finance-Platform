using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public class UpdateCategoryCommand : IRequest<CategoryDto>
{
    public long UserId { get; set; }
    public long CategoryId { get; set; }
    public UpdateCategoryRequest Request { get; set; } = new();
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand command, CancellationToken ct)
    {
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, ct);
        if (category == null || category.UserId != command.UserId)
            throw new NotFoundException("Category", command.CategoryId);

        if (category.IsSystem)
            throw new DomainException("SYSTEM_CATEGORY", "Cannot modify a system category.");

        category.Name = command.Request.Name;
        category.Icon = command.Request.Icon;
        category.Color = command.Request.Color;
        category.BudgetAmount = command.Request.BudgetAmount;
        category.IsActive = command.Request.IsActive;
        category.SortOrder = command.Request.SortOrder;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync(ct);

        return new CategoryDto
        {
            Id = category.Id,
            UserId = category.UserId,
            ParentId = category.ParentId,
            Name = category.Name,
            Type = category.Type.ToString(),
            Icon = category.Icon,
            Color = category.Color,
            BudgetAmount = category.BudgetAmount,
            IsSystem = category.IsSystem,
            IsActive = category.IsActive,
            SortOrder = category.SortOrder,
            CreatedAt = category.CreatedAt
        };
    }
}
