using System.Collections;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace LazyTravel.Middleware;

/// <summary>
/// Adds authentication in front of the existing travel-group interaction endpoints.
/// The underlying travel-group implementation still reads its historical ltvmid cookie,
/// so authenticated requests receive a request-scoped bridge to the signed-in member ID.
/// </summary>
public sealed class MemberActionAuthenticationMiddleware
{
    private const string LegacyMemberCookieName = "ltvmid";

    private static readonly string[] ProtectedPostPaths =
    {
        "/TravelGroups/ToggleFavorite",
        "/TravelGroups/Join",
        "/TravelGroups/CancelJoin",
        "/TravelGroups/Leave",
        "/Report/SubmitTargetReport",
    };

    private readonly RequestDelegate _next;

    public MemberActionAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isProtectedPost = HttpMethods.IsPost(context.Request.Method) &&
                              ProtectedPostPaths.Any(path =>
                                  context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase));

        if (isProtectedPost && context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                ok = false,
                requiresLogin = true,
                message = "請先登入後再進行此操作。",
            });
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true &&
            int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var memberId))
        {
            BridgeLegacyMemberCookie(context, memberId);

            if (isProtectedPost)
            {
                await ReplaceClientSuppliedMemberIdAsync(context, memberId);
            }
        }

        await _next(context);
    }

    private static void BridgeLegacyMemberCookie(HttpContext context, int memberId)
    {
        var cookies = context.Request.Cookies.ToDictionary(pair => pair.Key, pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
        cookies[LegacyMemberCookieName] = memberId.ToString();
        context.Features.Set<IRequestCookiesFeature>(
            new RequestCookiesFeature(new DictionaryRequestCookieCollection(cookies)));
    }

    private static async Task ReplaceClientSuppliedMemberIdAsync(HttpContext context, int memberId)
    {
        var query = context.Request.Query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
        query["viewerMemberId"] = new StringValues(memberId.ToString());
        context.Request.Query = new QueryCollection(query);

        if (!context.Request.HasFormContentType)
        {
            return;
        }

        var form = await context.Request.ReadFormAsync();
        var values = form.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
        values["ViewerMemberId"] = new StringValues(memberId.ToString());
        context.Request.Form = new FormCollection(values, form.Files);
    }

    private sealed class DictionaryRequestCookieCollection : IRequestCookieCollection
    {
        private readonly IReadOnlyDictionary<string, string> _cookies;

        public DictionaryRequestCookieCollection(IReadOnlyDictionary<string, string> cookies)
        {
            _cookies = cookies;
        }

        public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;
        public int Count => _cookies.Count;
        public ICollection<string> Keys => _cookies.Keys.ToArray();
        public bool ContainsKey(string key) => _cookies.ContainsKey(key);
        public bool TryGetValue(string key, out string value) => _cookies.TryGetValue(key, out value!);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
