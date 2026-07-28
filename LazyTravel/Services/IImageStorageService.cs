using Microsoft.AspNetCore.Http;

namespace LazyTravel.Services
{
    // 圖片儲存服務,把使用者上傳的圖片存到雲端(Cloudflare R2),回傳可以直接在畫面上使用的公開網址。
    // 不管誰在自己電腦上跑這個網站,存進去的圖片大家都看得到,不會有「只有上傳的人自己電腦看得到」的問題。
    public interface IImageStorageService
    {
        // folder 用來分類存放位置,例如 "reports"(檢舉截圖)、"vlog"(Vlog 文章圖片)
        Task<string> UploadAsync(IFormFile file, string folder);
    }
}
