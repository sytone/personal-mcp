using System.ComponentModel;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class PropertyTools
{
    [McpServerTool(Name = "get_note_info"), Description("Get existence, metadata, and basic stats for a note.")]
    public static object GetNoteInfo(VaultService vault, [Description("Note path")] string path)
    {
        var info = vault.GetNoteInfo(path);
        return new { path, exists = info.exists, metadata = info.metadata, stats = new { size = info.stats.size, words = info.stats.words } };
    }

    [McpServerTool(Name = "get_frontmatter"), Description("Get a note's frontmatter as a key-value map.")]
    public static object GetFrontmatter(VaultService vault, [Description("Note path")] string path)
    {
        var fm = vault.GetFrontmatter(path);
        return new { path, frontmatter = fm };
    }

    [McpServerTool(Name = "set_property"), Description("Set or remove a frontmatter property.")]
    public static object SetProperty(VaultService vault,
        [Description("Note path")] string path,
        [Description("Property key")] string key,
        [Description("Property value; if null, removes the key")] string? value)
    {
        var ok = vault.SetProperty(path, key, value);
        return new { success = ok, path, key, value };
    }

    [McpServerTool(Name = "set_property_list"), Description("Set a frontmatter list property (replaces existing).")]
    public static object SetPropertyList(VaultService vault,
        [Description("Note path")] string path,
        [Description("Property key")] string key,
        [Description("List of values")] string[] values)
    {
        var ok = vault.SetPropertyList(path, key, values);
        return new { success = ok, path, key, values };
    }

    [McpServerTool(Name = "remove_property"), Description("Remove a frontmatter property.")]
    public static object RemoveProperty(VaultService vault,
        [Description("Note path")] string path,
        [Description("Property key")] string key)
    {
        var ok = vault.RemoveProperty(path, key);
        return new { success = ok, path, key, removed = ok };
    }

    [McpServerTool(Name = "search_by_property"), Description("Find notes by frontmatter property value (exact or contains).")]
    public static object SearchByProperty(VaultService vault,
        [Description("Property key")] string key,
        [Description("Property value (optional)")] string? value = null,
        [Description("If true, substring match; otherwise exact")] bool contains = false,
        [Description("Case-insensitive match")] bool case_insensitive = true)
    {
        var results = vault.SearchByProperty(key, value, contains, case_insensitive)
            .Select(x => new { path = x.path, value = x.value })
            .ToList();
        return new { key, value, count = results.Count, results };
    }
}
