using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class EditTools
{
    [McpServerTool(Name = "update_note"), Description("Replace or append content to a note.")]
    public static object UpdateNote(VaultService vault,
        IndexService index,
        [Description("Path to the note")] string path,
        [Description("New content")] string content,
        [Description("Create if missing")] bool create_if_not_exists = false,
        [Description("'replace' or 'append'")] string merge_strategy = "replace")
    {
        var abs = vault.GetAbsolutePath(path);
        var exists = File.Exists(abs);
        if (!exists && !create_if_not_exists) throw new FileNotFoundException("Note does not exist", abs);

        if (merge_strategy.Equals("append", StringComparison.OrdinalIgnoreCase) && exists)
        {
            File.AppendAllText(abs, content);
        }
        else
        {
            vault.WriteNote(path, content, overwrite: true);
        }
        index.UpdateFileByRel(path);
        return new { success = true, path };
    }

    [McpServerTool(Name = "edit_note_content"), Description("Search and replace text (first or all occurrences)")]
    public static object EditNoteContent(VaultService vault,
        IndexService index,
        [Description("Path to the note")] string path,
        [Description("Exact search text")] string search_text,
        [Description("Replacement text")] string replacement_text,
        [Description("'first' or 'all'")] string occurrence = "first")
    {
        var abs = vault.GetAbsolutePath(path);
        if (!File.Exists(abs)) throw new FileNotFoundException("Note not found", abs);
        var text = File.ReadAllText(abs);
        if (occurrence.Equals("all", StringComparison.OrdinalIgnoreCase))
            text = text.Replace(search_text, replacement_text);
        else
        {
            var idx = text.IndexOf(search_text, StringComparison.Ordinal);
            if (idx >= 0)
                text = text.Remove(idx, search_text.Length).Insert(idx, replacement_text);
        }
        File.WriteAllText(abs, text);
        index.UpdateFileByRel(path);
        return new { success = true, path };
    }

    [McpServerTool(Name = "edit_note_section"), Description("Insert/replace/append at a markdown heading section.")]
    public static object EditNoteSection(VaultService vault,
        IndexService index,
        [Description("Path to the note")] string path,
        [Description("Heading that identifies the section, e.g., '## Tasks'")] string section_identifier,
        [Description("Content to insert/replace/append")] string content,
        [Description("Operation: insert_after|insert_before|replace_section|append_to_section|edit_heading")] string operation = "insert_after",
        [Description("Create section if missing")] bool create_if_missing = false)
    {
        var abs = vault.GetAbsolutePath(path);
        var text = File.Exists(abs) ? File.ReadAllText(abs) : string.Empty;

        // Normalize and find heading line
        var lines = text.Split('\n').ToList();
        int headingIdx = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().Equals(section_identifier.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                headingIdx = i;
                break;
            }
        }

        if (headingIdx == -1)
        {
            if (!create_if_missing) throw new InvalidOperationException("Section not found");
            // append section at end
            if (lines.Count > 0 && lines[^1].Length > 0) lines.Add("");
            lines.Add(section_identifier.Trim());
            headingIdx = lines.Count - 1;
        }

        switch (operation)
        {
            case "insert_after":
                lines.Insert(headingIdx + 1, content);
                break;
            case "insert_before":
                lines.Insert(headingIdx, content);
                break;
            case "replace_section":
                // replace heading and its content until next heading of same or higher level
                var level = section_identifier.TakeWhile(c => c == '#').Count();
                int end = lines.Count;
                for (int i = headingIdx + 1; i < lines.Count; i++)
                {
                    var t = lines[i].TrimStart();
                    var l = t.TakeWhile(c => c == '#').Count();
                    if (l > 0 && l <= level) { end = i; break; }
                }
                lines.RemoveRange(headingIdx, end - headingIdx);
                lines.Insert(headingIdx, content);
                break;
            case "append_to_section":
                lines.Insert(headingIdx + 1, content);
                break;
            case "edit_heading":
                lines[headingIdx] = content;
                break;
            default:
                throw new ArgumentException("Invalid operation");
        }

        File.WriteAllText(abs, string.Join('\n', lines));
        index.UpdateFileByRel(path);
        return new { success = true, path };
    }

    [McpServerTool(Name = "delete_note"), Description("Delete a note.")]
    public static object DeleteNote(VaultService vault, IndexService index, [Description("Path to the note")] string path)
    {
        var abs = vault.GetAbsolutePath(path);
        if (File.Exists(abs)) File.Delete(abs);
        index.RemoveFileByRel(path);
        return new { success = true, path };
    }
}
