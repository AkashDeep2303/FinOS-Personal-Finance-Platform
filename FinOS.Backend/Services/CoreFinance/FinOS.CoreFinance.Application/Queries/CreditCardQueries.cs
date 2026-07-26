using FinOS.CoreFinance.Domain.Entities;using FinOS.CoreFinance.Domain.Interfaces;using MediatR;
namespace FinOS.CoreFinance.Application.Queries;
public record GetCreditCardsQuery(long UserId):IRequest<IReadOnlyList<CreditCardDetail>>;
public class GetCreditCardsHandler(ICreditCardRepository r):IRequestHandler<GetCreditCardsQuery,IReadOnlyList<CreditCardDetail>>{public Task<IReadOnlyList<CreditCardDetail>> Handle(GetCreditCardsQuery q,CancellationToken ct)=>r.GetAsync(q.UserId,ct);}
