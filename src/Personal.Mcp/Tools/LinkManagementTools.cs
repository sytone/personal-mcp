using System.ComponentModel;
using System.IO;
using System.Linq;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class LinkManagementTools
{
    [McpServerTool(Name = "get_backlinks"), Description("Find all notes that link to a specific note.")]
    public static object GetBacklinks(LinkService links,
        [Description("Target note path")] string path,
        [Description("Include context")] bool include_context = true,
        [Description("Context length")] int context_length = 100)
    {
        var list = links.GetBacklinks(path, context_length).Select(x => new { source_path = x.sourcePath, target = x.target, context = include_context ? x.context : null }).ToList();
        return new { target_note = path, backlink_count = list.Count, backlinks = list };
    }

    [McpServerTool(Name = "find_broken_links"), Description("Find all broken wiki-style links in the vault or directory.")]
    public static object FindBrokenLinks(LinkService links,
        [Description("Directory (optional)")] string? directory = null,
        [Description("Single note (optional)")] string? single_note = null)
    {
        if (!string.IsNullOrWhiteSpace(single_note))
        {
            var list = links.FindBrokenLinks(Path.GetDirectoryName(single_note)?.Replace("\\", "/"));
            list = list.Where(b => string.Equals(b.sourcePath, single_note.Replace("\\", "/"), StringComparison.OrdinalIgnoreCase)).ToList();
            return new { directory = Path.GetDirectoryName(single_note)?.Replace("\\", "/"), broken_link_count = list.Count, affected_notes = list.Select(x => x.sourcePath).Distinct().Count(), broken_links = list.Select(x => new { source_path = x.sourcePath, broken_link = x.brokenLink, display_text = x.displayText }) };
        }
        else
        {
            var list = links.FindBrokenLinks(directory);
            return new { directory = directory ?? "/", broken_link_count = list.Count, affected_notes = list.Select(x => x.sourcePath).Distinct().Count(), broken_links = list.Select(x => new { source_path = x.sourcePath, broken_link = x.brokenLink, display_text = x.displayText }) };
        }
    }
}
