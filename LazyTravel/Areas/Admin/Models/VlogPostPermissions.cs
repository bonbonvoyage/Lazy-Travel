using LazyTravel.Models;

namespace LazyTravel.Areas.Admin.Models;

// 控制 Edit（小編）／Details（主管、系統管理員）兩個頁面要顯示哪些操作按鈕。
// 目前還沒有真的角色權限系統（Cookie 認證/RBAC 尚未接上），CanRestore 暫時對所有能看到
// Details 頁「已刪除」狀態的人開放（主管、系統管理員目前共用同一頁面），
// 之後角色權限接上後，把這裡換成「依登入者角色」判斷即可，計算邏輯不用大改。
public class VlogPostPermissions
{
    // Edit 頁（小編）
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }

    // Details 頁（主管、系統管理員）
    public bool CanApprove { get; set; }
    public bool CanReturnToDraft { get; set; }
    public bool CanDelete { get; set; }
    public bool CanViewReport { get; set; }
    public bool CanRestore { get; set; }

    public static VlogPostPermissions For(VlogPost post, bool hasPendingReport)
    {
        var permissions = new VlogPostPermissions
        {
            // 小編只能編輯／送審自己還沒送出的草稿；送審或已發布後要請主管「退回草稿」才能再編輯。
            CanEdit = !post.IsDelete && post.Status == VlogPostStatus.Draft,
        };
        permissions.CanSubmit = permissions.CanEdit;

        if (post.IsDelete)
        {
            permissions.CanRestore = true;
            return permissions;
        }

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
            permissions.CanApprove = true;
            permissions.CanReturnToDraft = true;
            permissions.CanDelete = true;
            return permissions;
        }

        // 已發布（或極少數主管直接開草稿 Details 頁的情況）
        permissions.CanReturnToDraft = post.Status != VlogPostStatus.Draft;
        permissions.CanDelete = true;
        return permissions;
    }
}
