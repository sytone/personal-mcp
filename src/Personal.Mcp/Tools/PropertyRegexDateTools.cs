using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class PropertyRegexDateTools
{
    [McpServerTool(Name = "search_by_regex"), Description("Regex search across notes with context snippets.")]
    public static object SearchByRegex(VaultService vault,
        [Description("Regex pattern")] string pattern,
        [Description("Flags: ignorecase|multiline|dotall")] string[]? flags = null,
        [Description("Context characters")] int context_length = 100,
        [Description("Max results")] int max_results = 50)
    {
        var options = RegexOptions.Compiled;
        if (flags != null)
        {
            foreach (var f in flags)
            {
                switch (f?.ToLowerInvariant())
                {
                    case "ignorecase": options |= RegexOptions.IgnoreCase; break;
                    case "multiline": options |= RegexOptions.Multiline; break;
                    case "dotall": options |= RegexOptions.Singleline; break;
                }
            }
        }

        var results = new List<object>();
        foreach (var file in Directory.EnumerateFiles(vault.VaultPath, "*.md", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(vault.VaultPath, file).Replace("\\", "/");
            var text = File.ReadAllText(file);
            var matches = Regex.Matches(text, pattern, options);
            if (matches.Count == 0) continue;
            var items = new List<object>();
            foreach (Match m in matches)
            {
                var start = Math.Max(0, m.Index - context_length);
                var end = Math.Min(text.Length, m.Index + m.Length + context_length);
                var ctx = text.Substring(start, end - start);
                items.Add(new { match = m.Value, index = m.Index, context = ctx });
                if (items.Count >= max_results) break;
            }
            results.Add(new { path = rel, match_count = items.Count, matches = items });
            if (results.Count >= max_results) break;
        }
        return new { pattern, count = results.Count, results };
    }

    [McpServerTool(Name = "search_by_date"), Description("Find notes by created/modified date within or exactly N days.")]
    public static object SearchByDate(VaultService vault,
        [Description("'created' or 'modified'")] string date_type = "modified",
        [Description("Days ago")] int days_ago = 7,
        [Description("'within' or 'exactly'")] string @operator = "within")
    {
        var now = DateTimeOffset.Now;
        var results = new List<object>();
        foreach (var file in Directory.EnumerateFiles(vault.VaultPath, "*.md", SearchOption.AllDirectories))
        {
            var info = new FileInfo(file);
            var dt = date_type.Equals("created", StringComparison.OrdinalIgnoreCase)
                ? info.CreationTime
                : info.LastWriteTime;
            var ageDays = (now - dt).TotalDays;
            bool match = @operator.Equals("within", StringComparison.OrdinalIgnoreCase)
                ? ageDays <= days_ago
                : Math.Abs(ageDays - days_ago) < 0.5; // approx
            if (match)
            {
                results.Add(new { path = Path.GetRelativePath(vault.VaultPath, file).Replace("\\", "/"), date = dt, days_ago = (int)Math.Round(ageDays) });
            }
        }
        return new { query = $"Notes {date_type} {@operator} last {days_ago} days", count = results.Count, results };
    }
}
