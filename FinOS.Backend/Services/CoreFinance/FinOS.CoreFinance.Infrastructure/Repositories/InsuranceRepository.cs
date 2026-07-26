using Dapper; using FinOS.Common.Interfaces; using FinOS.CoreFinance.Domain.Entities; using FinOS.CoreFinance.Domain.Interfaces;
namespace FinOS.CoreFinance.Infrastructure.Repositories;
public class InsuranceRepository(IConnectionFactory f):IInsuranceRepository
{
 public async Task<IReadOnlyList<InsurancePolicy>> GetAsync(long u,CancellationToken ct=default){using var c=f.CreateConnection();return (await c.QueryAsync<InsurancePolicy>("SELECT * FROM Core.InsurancePolicies WHERE UserId=@u AND DeletedAt IS NULL ORDER BY RenewalDate",new{u})).ToList();}
 public async Task<InsurancePolicy> AddAsync(InsurancePolicy p,CancellationToken ct=default){using var c=f.CreateConnection();p.Id=await c.ExecuteScalarAsync<long>(@"INSERT Core.InsurancePolicies(UserId,PolicyType,Provider,PolicyNumber,CoverageAmount,PremiumAmount,PremiumFrequency,StartDate,EndDate,RenewalDate,Nominee,Notes,Status) VALUES(@UserId,@PolicyType,@Provider,@PolicyNumber,@CoverageAmount,@PremiumAmount,@PremiumFrequency,@StartDate,@EndDate,@RenewalDate,@Nominee,@Notes,@Status);SELECT CAST(SCOPE_IDENTITY() AS BIGINT)",p);return p;}
 public async Task<bool> DeleteAsync(long id,long u,CancellationToken ct=default){using var c=f.CreateConnection();return await c.ExecuteAsync("UPDATE Core.InsurancePolicies SET DeletedAt=SYSUTCDATETIME(),Status=N'Cancelled' WHERE Id=@id AND UserId=@u AND DeletedAt IS NULL",new{id,u})==1;}
}
