using FinOS.Common.Exceptions; using FinOS.CoreFinance.Domain.Entities; using FinOS.CoreFinance.Domain.Interfaces; using MediatR;
namespace FinOS.CoreFinance.Application.Commands;
public record AddInsurancePolicyCommand(long UserId,InsurancePolicy Policy):IRequest<InsurancePolicy>;
public record DeleteInsurancePolicyCommand(long UserId,long Id):IRequest;
public class AddInsurancePolicyHandler(IInsuranceRepository r):IRequestHandler<AddInsurancePolicyCommand,InsurancePolicy>{public Task<InsurancePolicy> Handle(AddInsurancePolicyCommand q,CancellationToken ct){q.Policy.UserId=q.UserId;return r.AddAsync(q.Policy,ct);}}
public class DeleteInsurancePolicyHandler(IInsuranceRepository r):IRequestHandler<DeleteInsurancePolicyCommand>{public async Task Handle(DeleteInsurancePolicyCommand q,CancellationToken ct){if(!await r.DeleteAsync(q.Id,q.UserId,ct))throw new NotFoundException("InsurancePolicy",q.Id);}}
