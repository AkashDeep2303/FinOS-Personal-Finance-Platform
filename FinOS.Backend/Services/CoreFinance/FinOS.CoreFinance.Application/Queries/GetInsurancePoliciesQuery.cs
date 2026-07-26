using FinOS.CoreFinance.Domain.Entities; using FinOS.CoreFinance.Domain.Interfaces; using MediatR;
namespace FinOS.CoreFinance.Application.Queries;
public record GetInsurancePoliciesQuery(long UserId):IRequest<IReadOnlyList<InsurancePolicy>>;
public class GetInsurancePoliciesHandler(IInsuranceRepository r):IRequestHandler<GetInsurancePoliciesQuery,IReadOnlyList<InsurancePolicy>>{public Task<IReadOnlyList<InsurancePolicy>> Handle(GetInsurancePoliciesQuery q,CancellationToken ct)=>r.GetAsync(q.UserId,ct);}
