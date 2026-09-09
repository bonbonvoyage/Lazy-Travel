using System.ComponentModel.DataAnnotations;
using LazyTravel.Models;

namespace LazyTravel.ViewModels
{
    // Judge 表單送出的資料。官方 Reports.AdminNotes 欄位是 nvarchar(500),
    // 這裡刻意收緊到 200 字(業務規則,比資料庫欄位更嚴格),資料庫欄位本身不用改
    public class JudgeRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請選擇判定結果")]
        public ReportStatus Decision { get; set; }

        [StringLength(200, ErrorMessage = "處置備註不可超過 200 字")]
        public string? Note { get; set; }

        // 只有判定「不成立」時才有意義,判定成立時忽略
        public bool IsMalicious { get; set; }

        // 從 Details 頁帶回來的篩選條件,判定完導回 Details 時要原樣帶著走,不能弄丟
        public string? ReturnUrl { get; set; }
    }
}
