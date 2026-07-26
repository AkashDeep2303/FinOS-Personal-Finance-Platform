using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Budget.API.Controllers;

public abstract class AuthenticatedControllerBase : ControllerBase
{
    protected long AuthenticatedUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue("userId");
            return long.TryParse(value, out var userId) && userId > 0
                ? userId
                : throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
        }
    }
}
