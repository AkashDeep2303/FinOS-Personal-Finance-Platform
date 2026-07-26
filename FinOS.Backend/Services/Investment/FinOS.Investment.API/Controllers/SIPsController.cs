using System.Security.Claims;
using FinOS.Common.Models;
using FinOS.Investment.Application.Commands;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Investment.API.Controllers;

[ApiController,Route("api/[controller]"),Authorize]
public class SIPsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SIPsController(IMediator mediator)=>_mediator=mediator;
    private long UserId=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub")??throw new UnauthorizedAccessException());

    [HttpGet("me")] public async Task<ActionResult<ApiResponse<List<SIPDto>>>> GetMine([FromQuery]bool? isActive)=>Ok(ApiResponse<List<SIPDto>>.Ok(await _mediator.Send(new GetSIPListQuery(UserId,isActive))));
    [HttpPost] public async Task<ActionResult<ApiResponse<SIPDto>>> Create(CreateSIPRequest request)=>Ok(ApiResponse<SIPDto>.Ok(await _mediator.Send(new CreateSIPCommand(UserId,request)),"SIP created"));
    [HttpPut("{id:long}")] public async Task<ActionResult<ApiResponse<SIPDto>>> Update(long id,UpdateSIPRequest request)=>Ok(ApiResponse<SIPDto>.Ok(await _mediator.Send(new UpdateSIPCommand(UserId,id,request)),"SIP updated"));
    [HttpPatch("{id:long}/status")] public async Task<ActionResult<ApiResponse<object>>> Status(long id,ChangeSIPStatusRequest request){await _mediator.Send(new ChangeSIPStatusCommand(UserId,id,request.IsActive));return Ok(ApiResponse<object>.Ok(new{},"SIP status updated"));}
    [HttpDelete("{id:long}")] public async Task<ActionResult<ApiResponse<object>>> Delete(long id){await _mediator.Send(new DeleteSIPCommand(UserId,id));return Ok(ApiResponse<object>.Ok(new{},"SIP deleted"));}
    [HttpPost("process"), Authorize(Roles = "Admin,SuperAdmin")] public async Task<ActionResult<ApiResponse<int>>> Process(){var count=await _mediator.Send(new ProcessSIPInstallmentsCommand());return Ok(ApiResponse<int>.Ok(count));}
}
