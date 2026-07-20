using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public class CreateCategoryCommand : IRequest<CategoryDto>
{
    public long UserId { get; set; }
    public CreateCategoryRequest Request { get; set; } = new();
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand command, CancellationToken ct)
    {
        if (!Enum.TryParse<CategoryType>(command.Request.Type, true, out var categoryType))
            throw new DomainException("INVALID_TYPE", $"Invalid category type: {command.Request.Type}");

        var category = new Category
        {
            UserId = command.UserId,
            ParentId = command.Request.ParentId,
            Name = command.Request.Name,
            Type = categoryType,
            Icon = command.Request.Icon,
            Color = command.Request.Color,
            BudgetAmount = command.Request.BudgetAmount,
            IsSystem = false,
            IsActive = true,
            SortOrder = command.Request.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _categoryRepository.AddAsync(category, ct);
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
