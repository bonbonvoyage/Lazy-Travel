using LazyTravel.Shared.Models.EfModels;

namespace LazyTravel.ViewModels;

public class ExploreEditorViewModel
{
    public VlogPost Post { get; set; } = null!;
    public VlogPostRoomExport? Source { get; set; }
    public List<ItineraryNode> Nodes { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public bool IsFromRoom => Source is not null;
    public string DateRangeText => Post.TravelDate.HasValue
        ? $"{Post.TravelDate:yyyy/MM/dd} - {Post.TravelDate.Value.AddDays(Math.Max(1, Post.TravelDays) - 1):yyyy/MM/dd}"
        : "未提供";
    public string Highlight => ArticleContentParser.Highlight(Post.Content);
    public string PlainContent => ArticleContentParser.Intro(Post.Content);
}

internal static class ArticleContentParser
{
    private const string HighlightStart = "<!--LT-HIGHLIGHT:";
    private const string HighlightEnd = "-->";

    public static string Highlight(string? content)
    {
        var raw = content ?? "";
        var start = raw.IndexOf(HighlightStart, StringComparison.Ordinal);
        if (start < 0) return "";
        start += HighlightStart.Length;
        var end = raw.IndexOf(HighlightEnd, start, StringComparison.Ordinal);
        if (end < 0) return "";
        return System.Net.WebUtility.HtmlDecode(raw[start..end]).Trim();
    }

    public static string Intro(string? content)
    {
        var raw = RemoveHighlight(content ?? "");
        return System.Net.WebUtility.HtmlDecode(
            System.Text.RegularExpressions.Regex.Replace(
                System.Text.RegularExpressions.Regex.Replace(raw, @"</p>|<br\s*/?>|</div>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                "<[^>]*>",
                "")).Trim();
    }

    private static string RemoveHighlight(string raw)
    {
        var start = raw.IndexOf(HighlightStart, StringComparison.Ordinal);
        if (start < 0) return raw;
        var end = raw.IndexOf(HighlightEnd, start + HighlightStart.Length, StringComparison.Ordinal);
        if (end < 0) return raw[..start];
        return raw.Remove(start, end + HighlightEnd.Length - start);
    }
}
