using System.Security.Claims;
using System.Security.Cryptography;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Middleware;

/// <summary>Added independently of AuthController: an enabled account cannot use its password-only cookie.</summary>
public sealed class TwoFactorAuthenticationMiddleware(RequestDelegate next)
{
    private static readonly PathString[] AllowedPrefixes = ["/Auth/TwoFactor", "/Auth/Logout", "/Auth/PasswordRecovery", "/Auth/Authenticator", "/css", "/js", "/lib", "/members"];

    public async Task InvokeAsync(HttpContext context, LazyTravelDBContext db, IDataProtectionProvider protection)
    {
        var path = context.Request.Path;
        if (AllowedPrefixes.Any(prefix => path.StartsWithSegments(prefix)) || context.User.Identity?.IsAuthenticated != true)
        {
            await next(context); return;
        }

        var rawId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(rawId, out var memberId)) { await next(context); return; }
        var enabled = await db.Users.AsNoTracking().AnyAsync(m => m.Id == memberId && m.TwoFactorEnabled && !m.IsDelete && m.Status != 2);
        if (!enabled || HasVerifiedSession(context, protection, memberId)) { await next(context); return; }

        if (context.Request.Headers.Accept.Any(v => v?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "請先完成兩步驟驗證。" }); return;
        }
        var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"/Auth/TwoFactor?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    public static void MarkVerified(HttpResponse response, IDataProtectionProvider protection, int memberId)
    {
        var value = protection.CreateProtector("LazyTravel.TwoFactorLogin.v1").Protect($"{memberId}|{DateTimeOffset.UtcNow.AddHours(8):O}");
        response.Cookies.Append("LazyTravel.Member.TwoFactor", value, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTimeOffset.UtcNow.AddHours(8), IsEssential = true });
    }

    private static bool HasVerifiedSession(HttpContext context, IDataProtectionProvider protection, int memberId)
    {
        if (!context.Request.Cookies.TryGetValue("LazyTravel.Member.TwoFactor", out var value)) return false;
        try { var pieces = protection.CreateProtector("LazyTravel.TwoFactorLogin.v1").Unprotect(value).Split('|'); return pieces.Length == 2 && pieces[0] == memberId.ToString() && DateTimeOffset.TryParse(pieces[1], out var expires) && expires > DateTimeOffset.UtcNow; }
        catch (CryptographicException) { return false; }
    }
}
