using ModelContextProtocol;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;
using System.ComponentModel;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public sealed class JournalTools
{
    private readonly IVaultService _vault;
    private readonly IndexService _index;

    public JournalTools(IVaultService vault, IndexService index)
    {
        _vault = vault;
        _index = index;
    }

    [McpServerTool, Description("Read journal entries from a markdown-based knowledge store.")]
    public string ReadJournalEntries(
        [Description("Name of the folder that contains the journal files, relative to vault root. Defaults to '1 Journal'.")] string? journalPath = null,
        [Description("Start date (YYYY-MM-DD). Optional.")] string? fromDate = null,
        [Description("End date (YYYY-MM-DD). Optional.")] string? toDate = null,
        [Description("Maximum number of entries to return, newest first.")] int maxEntries = 10,
        [Description("Include full content (true) or just list paths and dates (false)."), DefaultValue(true)] bool includeContent = true)
    {
        // Resolve default journal path using vault-level settings then environment variable, otherwise fall back to hard-coded default
        if (string.IsNullOrWhiteSpace(journalPath))
        {
            var cfg = _vault.GetVaultSetting("journalPath");
            journalPath = string.IsNullOrWhiteSpace(cfg) ? "1 Journal" : cfg;
        }

        DateTime? from = TryParseDate(fromDate);
        DateTime? to = TryParseDate(toDate);

        // Use VaultService to enumerate all markdown files in the journal directory
        // Handle case where directory doesn't exist
        List<(string RelPath, DateTime Date)> mdFiles;
        try
        {
            mdFiles = _vault.EnumerateMarkdownFiles(journalPath)
                .Select(file =>
                {
                    var (exists, lastModified, _) = _vault.GetFileStats(file.rel);
                    var date = ExtractDateFromPath(file.rel) ?? lastModified;
                    return (file.rel, date);
                })
                .OrderByDescending(x => x.date)
                .Where(x => (!from.HasValue || x.date.Date >= from.Value.Date) && (!to.HasValue || x.date.Date <= to.Value.Date))
                .Take(Math.Max(1, maxEntries))
                .ToList();
        }
        catch (DirectoryNotFoundException)
        {
            // Directory doesn't exist, return no entries found
            return "No journal entries found.";
        }
        catch (ArgumentException)
        {
            // Invalid path (e.g., contains "..", starts with "/")
            return "No journal entries found.";
        }

        if (mdFiles.Count == 0)
        {
            return "No journal entries found.";
        }

        if (!includeContent)
        {
            var list = mdFiles.Select(x => $"{x.Date:yyyy-MM-dd} | {x.RelPath}");
            return string.Join("\n", list);
        }

        var sb = new StringBuilder();
        for (int i = 0; i < mdFiles.Count; i++)
        {
            var item = mdFiles[i];
            string content;
            try
            {
                // Read full content (including frontmatter) via VaultService
                content = _vault.ReadNoteRaw(item.RelPath);
            }
            catch (Exception ex)
            {
                content = $"<Error reading file: {ex.Message}>";
            }

            if (i > 0) sb.AppendLine("\n---\n");
            sb.AppendLine($"Date: {item.Date:yyyy-MM-dd}");
            sb.AppendLine($"File: {item.RelPath}");
            sb.AppendLine();
            sb.AppendLine(content);
        }

        return sb.ToString();
    }

    [McpServerTool, Description("Add an entry to the weekly journal file for a given date (defaults to today). If a matching day heading exists (e.g., '## 14 Thursday'), the entry is appended under it; otherwise it's appended to the end of the file.")]
    public string AddJournalEntry(
        [Description("The content to add to the journal.")] string entryContent,
        [Description("Name of the folder that contains the journal files, relative to vault root. Defaults to '1 Journal'.")] string? journalPath = null,
        [Description("Target date in YYYY-MM-DD. Optional; defaults to today.")] string? date = null)
    {
        if (string.IsNullOrWhiteSpace(entryContent))
        {
            return "No content provided.";
        }

        // Resolve default journal path using vault settings/env variable if not provided
        if (string.IsNullOrWhiteSpace(journalPath))
        {
            var cfg = _vault.GetVaultSetting("journalPath");
            journalPath = string.IsNullOrWhiteSpace(cfg) ? "1 Journal" : cfg;
        }

        try
        {
            DateTime targetDate = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsed))
            {
                targetDate = parsed;
            }

            // Determine weekly file name (ISO week)
            int isoYear = ISOWeek.GetYear(targetDate);
            int isoWeek = ISOWeek.GetWeekOfYear(targetDate);
            string weeklyFileName = $"{isoYear}-W{isoWeek:00}.md";

            // Try to find an existing weekly file using VaultService
            string? weeklyRelPath = null;
            
            // Only check if journal path exists if we're looking for existing files
            if (_vault.DirectoryExists(journalPath))
            {
                foreach (var (abs, rel) in _vault.EnumerateMarkdownFiles(journalPath))
                {
                    var fileName = _vault.GetFileName(rel);
                    if (fileName.Equals(weeklyFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        weeklyRelPath = rel;
                        break;
                    }
                }
            }

            // If not found, create path in default location: <journalPath>/<year>/<year>-Www.md
            if (weeklyRelPath is null)
            {
                weeklyRelPath = $"{journalPath}/{isoYear}/{weeklyFileName}".Replace("\\", "/");
            }

            // Read existing content or create stub using VaultService
            string content;
            var (exists, _, _) = _vault.GetNoteInfo(weeklyRelPath);
            if (exists)
        {
            try
            {
                content = _vault.ReadNoteRaw(weeklyRelPath);
            }
            catch
            {
                content = CreateMinimalWeeklyStub(isoYear, isoWeek);
            }
        }
        else
        {
            content = CreateMinimalWeeklyStub(isoYear, isoWeek);
        }

        // Build the heading for the given day within the week, e.g., '## 14 Thursday'
        string heading = $"## {targetDate.Day} {targetDate:dddd}";
        var lines = new List<string>(content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));

        // Find the heading line index
        int headingIndex = lines.FindIndex(l => l.Trim().Equals(heading, StringComparison.Ordinal));
        string entryToInsert = entryContent.TrimEnd();
        string timePrefix = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);

        if (headingIndex >= 0)
        {
            // Heading exists: Insert after the last line of this section (until next '## ' or end)
            int insertIndex = headingIndex + 1;
            var sectionHeaderRegex = new Regex(@"^##\s+\d{1,2}\s+\w+\s*$", RegexOptions.Compiled);
            while (insertIndex < lines.Count && !sectionHeaderRegex.IsMatch(lines[insertIndex]))
            {
                insertIndex++;
            }
            // Only add a blank line if the previous line is the heading itself; otherwise append directly
            if (insertIndex == headingIndex + 1)
            {
                // First entry under this heading: ensure one blank line after the heading for readability
                lines.Insert(insertIndex, string.Empty);
                insertIndex++;
            }
            lines.Insert(insertIndex, $"- {timePrefix} - {entryToInsert}");
        }
        else
        {
            // Heading not found: find correct insertion point to maintain sequential day order
            var sectionHeaderRegex = new Regex(@"^##\s+(\d{1,2})\s+\w+\s*$", RegexOptions.Compiled);
            int targetDay = targetDate.Day;
            int insertBeforeIndex = -1;

            // Find all day headings and their line indices
            for (int i = 0; i < lines.Count; i++)
            {
                var match = sectionHeaderRegex.Match(lines[i]);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int dayNum))
                {
                    if (dayNum > targetDay)
                    {
                        // Found a day that comes after our target day
                        insertBeforeIndex = i;
                        break;
                    }
                }
            }

            if (insertBeforeIndex >= 0)
            {
                // Insert the new heading before the found heading (with proper spacing)
                // Add blank line before if previous line has content
                if (insertBeforeIndex > 0 && !string.IsNullOrWhiteSpace(lines[insertBeforeIndex - 1]))
                {
                    lines.Insert(insertBeforeIndex, string.Empty);
                    insertBeforeIndex++;
                }
                lines.Insert(insertBeforeIndex, heading);
                lines.Insert(insertBeforeIndex + 1, string.Empty);
                lines.Insert(insertBeforeIndex + 2, $"- {timePrefix} - {entryToInsert}");
                lines.Insert(insertBeforeIndex + 3, string.Empty); // Blank line after section
            }
            else
            {
                // No later days found: append at end
                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                {
                    lines.Add(string.Empty);
                }
                lines.Add(heading);
                lines.Add(string.Empty);
                lines.Add($"- {timePrefix} - {entryToInsert}");
            }
        }

        // Write using VaultService (creates directories automatically)
        var updatedContent = string.Join(Environment.NewLine, lines);
        try
        {
            _vault.WriteNote(weeklyRelPath, updatedContent, overwrite: true);
        }
        catch (Exception ex)
        {
            return $"Error writing journal entry: {ex.Message}";
        }

        // Update the search index
        try
        {
            _index.UpdateFileByRel(weeklyRelPath);
        }
        catch
        {
            // Index update is non-critical; continue
        }

        return $"Added entry to {weeklyRelPath} under '{heading}'.";
        }
        catch (ArgumentException ex)
        {
            // Path validation errors (e.g., contains "..", starts with "/", escapes vault)
            return $"Error: Invalid journal path - {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            // Path escapes vault
            return $"Error: Invalid journal path - {ex.Message}";
        }
        catch (Exception ex)
        {
            // Any other unexpected errors
            return $"Error adding journal entry: {ex.Message}";
        }
    }

    private static string CreateMinimalWeeklyStub(int isoYear, int isoWeek)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Week {isoWeek} in {isoYear}");
        sb.AppendLine();
        return sb.ToString();
    }

    private static DateTime? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, out var dt)) return dt;
        return null;
    }

    private static readonly Regex DateRegex = new(
        pattern: @"(?<!\d)(\d{4})[-/](\d{2})[-/](\d{2})(?!\d)",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Matches ISO week format like 2025-W33 or 2025/ W33 variations
    private static readonly Regex IsoWeekRegex = new(
        pattern: @"(?<!\d)(\d{4})[-/]W(\d{2})(?!\d)",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static DateTime? ExtractDateFromPath(string relativePath)
    {
        // Extract just the filename from the relative path
        var lastSlash = relativePath.LastIndexOfAny(['/', '\\']);
        var fileName = lastSlash >= 0 ? relativePath.Substring(lastSlash + 1) : relativePath;
        
        // Remove .md extension if present
        if (fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName.Substring(0, fileName.Length - 3);
        }

        // Try YYYY-MM-DD
        var match = DateRegex.Match(fileName);
        if (!match.Success)
        {
            match = DateRegex.Match(relativePath);
        }
        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out int y) &&
            int.TryParse(match.Groups[2].Value, out int m) &&
            int.TryParse(match.Groups[3].Value, out int d))
        {
            if (m is >= 1 and <= 12 && d is >= 1 and <= 31)
            {
                try { return new DateTime(y, m, d); } catch { /* ignore invalid */ }
            }
        }

        // Try ISO week YYYY-Www => convert to Monday of that ISO week
        var wmatch = IsoWeekRegex.Match(fileName);
        if (!wmatch.Success)
        {
            wmatch = IsoWeekRegex.Match(relativePath);
        }
        if (wmatch.Success &&
            int.TryParse(wmatch.Groups[1].Value, out int wy) &&
            int.TryParse(wmatch.Groups[2].Value, out int ww))
        {
            try
            {
                // Use Monday of the ISO week as the representative date
                return ISOWeek.ToDateTime(wy, ww, DayOfWeek.Monday);
            }
            catch
            {
                // ignore invalid week/year combos
            }
        }

        return null;
    }
}
