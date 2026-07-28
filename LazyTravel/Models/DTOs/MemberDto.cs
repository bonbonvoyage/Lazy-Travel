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
}
