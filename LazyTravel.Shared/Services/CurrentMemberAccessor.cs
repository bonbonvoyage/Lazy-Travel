using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace LazyTravel.Shared.Services
{
    public class CurrentMemberAccessor : ICurrentMemberAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentMemberAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetCurrentMemberId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            // 正式登入之後：SignInManager<Member> 登入時，Identity 預設會把
            // MemberId 放進 ClaimTypes.NameIdentifier 這個 claim 裡，這裡直接讀。
            // 等組長把登入串上，這段完全不用改，也不需要知道他怎麼實作登入的，
            // 只要他是用標準的 Identity Cookie 驗證登入就會自動接得上。
            var claim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var id))
            {
                return id;
            }

            // 🚧🚧🚧 開發期暫時的後門 🚧🚧🚧
            // 登入功能還沒做好之前，允許在網址後面帶 ?devMemberId=1 假裝自己是
            // 某個會員，方便先把個人頁的後端邏輯串起來測試。
            // 只在 ASPNETCORE_ENVIRONMENT=Development 時生效（這裡直接讀環境變數，
            // 不依賴 IWebHostEnvironment——LazyTravel.Shared 這個類別庫沒有參考到
            // ASP.NET Core 的 Hosting 套件，用 IWebHostEnvironment 會編譯失敗）。
            // 等組長把真正的登入接上之後，這一段（連同這個 if 區塊）要整段刪掉。
            var isDevelopment = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase);

            if (isDevelopment)
            {
                var devIdRaw = _httpContextAccessor.HttpContext?.Request.Query["devMemberId"].ToString();
                if (int.TryParse(devIdRaw, out var devId))
                {
                    return devId;
                }
            }

            return null;
        }
    }
}
