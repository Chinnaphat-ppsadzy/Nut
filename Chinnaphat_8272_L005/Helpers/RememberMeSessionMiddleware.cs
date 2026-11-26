using System.Security.Cryptography;
using System.Text;
using Chinnaphat_8272_L005.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Chinnaphat_8272_L005.Helpers;

/// <summary>
/// Restores the existing server-side session when a valid Remember Me cookie is present.
/// The protected token is tied to the current password hash, so changing the password
/// invalidates tokens issued before that change.
/// </summary>
public sealed class RememberMeSessionMiddleware
{
    public const string CookieName = "CourtBook.RememberMe";

    private readonly RequestDelegate _next;
    private readonly IDataProtector _protector;

    public RememberMeSessionMiddleware(RequestDelegate next, IDataProtectionProvider dataProtectionProvider)
    {
        _next = next;
        _protector = dataProtectionProvider.CreateProtector("CourtBook.RememberMe.v1");
    }

    public async Task InvokeAsync(HttpContext context, Csi402dbContext db)
    {
        if (!uint.TryParse(context.Session.GetString("UserId"), out _) &&
            context.Request.Cookies.TryGetValue(CookieName, out var token))
        {
            try
            {
                var payload = _protector.Unprotect(token);
                var parts = payload.Split('|', 3);
                if (parts.Length == 3 && uint.TryParse(parts[0], out var userId) &&
                    long.TryParse(parts[1], out var expirySeconds) &&
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds() < expirySeconds)
                {
                    var user = await db.Users.Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.UserId == userId);
                    var storedHash = user?.PasswordHash ?? string.Empty;
                    var tokenHash = parts[2];
                    var isActive = user != null && !new[] { "inactive", "banned", "disabled", "suspended" }
                        .Contains(user.Status?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

                    if (isActive && user!.Role != null &&
                        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(tokenHash), Encoding.UTF8.GetBytes(storedHash)))
                    {
                        context.Session.SetString("UserId", user.UserId.ToString());
                        context.Session.SetString("FullName", user.FullName);
                        context.Session.SetString("Email", user.Email);
                        context.Session.SetString("RoleCode", user.Role.RoleCode);
                        context.Session.SetString("RoleName", user.Role.RoleName);
                        if (!string.IsNullOrWhiteSpace(user.Phone))
                            context.Session.SetString("Phone", user.Phone);
                    }
                    else
                    {
                        DeleteCookie(context);
                    }
                }
                else
                {
                    DeleteCookie(context);
                }
            }
            catch (CryptographicException)
            {
                DeleteCookie(context);
            }
        }

        await _next(context);
    }

    private static void DeleteCookie(HttpContext context) => context.Response.Cookies.Delete(CookieName, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/"
    });
}
