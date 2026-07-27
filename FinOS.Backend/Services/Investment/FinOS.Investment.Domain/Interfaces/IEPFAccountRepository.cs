using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Interfaces;

public interface IEPFAccountRepository : IRepository<Domain.Entities.EPFAccount>
{
    Task<List<Domain.Entities.EPFAccount>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Domain.Entities.EPFAccount?> GetWithContributionsAsync(long epfAccountId, CancellationToken ct = default);
    Task<long> CreateAccountAsync(long userId, string? uan, string? establishmentCode, string? employerName, decimal employeePct, decimal employerPct, decimal salary, decimal balance, decimal interestRate, DateTime startDate, CancellationToken ct = default);
    Task<Domain.Entities.EPFContribution> AddContributionAsync(long accountId, long userId, DateTime month, decimal salary, CancellationToken ct = default);
}
