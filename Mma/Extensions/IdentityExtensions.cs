using System.Security.Claims;

namespace Mma.Extensions;

public static class IdentityExtensions
{
    public static string GetValue(this ClaimsPrincipal principal, string type)
    {
        var value = principal.FindFirst(type)!.Value;
        if (value.IsNotNullOrEmpty())
            return value;
        return "";
    }
    public static List<string> Roles(this ClaimsIdentity identity)
    {
        return [.. identity.Claims
                           .Where(c => c.Type == ClaimTypes.Role)
                           .Select(c => c.Value)];
    }
    public static List<string> Roles(this ClaimsPrincipal identity)
    {
        return [.. identity.Claims
                           .Where(c => c.Type == ClaimTypes.Role)
                           .Select(c => c.Value)];
    }
}