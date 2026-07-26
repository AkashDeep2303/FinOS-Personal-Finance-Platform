using FinOS.Common.Models;
using FinOS.CoreFinance.Application.Commands;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Application.Queries;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.CoreFinance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICategoryRepository _categoryRepository;

    public CategoriesController(IMediator mediator, ICategoryRepository categoryRepository)
    {
        _mediator = mediator;
        _categoryRepository = categoryRepository;
    }

    private long GetUserId() => long.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value ?? "0");

    [HttpGet]
    public async Task<ApiResponse<List<CategoryDto>>> GetCategories()
    {
        var categories = await _mediator.Send(new GetCategoriesByUserQuery { UserId = GetUserId() });
        return ApiResponse<List<CategoryDto>>.Ok(categories);
    }

    [HttpGet("type/{type}")]
    public async Task<ApiResponse<List<CategoryDto>>> GetCategoriesByType(string type)
    {
        var allCategories = await _mediator.Send(new GetCategoriesByUserQuery { UserId = GetUserId() });
        var filtered = allCategories.Where(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        return ApiResponse<List<CategoryDto>>.Ok(filtered);
    }

    [HttpGet("system")]
    [AllowAnonymous]
    public async Task<ApiResponse<List<CategoryDto>>> GetSystemCategories()
    {
        var systemCategories = await _categoryRepository.GetSystemCategoriesAsync();
        var dtos = systemCategories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Type = c.Type.ToString(),
            Icon = c.Icon,
            Color = c.Color,
            IsSystem = c.IsSystem,
            SortOrder = c.SortOrder,
            CashFlowClassification = c.CashFlowClassification
        }).ToList();
        return ApiResponse<List<CategoryDto>>.Ok(dtos);
    }

    [HttpPost]
    public async Task<ApiResponse<CategoryDto>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var result = await _mediator.Send(new CreateCategoryCommand { UserId = GetUserId(), Request = request });
        return ApiResponse<CategoryDto>.Ok(result, "Category created successfully");
    }

    [HttpPut("{id:long}")]
    public async Task<ApiResponse<CategoryDto>> UpdateCategory(long id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand { UserId = GetUserId(), CategoryId = id, Request = request });
        return ApiResponse<CategoryDto>.Ok(result, "Category updated successfully");
    }
}
