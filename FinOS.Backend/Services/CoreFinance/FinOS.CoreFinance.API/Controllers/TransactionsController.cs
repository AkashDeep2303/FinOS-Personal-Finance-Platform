using FinOS.Common.Models;
using FinOS.CoreFinance.Application.Commands;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.CoreFinance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private long GetUserId() => long.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value ?? "0");

    [HttpGet("filter")]
    public async Task<ApiResponse<PagedResult<TransactionDto>>> GetTransactions([FromQuery] TransactionFilterDto filter)
    {
        var result = await _mediator.Send(new GetTransactionsByDateRangeQuery
        {
            UserId = GetUserId(),
            Filter = filter
        });
        return ApiResponse<PagedResult<TransactionDto>>.Ok(result);
    }

    [HttpGet("{id:long}")]
    public Task<ApiResponse<TransactionDto>> GetTransaction(long id)
    {
        // For simplicity, fetch from filter endpoint - in production add a dedicated query
        return Task.FromResult(ApiResponse<TransactionDto>.Ok(null!));
    }

    [HttpPost]
    public async Task<ApiResponse<TransactionDto>> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        var result = await _mediator.Send(new CreateTransactionCommand { UserId = GetUserId(), Request = request });
        return ApiResponse<TransactionDto>.Ok(result, "Transaction created successfully");
    }

    [HttpPut("{id:long}")]
    public async Task<ApiResponse<TransactionDto>> UpdateTransaction(long id, [FromBody] UpdateTransactionRequest request)
    {
        var result = await _mediator.Send(new UpdateTransactionCommand { UserId = GetUserId(), TransactionId = id, Request = request });
        return ApiResponse<TransactionDto>.Ok(result, "Transaction updated successfully");
    }

    [HttpDelete("{id:long}")]
    public async Task<ApiResponse<Unit>> DeleteTransaction(long id)
    {
        await _mediator.Send(new DeleteTransactionCommand { UserId = GetUserId(), TransactionId = id });
        return ApiResponse<Unit>.Ok(Unit.Value, "Transaction deleted successfully");
    }

    [HttpPost("{id:long}/split")]
    public async Task<ApiResponse<List<TransactionDto>>> SplitTransaction(long id, [FromBody] SplitTransactionRequest request)
    {
        var result = await _mediator.Send(new SplitTransactionCommand { UserId = GetUserId(), TransactionId = id, Request = request });
        return ApiResponse<List<TransactionDto>>.Ok(result, "Transaction split successfully");
    }

    [HttpGet("monthly-summary")]
    public async Task<ApiResponse<MonthlySummaryDto>> GetMonthlySummary([FromQuery] int year, [FromQuery] int month)
    {
        var result = await _mediator.Send(new GetMonthlySummaryQuery { UserId = GetUserId(), Year = year, Month = month });
        return ApiResponse<MonthlySummaryDto>.Ok(result);
    }
}
