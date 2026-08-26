using System;
using System.ComponentModel.DataAnnotations;

namespace LazyTravel.Shared.Models.DTOs
{
	// ==========================================
	// 1.2 & 1.4 檢視會員詳細資料 (包含訂閱與登入稽核)
	// ==========================================
	public class MemberDetailDto
	{
		public int MemberID { get; set; }
		public string Email { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;

		public string? Phone { get; set; }
		public string? LineId { get; set; }
		public string? InstagramUrl { get; set; }
		public string? FacebookUrl { get; set; }
		public bool IsEmailConfirmed { get; set; }

		public string? AvatarUrl { get; set; }
		public DateOnly? BirthDate { get; set; }
		public byte Gender { get; set; }
		public string? Occupation { get; set; }
		public string? MBTI { get; set; }
		public string? Bio { get; set; }

		public byte Status { get; set; }
		// 🌟 已移除：public byte Role { get; set; }
		public DateTime CreatedAt { get; set; }

		public string CurrentPlanName { get; set; } = string.Empty;
		public DateTime? PlanExpiryDate { get; set; }

		public DateTime? LastLoginAt { get; set; }
		public string? LastLoginIp { get; set; }
	}

	// ==========================================
	// 1.3 編輯會員資料 
	// ==========================================
	public class MemberEditDto
	{
		public int MemberID { get; set; }

		[Display(Name = "強制重置姓名 (變更為預設)")]
		public bool ResetName { get; set; }

		[Display(Name = "強制清空個人簡介")]
		public bool ResetBio { get; set; }

		[Display(Name = "強制移除大頭貼")]
		public bool RemoveAvatar { get; set; }

		[Display(Name = "帳號狀態")]
		public byte Status { get; set; }

		[Display(Name = "發送系統通知信告知會員")]
		public bool SendNotification { get; set; }

		[Display(Name = "操作原因 (必填稽核用) *")]
		[Required(ErrorMessage = "請填寫操作原因以供稽核")]
		[StringLength(200)]
		public string AdminReason { get; set; } = string.Empty;
	}
}