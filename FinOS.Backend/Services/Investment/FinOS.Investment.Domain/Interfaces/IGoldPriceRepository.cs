using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Interfaces;

public interface IGoldPriceRepository : IRepository<Domain.Entities.GoldPriceHistory>
{
    Task<Domain.Entities.GoldPriceHistory?> GetLatestPriceAsync(Domain.Enums.GoldType goldType, CancellationToken ct = default);
    Task<List<Domain.Entities.GoldPriceHistory>> GetPriceHistoryAsync(Domain.Enums.GoldType goldType, DateTime from, DateTime to, CancellationToken ct = default);
}
