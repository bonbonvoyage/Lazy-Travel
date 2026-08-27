using LazyTravel.Shared.Models.DTOs;

namespace LazyTravel.Shared.Services
{
	public interface IMemberAuthService
	{
		(bool Success, string Message, MemberAuthDto? MemberData) Login(string email, string password, string ipAddress, string userAgent);
	}
}