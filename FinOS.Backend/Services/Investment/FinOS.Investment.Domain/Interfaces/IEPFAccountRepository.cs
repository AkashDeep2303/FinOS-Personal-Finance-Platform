using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Interfaces;

public interface IEPFAccountRepository : IRepository<Domain.Entities.EPFAccount>
{
    Task<List<Domain.Entities.EPFAccount>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Domain.Entities.EPFAccount?> GetWithContributionsAsync(long epfAccountId, CancellationToken ct = default);
}
