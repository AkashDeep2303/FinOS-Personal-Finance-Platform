using FinOS.Identity.Application.DTOs;
using FinOS.Identity.Domain.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Queries;

public sealed record GetActiveSessionsQuery(long UserId, string CurrentJwtId)
    : IRequest<IReadOnlyList<SessionDto>>;

public sealed class GetActiveSessionsQueryHandler(IRefreshTokenRepository repository)
    : IRequestHandler<GetActiveSessionsQuery, IReadOnlyList<SessionDto>>
{
    public async Task<IReadOnlyList<SessionDto>> Handle(
        GetActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await repository.GetActiveByUserIdAsync(request.UserId, cancellationToken);
        return sessions
            .Select(token => new SessionDto(
                token.Id,
                token.CreatedAt,
                token.ExpiresAt,
                string.Equals(token.JwtId, request.CurrentJwtId, StringComparison.Ordinal)))
            .ToList();
    }
}
