namespace LazyTravel.Models.ViewModels
{
	public class MemberIndexVm
	{
		public int MemberID { get; set; }
		public string Email { get; set; }
		public string Name { get; set; }
		public string GenderText { get; set; }
		public string AgeText { get; set; }
		public string Occupation { get; set; }
		public string MBTI { get; set; }

		public string StatusText { get; set; }
		public string StatusBadgeClass { get; set; } 

		public string CreatedAtString { get; set; }
		public string PlanName { get; set; }
	}
}