using System.ComponentModel;
using System.Globalization;
using System.IO.Abstractions;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class OrganizationTools
{
    [McpServerTool(Name = "list_notes"), Description("List notes in a directory (recursive by default). Can filter by creation or modification date.")]
    public static object ListNotes(
        IVaultService vault,
        IFileSystem fileSystem,
        [Description("Directory to list, e.g., 'Daily' (optional)")] string? directory = null,
        [Description("Recurse into subfolders")] bool recursive = true,
        [Description("Filter by date (ISO format: YYYY-MM-DD) or date range (YYYY-MM-DD:YYYY-MM-DD)")] string? date_filter = null,
        [Description("Date property to filter on: 'created', 'modified', or 'both' (default: 'modified')")] string date_property = "modified")
    {
        var root = vault.VaultPath;
        var baseDir = string.IsNullOrWhiteSpace(directory) ? root : vault.GetAbsolutePath(directory);
        var opts = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        
        DateTimeOffset? startDate = null;
        DateTimeOffset? endDate = null;
        
        // Parse date filter
        if (!string.IsNullOrWhiteSpace(date_filter))
        {
            if (date_filter.Contains(':'))
            {
                // Date range format: YYYY-MM-DD:YYYY-MM-DD
                var parts = date_filter.Split(':', 2);
                if (DateTimeOffset.TryParseExact(parts[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var start))
                {
                    startDate = start.Date;
                }
                if (parts.Length > 1 && DateTimeOffset.TryParseExact(parts[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var end))
                {
                    endDate = end.Date.AddDays(1).AddTicks(-1); // End of day
                }
            }
            else
            {
                // Single date format: YYYY-MM-DD
                if (DateTimeOffset.TryParseExact(date_filter.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    startDate = date.Date;
                    endDate = date.Date.AddDays(1).AddTicks(-1); // End of day
                }
            }
        }
        
        var notes = fileSystem.Directory.EnumerateFiles(baseDir, "*.md", opts)
            .Select(p => 
            {
                var fileInfo = fileSystem.FileInfo.New(p);
                return new 
                { 
                    path = fileSystem.Path.GetRelativePath(root, p).Replace("\\", "/"), 
                    name = fileSystem.Path.GetFileName(p),
                    created = fileInfo.CreationTime,
                    modified = fileInfo.LastWriteTime,
                    fileInfo
                };
            })
            .Where(n => 
            {
                if (startDate == null && endDate == null)
                    return true;
                
                var checkCreated = date_property == "created" || date_property == "both";
                var checkModified = date_property == "modified" || date_property == "both";
                
                bool matchesCreated = false;
                bool matchesModified = false;
                
                if (checkCreated)
                {
                    if (startDate.HasValue && endDate.HasValue)
                        matchesCreated = n.created >= startDate.Value && n.created <= endDate.Value;
                    else if (startDate.HasValue)
                        matchesCreated = n.created >= startDate.Value;
                    else if (endDate.HasValue)
                        matchesCreated = n.created <= endDate.Value;
                }
                
                if (checkModified)
                {
                    if (startDate.HasValue && endDate.HasValue)
                        matchesModified = n.modified >= startDate.Value && n.modified <= endDate.Value;
                    else if (startDate.HasValue)
                        matchesModified = n.modified >= startDate.Value;
                    else if (endDate.HasValue)
                        matchesModified = n.modified <= endDate.Value;
                }
                
                return date_property == "both" ? (matchesCreated || matchesModified) : (matchesCreated || matchesModified);
            })
            .Select(n => new 
            { 
                n.path, 
                n.name,
                created = n.created.ToString("yyyy-MM-dd HH:mm:ss"),
                modified = n.modified.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .OrderBy(n => n.path)
            .ToList();
            
        return new 
        { 
            directory = directory ?? "/", 
            recursive, 
            date_filter = date_filter ?? "none",
            date_property,
            start_date = startDate?.ToString("yyyy-MM-dd"),
            end_date = endDate?.ToString("yyyy-MM-dd"),
            count = notes.Count, 
            notes 
        };
    }

    [McpServerTool(Name = "list_folders"), Description("List folders in a directory (recursive by default).")]
    public static object ListFolders(
        IVaultService vault,
        IFileSystem fileSystem,
        [Description("Directory to list from (optional)")] string? directory = null,
        [Description("Recurse into subfolders")] bool recursive = true)
    {
        var root = vault.VaultPath;
        var baseDir = string.IsNullOrWhiteSpace(directory) ? root : vault.GetAbsolutePath(directory);
        var opts = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var folders = fileSystem.Directory.EnumerateDirectories(baseDir, "*", opts)
            .Select(p => new { path = fileSystem.Path.GetRelativePath(root, p).Replace("\\", "/"), name = fileSystem.Path.GetFileName(p) })
            .OrderBy(n => n.path)
            .ToList();
        return new { directory = directory ?? "/", recursive, count = folders.Count, folders };
    }

    [McpServerTool(Name = "create_folder"), Description("Create a folder and parents if needed.")]
    public static object CreateFolder(
        IVaultService vault,
        IFileSystem fileSystem,
        [Description("Folder path to create, e.g., 'Research/Studies/2024'")] string folder_path,
        [Description("Create placeholder file .gitkeep")] bool create_placeholder = true)
    {
        var abs = vault.GetAbsolutePath(folder_path);
        fileSystem.Directory.CreateDirectory(abs);
        string? placeholder = null;
        if (create_placeholder)
        {
            placeholder = fileSystem.Path.Combine(abs, ".gitkeep");
            if (!fileSystem.File.Exists(placeholder)) 
                fileSystem.File.WriteAllText(placeholder, string.Empty);
            placeholder = fileSystem.Path.GetRelativePath(vault.VaultPath, placeholder).Replace("\\", "/");
        }
        // Build list of created folder hierarchy
        var parts = folder_path.Replace("\\", "/").Split('/', StringSplitOptions.RemoveEmptyEntries);
        var acc = new List<string>();
        for (int i = 0; i < parts.Length; i++)
            acc.Add(string.Join('/', parts.Take(i + 1)));
        return new { folder = folder_path, created = true, placeholder_file = placeholder, folders_created = acc };
    }
}
