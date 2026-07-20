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
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private long GetUserId() => long.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value ?? "0");

    [HttpGet]
    public async Task<ApiResponse<List<AccountDto>>> GetAccounts([FromQuery] bool activeOnly = true)
    {
        var accounts = await _mediator.Send(new GetAccountsByUserQuery { UserId = GetUserId(), ActiveOnly = activeOnly });
        return ApiResponse<List<AccountDto>>.Ok(accounts);
    }

    [HttpGet("{id:long}")]
    public async Task<ApiResponse<AccountDto>> GetAccount(long id)
    {
        var accounts = await _mediator.Send(new GetAccountsByUserQuery { UserId = GetUserId(), ActiveOnly = false });
        var account = accounts.FirstOrDefault(a => a.Id == id);
        if (account == null)
            return ApiResponse<AccountDto>.Fail("Account not found");
        return ApiResponse<AccountDto>.Ok(account);
    }

    [HttpPost]
    public async Task<ApiResponse<AccountDto>> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var result = await _mediator.Send(new CreateAccountCommand { UserId = GetUserId(), Request = request });
        return ApiResponse<AccountDto>.Ok(result, "Account created successfully");
    }

    [HttpPut("{id:long}")]
    public async Task<ApiResponse<AccountDto>> UpdateAccount(long id, [FromBody] UpdateAccountRequest request)
    {
        var result = await _mediator.Send(new UpdateAccountCommand { UserId = GetUserId(), AccountId = id, Request = request });
        return ApiResponse<AccountDto>.Ok(result, "Account updated successfully");
    }

    [HttpDelete("{id:long}")]
    public async Task<ApiResponse<Unit>> DeleteAccount(long id)
    {
        await _mediator.Send(new DeleteAccountCommand { UserId = GetUserId(), AccountId = id });
        return ApiResponse<Unit>.Ok(Unit.Value, "Account deleted successfully");
    }
}
