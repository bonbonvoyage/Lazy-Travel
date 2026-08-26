using System.ComponentModel.DataAnnotations;

namespace LazyTravel.Shared.Models.DTOs
{
	public class AdminLoginDto
	{
		[Required(ErrorMessage = "請輸入企業信箱")]
		[EmailAddress(ErrorMessage = "信箱格式不正確")]
		public string Email { get; set; }

		[Required(ErrorMessage = "請輸入密碼")]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		public bool RememberMe { get; set; }
	}
}