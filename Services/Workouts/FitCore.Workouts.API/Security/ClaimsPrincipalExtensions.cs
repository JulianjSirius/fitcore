using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FitCore.Workouts.API.Security;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out userId);
    }

    public static string? GetRole(this ClaimsPrincipal user)
        => user.FindFirstValue("Rol")
            ?? user.FindFirstValue(ClaimTypes.Role);
}
