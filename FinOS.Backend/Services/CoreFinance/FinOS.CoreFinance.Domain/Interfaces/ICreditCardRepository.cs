using FinOS.CoreFinance.Domain.Entities;
namespace FinOS.CoreFinance.Domain.Interfaces;
public interface ICreditCardRepository{Task<IReadOnlyList<CreditCardDetail>> GetAsync(long userId,CancellationToken ct=default);Task<CreditCardDetail?> UpsertAsync(CreditCardDetail card,CancellationToken ct=default);}
