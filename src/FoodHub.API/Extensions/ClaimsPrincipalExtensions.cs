using System.Security.Claims;
using FoodHub.Application.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAppException("Token has no user id.");
    }

    public static Role GetRole(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<Role>(value, out var role) ? role : throw new UnauthorizedAppException("Token has no role.");
    }
}
