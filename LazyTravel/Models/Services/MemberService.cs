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

		// ==========================================
		// 1.1 會員列表與進階檢索 (含分頁)
		// ==========================================
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

		// ==========================================
		// 1.2 & 1.4 取得單一會員詳細資料 (加入訂閱與登入稽核)
		// ==========================================
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
				Role = m.Role,
				CreatedAt = m.CreatedAt,
				CurrentPlanName = activeSub != null ? activeSub.Plan.PlanName : "免費會員",
				PlanExpiryDate = activeSub?.EndDate,
				LastLoginAt = m.LastLoginAt,
				LastLoginIp = m.LastLoginIp
			};
		}

		// ==========================================
		// 1.3 編輯會員資料 (變更為「重置」邏輯)
		// ==========================================
		public bool EditMember(MemberEditDto dto)
		{
			var member = _context.Members.FirstOrDefault(x => x.MemberId == dto.MemberID);
			if (member == null) return false;

			member.Status = dto.Status;

			if (dto.ResetName)
			{
				member.Name = "違規暱稱_請修改";
			}
			if (dto.ResetBio)
			{
				member.Bio = null;
			}
			if (dto.RemoveAvatar)
			{
				member.AvatarUrl = null;
			}

			var log = new LazyTravel.Models.EfModels.AdminLog
			{
				AdminId = 1, // 實際開發時從當前登入者抓取
				Action = dto.Status == 2 ? "停權會員" : "處分會員資料",
				TargetTable = "Members",
				TargetId = member.MemberId,
				Description = $"重置姓名:{dto.ResetName} | 清空簡介:{dto.ResetBio} | 移除頭像:{dto.RemoveAvatar} | 理由:{dto.AdminReason}",
				CreatedAt = DateTime.Now
			};
			_context.AdminLogs.Add(log);

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

		// ==========================================
		// 🌟 實作：取得會員模組的操作紀錄 (TargetTable 為 Members)
		// ==========================================
		public (IEnumerable<AdminLogDto> Data, int TotalCount) GetMemberAdminLogs(
			string adminKeyword = null,
			string targetKeyword = null,
			string action = null,
			int page = 1)
		{
			int pageSize = 10;

			// 1. 先抓出所有對 TargetTable = "Members" 的紀錄
			var query = _context.AdminLogs
				.Include(log => log.Admin)
				.Where(log => log.TargetTable == "Members")
				.AsNoTracking()
				.AsQueryable();

			// 2. 篩選：操作管理員姓名
			if (!string.IsNullOrWhiteSpace(adminKeyword))
			{
				query = query.Where(log => log.Admin != null && log.Admin.Name.Contains(adminKeyword));
			}

			// 3. 篩選：操作動作分類
			if (!string.IsNullOrWhiteSpace(action))
			{
				query = query.Where(log => log.Action == action);
			}

			// 4. 計算總筆數 (用於分頁)
			int totalCount = query.Count();

			// 5. 進行分頁並投影
			var logs = query
				.OrderByDescending(log => log.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			// 6. 轉換為 DTO，並透過 TargetId 反向關聯抓出「受處分會員的真實姓名」
			var result = logs.Select(log => {
				// 從 Members 表找出該 TargetId 的會員
				var targetMemberName = _context.Members
					.Where(m => m.MemberId == log.TargetId)
					.Select(m => m.Name)
					.FirstOrDefault() ?? "未知會員 (已刪除)";

				return new AdminLogDto
				{
					LogID = log.LogId,
					AdminID = log.AdminId,
					AdminName = log.Admin != null ? log.Admin.Name : "系統管理員",
					Action = log.Action,
					TargetID = log.TargetId,
					TargetMemberName = targetMemberName,
					Description = log.Description,
					IPAddress = log.Ipaddress,
					CreatedAt = log.CreatedAt
				};
			}).ToList();

			// 7. 若有搜尋「被處分會員姓名」，在前台拿到完整對象後進行最後一層過濾
			if (!string.IsNullOrWhiteSpace(targetKeyword))
			{
				result = result.Where(r => r.TargetMemberName.Contains(targetKeyword)).ToList();
				totalCount = result.Count; // 重新計算過濾後的總數
			}

			return (result, totalCount);
		}
	}
}