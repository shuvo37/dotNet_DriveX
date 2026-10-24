using System.Security.Claims;
using DriveX.Exceptions;

namespace DriveX.Extensions;

// Small helpers to read who is calling from the JWT (the token the React app sends).
public static class ClaimsPrincipalExtensions
{
    // The "userId" claim written by JwtService when the user logged in.
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("userId")?.Value;
        return long.TryParse(value, out var id) ? id : throw new AppException("Invalid token");
    }

    // The "sub" claim holds the user's email (see JwtService).
    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirst("sub")?.Value ?? throw new AppException("Invalid token");

    public static bool IsStaff(this ClaimsPrincipal user) =>
        user.IsInRole("ADMIN") || user.IsInRole("STUFF");
}
