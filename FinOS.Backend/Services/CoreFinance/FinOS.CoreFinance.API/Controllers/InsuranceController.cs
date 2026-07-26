using FinOS.Common.Models; using FinOS.CoreFinance.Application.Commands; using FinOS.CoreFinance.Application.Queries; using FinOS.CoreFinance.Domain.Entities; using MediatR; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace FinOS.CoreFinance.API.Controllers;
[ApiController,Route("api/insurance"),Authorize]
public class InsuranceController(IMediator m):ControllerBase
{
 long U=>long.Parse(User.FindFirst("sub")?.Value??User.FindFirst("userId")?.Value??"0");
 [HttpGet] public async Task<ApiResponse<IReadOnlyList<InsurancePolicy>>> Get()=>ApiResponse<IReadOnlyList<InsurancePolicy>>.Ok(await m.Send(new GetInsurancePoliciesQuery(U)));
 [HttpPost] public async Task<ApiResponse<InsurancePolicy>> Add(InsurancePolicy p)=>ApiResponse<InsurancePolicy>.Ok(await m.Send(new AddInsurancePolicyCommand(U,p)));
 [HttpDelete("{id:long}")] public async Task<ApiResponse<object>> Delete(long id){await m.Send(new DeleteInsurancePolicyCommand(U,id));return ApiResponse<object>.Ok(new{});}
}
