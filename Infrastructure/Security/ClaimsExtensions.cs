using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Infrastructure.Security;

public static class ClaimsExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var rawValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (Guid.TryParse(rawValue, out var userId))
        {
            return userId;
        }

        throw new AppException("Authenticated user id claim is missing.", System.Net.HttpStatusCode.Unauthorized);
    }

    public static string GetRequiredEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email)
            ?? throw new AppException("Authenticated email claim is missing.", System.Net.HttpStatusCode.Unauthorized);

    public static UserRole GetRequiredRole(this ClaimsPrincipal principal)
    {
        var rawRole = principal.FindFirstValue(ClaimTypes.Role)
            ?? throw new AppException("Authenticated role claim is missing.", System.Net.HttpStatusCode.Unauthorized);

        return Enum.Parse<UserRole>(rawRole, ignoreCase: true);
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole(UserRole.Admin.ToString());
}
