using System.ComponentModel.DataAnnotations;
using LazyTravel.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers;

[AllowAnonymous]
[Route("Auth/PasswordRecovery")]
public sealed class PasswordRecoveryController(PasswordRecoveryService recoveryService) : Controller
{
    [HttpPost("RequestCode")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCode([FromForm] RequestCodeInput input)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "请输入有效的注册电子信箱。" });
        var result = await recoveryService.RequestCodeAsync(input.Email);
        return Ok(new { success = result.Success, requestId = result.RequestId, message = result.Message, demoCode = result.DemoCode });
    }

    [HttpPost("VerifyEmailCode")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmailCode([FromForm] VerifyCodeInput input)
    {
        var result = await recoveryService.VerifyEmailCodeAsync(input.RequestId, input.Code);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpPost("ResetPassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordInput input)
    {
        var result = await recoveryService.ResetPasswordAsync(input.RequestId, input.Password, input.ConfirmPassword);
        if (!result.Success) return BadRequest(new { message = result.Message });

        await HttpContext.SignOutAsync("MemberAuth");
        return Ok(new { success = true, message = result.Message });
    }

    public sealed class RequestCodeInput
    {
        [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    }

    public sealed class VerifyCodeInput
    {
        [Required, StringLength(48)] public string RequestId { get; set; } = string.Empty;
        [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; set; } = string.Empty;
    }

    public sealed class ResetPasswordInput
    {
        [Required, StringLength(48)] public string RequestId { get; set; } = string.Empty;
        [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
        [Required, StringLength(128, MinimumLength = 8)] public string ConfirmPassword { get; set; } = string.Empty;
    }
}
