using FinOS.CoreFinance.Domain.Entities;namespace FinOS.CoreFinance.Domain.Interfaces;
public interface IAssetRepository{Task<IReadOnlyList<Asset>> GetAsync(long userId,CancellationToken ct=default);Task<Asset>AddAsync(Asset asset,CancellationToken ct=default);Task<bool>DeleteAsync(long id,long userId,CancellationToken ct=default);}
