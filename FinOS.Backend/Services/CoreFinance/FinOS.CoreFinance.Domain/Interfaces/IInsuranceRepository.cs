using FinOS.CoreFinance.Domain.Entities;
namespace FinOS.CoreFinance.Domain.Interfaces;
public interface IInsuranceRepository
{
 Task<IReadOnlyList<InsurancePolicy>> GetAsync(long userId,CancellationToken ct=default);
 Task<InsurancePolicy> AddAsync(InsurancePolicy policy,CancellationToken ct=default);
 Task<bool> DeleteAsync(long id,long userId,CancellationToken ct=default);
}
