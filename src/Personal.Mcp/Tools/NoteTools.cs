using System.ComponentModel;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class NoteTools
{
    [McpServerTool(Name = "read_note"), Description("Read the content and metadata of a specific note.")]
    public static object ReadNote(IVaultService vault, [Description("Path to the note, e.g., 'Daily/2024-01-15.md'")] string path)
    {
        var (content, metadata) = vault.ReadNote(path);
        return new { path, content, metadata };
    }

    [McpServerTool(Name = "create_note"), Description("Create a new note or update an existing one.")]
    public static object CreateNote(IVaultService vault,
        IndexService index,
        [Description("Path where the note should be created")] string path,
        [Description("Markdown content of the note")] string content,
        [Description("Whether to overwrite existing notes")] bool overwrite = false)
    {
        vault.WriteNote(path, content, overwrite);
        index.UpdateFileByRel(path);
        return new { success = true, path };
    }
}
