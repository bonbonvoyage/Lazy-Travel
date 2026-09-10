using Microsoft.AspNetCore.Http;

namespace LazyTravel.Shared.Models.DTOs
{
    // 會員主頁「檢舉」按鈕專用：畫面上固定四個中文選項讓使用者點選（不是自己打字），
    // 見 scene.dc.html 的 REPORT_REASONS，Reason 這裡就是直接傳其中一個選項的原文，
    // 後端（MembersController.ReportMember）再把它對應到 ReportReasonCategory 這個分類 enum
    // （純粹是給後台篩選用的分類標籤，跟 Reason 這個必填的檢舉原因文字是兩回事，兩個都會存）。
    //
    // Evidence 是選填的檢舉截圖佐證（跟期中版本 /Report/Create 的 ReportSubmitViewModel.Evidence
    // 是同一種用法），因為多了檔案欄位，MembersController.ReportMember 已經改成收
    // multipart/form-data（[FromForm]），不能再用 JSON body 送出，scene.dc.html 那邊的
    // submitReportAction 也跟著改成用 FormData。
    public class ReportMemberDto
    {
        public int TargetMemberId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public IFormFile? Evidence { get; set; }
    }
}
