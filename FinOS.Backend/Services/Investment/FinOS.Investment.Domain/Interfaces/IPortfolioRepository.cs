using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Interfaces;

public interface IPortfolioRepository : IRepository<Domain.Entities.Portfolio>
{
    Task<List<Domain.Entities.Portfolio>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Domain.Entities.Portfolio?> GetWithHoldingsAsync(long portfolioId, CancellationToken ct = default);
}
