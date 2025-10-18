using System.ComponentModel;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class MoveRenameTools
{
    [McpServerTool(Name = "move_note"), Description("Move or rename a note.")]
    public static object MoveNote(VaultService vault,
        LinkService links,
        IndexService index,
        [Description("Source path")] string source_path,
        [Description("Destination path (can include new filename)")] string destination_path,
        [Description("Update links across vault")] bool update_links = true)
    {
        var srcAbs = vault.GetAbsolutePath(source_path);
        var dstAbs = vault.GetAbsolutePath(destination_path);
        Directory.CreateDirectory(Path.GetDirectoryName(dstAbs)!);
        File.Move(srcAbs, dstAbs, overwrite: true);

        int filesUpdated = 0, replacements = 0;
        if (update_links)
        {
            var res = links.UpdateLinksForRename(source_path, destination_path);
            filesUpdated = res.filesUpdated;
            replacements = res.replacements;
        }

        // Update index for moved file and any files changed by link rewrite
        index.RenameFileByRel(source_path, destination_path);
        if (filesUpdated > 0)
        {
            foreach (var (abs, rel) in vault.EnumerateMarkdownFiles())
            {
                // naive: refresh all, or we could track which changed; for now refresh all when links updated
                index.UpdateFileByRel(rel);
            }
        }
        return new { success = true, source = source_path, destination = destination_path, link_updates = new { files_updated = filesUpdated, replacements } };
    }

    [McpServerTool(Name = "rename_note"), Description("Rename a note within the same directory.")]
    public static object RenameNote(VaultService vault,
        LinkService links,
        IndexService index,
        [Description("Old path")] string old_path,
        [Description("New path (same directory)")] string new_path,
        [Description("Update links across vault")] bool update_links = true)
    {
        var oldAbs = vault.GetAbsolutePath(old_path);
        var newAbs = vault.GetAbsolutePath(new_path);
        if (!string.Equals(Path.GetDirectoryName(oldAbs), Path.GetDirectoryName(newAbs), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Rename must stay in the same directory. Use move_note to move.")
            ;
        File.Move(oldAbs, newAbs, overwrite: true);

        int filesUpdated = 0, replacements = 0;
        if (update_links)
        {
            var res = links.UpdateLinksForRename(old_path, new_path);
            filesUpdated = res.filesUpdated;
            replacements = res.replacements;
        }

        index.RenameFileByRel(old_path, new_path);
        if (filesUpdated > 0)
        {
            foreach (var (abs, rel) in vault.EnumerateMarkdownFiles())
            {
                index.UpdateFileByRel(rel);
            }
        }
        return new { success = true, old_path, new_path, operation = "renamed", link_updates = new { files_updated = filesUpdated, replacements } };
    }

    [McpServerTool(Name = "move_folder"), Description("Move a folder and all contents.")]
    public static object MoveFolder(VaultService vault,
        [Description("Source folder")] string source_folder,
        [Description("Destination folder")] string destination_folder,
        [Description("Update links (future)")] bool update_links = true)
    {
        var srcAbs = vault.GetAbsolutePath(source_folder);
        var dstAbs = vault.GetAbsolutePath(destination_folder);
        Directory.CreateDirectory(dstAbs);
        foreach (var dirPath in Directory.EnumerateDirectories(srcAbs, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(srcAbs, dstAbs));
        }
        foreach (var filePath in Directory.EnumerateFiles(srcAbs, "*", SearchOption.AllDirectories))
        {
            var dest = filePath.Replace(srcAbs, dstAbs);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(filePath, dest, overwrite: true);
        }
        // Remove old tree
        Directory.Delete(srcAbs, recursive: true);
        return new { source = source_folder, destination = destination_folder, moved = true };
    }
}
