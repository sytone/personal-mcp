using System.ComponentModel;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class ImageExtractionTools
{
    [McpServerTool(Name = "view_note_images"), Description("List images embedded or linked in a note and whether they exist.")]
    public static object ViewNoteImages(VaultService vault, [Description("Note path")] string path)
    {
        var items = vault.GetNoteImages(path)
            .Select(i => new { raw = i.raw, path = i.resolvedPath, exists = i.exists })
            .ToList();
        return new { path, count = items.Count, images = items };
    }
}
