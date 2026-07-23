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
public class EPFController : ControllerBase
{
    private readonly IMediator _mediator;
    public EPFController(IMediator mediator)=>_mediator=mediator;
    private long UserId=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub")??throw new UnauthorizedAccessException());

    [HttpGet("me")] public async Task<ActionResult<ApiResponse<EPFTrackerDto?>>> Mine()=>Ok(ApiResponse<EPFTrackerDto?>.Ok(await _mediator.Send(new GetEPFTrackerQuery(UserId))));
    [HttpPost] public async Task<ActionResult<ApiResponse<EPFTrackerDto>>> Create(CreateEPFAccountRequest request)=>Ok(ApiResponse<EPFTrackerDto>.Ok(await _mediator.Send(new CreateEPFAccountCommand(UserId,request)),"EPF account created"));
    [HttpPost("{id:long}/contributions")] public async Task<ActionResult<ApiResponse<EPFContributionDto>>> Add(long id,AddEPFContributionRequest request)=>Ok(ApiResponse<EPFContributionDto>.Ok(await _mediator.Send(new AddEPFContributionCommand(UserId,id,request)),"Contribution added"));
    [HttpGet("{id:long}/statement")] public async Task<ActionResult<ApiResponse<List<EPFContributionDto>>>> Statement(long id)=>Ok(ApiResponse<List<EPFContributionDto>>.Ok(await _mediator.Send(new GetEPFStatementQuery(id,UserId))));
    [HttpGet("{id:long}/projection")] public async Task<ActionResult<ApiResponse<EPFProjectionDto>>> Projection(long id,[FromQuery]int? retirementAge,[FromQuery]int? currentAge)=>Ok(ApiResponse<EPFProjectionDto>.Ok(await _mediator.Send(new GetEPFProjectionQuery(id,UserId,retirementAge,currentAge))));
}
