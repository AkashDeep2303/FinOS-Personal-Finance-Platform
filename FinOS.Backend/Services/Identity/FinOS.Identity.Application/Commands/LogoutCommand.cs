using FinOS.Identity.Domain.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Commands;

public sealed record LogoutCommand(long UserId, string? RefreshToken, string? IpAddress) : IRequest<Unit>;

public sealed class LogoutCommandHandler(IRefreshTokenRepository repository)
    : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return Unit.Value;
        var token = await repository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (token is null || token.UserId != request.UserId || token.IsRevoked)
            return Unit.Value;
        await repository.RevokeAsync(
            request.RefreshToken, request.IpAddress ?? "unknown", null, cancellationToken);
        return Unit.Value;
    }
}
