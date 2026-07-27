using FinOS.Investment.Application.Commands;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Application.Queries;
using FinOS.Common.Models;
using FinOS.Investment.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfoliosController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPortfolioRepository _portfolioRepository;

    public PortfoliosController(IMediator mediator, IPortfolioRepository portfolioRepository)
    {
        _mediator = mediator;
        _portfolioRepository = portfolioRepository;
    }

    [HttpGet("me")]
    public Task<ActionResult<ApiResponse<List<PortfolioListDto>>>> GetMine() =>
        GetForAuthenticatedUser();

    [Obsolete("Use GET api/portfolios/me.")]
    [HttpGet("user/{userId:long}")]
    public async Task<ActionResult<ApiResponse<List<PortfolioListDto>>>> GetByUser(long userId)
    {
        if (userId != AuthenticatedUserId) return Forbid();
        return await GetForAuthenticatedUser();
    }

    private async Task<ActionResult<ApiResponse<List<PortfolioListDto>>>> GetForAuthenticatedUser()
    {
        var portfolios = await _portfolioRepository.GetByUserIdAsync(AuthenticatedUserId);
        var result = new List<PortfolioListDto>();

        foreach (var portfolio in portfolios.Where(p => p.DeletedAt == null))
        {
            var withHoldings = await _portfolioRepository.GetWithHoldingsAsync(portfolio.Id);
            var holdings = withHoldings?.Holdings?.Where(h => h.IsActive && h.DeletedAt == null).ToList() ?? new();
            var invested = holdings.Sum(h => h.InvestedAmount);
            var current = holdings.Sum(h => h.CurrentValue);

            result.Add(new PortfolioListDto
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                Description = portfolio.Description,
                TotalInvested = invested,
                CurrentValue = current,
                TotalReturnPct = invested > 0 ? Math.Round((current - invested) / invested * 100, 2) : 0,
                HoldingCount = holdings.Count,
                IsDefault = portfolio.IsDefault
            });
        }

        return Ok(ApiResponse<List<PortfolioListDto>>.Ok(result));
    }

    [HttpGet("{id}/summary")]
    public async Task<ActionResult<ApiResponse<PortfolioSummaryDto>>> GetSummary(long id)
    {
        if (!await OwnsPortfolio(id)) return NotFound(ApiResponse<PortfolioSummaryDto>.Fail("Portfolio not found"));
        var result = await _mediator.Send(new GetPortfolioSummaryQuery(id));
        if (result == null) return NotFound(ApiResponse<PortfolioSummaryDto>.Fail("Portfolio not found"));
        return Ok(ApiResponse<PortfolioSummaryDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PortfolioDto>>> Create([FromBody] CreatePortfolioRequest request)
    {
        request.UserId = AuthenticatedUserId;
        var result = await _mediator.Send(new CreatePortfolioCommand(request));
        return CreatedAtAction(nameof(GetSummary), new { id = result.Id }, ApiResponse<PortfolioDto>.Ok(result, "Portfolio created successfully"));
    }

    private async Task<bool> OwnsPortfolio(long portfolioId)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId);
        return portfolio is { DeletedAt: null } && portfolio.UserId == AuthenticatedUserId;
    }
}
