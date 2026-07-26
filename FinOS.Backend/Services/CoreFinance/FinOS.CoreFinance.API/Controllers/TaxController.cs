using FinOS.Common.Models;
using FinOS.CoreFinance.Application.Commands;
using FinOS.CoreFinance.Application.Queries;
using FinOS.CoreFinance.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FinOS.CoreFinance.API.Controllers;
[ApiController,Route("api/tax"),Authorize]
public class TaxController(IMediator mediator):ControllerBase
{
    long UserId=>long.Parse(User.FindFirst("sub")?.Value??User.FindFirst("userId")?.Value??"0");
    [HttpGet("profiles/{financialYear}")] public async Task<ApiResponse<TaxProfile?>> Profile(string financialYear)=>ApiResponse<TaxProfile?>.Ok(await mediator.Send(new GetTaxProfileQuery(UserId,financialYear)));
    [HttpPut("profiles/{financialYear}")] public async Task<ApiResponse<TaxProfile>> Save(string financialYear,SaveTaxProfileRequest r)=>ApiResponse<TaxProfile>.Ok(await mediator.Send(new SaveTaxProfileCommand(UserId,financialYear,r.PreferredRegime,r.InputJson)));
    [HttpGet("rules/{financialYear}")] public async Task<ApiResponse<IReadOnlyList<object>>> Rules(string financialYear)=>ApiResponse<IReadOnlyList<object>>.Ok(await mediator.Send(new GetTaxRulesQuery(financialYear)));
    [HttpPost("projections/{financialYear}/calculate")]
    public async Task<ApiResponse<FinOS.CoreFinance.Application.DTOs.TaxRegimeComparisonDto>> Calculate(
        string financialYear, CancellationToken cancellationToken) =>
        ApiResponse<FinOS.CoreFinance.Application.DTOs.TaxRegimeComparisonDto>.Ok(
            await mediator.Send(new CalculateTaxComparisonCommand(UserId, financialYear), cancellationToken));
    [HttpPost("admin/rules")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ApiResponse<TaxRuleVersion>> CreateRule(
        CreateTaxRuleVersionRequest request,
        CancellationToken cancellationToken) =>
        ApiResponse<TaxRuleVersion>.Ok(await mediator.Send(
            new CreateTaxRuleVersionCommand(
                request.FinancialYear, request.AssessmentYear, request.Regime,
                request.Version, request.ConfigurationJson,
                request.EffectiveFrom, request.EffectiveTo), cancellationToken));

    [HttpPost("admin/rules/{id:long}/publish")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ApiResponse<TaxRuleVersion>> PublishRule(
        long id,
        CancellationToken cancellationToken) =>
        ApiResponse<TaxRuleVersion>.Ok(await mediator.Send(
            new PublishTaxRuleVersionCommand(id), cancellationToken));
}
public record SaveTaxProfileRequest(string? PreferredRegime,string InputJson);
public record CreateTaxRuleVersionRequest(
    string FinancialYear,
    string AssessmentYear,
    string Regime,
    string Version,
    string ConfigurationJson,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);
