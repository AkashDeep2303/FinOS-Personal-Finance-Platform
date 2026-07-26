using FinOS.Common.Exceptions;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public sealed record ResolveImportErrorCommand(long UserId, long Id, long? TransactionId) : IRequest;

public sealed class ResolveImportErrorHandler(IDataCenterRepository repository)
    : IRequestHandler<ResolveImportErrorCommand>
{
    public async Task Handle(ResolveImportErrorCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.ResolveImportErrorAsync(
            request.Id,
            request.UserId,
            request.TransactionId,
            cancellationToken))
        {
            throw new NotFoundException("ImportError", request.Id);
        }
    }
}
