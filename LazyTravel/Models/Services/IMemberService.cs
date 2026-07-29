using LazyTravel.Models.DTOs;
using System.Collections.Generic;

namespace LazyTravel.Models.Services
{
	public interface IMemberService
	{
		// ==========================================
		// 1.1 會員列表與進階檢索 (含分頁)
		// ==========================================
		(IEnumerable<MemberDto> Data, int TotalCount) GetAllMembers(
			string keyword = null,
			byte? status = null,
			byte? gender = null,
			int page = 1);

		// ==========================================
		// 1.2 取得單一會員詳細資料
		// ==========================================
		MemberDetailDto GetMemberDetail(int id);

		// ==========================================
		// 1.3 編輯會員資料與狀態
		// ==========================================
		bool EditMember(MemberEditDto dto);

		// ==========================================
		// 🌟 新增：取得會員專屬的操作日誌 (支援獨立搜尋與分頁)
		// ==========================================
		(IEnumerable<AdminLogDto> Data, int TotalCount) GetMemberAdminLogs(
			string adminKeyword = null,
			string targetKeyword = null,
			string action = null,
			int page = 1);
	}
}