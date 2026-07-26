using FinOS.Investment.Domain.Entities;

namespace FinOS.Investment.Domain.Interfaces;

public interface IInvestmentTypeRepository
{
    Task<IReadOnlyList<InvestmentType>> GetAllAsync(CancellationToken ct = default);
}
