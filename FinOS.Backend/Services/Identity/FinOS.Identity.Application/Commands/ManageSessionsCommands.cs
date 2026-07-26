using FinOS.Identity.Domain.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Commands;

public sealed record RevokeSessionCommand(
    long UserId, long SessionId, string CurrentJwtId, string? IpAddress) : IRequest<Unit>;

public sealed class RevokeSessionCommandHandler(IRefreshTokenRepository repository)
    : IRequestHandler<RevokeSessionCommand, Unit>
{
    public async Task<Unit> Handle(
        RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != request.UserId ||
            string.Equals(session.JwtId, request.CurrentJwtId, StringComparison.Ordinal))
            return Unit.Value;

        await repository.RevokeByIdAsync(
            request.UserId, request.SessionId, request.IpAddress, cancellationToken);
        return Unit.Value;
    }
}

public sealed record RevokeOtherSessionsCommand(
    long UserId, string CurrentJwtId, string? IpAddress) : IRequest<Unit>;

public sealed class RevokeOtherSessionsCommandHandler(IRefreshTokenRepository repository)
    : IRequestHandler<RevokeOtherSessionsCommand, Unit>
{
    public async Task<Unit> Handle(
        RevokeOtherSessionsCommand request, CancellationToken cancellationToken)
    {
        await repository.RevokeAllExceptJwtIdAsync(
            request.UserId, request.CurrentJwtId, request.IpAddress, cancellationToken);
        return Unit.Value;
    }
}
