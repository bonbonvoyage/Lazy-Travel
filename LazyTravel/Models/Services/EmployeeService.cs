using LazyTravel.Models.EfModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LazyTravel.Models.Services
{
	public class EmployeeService : IEmployeeService
	{
		private readonly LazyTravelDBContext _context;

		public EmployeeService(LazyTravelDBContext context)
		{
			_context = context;
		}

		public IEnumerable<EmployeeDto> GetAllEmployees()
		{
			var employees = _context.Members
				.Where(m => m.Role >= 1) // 只要管理員 (Role=1) 與超級管理員 (Role=2)
				.Select(m => new EmployeeDto
				{
					MemberID = m.MemberId,
					Name = m.Name,
					Email = m.Email,
					Role = m.Role,
					// 利用 EF Core 直接關聯抓取該管理員的所有權限代碼
					Permissions = _context.AdminPermissions
									.Where(p => p.AdminId == m.MemberId)
									.Select(p => p.PermissionCode)
									.ToList()
				})
				.ToList();

			return employees;
		}

		public (bool Success, string Message) PromoteToAdmin(string email, List<string> permissions, int currentAdminId)
		{
			var member = _context.Members.FirstOrDefault(m => m.Email == email);

			// 嚴格的商業邏輯防呆
			if (member == null) return (false, "指派失敗：找不到此 Email 的會員！");
			if (member.Role >= 1) return (false, "指派失敗：此會員已經是管理員或系統擁有者！");
			if (member.Status != 1) return (false, "指派失敗：此會員目前被停權，無法升級為管理員！");

			member.Role = 1; // 升級為一般管理員

			// 寫入全新權限
			foreach (var p in permissions)
			{
				_context.AdminPermissions.Add(new AdminPermission
				{
					AdminId = member.MemberId,
					PermissionCode = p,
					CreatedAt = DateTime.Now
				});
			}

			_context.AdminLogs.Add(new AdminLog
			{
				AdminId = currentAdminId,
				Action = "指派新員工",
				TargetTable = "Members",
				TargetId = member.MemberId,
				Description = $"將會員 {member.Email} 升級為管理員，賦予權限群組：{string.Join(", ", permissions)}",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功將 {member.Name} 升級為管理員並發配權限！");
		}

		public (bool Success, string Message) EditPermissions(int adminId, List<string> permissions, int currentAdminId)
		{
			var admin = _context.Members.FirstOrDefault(m => m.MemberId == adminId && m.Role >= 1);
			if (admin == null) return (false, "修改失敗：找不到此管理員！");
			if (admin.Role == 2) return (false, "拒絕存取：系統擁有者(超級管理員)擁有全站最高權限，不需也無法修改！");

			// 刪除舊有權限
			var oldPermissions = _context.AdminPermissions.Where(p => p.AdminId == adminId);
			_context.AdminPermissions.RemoveRange(oldPermissions);

			// 寫入新勾選的權限
			foreach (var p in permissions)
			{
				_context.AdminPermissions.Add(new AdminPermission
				{
					AdminId = admin.MemberId,
					PermissionCode = p,
					CreatedAt = DateTime.Now
				});
			}

			_context.AdminLogs.Add(new AdminLog
			{
				AdminId = currentAdminId,
				Action = "修改員工權限",
				TargetTable = "AdminPermissions",
				TargetId = admin.MemberId,
				Description = $"修改管理員 {admin.Name} 的權限群組為：{string.Join(", ", permissions)}",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功更新 {admin.Name} 的權限！");
		}

		public (bool Success, string Message) DemoteAdmin(int targetAdminId, int currentAdminId)
		{
			// 防呆：阻擋開除自己 (防止死鎖)
			if (targetAdminId == currentAdminId) return (false, "嚴重錯誤：您不能撤銷自己的管理員資格！");

			var admin = _context.Members.FirstOrDefault(m => m.MemberId == targetAdminId && m.Role >= 1);
			if (admin == null) return (false, "撤銷失敗：找不到此管理員！");
			if (admin.Role == 2) return (false, "拒絕存取：無法撤銷系統擁有者的資格！");

			admin.Role = 0; // 降級回一般會員

			// 刪除其所有權限明細 (雖然關聯設定 Cascade Delete，但顯式刪除更保險)
			var permissions = _context.AdminPermissions.Where(p => p.AdminId == targetAdminId);
			_context.AdminPermissions.RemoveRange(permissions);

			_context.AdminLogs.Add(new AdminLog
			{
				AdminId = currentAdminId,
				Action = "撤銷員工資格",
				TargetTable = "Members",
				TargetId = admin.MemberId,
				Description = $"已撤銷 {admin.Name} 的管理員資格，降級為一般會員",
				CreatedAt = DateTime.Now
			});

			_context.SaveChanges();
			return (true, $"成功撤銷 {admin.Name} 的管理員資格！");
		}
	}
}