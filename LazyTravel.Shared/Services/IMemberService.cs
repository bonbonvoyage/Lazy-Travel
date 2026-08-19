using LazyTravel.Models.DTOs;
using System.Collections.Generic;

namespace LazyTravel.Services
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
		// currentAdminId:實際執行處分的員工,會寫進 AdminLogs 的操作人欄位
		bool EditMember(MemberEditDto dto, int currentAdminId);

		// 🌟 新增：記錄調閱會員個資的日誌
		void LogPiiUnmask(int memberId, int currentAdminId, string adminIp);

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