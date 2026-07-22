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

			member.Status = dto.Status;

			if (dto.ResetName) member.Name = "違規暱稱_請修改";
			if (dto.ResetBio) member.Bio = null;
			if (dto.RemoveAvatar) member.AvatarUrl = null;

			// 🌟 改為寫入新的企業級 AdminAuditLogs
			var log = new AdminAuditLog
			{
				EmployeeId = 1, // 實際開發時從當前登入者抓取
				Action = dto.Status == 2 ? "member:account:block" : "member:account:update",
				TargetResource = "Members",
				TargetId = member.MemberId.ToString(),
				Ipaddress = "127.0.0.1", // 暫時寫死，後續可從 HttpContext 抓
				CreatedAt = DateTime.Now
			};
			_context.AdminAuditLogs.Add(log);

			if (dto.SendNotification)
			{
				var notification = new Notification
				{
					MemberId = member.MemberId,
					Type = 1,
					Content = $"您的帳號資料因違反社群規範已被系統重置。原因：{dto.AdminReason}。請盡速登入修改。",
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

			// 🌟 改為查詢 AdminAuditLogs，並 Include Employee 表
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
				// 新架構的 action 可能是 member:account:block
				query = query.Where(log => log.Action.Contains(action));
			}

			int totalCount = query.Count();

			var logs = query
				.OrderByDescending(log => log.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			var result = logs.Select(log => {
				var targetMemberName = "未知會員";
				if (int.TryParse(log.TargetId, out int memberId))
				{
					targetMemberName = _context.Members
						.Where(m => m.MemberId == memberId)
						.Select(m => m.Name)
						.FirstOrDefault() ?? "未知會員 (已刪除)";
				}

				return new AdminLogDto
				{
					LogID = log.LogId,
					EmployeeID = log.EmployeeId,
					AdminName = log.Employee != null ? log.Employee.Name : "系統",
					Action = log.Action,
					TargetResource = log.TargetResource,
					TargetID = log.TargetId,
					TargetMemberName = targetMemberName,
					Description = "透過新架構執行: " + log.Action,
					IPAddress = log.Ipaddress,
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