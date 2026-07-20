using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public class GetCategoriesByUserQuery : IRequest<List<CategoryDto>>
{
    public long UserId { get; set; }
}

public class GetCategoriesByUserQueryHandler : IRequestHandler<GetCategoriesByUserQuery, List<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesByUserQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesByUserQuery query, CancellationToken ct)
    {
        var categories = await _categoryRepository.GetByUserIdAsync(query.UserId, ct);
        var allDtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            UserId = c.UserId,
            ParentId = c.ParentId,
            Name = c.Name,
            Type = c.Type.ToString(),
            Icon = c.Icon,
            Color = c.Color,
            BudgetAmount = c.BudgetAmount,
            IsSystem = c.IsSystem,
            IsActive = c.IsActive,
            SortOrder = c.SortOrder,
            CreatedAt = c.CreatedAt
        }).ToList();

        // Build tree structure
        var lookup = allDtos.ToDictionary(c => c.Id);
        var roots = new List<CategoryDto>();

        foreach (var dto in allDtos)
        {
            if (dto.ParentId.HasValue && lookup.TryGetValue(dto.ParentId.Value, out var parent))
            {
                parent.Children.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        return roots;
    }
}
