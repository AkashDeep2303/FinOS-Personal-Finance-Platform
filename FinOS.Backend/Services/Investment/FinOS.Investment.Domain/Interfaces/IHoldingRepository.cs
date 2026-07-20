using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Interfaces;

public interface IHoldingRepository : IRepository<Domain.Entities.Holding>
{
    Task<List<Domain.Entities.Holding>> GetByPortfolioIdAsync(long portfolioId, CancellationToken ct = default);
    Task<Domain.Entities.Holding?> GetWithTransactionsAsync(long holdingId, CancellationToken ct = default);
    Task<List<Domain.Entities.Holding>> GetActiveByPortfolioIdAsync(long portfolioId, CancellationToken ct = default);
}
