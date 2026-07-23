using LazyTravel.Models.DTOs;
using LazyTravel.Models.EfModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LazyTravel.Models.Services
{
	public class EmployeeService : IEmployeeService
	{
		private readonly LazyTravelDBContext _context;

		public EmployeeService(LazyTravelDBContext context)
		{
			_context = context;
		}

		// ==========================================
		// 員工管理區塊
		// ==========================================
		public IEnumerable<EmployeeDto> GetAllEmployees(string keyword = null)
		{
			var query = _context.Employees
				.Include(e => e.EmployeeRoles)
					.ThenInclude(er => er.Role)
						.ThenInclude(r => r.RolePermissions)
							.ThenInclude(rp => rp.Permission)
				.AsNoTracking()
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				query = query.Where(e => e.Name.Contains(keyword) || e.Email.Contains(keyword) || e.EmployeeNo.Contains(keyword));
			}

			return query.Select(e => new EmployeeDto
			{
				EmployeeID = e.EmployeeId,
				EmployeeNo = e.EmployeeNo,
				Name = e.Name,
				Email = e.Email,
				Department = e.Department,
				Status = e.Status,
				Roles = e.EmployeeRoles.Select(er => er.Role.RoleName).ToList(),
				Permissions = e.EmployeeRoles
									.SelectMany(er => er.Role.RolePermissions.Select(rp => rp.Permission))
									.Select(p => p.PermissionCode)
									.Distinct()
									.ToList()
			}).ToList();
		}

		public (bool Success, string Message) CreateEmployee(string name, string email, string password, List<int> roleIds, int currentAdminId)
		{
			if (_context.Employees.Any(e => e.Email == email)) return (false, "建立失敗：此 Email 已經是內部員工！");

			var newEmp = new Employee
			{
				EmployeeNo = "EMP-" + DateTime.Now.ToString("yyMMdd") + new Random().Next(100, 999),
				Email = email,
				PasswordHash = HashPassword(password),
				Name = name,
				Department = "新進人員",
				Status = 1,
				CreatedAt = DateTime.Now
			};

			_context.Employees.Add(newEmp);
			_context.SaveChanges();

			foreach (var rId in roleIds)
			{
				_context.EmployeeRoles.Add(new EmployeeRole { EmployeeId = newEmp.EmployeeId, RoleId = rId, GrantedAt = DateTime.Now });
			}

			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = currentAdminId,
				Action = "system:employee:create",
				TargetResource = "Employees",
				TargetId = newEmp.EmployeeId.ToString(),
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功建立員工 {newEmp.Name}，並指派對應職務！");
		}

		private static string HashPassword(string password)
		{
			// ponytail: SHA256,夠用就好,等站上真的接了登入驗證再換成正式的雜湊演算法
			return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
		}

		public (bool Success, string Message) EditEmployeeRoles(int employeeId, List<int> roleIds, int currentAdminId)
		{
			var emp = _context.Employees.FirstOrDefault(e => e.EmployeeId == employeeId);
			if (emp == null) return (false, "修改失敗：找不到此員工！");
			if (emp.EmployeeId == 1) return (false, "拒絕存取：無法修改系統擁有者的角色配置！");

			var oldRoles = _context.EmployeeRoles.Where(er => er.EmployeeId == employeeId);
			_context.EmployeeRoles.RemoveRange(oldRoles);

			foreach (var rId in roleIds)
			{
				_context.EmployeeRoles.Add(new EmployeeRole { EmployeeId = emp.EmployeeId, RoleId = rId, GrantedAt = DateTime.Now });
			}

			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = currentAdminId,
				Action = "system:employee:update_roles",
				TargetResource = "EmployeeRoles",
				TargetId = emp.EmployeeId.ToString(),
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功更新 {emp.Name} 的職務配置！");
		}

		public (bool Success, string Message) DemoteEmployee(int employeeId, int currentAdminId)
		{
			if (employeeId == currentAdminId) return (false, "嚴重錯誤：您不能開除自己！");

			var emp = _context.Employees.FirstOrDefault(e => e.EmployeeId == employeeId);
			if (emp == null) return (false, "撤銷失敗：找不到此員工！");
			if (emp.EmployeeId == 1) return (false, "拒絕存取：無法撤銷系統擁有者的資格！");

			emp.Status = 2; // 停權

			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = currentAdminId,
				Action = "system:employee:disable",
				TargetResource = "Employees",
				TargetId = emp.EmployeeId.ToString(),
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功將 {emp.Name} 停權！");
		}

		// ==========================================
		// 角色 (Roles) 管理區塊
		// ==========================================
		public IEnumerable<RoleDto> GetAllRoles()
		{
			return _context.Roles
				.Include(r => r.RolePermissions)
					.ThenInclude(rp => rp.Permission)
				.Select(r => new RoleDto
				{
					RoleID = r.RoleId,
					RoleCode = r.RoleCode,
					RoleName = r.RoleName,
					Description = r.Description,
					Permissions = r.RolePermissions.Select(rp => rp.Permission.Description).ToList(),
					PermissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList()
				}).ToList();
		}

		public (bool Success, string Message) CreateRole(RoleDto dto, List<int> permissionIds, int currentAdminId)
		{
			if (_context.Roles.Any(r => r.RoleCode == dto.RoleCode))
				return (false, "建立失敗：角色代碼 (RoleCode) 已存在！");

			var role = new Role
			{
				RoleCode = dto.RoleCode,
				RoleName = dto.RoleName,
				Description = dto.Description
			};

			foreach (var pid in permissionIds)
			{
				role.RolePermissions.Add(new RolePermission { PermissionId = pid });
			}

			_context.Roles.Add(role);

			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = currentAdminId,
				Action = "system:role:create",
				TargetResource = "Roles",
				TargetId = dto.RoleCode,
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功建立新職務角色：{dto.RoleName}！");
		}

		public (bool Success, string Message) EditRole(int roleId, List<int> permissionIds, int currentAdminId)
		{
			var role = _context.Roles.Include(r => r.RolePermissions).FirstOrDefault(r => r.RoleId == roleId);
			if (role == null) return (false, "找不到此角色！");

			if (role.RoleId == 1) return (false, "拒絕存取：無法修改系統內建的最高擁有者角色！");

			_context.RolePermissions.RemoveRange(role.RolePermissions);
			foreach (var pid in permissionIds)
			{
				_context.RolePermissions.Add(new RolePermission { RoleId = role.RoleId, PermissionId = pid });
			}

			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = currentAdminId,
				Action = "system:role:update",
				TargetResource = "Roles",
				TargetId = role.RoleId.ToString(),
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功更新角色 [{role.RoleName}] 的權限配置！");
		}

		public (bool Success, string Message) DeleteRole(int roleId, int currentAdminId)
		{
			var role = _context.Roles.FirstOrDefault(r => r.RoleId == roleId);
			if (role == null) return (false, "找不到此角色！");
			if (role.RoleId == 1) return (false, "拒絕存取：無法刪除系統擁有者角色！");

			if (_context.EmployeeRoles.Any(er => er.RoleId == roleId))
				return (false, "刪除失敗：目前還有員工被指派為此角色，請先解除指派！");

			_context.Roles.Remove(role);

			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = currentAdminId,
				Action = "system:role:delete",
				TargetResource = "Roles",
				TargetId = role.RoleId.ToString(),
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功刪除角色：{role.RoleName}");
		}

		// ==========================================
		// 🌟 權限字典 (Permissions) CRUD 實作
		// ==========================================
		public IEnumerable<PermissionDto> GetAllPermissions(string keyword = null)
		{
			var query = _context.Permissions.AsNoTracking().AsQueryable();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				query = query.Where(p => p.PermissionCode.Contains(keyword) || p.ModuleName.Contains(keyword) || p.Description.Contains(keyword));
			}

			return query.Select(p => new PermissionDto
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
				return (false, "建立失敗：該權限代碼 (PermissionCode) 已存在！");

			var perm = new Permission
			{
				PermissionCode = dto.PermissionCode,
				ModuleName = dto.ModuleName,
				Description = dto.Description
			};

			_context.Permissions.Add(perm);

			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = currentAdminId,
				Action = "system:permission:create",
				TargetResource = "Permissions",
				TargetId = dto.PermissionCode,
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功新增底層權限：{dto.Description} ({dto.PermissionCode})！");
		}

		public (bool Success, string Message) EditPermission(PermissionDto dto, int currentAdminId)
		{
			var perm = _context.Permissions.FirstOrDefault(p => p.PermissionId == dto.PermissionID);
			if (perm == null) return (false, "修改失敗：找不到該權限！");

			perm.ModuleName = dto.ModuleName;
			perm.Description = dto.Description;

			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = currentAdminId,
				Action = "system:permission:update",
				TargetResource = "Permissions",
				TargetId = perm.PermissionCode,
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功更新權限：{perm.PermissionCode}！");
		}

		public (bool Success, string Message) DeletePermission(int permissionId, int currentAdminId)
		{
			var perm = _context.Permissions.Include(p => p.RolePermissions).FirstOrDefault(p => p.PermissionId == permissionId);
			if (perm == null) return (false, "刪除失敗：找不到該權限！");

			if (perm.RolePermissions.Any())
				return (false, $"刪除失敗：目前有 {perm.RolePermissions.Count} 個角色正包含此權限，請先從角色中拔除！");

			_context.Permissions.Remove(perm);

			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = currentAdminId,
				Action = "system:permission:delete",
				TargetResource = "Permissions",
				TargetId = perm.PermissionCode,
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功刪除權限：{perm.PermissionCode}");
		}

		// ==========================================
		// 稽核日誌區塊 (Audit Logs)
		// ==========================================
		public (IEnumerable<AdminLogDto> Data, int TotalCount) GetEmployeeAdminLogs(string keyword = null, int page = 1)
		{
			int pageSize = 10;
			var query = _context.AdminAuditLogs
				.Include(log => log.Employee)
				.Where(log => log.TargetResource == "Employees" || log.TargetResource == "EmployeeRoles")
				.AsNoTracking()
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				query = query.Where(log => (log.Employee != null && log.Employee.Name.Contains(keyword)) || log.Action.Contains(keyword) || log.TargetId.Contains(keyword));
			}

			int totalCount = query.Count();
			var logs = query
				.OrderByDescending(log => log.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(log => new AdminLogDto
				{
					LogID = log.LogId,
					EmployeeID = log.EmployeeId,
					AdminName = log.Employee != null ? log.Employee.Name : "系統",
					Action = log.Action,
					TargetResource = log.TargetResource,
					TargetID = log.TargetId,
					TargetMemberName = "員工編號: " + log.TargetId,
					Description = "員工異動: " + log.Action,
					IPAddress = log.IPAddress,
					CreatedAt = log.CreatedAt
				}).ToList();

			return (logs, totalCount);
		}

		public (IEnumerable<AdminLogDto> Data, int TotalCount) GetRoleAdminLogs(string keyword = null, int page = 1)
		{
			int pageSize = 10;
			var query = _context.AdminAuditLogs
				.Include(log => log.Employee)
				.Where(log => log.TargetResource == "Roles" || log.TargetResource == "Permissions")
				.AsNoTracking()
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				query = query.Where(log => (log.Employee != null && log.Employee.Name.Contains(keyword)) || log.Action.Contains(keyword) || log.TargetId.Contains(keyword));
			}

			int totalCount = query.Count();
			var logs = query
				.OrderByDescending(log => log.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(log => new AdminLogDto
				{
					LogID = log.LogId,
					EmployeeID = log.EmployeeId,
					AdminName = log.Employee != null ? log.Employee.Name : "系統",
					Action = log.Action,
					TargetResource = log.TargetResource,
					TargetID = log.TargetId,
					TargetMemberName = log.TargetId,
					Description = "架構異動: " + log.Action,
					IPAddress = log.IPAddress,
					CreatedAt = log.CreatedAt
				}).ToList();

			return (logs, totalCount);
		}
	}
}
