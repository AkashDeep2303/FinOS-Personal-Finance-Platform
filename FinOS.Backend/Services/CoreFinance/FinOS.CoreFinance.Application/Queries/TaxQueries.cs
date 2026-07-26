using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;
namespace FinOS.CoreFinance.Application.Queries;
public record GetTaxProfileQuery(long UserId,string FinancialYear):IRequest<TaxProfile?>;
public record GetTaxRulesQuery(string FinancialYear):IRequest<IReadOnlyList<object>>;
public class GetTaxProfileHandler(ITaxRepository r):IRequestHandler<GetTaxProfileQuery,TaxProfile?> { public Task<TaxProfile?> Handle(GetTaxProfileQuery q,CancellationToken ct)=>r.GetProfileAsync(q.UserId,q.FinancialYear,ct); }
public class GetTaxRulesHandler(ITaxRepository r):IRequestHandler<GetTaxRulesQuery,IReadOnlyList<object>> { public async Task<IReadOnlyList<object>> Handle(GetTaxRulesQuery q,CancellationToken ct)=>(await r.GetPublishedRulesAsync(q.FinancialYear,ct)).Select(x=>(object)new{id=x.Id,financialYear=x.FinancialYear,assessmentYear=x.AssessmentYear,regime=x.Regime,version=x.Version}).ToList(); }
