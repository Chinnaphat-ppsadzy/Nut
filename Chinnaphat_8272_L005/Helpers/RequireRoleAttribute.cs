using Chinnaphat_8272_L005.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Chinnaphat_8272_L005.Helpers;

/// <summary>
/// Protects staff endpoints by resolving the current role from the server-side session user.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireRoleAttribute : TypeFilterAttribute
{
    public RequireRoleAttribute(params string[] roleCodes)
        : base(typeof(RequireRoleFilter))
    {
        Arguments = new object[] { roleCodes };
    }
}

public sealed class RequireRoleFilter : IAsyncAuthorizationFilter
{
    private readonly Csi402dbContext _db;
    private readonly string[] _roleCodes;

    public RequireRoleFilter(Csi402dbContext db, string[] roleCodes)
    {
        _db = db;
        _roleCodes = roleCodes;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var userIdValue = http.Session.GetString("UserId");

        if (!uint.TryParse(userIdValue, out var userId))
        {
            var returnUrl = http.Request.Path + http.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "User", new { returnUrl });
            return;
        }

        var currentUser = await _db.Users
            .Where(user => user.UserId == userId)
            .Select(user => new { RoleCode = user.Role.RoleCode, user.Status })
            .FirstOrDefaultAsync();

        if (currentUser == null ||
            string.Equals(currentUser.Status, "inactive", StringComparison.OrdinalIgnoreCase) ||
            !_roleCodes.Contains(currentUser.RoleCode, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}
