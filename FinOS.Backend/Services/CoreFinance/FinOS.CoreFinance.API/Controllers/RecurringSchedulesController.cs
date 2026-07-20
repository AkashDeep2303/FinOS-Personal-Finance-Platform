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
public class RecurringSchedulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecurringSchedulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private long GetUserId() => long.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value ?? "0");

    [HttpGet]
    public async Task<ApiResponse<List<RecurringScheduleDto>>> GetRecurringSchedules()
    {
        var result = await _mediator.Send(new GetRecurringSchedulesQuery { UserId = GetUserId() });
        return ApiResponse<List<RecurringScheduleDto>>.Ok(result);
    }

    [HttpPost]
    public async Task<ApiResponse<RecurringScheduleDto>> CreateRecurringSchedule([FromBody] CreateRecurringScheduleRequest request)
    {
        var result = await _mediator.Send(new CreateRecurringScheduleCommand { UserId = GetUserId(), Request = request });
        return ApiResponse<RecurringScheduleDto>.Ok(result, "Recurring schedule created successfully");
    }

    [HttpPost("process-due")]
    public async Task<ApiResponse<int>> ProcessDueTransactions()
    {
        var count = await _mediator.Send(new ProcessRecurringTransactionsCommand());
        return ApiResponse<int>.Ok(count, $"{count} recurring transactions processed");
    }
}
