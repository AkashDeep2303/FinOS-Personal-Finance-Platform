namespace FinOS.Investment.Domain.Interfaces;

public interface ITargetAllocationRepository
{
    Task<IReadOnlyDictionary<string, decimal>> GetAsync(long portfolioId, CancellationToken ct = default);
    Task ReplaceAsync(long portfolioId, IReadOnlyDictionary<string, decimal> targets, CancellationToken ct = default);
}
