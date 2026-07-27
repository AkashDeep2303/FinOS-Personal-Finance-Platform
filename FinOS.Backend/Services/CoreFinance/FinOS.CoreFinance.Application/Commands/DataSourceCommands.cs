using FinOS.Common.Exceptions;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public sealed record AddDataSourceCommand(long UserId, DataSource Source) : IRequest<DataSource>;
public sealed record DeleteDataSourceCommand(long UserId, long Id) : IRequest;

public sealed class AddDataSourceHandler(IDataSourceRepository repository)
    : IRequestHandler<AddDataSourceCommand, DataSource>
{
    public Task<DataSource> Handle(AddDataSourceCommand request, CancellationToken cancellationToken)
    {
        request.Source.UserId = request.UserId;
        request.Source.ConnectionMode = "ManualImport";
        request.Source.Status = "Active";
        return repository.AddAsync(request.Source, cancellationToken);
    }
}

public sealed class DeleteDataSourceHandler(IDataSourceRepository repository)
    : IRequestHandler<DeleteDataSourceCommand>
{
    public async Task Handle(DeleteDataSourceCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.DeleteAsync(request.Id, request.UserId, cancellationToken))
        {
            throw new NotFoundException("DataSource", request.Id);
        }
    }
}
