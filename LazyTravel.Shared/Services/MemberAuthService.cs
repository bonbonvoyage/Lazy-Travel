using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;

namespace LazyTravel.Shared.Services
{
	public class MemberAuthService : IMemberAuthService
	{
		private static readonly PasswordHasher<Member> PasswordHasher = new();
		private readonly LazyTravelDBContext _context;

		public MemberAuthService(LazyTravelDBContext context)
		{
			_context = context;
		}

		public (bool Success, string Message, MemberAuthDto? MemberData) Login(string email, string password, string ipAddress, string userAgent)
		{
			var member = _context.Users.FirstOrDefault(m => m.Email == email && !m.IsDelete);

			if (member == null)
			{
				return (false, "帳號或密碼錯誤。", null);
			}

			if (member.Status == 2)
			{
				return (false, "此帳號已被停權,請聯繫客服。", null);
			}

			bool isPasswordValid;
			try
			{
				var verificationResult = PasswordHasher.VerifyHashedPassword(member, member.PasswordHash ?? string.Empty, password);
				isPasswordValid = verificationResult != PasswordVerificationResult.Failed;

				if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
				{
					member.PasswordHash = PasswordHasher.HashPassword(member, password);
				}
			}
			catch
			{
				isPasswordValid = false;
			}

			_context.LoginHistories.Add(new LoginHistory
			{
				MemberId = member.Id,
				LoginIp = ipAddress,
				IsSuccess = isPasswordValid,
				UserAgent = userAgent,
				AttemptedAt = DateTime.Now
			});

			if (!isPasswordValid)
			{
				_context.SaveChanges();
				return (false, "帳號或密碼錯誤。", null);
			}

			member.LastLoginAt = DateTime.Now;
			member.LastLoginIp = ipAddress;
			_context.SaveChanges();

			return (true, "登入成功!!", new MemberAuthDto
			{
				MemberID = member.Id,
				Name = member.Name,
				Email = member.Email,
				AvatarUrl = member.AvatarUrl
			});
		}
	}
}
