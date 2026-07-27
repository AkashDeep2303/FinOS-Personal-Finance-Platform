using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public sealed record GetFinancialDocumentsQuery(long UserId) : IRequest<IReadOnlyList<FinancialDocument>>;

public sealed class GetFinancialDocumentsHandler(IFinancialDocumentRepository repository)
    : IRequestHandler<GetFinancialDocumentsQuery, IReadOnlyList<FinancialDocument>>
{
    public Task<IReadOnlyList<FinancialDocument>> Handle(GetFinancialDocumentsQuery request, CancellationToken cancellationToken) =>
        repository.GetAsync(request.UserId, cancellationToken);
}
