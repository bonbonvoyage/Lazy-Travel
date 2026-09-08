using System;
using System.Text.RegularExpressions;

namespace LazyTravel.Shared.Services
{
    // 會員可以在自己的個人頁編輯 LINE ID／Instagram／Facebook 這三個聯絡方式，
    // 這三個是使用者自己貼上來的文字，後端不能照單全收直接存進資料庫：
    //   - LINE ID：只做基本格式檢查（長度、允許的字元），擋掉明顯不是 LINE ID 的亂打內容。
    //   - Instagram／Facebook：必須是真的指向 instagram.com／facebook.com 的
    //     http(s) 網址，不接受 javascript:、data: 這類危險 scheme，也不接受隨便
    //     一個外部網站的網址——避免有人把釣魚網站包裝成「我的 IG 主頁」，通訊錄
    //     那邊看到的人點下去會被騙去假網站。
    //     單純「點一個連結」本身不會讓瀏覽器中毒（瀏覽器不會因為導覽就自動執行
    //     任何程式或下載任何東西），這裡真正要防的風險是「連結指向的網站是假
    //     的、用來釣魚」，所以用網域白名單擋，而不是嘗試偵測「病毒」。
    //
    // 前端（scene.dc.html 的 validateSocialUrl／validateLineId）用同一套規則先擋
    // 一次，只是為了讓使用者馬上看到哪裡填錯、不用等一趟網路來回；那邊不是唯一
    // 防線，真正擋得住的是這裡——就算有人繞過畫面直接打 API，這裡還是會擋下來。
    public static class SocialLinkValidator
    {
        private static readonly string[] InstagramHosts = { "instagram.com", "www.instagram.com" };
        private static readonly string[] FacebookHosts = { "facebook.com", "www.facebook.com", "m.facebook.com", "fb.com", "www.fb.com" };

        // raw 是 null／空白：代表使用者清空這個欄位，視為合法，normalized 給 null。
        public static bool TryNormalizeSocialUrl(string? raw, bool isInstagram, out string? normalized, out string? error)
        {
            normalized = null;
            error = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            var trimmed = raw.Trim();
            if (trimmed.Length > 300)
            {
                error = "網址太長了，麻煩確認一下貼的內容。";
                return false;
            }

            // 使用者可能只貼帳號本身或省略開頭的 https://，這裡補上 scheme 再解析，
            // 跟前端 prettySocialLink()／validateSocialUrl() 的邏輯一致。
            var withScheme = trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                              trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? trimmed
                : "https://" + trimmed.TrimStart('/');

            if (!Uri.TryCreate(withScheme, UriKind.Absolute, out var uri))
            {
                error = "看起來不是一個有效的網址。";
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                error = "網址只能是 http 或 https 開頭。";
                return false;
            }

            var allowedHosts = isInstagram ? InstagramHosts : FacebookHosts;
            var host = uri.Host.ToLowerInvariant();
            var hostOk = Array.Exists(allowedHosts, h => host == h);
            if (!hostOk)
            {
                error = isInstagram
                    ? "請貼 instagram.com 的個人主頁網址。"
                    : "請貼 facebook.com 的個人主頁網址。";
                return false;
            }

            normalized = uri.ToString();
            return true;
        }

        // LINE 官方 ID 規則大致是 4~20 個半形英數字/底線/句點/連字號，這裡做同樣
        // 寬鬆的檢查就好，不用做到跟 LINE 一模一樣（這欄位只是拿來顯示，不是真的
        // 去驗證帳號存在）。
        public static bool TryNormalizeLineId(string? raw, out string? normalized, out string? error)
        {
            normalized = null;
            error = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            var trimmed = raw.Trim();
            if (trimmed.Length < 4 || trimmed.Length > 20 || !Regex.IsMatch(trimmed, "^[A-Za-z0-9_.-]+$"))
            {
                error = "LINE ID 格式怪怪的（建議 4~20 個半形英數字、底線、句點或連字號）。";
                return false;
            }

            normalized = trimmed;
            return true;
        }
    }
}
