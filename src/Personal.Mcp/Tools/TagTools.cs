using System.ComponentModel;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class TagTools
{
    [McpServerTool(Name = "list_tags"), Description("List all tags across the vault with counts.")]
    public static object ListTags(TagService tags,
        [Description("Include counts")] bool include_counts = true,
        [Description("Sort by 'name' or 'count'")] string sort_by = "name")
    {
        var dict = tags.ListTags();
        var list = dict.Select(kv => new { name = kv.Key, count = kv.Value }).ToList();
        list = sort_by.Equals("count", StringComparison.OrdinalIgnoreCase)
            ? list.OrderByDescending(t => t.count).ThenBy(t => t.name).ToList()
            : list.OrderBy(t => t.name).ToList();
        return new { total_tags = list.Count, tags = list };
    }

    [McpServerTool(Name = "add_tags"), Description("Add one or more tags to a note's frontmatter.")]
    public static object AddTags(VaultService vault,
        [Description("Note path")] string path,
        [Description("Tags to add")] string[] tags)
    {
        var ok = vault.AddTags(path, tags);
        return new { success = ok, path, added = tags };
    }

    [McpServerTool(Name = "update_tags"), Description("Replace or merge tags in a note's frontmatter.")]
    public static object UpdateTags(VaultService vault,
        [Description("Note path")] string path,
        [Description("New tag set or tags to merge")] string[] tags,
        [Description("When true, merges with existing; when false, replaces")] bool merge = true)
    {
        var ok = vault.UpdateTags(path, tags, merge);
        return new { success = ok, path, tags, merge };
    }

    [McpServerTool(Name = "remove_tags"), Description("Remove tags from a note's frontmatter.")]
    public static object RemoveTags(VaultService vault,
        [Description("Note path")] string path,
        [Description("Tags to remove")] string[] tags)
    {
        var ok = vault.RemoveTags(path, tags);
        return new { success = ok, path, removed = tags };
    }
}
