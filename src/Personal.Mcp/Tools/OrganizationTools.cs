using System.ComponentModel;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class OrganizationTools
{
    [McpServerTool(Name = "list_notes"), Description("List notes in a directory (recursive by default).")]
    public static object ListNotes(VaultService vault,
        [Description("Directory to list, e.g., 'Daily' (optional)")] string? directory = null,
        [Description("Recurse into subfolders")] bool recursive = true)
    {
        var root = vault.VaultPath;
        var baseDir = string.IsNullOrWhiteSpace(directory) ? root : vault.GetAbsolutePath(directory);
        var opts = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var notes = Directory.EnumerateFiles(baseDir, "*.md", opts)
            .Select(p => new { path = Path.GetRelativePath(root, p).Replace("\\", "/"), name = Path.GetFileName(p) })
            .OrderBy(n => n.path)
            .ToList();
        return new { directory = directory ?? "/", recursive, count = notes.Count, notes };
    }

    [McpServerTool(Name = "list_folders"), Description("List folders in a directory (recursive by default).")]
    public static object ListFolders(VaultService vault,
        [Description("Directory to list from (optional)")] string? directory = null,
        [Description("Recurse into subfolders")] bool recursive = true)
    {
        var root = vault.VaultPath;
        var baseDir = string.IsNullOrWhiteSpace(directory) ? root : vault.GetAbsolutePath(directory);
        var opts = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var folders = Directory.EnumerateDirectories(baseDir, "*", opts)
            .Select(p => new { path = Path.GetRelativePath(root, p).Replace("\\", "/"), name = Path.GetFileName(p) })
            .OrderBy(n => n.path)
            .ToList();
        return new { directory = directory ?? "/", recursive, count = folders.Count, folders };
    }

    [McpServerTool(Name = "create_folder"), Description("Create a folder and parents if needed.")]
    public static object CreateFolder(VaultService vault,
        [Description("Folder path to create, e.g., 'Research/Studies/2024'")] string folder_path,
        [Description("Create placeholder file .gitkeep")] bool create_placeholder = true)
    {
        var abs = vault.GetAbsolutePath(folder_path);
        Directory.CreateDirectory(abs);
        string? placeholder = null;
        if (create_placeholder)
        {
            placeholder = Path.Combine(abs, ".gitkeep");
            if (!File.Exists(placeholder)) File.WriteAllText(placeholder, string.Empty);
            placeholder = Path.GetRelativePath(vault.VaultPath, placeholder).Replace("\\", "/");
        }
        // Build list of created folder hierarchy
        var parts = folder_path.Replace("\\", "/").Split('/', StringSplitOptions.RemoveEmptyEntries);
        var acc = new List<string>();
        for (int i = 0; i < parts.Length; i++)
            acc.Add(string.Join('/', parts.Take(i + 1)));
        return new { folder = folder_path, created = true, placeholder_file = placeholder, folders_created = acc };
    }
}
