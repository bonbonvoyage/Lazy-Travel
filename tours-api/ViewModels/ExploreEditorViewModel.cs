using LazyTravel.Shared.Models.EfModels;

namespace LazyTravel.ViewModels;

public class ExploreEditorViewModel
{
    public VlogPost Post { get; set; } = null!;
    public List<ItineraryNode> Nodes { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public string DateRangeText => Post.TravelDate.HasValue
        ? $"{Post.TravelDate:yyyy/MM/dd} - {Post.TravelDate.Value.AddDays(Math.Max(1, Post.TravelDays) - 1):yyyy/MM/dd}"
        : "未提供";
    public string Highlight => ArticleContentParser.Highlight(Post.Content);
    public string Region => ArticleContentParser.Region(Post.Content);
    public string PlainContent => ArticleContentParser.Intro(Post.Content);
}

internal static class ArticleContentParser
{
    private const string HighlightStart = "<!--LT-HIGHLIGHT:";
    private const string HighlightEnd = "-->";
    private const string RegionStart = "<!--LT-REGION:";
    private const string RegionEnd = "-->";

    public static string Highlight(string? content) => ReadMeta(content, HighlightStart, HighlightEnd);

    public static string Region(string? content) => ReadMeta(content, RegionStart, RegionEnd);

    public static string Intro(string? content)
    {
        var raw = RemoveMeta(RemoveMeta(content ?? "", HighlightStart, HighlightEnd), RegionStart, RegionEnd);
        return System.Net.WebUtility.HtmlDecode(
            System.Text.RegularExpressions.Regex.Replace(
                System.Text.RegularExpressions.Regex.Replace(raw, @"</p>|<br\s*/?>|</div>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                "<[^>]*>",
                "")).Trim();
    }

    private static string ReadMeta(string? content, string startMarker, string endMarker)
    {
        var raw = content ?? "";
        var start = raw.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0) return "";
        start += startMarker.Length;
        var end = raw.IndexOf(endMarker, start, StringComparison.Ordinal);
        if (end < 0) return "";
        return System.Net.WebUtility.HtmlDecode(raw[start..end]).Trim();
    }

    private static string RemoveMeta(string raw, string startMarker, string endMarker)
    {
        var start = raw.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0) return raw;
        var end = raw.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        if (end < 0) return raw[..start];
        return raw.Remove(start, end + endMarker.Length - start);
    }
}
