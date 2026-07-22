using System.Collections.Generic;
using LazyTravel.Models.DTOs;

namespace LazyTravel.Models.Services
{
	public class EmployeeDto
	{
		public int EmployeeID { get; set; }
		public string EmployeeNo { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Department { get; set; }
		public byte Status { get; set; }
		public List<string> Roles { get; set; } = new List<string>();
		public List<string> Permissions { get; set; } = new List<string>();
	}

	public class RoleDto
	{
		public int RoleID { get; set; }
		public string RoleCode { get; set; }
		public string RoleName { get; set; }
		public string Description { get; set; }
		public List<string> Permissions { get; set; } = new List<string>();
		public List<int> PermissionIds { get; set; } = new List<int>();
	}

	public class PermissionDto
	{
		public int PermissionID { get; set; }
		public string PermissionCode { get; set; }
		public string ModuleName { get; set; }
		public string Description { get; set; }
	}

	public interface IEmployeeService
	{
		// --- 員工管理 ---
		IEnumerable<EmployeeDto> GetAllEmployees(string keyword = null);
		(bool Success, string Message) PromoteToEmployee(string email, List<int> roleIds, int currentAdminId);
		(bool Success, string Message) EditEmployeeRoles(int employeeId, List<int> roleIds, int currentAdminId);
		(bool Success, string Message) DemoteEmployee(int employeeId, int currentAdminId);
		(IEnumerable<AdminLogDto> Data, int TotalCount) GetEmployeeAdminLogs(string keyword = null, int page = 1);

		// --- 角色 (Roles) 管理 ---
		IEnumerable<RoleDto> GetAllRoles();
		(bool Success, string Message) CreateRole(RoleDto dto, List<int> permissionIds, int currentAdminId);
		(bool Success, string Message) EditRole(int roleId, List<int> permissionIds, int currentAdminId);
		(bool Success, string Message) DeleteRole(int roleId, int currentAdminId);

		// --- 🌟 權限字典 (Permissions) CRUD 新增 ---
		IEnumerable<PermissionDto> GetAllPermissions(string keyword = null);
		(bool Success, string Message) CreatePermission(PermissionDto dto, int currentAdminId);
		(bool Success, string Message) EditPermission(PermissionDto dto, int currentAdminId);
		(bool Success, string Message) DeletePermission(int permissionId, int currentAdminId);

		// --- 稽核日誌 ---
		(IEnumerable<AdminLogDto> Data, int TotalCount) GetRoleAdminLogs(string keyword = null, int page = 1);
	}
}