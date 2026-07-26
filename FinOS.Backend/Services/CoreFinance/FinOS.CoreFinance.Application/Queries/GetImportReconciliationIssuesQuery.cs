using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public sealed record GetImportReconciliationIssuesQuery(
    long UserId,
    int Limit = 100,
    bool IncludeResolved = false) : IRequest<IReadOnlyList<ImportReconciliationIssue>>;

public sealed class GetImportReconciliationIssuesHandler(IDataCenterRepository repository)
    : IRequestHandler<GetImportReconciliationIssuesQuery, IReadOnlyList<ImportReconciliationIssue>>
{
    public Task<IReadOnlyList<ImportReconciliationIssue>> Handle(
        GetImportReconciliationIssuesQuery request,
        CancellationToken cancellationToken) =>
        repository.GetReconciliationIssuesAsync(
            request.UserId,
            Math.Clamp(request.Limit, 1, 200),
            request.IncludeResolved,
            cancellationToken);
}
