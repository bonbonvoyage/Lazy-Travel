using LazyTravel.Models;
using LazyTravel.Models.EfModels;

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

    public static VlogPostPermissions For(VlogPost post, bool hasPendingReport)
    {
        var isOfficial = MemberLookup.IsOfficial(post.MemberId);
        var permissions = new VlogPostPermissions { IsOfficial = isOfficial };

        if (post.IsDelete)
        {
            permissions.CanRestore = true;
            return permissions;
        }

        if (!isOfficial)
        {
            // 會員文章：不是小編寫的，沒有草稿/送審/退回草稿這條路。
            // 小編永遠可以提出檢舉；主管只有在「有檢舉待處理」時才出手：查看檢舉、判不成立、或刪除。
            permissions.CanReport = true;

            if (hasPendingReport)
            {
                permissions.CanViewReport = true;
                permissions.CanDismissReport = true;
                permissions.CanDelete = true;
            }

            return permissions;
        }

        // ---------- 以下都是官方文章 ----------

        // 小編只能編輯／送審自己還沒送出的草稿；送審或已發布後要請主管「退回草稿」才能再編輯。
        permissions.CanEdit = post.Status == VlogPostStatus.Draft;
        permissions.CanSubmit = permissions.CanEdit;

        if (hasPendingReport)
        {
            // 有檢舉待處理時，優先處理檢舉，不顯示審核通過（避免跟檢舉判定衝突）
            permissions.CanViewReport = true;
            permissions.CanReturnToDraft = true;
            permissions.CanDelete = true;
            return permissions;
        }

        if (post.Status == VlogPostStatus.PendingReview)
        {
            // 送審中：主管只能通過或退回，不給直接刪除（避免刪掉小編還在等審核的成果）
            permissions.CanApprove = true;
            permissions.CanReturnToDraft = true;
            return permissions;
        }

        if (post.Status == VlogPostStatus.Published)
        {
            permissions.CanReturnToDraft = true;
            permissions.CanDelete = true;
        }

        return permissions;
    }
}
