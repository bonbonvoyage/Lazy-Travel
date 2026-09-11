using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LazyTravel.Shared.Services
{
    // R2 帳號金鑰還沒建好之前的替代方案：直接存進網站自己的 wwwroot/uploads/{folder}/，
    // 跟 VlogPostImageUploadService 註解裡提到的舊圖存放方式是同一套做法。
    //
    // 注意：這個類別庫（LazyTravel.Shared）沒有參考 ASP.NET Core 的 Hosting 套件，
    // 所以不能注入 IWebHostEnvironment——wwwroot 的實際路徑改成用建構子參數
    // （webRootPath）直接傳進來，由 tours-api 的 Program.cs 註冊時提供
    // （用 builder.Environment.WebRootPath，那邊是 Web 專案，可以正常拿到）。
    //
    // 等組長把 Cloudflare R2 的正式帳號、appsettings 的
    // CloudflareR2:AccessKey / SecretKey / ServiceUrl 都填好之後，
    // 只要在 Program.cs 把註冊的實作換成 R2ImageStorageService，
    // 其他程式碼（MemberProfileService、MembersController）完全不用動。
    public class LocalImageStorageService : IImageStorageService
    {
        private readonly string _webRootPath;

        // 開發期的基本把關：只收圖片、限制大小，避免有人亂丟檔案進 wwwroot。
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public LocalImageStorageService(string webRootPath)
        {
            _webRootPath = webRootPath;
        }

        public async Task<string> UploadAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("沒有收到檔案內容。");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException("檔案太大，上限 5MB。");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                throw new ArgumentException("不支援的檔案格式，只接受 jpg/png/webp/gif。");
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var folderPath = Path.Combine(_webRootPath, "uploads", folder);
            Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, fileName);
            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 網站相對路徑，配合 Program.cs 已經有的 app.UseStaticFiles() 就能直接當
            // <img src> 用，不用管現在開發機是哪個 host/port。
            return $"/uploads/{folder}/{fileName}";
        }
    }
}
