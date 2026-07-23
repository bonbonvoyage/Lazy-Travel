namespace LazyTravel.Models.DTOs
{
	public class AdminLogDto
	{
		public long LogID { get; set; }
		public int EmployeeID { get; set; }
		public string AdminName { get; set; }
		public string Action { get; set; }
		public string TargetResource { get; set; }
		public string TargetID { get; set; }
		public string TargetMemberName { get; set; }
		public string Description { get; set; }
		public string IPAddress { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}