using System.Text.RegularExpressions;

namespace DailyWork.Mcp.Obsidian.Services;

public partial class WikilinkService
{
    [GeneratedRegex(@"\[\[([^\]|]+)(?:\|[^\]]+)?\]\]")]
    private static partial Regex WikilinkPattern();

    public List<string> ExtractLinks(string content)
    {
        MatchCollection matches = WikilinkPattern().Matches(content);

        return matches
            .Select(m => m.Groups[1].Value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
