using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;
namespace FinOS.CoreFinance.Application.Commands;
public record SaveTaxProfileCommand(long UserId,string FinancialYear,string? PreferredRegime,string InputJson):IRequest<TaxProfile>;
public class SaveTaxProfileHandler(ITaxRepository r):IRequestHandler<SaveTaxProfileCommand,TaxProfile>
{ public Task<TaxProfile> Handle(SaveTaxProfileCommand q,CancellationToken ct)=>r.UpsertProfileAsync(new(){UserId=q.UserId,FinancialYear=q.FinancialYear,PreferredRegime=q.PreferredRegime,InputJson=q.InputJson,UpdatedAt=DateTime.UtcNow},ct); }
