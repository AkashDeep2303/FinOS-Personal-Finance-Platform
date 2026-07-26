using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public sealed record GetDataCenterOverviewQuery(
    long UserId,
    int ImportLimit = 20,
    int IssueLimit = 50) : IRequest<DataCenterOverview>;

public sealed class GetDataCenterOverviewHandler(IDataCenterRepository repository)
    : IRequestHandler<GetDataCenterOverviewQuery, DataCenterOverview>
{
    public Task<DataCenterOverview> Handle(
        GetDataCenterOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var importLimit = Math.Clamp(request.ImportLimit, 1, 100);
        var issueLimit = Math.Clamp(request.IssueLimit, 1, 200);
        return repository.GetOverviewAsync(
            request.UserId,
            importLimit,
            issueLimit,
            cancellationToken);
    }
}
