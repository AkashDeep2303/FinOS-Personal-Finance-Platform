using FinOS.Analytics.Application.DTOs;using FinOS.Analytics.Application.Queries;using FinOS.Common.Models;using MediatR;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace FinOS.Analytics.API.Controllers;
[ApiController,Route("api/reports"),Authorize]public class ReportsController(IMediator m):AuthenticatedControllerBase{[HttpGet("financial-year-review")]public async Task<ApiResponse<FinancialYearReviewDto>>Review([FromQuery]int startYear)=>ApiResponse<FinancialYearReviewDto>.Ok(await m.Send(new GetFinancialYearReviewQuery(AuthenticatedUserId,startYear)));}
