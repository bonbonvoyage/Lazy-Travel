using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LazyTravel.Controllers
{
	public class AuthController : Controller
	{
		private readonly IMemberAuthService _memberAuthService;

		public AuthController(IMemberAuthService memberAuthService)
		{
			_memberAuthService = memberAuthService;
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(MemberLoginDto dto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
			string ua = Request.Headers["User-Agent"].ToString();

			var result = _memberAuthService.Login(dto.Email, dto.Password, ip, ua);

			if (!result.Success || result.MemberData is null)
			{
				return BadRequest(result.Message);
			}

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, result.MemberData.MemberID.ToString()),
				new Claim(ClaimTypes.Name, result.MemberData.Name),
				new Claim(ClaimTypes.Email, result.MemberData.Email)
			};

			var identity = new ClaimsIdentity(claims, "MemberAuth");
			var authProperties = new AuthenticationProperties { IsPersistent = dto.RememberMe };

			await HttpContext.SignInAsync("MemberAuth", new ClaimsPrincipal(identity), authProperties);

			return Ok(result.MemberData);
		}

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync("MemberAuth");
			return Ok();
		}
	}
}