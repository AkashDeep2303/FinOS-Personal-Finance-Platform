using FinOS.Common.Models;
using FinOS.Identity.Application.Commands;
using FinOS.Identity.Application.DTOs;
using FinOS.Identity.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace FinOS.Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        var command = new RegisterCommand
        {
            Request = request,
            IpAddress = GetClientIpAddress(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Registration successful"));
    }

    /// <summary>
    /// Authenticate a user and generate tokens
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var command = new LoginCommand
        {
            Request = request,
            IpAddress = GetClientIpAddress(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful"));
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var command = new RefreshTokenCommand
        {
            Request = request,
            IpAddress = GetClientIpAddress()
        };

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Token refreshed successfully"));
    }

    /// <summary>
    /// Logout the user by revoking their refresh tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request, CancellationToken ct)
    {
        // Get user ID from claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                         ?? User.FindFirst("sub");

        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid token"));
        }

        // Revoke the supplied refresh token when it belongs to the authenticated user.
        await _mediator.Send(new LogoutCommand(userId, request?.RefreshToken, GetClientIpAddress()), ct);
        _logger.LogInformation("User {UserId} logged out", userId);

        return Ok(ApiResponse<object>.Ok(new { }, "Logged out successfully"));
    }

    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        if (!TryGetSessionContext(out var userId, out var jwtId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid token"));

        var sessions = await _mediator.Send(new GetActiveSessionsQuery(userId, jwtId), ct);
        return Ok(ApiResponse<IReadOnlyList<SessionDto>>.Ok(sessions));
    }

    [HttpDelete("sessions/{sessionId:long}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeSession(long sessionId, CancellationToken ct)
    {
        if (!TryGetSessionContext(out var userId, out var jwtId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid token"));

        await _mediator.Send(
            new RevokeSessionCommand(userId, sessionId, jwtId, GetClientIpAddress()), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Session revoked"));
    }

    [HttpPost("sessions/revoke-others")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeOtherSessions(CancellationToken ct)
    {
        if (!TryGetSessionContext(out var userId, out var jwtId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid token"));

        await _mediator.Send(
            new RevokeOtherSessionsCommand(userId, jwtId, GetClientIpAddress()), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Other sessions revoked"));
    }

    private bool TryGetSessionContext(out long userId, out string jwtId)
    {
        userId = 0;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                          ?? User.FindFirst("sub");
        jwtId = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                ?? User.FindFirst("jti")?.Value
                ?? string.Empty;
        return userIdClaim is not null &&
               long.TryParse(userIdClaim.Value, out userId) &&
               !string.IsNullOrWhiteSpace(jwtId);
    }

    private string? GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }
        return ipAddress;
    }
}
