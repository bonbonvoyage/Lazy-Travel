using System.ComponentModel.DataAnnotations;

namespace LazyTravel.Shared.Models.DTOs
{
	public class MemberLoginDto
	{
		[Required(ErrorMessage = "請輸入Email")]
		[EmailAddress(ErrorMessage = "信箱格式不正確")]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "請輸入密碼")]
		[DataType(DataType.Password)]
		public string Password { get; set; } = string.Empty;

		public bool RememberMe { get; set; }
	}

	// 登入成功後,拿來塞進 Cookie 身分證跟回傳給前端用,不需要會員的完整資料
	public class MemberAuthDto
	{
		public int MemberID { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? AvatarUrl { get; set; }
	}
}