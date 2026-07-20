using FinOS.Common.Models;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Goals.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoalTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public GoalTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ApiResponse<List<GoalTemplateDto>>> GetAllTemplates()
    {
        var templates = await _mediator.Send(new GetGoalTemplatesQuery());
        return ApiResponse<List<GoalTemplateDto>>.Ok(templates);
    }
}
