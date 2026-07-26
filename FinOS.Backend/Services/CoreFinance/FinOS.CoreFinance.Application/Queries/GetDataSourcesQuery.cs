using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public sealed record GetDataSourcesQuery(long UserId) : IRequest<IReadOnlyList<DataSource>>;

public sealed class GetDataSourcesHandler(IDataSourceRepository repository)
    : IRequestHandler<GetDataSourcesQuery, IReadOnlyList<DataSource>>
{
    public Task<IReadOnlyList<DataSource>> Handle(GetDataSourcesQuery request, CancellationToken cancellationToken) =>
        repository.GetAsync(request.UserId, cancellationToken);
}
