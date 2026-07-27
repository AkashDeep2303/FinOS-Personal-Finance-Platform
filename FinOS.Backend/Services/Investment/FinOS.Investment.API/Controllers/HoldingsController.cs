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
public class HoldingsController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHoldingRepository _holdingRepository;
    private readonly IPortfolioRepository _portfolioRepository;

    public HoldingsController(
        IMediator mediator,
        IHoldingRepository holdingRepository,
        IPortfolioRepository portfolioRepository)
    {
        _mediator = mediator;
        _holdingRepository = holdingRepository;
        _portfolioRepository = portfolioRepository;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<HoldingDto>>> GetById(long id)
    {
        if (!await OwnsHolding(id)) return NotFound(ApiResponse<HoldingDto>.Fail("Holding not found"));
        var result = await _mediator.Send(new GetHoldingDetailsQuery(id));
        return Ok(ApiResponse<HoldingDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<HoldingDto>>> Create([FromBody] CreateHoldingRequest request)
    {
        if (!await OwnsPortfolio(request.PortfolioId))
            return NotFound(ApiResponse<HoldingDto>.Fail("Portfolio not found"));
        var result = await _mediator.Send(new CreateHoldingCommand(request));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<HoldingDto>.Ok(result, "Holding created successfully"));
    }

    [HttpPut("{id}/price")]
    public async Task<ActionResult<ApiResponse<HoldingDto>>> UpdatePrice(long id, [FromBody] UpdateHoldingPriceRequest request)
    {
        if (!await OwnsHolding(id)) return NotFound(ApiResponse<HoldingDto>.Fail("Holding not found"));
        var result = await _mediator.Send(new UpdateHoldingPriceCommand(id, request));
        return Ok(ApiResponse<HoldingDto>.Ok(result, "Holding price updated successfully"));
    }

    [HttpPost("transaction")]
    public async Task<ActionResult<ApiResponse<InvestmentTransactionDto>>> RecordTransaction([FromBody] RecordTransactionRequest request)
    {
        if (!await OwnsHolding(request.HoldingId))
            return NotFound(ApiResponse<InvestmentTransactionDto>.Fail("Holding not found"));
        var result = await _mediator.Send(new RecordInvestmentTransactionCommand(request));
        return Ok(ApiResponse<InvestmentTransactionDto>.Ok(result, "Transaction recorded successfully"));
    }

    private async Task<bool> OwnsHolding(long holdingId)
    {
        var holding = await _holdingRepository.GetByIdAsync(holdingId);
        return holding is not null && await OwnsPortfolio(holding.PortfolioId);
    }

    private async Task<bool> OwnsPortfolio(long portfolioId)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId);
        return portfolio is { DeletedAt: null } && portfolio.UserId == AuthenticatedUserId;
    }
}
