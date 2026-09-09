namespace LazyTravel.Shared.Models.DTOs
{
	public class AdminLogDto
	{
		public long LogID { get; set; }
		public int EmployeeID { get; set; }
		public string AdminName { get; set; } = string.Empty;
		public string Action { get; set; } = string.Empty;
		public string TargetResource { get; set; } = string.Empty;
		public string TargetID { get; set; } = string.Empty;
		public string TargetMemberName { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string IPAddress { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
	}
}