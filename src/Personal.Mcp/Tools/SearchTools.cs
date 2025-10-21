using System.ComponentModel;
using Personal.Mcp.Services;
using ModelContextProtocol.Server;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class SearchTools
{
    [McpServerTool(Name = "search_notes"), Description("Full-text search across notes. Supports multiple search terms with relevance ranking.")]
    public static object SearchNotes(IndexService index, [Description("Search query")] string query, [Description("Max results (1-500)")] int max_results = 50)
    {
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Search query cannot be empty.");
        index.BuildOrUpdateIndex();
        var list = index.Search(query, Math.Clamp(max_results, 1, 500)).Select(r => new { path = r.path, snippet = r.snippet }).ToList();
        return new { results = list, total_count = list.Count, limit = max_results, truncated = list.Count >= max_results };
    }
}
