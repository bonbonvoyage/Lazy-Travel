using System.ComponentModel.DataAnnotations;
using LazyTravel.Models;
using Microsoft.AspNetCore.Http;

namespace LazyTravel.ViewModels
{
    // 前台送出檢舉的表單資料。目前前台還沒有登入系統,也還沒有 Vlog/會員/揪團的瀏覽頁,
    // 所以檢舉人帳號、被檢舉對象都先用手動輸入 Email/文字,等對應頁面做出來後可以改成自動帶入。
    public class ReportSubmitViewModel
    {
        [Required(ErrorMessage = "請輸入你的帳號 Email")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        [Display(Name = "你的帳號 Email")]
        public string ReporterEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇檢舉類型")]
        [Display(Name = "檢舉類型")]
        public ReportTargetType TargetType { get; set; } = ReportTargetType.Member;

        [Required(ErrorMessage = "請輸入被檢舉會員的帳號 Email")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        [Display(Name = "被檢舉會員帳號")]
        public string ReportedMemberEmail { get; set; } = string.Empty;

        // 檢舉「Vlog文章」或「揪團」時,填被檢舉內容的編號跟名稱(目前沒有瀏覽頁可以自動帶入,先手動填)
        [Display(Name = "被檢舉內容編號")]
        public int? TargetId { get; set; }

        [StringLength(200)]
        [Display(Name = "被檢舉內容名稱")]
        public string? TargetTitle { get; set; }

        [Display(Name = "檢舉類別")]
        public ReportReasonCategory ReasonCategory { get; set; } = ReportReasonCategory.Other;

        [Required(ErrorMessage = "請輸入檢舉原因")]
        [StringLength(200, ErrorMessage = "檢舉原因不可超過 200 字")]
        [Display(Name = "檢舉原因")]
        public string Reason { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "補充說明不可超過 500 字")]
        [Display(Name = "補充說明")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "請上傳檢舉截圖")]
        [Display(Name = "檢舉截圖")]
        public IFormFile? Evidence { get; set; }
    }
}
