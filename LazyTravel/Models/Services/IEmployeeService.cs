using System.Collections.Generic;

namespace LazyTravel.Models.Services
{
	public class EmployeeDto
	{
		public int MemberID { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public byte Role { get; set; }
		public List<string> Permissions { get; set; }
	}

	public interface IEmployeeService
	{
		IEnumerable<EmployeeDto> GetAllEmployees();
		(bool Success, string Message) PromoteToAdmin(string email, List<string> permissions, int currentAdminId);
		(bool Success, string Message) EditPermissions(int adminId, List<string> permissions, int currentAdminId);
		(bool Success, string Message) DemoteAdmin(int targetAdminId, int currentAdminId);
	}
}