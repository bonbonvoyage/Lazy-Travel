namespace LazyTravel.Models.DTOs
{
	public class MemberDto
	{
		public int MemberID { get; set; }
		public string Email { get; set; }
		public string Name { get; set; }
		public byte Gender { get; set; }
		public int? Age { get; set; }
		public string Occupation { get; set; }
		public string MBTI { get; set; }
		public byte Status { get; set; }
		public DateTime CreatedAt { get; set; }
		public string PlanName { get; set; }
	}

	// ==========================================
	// 會員模組專屬：操作紀錄傳輸物件
	// ==========================================
	public class AdminLogDto
	{
		public int LogID { get; set; }

		public int AdminID { get; set; }
		public string AdminName { get; set; } // 執行操作的管理員姓名

		public string Action { get; set; }

		public int? TargetID { get; set; }
		public string TargetMemberName { get; set; } // 被處分的會員姓名

		public string Description { get; set; }
		public string IPAddress { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
