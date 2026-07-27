using FinOS.Common.Models;using FinOS.CoreFinance.Application.Commands;using FinOS.CoreFinance.Application.Queries;using FinOS.CoreFinance.Domain.Entities;using MediatR;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace FinOS.CoreFinance.API.Controllers;
[ApiController,Route("api/credit-cards"),Authorize]
public class CreditCardsController(IMediator m):ControllerBase{
long U=>long.Parse(User.FindFirst("sub")?.Value??User.FindFirst("userId")?.Value??"0");
[HttpGet]public async Task<ApiResponse<IReadOnlyList<CreditCardDetail>>> Get()=>ApiResponse<IReadOnlyList<CreditCardDetail>>.Ok(await m.Send(new GetCreditCardsQuery(U)));
[HttpPut("{accountId:long}")]public async Task<ApiResponse<CreditCardDetail>> Put(long accountId,CreditCardDetail x){x.AccountId=accountId;return ApiResponse<CreditCardDetail>.Ok(await m.Send(new SaveCreditCardDetailsCommand(U,x)));}}
