using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LazyTravel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers;

[Authorize(AuthenticationSchemes = "MemberAuth")]
[Route("Auth/Authenticator")]
public sealed class AuthenticatorSetupController(AuthenticatorSetupService service) : Controller
{
    private int MemberId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("Status")]
    public async Task<IActionResult> Status() => Ok(await service.GetStatusAsync(MemberId));

    [HttpPost("Start"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Start()
    {
        var result = await service.StartAsync(MemberId);
        return result.Success
            ? Ok(new { success = true, result.SetupId, result.QrCodeDataUri, result.ManualKey })
            : BadRequest(new { message = result.Message });
    }

    [HttpPost("Confirm"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm([FromForm] ConfirmInput input)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "請輸入 Google Authenticator 顯示的 6 位驗證碼。" });
        var result = await service.ConfirmAsync(MemberId, input.SetupId, input.Code);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(new { message = result.Message });
    }

    public sealed class ConfirmInput
    {
        [Required, StringLength(48)] public string SetupId { get; set; } = "";
        [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; set; } = "";
    }
}
