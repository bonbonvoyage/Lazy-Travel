using LazyTravel.Models.DTOs;
using LazyTravel.Models.EfModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LazyTravel.Models.Services
{
	public class MemberService : IMemberService
	{
		private readonly LazyTravelDBContext _context;

		public MemberService(LazyTravelDBContext context)
		{
			_context = context;
		}

		public (IEnumerable<MemberDto> Data, int TotalCount) GetAllMembers(
			string keyword = null,
			byte? status = null,
			byte? gender = null,
			int page = 1)
		{
			int pageSize = 10;

			var query = _context.Members
				.Include(m => m.MemberSubscriptions)
				.ThenInclude(ms => ms.Plan)
				.AsNoTracking()
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				query = query.Where(m =>
					(m.Name != null && m.Name.Contains(keyword)) ||
					(m.Email != null && m.Email.Contains(keyword)));
			}

			if (status.HasValue)
			{
				query = query.Where(m => m.Status == status.Value);
			}

			if (gender.HasValue)
			{
				query = query.Where(m => m.Gender == gender.Value);
			}

			int totalCount = query.Count();

			var pagedData = query
				.OrderByDescending(m => m.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(m => new MemberDto
				{
					MemberID = m.MemberId,
					Email = m.Email,
					Name = m.Name,
					Gender = m.Gender,
					Age = m.BirthDate.HasValue ? (DateTime.Now.Year - m.BirthDate.Value.Year) : null,
					Occupation = m.Occupation,
					MBTI = m.Mbti,
					Status = m.Status,
					CreatedAt = m.CreatedAt,
					PlanName = m.MemberSubscriptions
								.Where(sub => sub.Status == 1 && sub.EndDate >= DateTime.Now)
								.Select(sub => sub.Plan.PlanName)
								.FirstOrDefault() ?? "免費會員"
				}).ToList();

			return (pagedData, totalCount);
		}

		public MemberDetailDto GetMemberDetail(int id)
		{
			var m = _context.Members
				.Include(x => x.MemberSubscriptions)
				.ThenInclude(ms => ms.Plan)
				.AsNoTracking()
				.FirstOrDefault(x => x.MemberId == id);

			if (m == null) return null;

			var activeSub = m.MemberSubscriptions
				.Where(sub => sub.Status == 1 && sub.EndDate >= DateTime.Now)
				.OrderByDescending(sub => sub.EndDate)
				.FirstOrDefault();

			return new MemberDetailDto
			{
				MemberID = m.MemberId,
				Email = m.Email,
				Name = m.Name,
				Phone = m.Phone,
				LineId = m.LineId,
				InstagramUrl = m.InstagramUrl,
				FacebookUrl = m.FacebookUrl,
				IsEmailConfirmed = m.IsEmailConfirmed,
				AvatarUrl = m.AvatarUrl,
				BirthDate = m.BirthDate,
				Gender = m.Gender,
				Occupation = m.Occupation,
				MBTI = m.Mbti,
				Bio = m.Bio,
				Status = m.Status,
				// 🌟 移除 Role 綁定
				CreatedAt = m.CreatedAt,
				CurrentPlanName = activeSub != null ? activeSub.Plan.PlanName : "免費會員",
				PlanExpiryDate = activeSub?.EndDate,
				LastLoginAt = m.LastLoginAt,
				LastLoginIp = m.LastLoginIp
			};
		}

		public bool EditMember(MemberEditDto dto)
		{
			var member = _context.Members.FirstOrDefault(x => x.MemberId == dto.MemberID);
			if (member == null) return false;

			// 判斷目前的狀態，決定寫入什麼 Action 到日誌中
			string actionCode = "";

			if (dto.Status == 2 && member.Status != 2)
			{
				actionCode = "member:account:block";
			}
			else if (dto.Status == 1 && member.Status == 2)
			{
				actionCode = "member:account:unblock";
			}
			else
			{
				actionCode = "member:account:update";
			}

			// 更新資料庫
			member.Status = dto.Status;
			if (dto.ResetName) member.Name = "違規暱稱_請修改";
			if (dto.ResetBio) member.Bio = null;
			if (dto.RemoveAvatar) member.AvatarUrl = null;

			// 🌟 關鍵修改：將管理員填寫的「操作原因」 (dto.AdminReason) 也寫入日誌的某個欄位
			// 由於目前 AdminAuditLog 資料表沒有專門存「原因」的欄位
			// 我們可以把它附加在 TargetId 後面，或者在我們將來擴充資料表時存入 Description 欄位
			// 目前的變通作法：把它存在 TargetId 欄位，用逗號分隔 (例如 "133, 測試原因")
			// 更好的做法：如果可以，請修改資料庫新增一個 Reason 欄位。這裡我先用 TargetId 欄位來示範。
			string logTargetId = $"{member.MemberId}";
			if (!string.IsNullOrWhiteSpace(dto.AdminReason))
			{
				logTargetId = $"{member.MemberId}|{dto.AdminReason}";
			}

			var log = new AdminAuditLog
			{
				EmployeeId = 1,
				Action = actionCode,
				TargetResource = "Members",
				TargetId = logTargetId, // 🌟 這裡把原因也包進去了
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now
			};
			_context.AdminAuditLogs.Add(log);

			if (dto.SendNotification)
			{
				var notification = new Notification
				{
					MemberId = member.MemberId,
					Type = 1,
					Content = $"您的帳號資料因違反社群規範已被系統管理員變更。原因：{dto.AdminReason}。若有疑問請聯繫客服。",
					IsRead = false,
					CreatedAt = DateTime.Now
				};
				_context.Notifications.Add(notification);
			}

			_context.SaveChanges();
			return true;
		}

		public (IEnumerable<AdminLogDto> Data, int TotalCount) GetMemberAdminLogs(
			string adminKeyword = null,
			string targetKeyword = null,
			string action = null,
			int page = 1)
		{
			int pageSize = 10;

			var query = _context.AdminAuditLogs
				.Include(log => log.Employee)
				.Where(log => log.TargetResource == "Members")
				.AsNoTracking()
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(adminKeyword))
			{
				query = query.Where(log => log.Employee != null && log.Employee.Name.Contains(adminKeyword));
			}

			if (!string.IsNullOrWhiteSpace(action))
			{
				if (action == "停權會員")
				{
					query = query.Where(log => log.Action == "member:account:block");
				}
				else if (action == "處分會員資料")
				{
					query = query.Where(log => log.Action == "member:account:update" || log.Action == "member:account:unblock");
				}
			}

			int totalCount = query.Count();

			var logs = query
				.OrderByDescending(log => log.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			var result = logs.Select(log => {

				// 🌟 解析我們剛才包在 TargetId 裡面的會員 ID 和原因
				string rawTargetId = log.TargetId ?? "";
				string memberIdStr = rawTargetId;
				string reason = "";

				if (rawTargetId.Contains("|"))
				{
					var parts = rawTargetId.Split('|');
					memberIdStr = parts[0];
					reason = parts.Length > 1 ? parts[1] : "";
				}

				var targetMemberName = "未知會員";
				if (int.TryParse(memberIdStr, out int memberId))
				{
					targetMemberName = _context.Members
						.Where(m => m.MemberId == memberId)
						.Select(m => m.Name)
						.FirstOrDefault() ?? "未知會員 (已刪除)";
				}

				string friendlyAction = log.Action;
				string friendlyDesc = "修改會員狀態或資料";

				if (log.Action == "member:account:block")
				{
					friendlyAction = "停權會員";
					friendlyDesc = "強制停權 (鎖定發文與互動權限)";
				}
				else if (log.Action == "member:account:unblock")
				{
					friendlyAction = "解除停權";
					friendlyDesc = "恢復帳號正常使用權限";
				}
				else if (log.Action == "member:account:update")
				{
					friendlyAction = "處分會員資料";
					friendlyDesc = "強制重置違規欄位 (姓名、簡介或大頭貼)";
				}

				// 🌟 將原因附加到 Description 的最後面
				if (!string.IsNullOrWhiteSpace(reason))
				{
					friendlyDesc = $"{friendlyDesc}。備註：{reason}";
				}

				return new AdminLogDto
				{
					LogID = log.LogId,
					EmployeeID = log.EmployeeId,
					AdminName = log.Employee != null ? log.Employee.Name : "系統",
					Action = friendlyAction,
					TargetResource = log.TargetResource,
					TargetID = memberIdStr, // 回傳乾淨的 ID 給前端
					TargetMemberName = targetMemberName,
					Description = friendlyDesc, // 🌟 包含原因的完整描述
					IPAddress = log.IPAddress,
					CreatedAt = log.CreatedAt
				};
			}).ToList();

			if (!string.IsNullOrWhiteSpace(targetKeyword))
			{
				result = result.Where(r => r.TargetMemberName.Contains(targetKeyword)).ToList();
				totalCount = result.Count;
			}

			return (result, totalCount);
		}
	}
}