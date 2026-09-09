namespace LazyTravel.Shared.Services;

// 文章內文（Quill 存的 HTML）驗證共用：去標籤取純文字給「必填」跟「不當字眼偵測」共用。
// 髒話清單是簡單的字串比對，不是語意分析，擋不住同音字/注音/符號夾雜刻意規避的寫法，
// 只能擋最直接的寫法，先求有基本防護；之後要擴充清單直接加在 BannedWords 裡就好。
public static class ContentModerationHelper
{
    private static readonly string[] BannedWords =
    {
        // 三字經／問候家人類
        "幹你娘", "幹恁娘", "幹你老師", "他媽的", "你他媽", "去你媽的", "操你媽", "死全家",
        // 單獨的「幹」不放進來：常出現在「幹部」「幹嘛」「能幹」這些正常詞裡，會誤擋。
        "幹！", "幹!",
        // 台語常見髒話
        "機掰", "機歪", "三小", "靠北", "靠杯", "夭壽", "俗辣", "北七",
        // 咒罵/羞辱類
        "去死", "王八蛋", "混蛋", "賤人", "婊子", "廢物", "死小孩", "死變態", "神經病",
        // 貶低智力/人格類（也常拿來當一般罵人詞用）
        "白痴", "白癡", "智障", "腦殘", "垃圾東西", "垃圾人",
    };

    public static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text).Trim();
        return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
    }

    public static bool ContainsProfanity(string? html)
    {
        var plainText = StripHtml(html);
        return BannedWords.Any(word => plainText.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
