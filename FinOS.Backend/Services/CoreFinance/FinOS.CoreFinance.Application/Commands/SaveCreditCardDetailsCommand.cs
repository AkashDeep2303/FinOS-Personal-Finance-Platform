using FinOS.Common.Exceptions;using FinOS.CoreFinance.Domain.Entities;using FinOS.CoreFinance.Domain.Interfaces;using MediatR;
namespace FinOS.CoreFinance.Application.Commands;
public record SaveCreditCardDetailsCommand(long UserId,CreditCardDetail Card):IRequest<CreditCardDetail>;
public class SaveCreditCardDetailsHandler(ICreditCardRepository r):IRequestHandler<SaveCreditCardDetailsCommand,CreditCardDetail>{public async Task<CreditCardDetail> Handle(SaveCreditCardDetailsCommand q,CancellationToken ct){q.Card.UserId=q.UserId;return await r.UpsertAsync(q.Card,ct)??throw new NotFoundException("CreditCardAccount",q.Card.AccountId);}}
