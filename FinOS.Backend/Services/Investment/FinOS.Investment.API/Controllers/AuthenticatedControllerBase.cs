using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Investment.API.Controllers;

public abstract class AuthenticatedControllerBase : ControllerBase
{
    protected long AuthenticatedUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!long.TryParse(value, out var userId) || userId <= 0)
                throw new UnauthorizedAccessException("The access token does not contain a valid user identifier.");
            return userId;
        }
    }
}
