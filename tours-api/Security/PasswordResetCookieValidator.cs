using System.Globalization;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LazyTravel.Security;

/// <summary>
/// Adds Security Stamp invalidation without altering the existing login controller.
/// A successful reset records its timestamp in the already-existing MemberTokens table.
/// </summary>
public sealed class PasswordResetCookieValidator(IServiceScopeFactory scopeFactory) : IPostConfigureOptions<CookieAuthenticationOptions>
{
    private const string Provider = "LazyTravel.AccountSecurity";
    private const string TokenName = "PasswordResetAfter";

    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        if (!string.Equals(name, "MemberAuth", StringComparison.Ordinal)) return;
        var previousValidator = options.Events.OnValidatePrincipal;
        options.Events.OnValidatePrincipal = async context =>
        {
            if (previousValidator is not null) await previousValidator(context);
            if (context.Principal?.Identity?.IsAuthenticated != true) return;

            var rawMemberId = context.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(rawMemberId, out var memberId)) return;

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LazyTravelDBContext>();
            var resetAtText = await db.Database.SqlQueryRaw<string>(
                    "SELECT [Value] FROM [dbo].[MemberTokens] WHERE [UserId] = {0} AND [LoginProvider] = {1} AND [Name] = {2}",
                    memberId, Provider, TokenName)
                .SingleOrDefaultAsync();

            if (!DateTimeOffset.TryParse(resetAtText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var resetAt)) return;
            if (context.Properties.IssuedUtc is not { } issuedAt || issuedAt <= resetAt)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync("MemberAuth");
            }
        };
    }
}
