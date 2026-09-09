using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.EfModels;
using System.Security.Claims; // 必須引用這個來讀取使用者的 Claims

namespace LazyTravel.Areas.Admin.Models;

// 控制 Edit（小編）／Details（主管、系統管理員）兩個頁面要顯示哪些操作按鈕。
// 依「官方文章」(MemberLookup.IsOfficial) 或「會員文章」分成兩套完全不同的流程：
//   官方文章：小編編輯/送審自己的草稿，主管審核/退回草稿/處理檢舉，走原本的 Draft→PendingReview→Published 流程。
//   會員文章：不是小編寫的，沒有草稿/送審/退回草稿這件事——小編只能檢視+提出檢舉，
//             主管只在「有檢舉」的時候出手：查看檢舉、判定不成立、或直接刪除。
// 目前還沒有真的角色權限系統（Cookie 認證/RBAC 尚未接上），這裡的區分完全靠「是不是官方文章」
// 加上頁面本身（Edit=小編、Details=主管）done，不是靠「登入者是誰」，之後角色權限接上後這裡不用大改。
public class VlogPostPermissions
{
    // 這篇是官方文章還是會員文章，View 上可以用來顯示標籤/決定文案
    public bool IsOfficial { get; set; }

    // Edit 頁（小編，僅官方文章）
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }

    // Details 頁（主管，僅官方文章）
    public bool CanApprove { get; set; }
    public bool CanReturnToDraft { get; set; }

    // Details 頁（主管，官方+會員文章都可能用到）
    public bool CanDelete { get; set; }
    public bool CanViewReport { get; set; }
    public bool CanRestore { get; set; }

    // Details 頁（主管，僅會員文章的檢舉處理）
    public bool CanDismissReport { get; set; }

    // Details 頁（小編，僅會員文章）
    public bool CanReport { get; set; }

	public static VlogPostPermissions For(VlogPost post, bool hasPendingReport, ClaimsPrincipal user)
	{
		var isOfficial = MemberLookup.IsOfficial(post.Member);
		var permissions = new VlogPostPermissions { IsOfficial = isOfficial };

		// 讀取當前使用者的權限 (包含超級管理員的萬能鑰匙)
		bool hasSuperAdmin = user.HasClaim("Permission", "ROLE_SUPER_ADMIN");
		bool hasCreate = user.HasClaim("Permission", "content:vlog:create") || hasSuperAdmin;
		bool hasUpdate = user.HasClaim("Permission", "content:vlog:update") || hasSuperAdmin;
		bool hasDelete = user.HasClaim("Permission", "content:vlog:delete") || hasSuperAdmin;
		bool hasSubmit = user.HasClaim("Permission", "content:vlog:submit") || hasSuperAdmin;
		bool hasRestore = user.HasClaim("Permission", "content:vlog:restore") || hasSuperAdmin;
		bool hasPublish = user.HasClaim("Permission", "content:vlog:publish") || hasSuperAdmin;
		bool hasReturn = user.HasClaim("Permission", "content:vlog:return") || hasSuperAdmin;
		// 🌟 1. 抓取新的檢舉權限
		bool hasAudit = user.HasClaim("Permission", "content:vlog:audit") || hasSuperAdmin;
		// 🌟 2. 抓取檢舉中心的審核與觀看權限 (用來控制查看檢舉、檢舉不成立按鈕)
		bool hasReportRead = user.HasClaim("Permission", "content:report:read") || hasSuperAdmin;
		bool hasReportAudit = user.HasClaim("Permission", "content:report:audit") || hasSuperAdmin;

		// 如果是已刪除的文章
		if (post.IsDelete)
		{
			permissions.CanRestore = hasRestore;
			return permissions;
		}

		// 如果是「會員文章」
		if (!isOfficial)
		{
			// 🌟 會員文章邏輯：
			if (hasPendingReport)
			{
				// 如果已經被檢舉(待處理)，顯示查看檢舉、檢舉不成立、刪除文章
				permissions.CanViewReport = hasReportRead;
				permissions.CanDismissReport = hasReportAudit;
				permissions.CanDelete = hasReportAudit;
			}
			else
			{
				// 如果還沒被檢舉，根據 content:vlog:audit 決定是否顯示檢舉表單
				permissions.CanReport = hasAudit;
			}
			return permissions;
		}

		// ---------- 以下都是官方文章 ----------

		if (post.Status == VlogPostStatus.Draft)
		{
			permissions.CanEdit = hasUpdate;
			permissions.CanSubmit = hasSubmit;
			//permissions.CanDelete = hasDelete;
			return permissions;
		}

		if (post.Status == VlogPostStatus.PendingReview)
		{
			// 待審核狀態，只有具備對應權限的主管才能看到按鈕
			permissions.CanApprove = hasPublish;
			permissions.CanReturnToDraft = hasReturn;
			return permissions;
		}

		if (post.Status == VlogPostStatus.Published)
		{
			// 已發布文章，可以設定主管能退回或刪除
			permissions.CanReturnToDraft = hasReturn;
			permissions.CanDelete = hasDelete;
		}

		return permissions;
	}
}