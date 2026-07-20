using FinOS.Common.Models;
using FinOS.Identity.Application.Commands;
using FinOS.Identity.Application.DTOs;
using FinOS.Identity.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the current user's profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token"));

        var query = new GetUserProfileQuery { UserId = userId.Value };
        var result = await _mediator.Send(query, ct);

        return Ok(ApiResponse<UserProfileDto>.Ok(result));
    }

    /// <summary>
    /// Get a user's profile by ID (admin access)
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(long id, CancellationToken ct)
    {
        var query = new GetUserProfileQuery { UserId = id };
        var result = await _mediator.Send(query, ct);

        return Ok(ApiResponse<UserProfileDto>.Ok(result));
    }

    /// <summary>
    /// Update the current user's profile
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token"));

        var command = new UpdateProfileCommand
        {
            UserId = userId.Value,
            Request = request
        };

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<UserProfileDto>.Ok(result, "Profile updated successfully"));
    }

    /// <summary>
    /// Change the current user's password
    /// </summary>
    [HttpPut("me/change-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token"));

        var command = new ChangePasswordCommand
        {
            UserId = userId.Value,
            Request = request,
            IpAddress = GetClientIpAddress()
        };

        await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok(new { }, "Password changed successfully"));
    }

    private long? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                         ?? User.FindFirst("sub");

        if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
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
