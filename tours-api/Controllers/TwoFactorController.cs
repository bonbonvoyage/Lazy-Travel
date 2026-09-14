using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LazyTravel.Middleware;
using LazyTravel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers;

[Authorize(AuthenticationSchemes = "MemberAuth")]
[Route("Auth/TwoFactor")]
public sealed class TwoFactorController(AuthenticatorSetupService service, IDataProtectionProvider protection) : Controller
{
    [HttpGet]
    public IActionResult Index(string? returnUrl)
    {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify([FromForm] VerifyInput input)
    {
        if (!ModelState.IsValid) return View("Index", input with { Error = "請輸入 6 位驗證碼。", ReturnUrl = SafeUrl(input.ReturnUrl) });
        var memberId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!await service.VerifyEnabledCodeAsync(memberId, input.Code)) return View("Index", input with { Error = "驗證碼錯誤，請確認 App 的時間後再試。", ReturnUrl = SafeUrl(input.ReturnUrl) });
        TwoFactorAuthenticationMiddleware.MarkVerified(Response, protection, memberId);
        return LocalRedirect(SafeUrl(input.ReturnUrl));
    }

    private string SafeUrl(string? value) => Url.IsLocalUrl(value) ? value! : "/";
    public sealed record VerifyInput { [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; init; } = ""; public string? ReturnUrl { get; init; } public string? Error { get; init; } }
}
