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
    private readonly ITemplateService _template;

    // Format strings for journal entries
    private const string TimeFormat = "HH:mm";
    private const string DefaultJournalPath = "1 Journal";
    private const string DefaultTasksHeading = "## Tasks This Week";

    public JournalTools(IVaultService vault, IndexService index, ITemplateService template)
    {
        _vault = vault;
        _index = index;
        _template = template;
    }

    [McpServerTool, Description("Read journal entries from a markdown-based knowledge store.")]
    public string ReadJournalEntries(
        [Description("Name of the folder that contains the journal files, relative to vault root. Defaults to '1 Journal'.")] string? journalPath = null,
        [Description("Start date (YYYY-MM-DD). Optional.")] string? fromDate = null,
        [Description("End date (YYYY-MM-DD). Optional.")] string? toDate = null,
        [Description("Maximum number of entries to return, newest first.")] int maxEntries = 10,
        [Description("Include full content (true) or just list paths and dates (false)."), DefaultValue(true)] bool includeContent = true)
    {
        journalPath = ResolveJournalPath(journalPath);

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

        journalPath = ResolveJournalPath(journalPath);

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

            // Find or construct the path to the weekly file
            string weeklyRelPath = ResolveWeeklyFilePath(journalPath, isoYear, isoWeek);

            // Load existing content or create new stub
            string content = LoadOrCreateWeeklyContent(weeklyRelPath, isoYear, isoWeek);

            // Build the heading for the given day within the week, e.g., '## 14 Thursday'
            string heading = $"## {targetDate.Day} {targetDate:dddd}";
            var lines = new List<string>(content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));

            // Find the heading line index
            int headingIndex = lines.FindIndex(l => l.Trim().Equals(heading, StringComparison.Ordinal));
            string entryToInsert = entryContent.TrimEnd();
            string timePrefix = DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture);

            if (headingIndex >= 0)
            {
                // Heading exists: Insert after the last line of this section (until next day section or end)
                int insertIndex = headingIndex + 1;
                while (insertIndex < lines.Count && !DaySectionHeaderRegex.IsMatch(lines[insertIndex]))
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
                int targetDay = targetDate.Day;
                int insertBeforeIndex = -1;

                // Find all day headings and their line indices
                for (int i = 0; i < lines.Count; i++)
                {
                    var match = DaySectionHeaderWithCaptureRegex.Match(lines[i]);
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

            // Write using VaultService and update index
            var updatedContent = string.Join(Environment.NewLine, lines);
            return WriteJournalContent(weeklyRelPath, updatedContent, $"Added entry to {weeklyRelPath} under '{heading}'.");
        }
        catch (ArgumentException ex)
        {
            return $"Error: Invalid journal path - {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Error: Invalid journal path - {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error adding journal entry: {ex.Message}";
        }
    }

    [McpServerTool, Description("Add a task or action item to the weekly journal under a configurable tasks heading (default: '## Tasks This Week') as a markdown checkbox. Useful for tracking action items from conversations.")]
    public string AddJournalTask(
        [Description("The task description to add.")] string taskDescription,
        [Description("Name of the folder that contains the journal files, relative to vault root. Defaults to '1 Journal'.")] string? journalPath = null,
        [Description("Target date in YYYY-MM-DD to determine which week. Optional; defaults to today.")] string? date = null,
        [Description("Mark task as completed. Defaults to false (unchecked)."), DefaultValue(false)] bool completed = false)
    {
        if (string.IsNullOrWhiteSpace(taskDescription))
        {
            return "No task description provided.";
        }

        journalPath = ResolveJournalPath(journalPath);

        // Resolve tasks heading from vault settings or use default
        string tasksHeading = _vault.GetVaultSetting("journalTasksHeading") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tasksHeading))
        {
            tasksHeading = DefaultTasksHeading;
        }
        else if (!tasksHeading.StartsWith("#"))
        {
            // Ensure heading starts with # if user didn't include it
            tasksHeading = $"## {tasksHeading}";
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

            // Find or construct the path to the weekly file
            string weeklyRelPath = ResolveWeeklyFilePath(journalPath, isoYear, isoWeek);

            // Load existing content or create new stub
            string content = LoadOrCreateWeeklyContent(weeklyRelPath, isoYear, isoWeek);

            var lines = new List<string>(content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));

            // Find the Tasks heading
            int tasksIndex = lines.FindIndex(l => l.Trim().Equals(tasksHeading, StringComparison.Ordinal));
            string checkbox = completed ? "[x]" : "[ ]";
            string taskLine = $"- {checkbox} {taskDescription.Trim()}";

            if (tasksIndex >= 0)
            {
                // Tasks section exists: insert after the heading (before next heading or end of section)
                int insertIndex = tasksIndex + 1;

                // Skip any blank lines immediately after the heading
                while (insertIndex < lines.Count && string.IsNullOrWhiteSpace(lines[insertIndex]))
                {
                    insertIndex++;
                }

                // Find the end of the tasks section (next heading or end of file)
                while (insertIndex < lines.Count && !Level2HeadingRegex.IsMatch(lines[insertIndex]))
                {
                    insertIndex++;
                }

                // Insert the task at the end of the tasks section
                lines.Insert(insertIndex, taskLine);
            }
            else
            {
                // Tasks section doesn't exist: create it after the title
                InsertNewSection(lines, tasksHeading, taskLine);
            }

            // Write updated content and update index
            var updatedContent = string.Join(Environment.NewLine, lines);
            return WriteJournalContent(weeklyRelPath, updatedContent, $"Added task to {weeklyRelPath} under '{tasksHeading}'.");
        }
        catch (ArgumentException ex)
        {
            return $"Error: Invalid journal path - {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Error: Invalid journal path - {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error adding journal task: {ex.Message}";
        }
    }

    /// <summary>
    /// Resolves the journal path from parameter or vault settings.
    /// </summary>
    private string ResolveJournalPath(string? journalPath)
    {
        if (!string.IsNullOrWhiteSpace(journalPath))
            return journalPath;

        var cfg = _vault.GetVaultSetting("journalPath");
        return string.IsNullOrWhiteSpace(cfg) ? DefaultJournalPath : cfg;
    }

    /// <summary>
    /// Finds or constructs the path to the weekly journal file for a given ISO week.
    /// </summary>
    private string ResolveWeeklyFilePath(string journalPath, int isoYear, int isoWeek)
    {
        string weeklyFileName = $"{isoYear}-W{isoWeek:00}.md";

        // Try direct paths first (most common locations)
        var candidatePaths = new[]
        {
            $"{journalPath}/{isoYear}/{weeklyFileName}",
            $"{journalPath}/{weeklyFileName}"
        };

        foreach (var path in candidatePaths)
        {
            var (exists, _, _) = _vault.GetNoteInfo(path);
            if (exists)
                return path;
        }

        // Fallback to enumeration if not found in common locations
        if (_vault.DirectoryExists(journalPath))
        {
            foreach (var (_, rel) in _vault.EnumerateMarkdownFiles(journalPath))
            {
                if (_vault.GetFileName(rel).Equals(weeklyFileName, StringComparison.OrdinalIgnoreCase))
                    return rel;
            }
        }

        // Return default path for creation
        return $"{journalPath}/{isoYear}/{weeklyFileName}".Replace("\\", "/");
    }

    /// <summary>
    /// Loads existing weekly content or creates a new stub if file doesn't exist.
    /// Uses the template service for creating new journal files with proper structure.
    /// </summary>
    private string LoadOrCreateWeeklyContent(string weeklyRelPath, int isoYear, int isoWeek)
    {
        var (exists, _, _) = _vault.GetNoteInfo(weeklyRelPath);
        if (exists)
        {
            try
            {
                return _vault.ReadNoteRaw(weeklyRelPath);
            }
            catch
            {
                return CreateTemplatedWeeklyStub(isoYear, isoWeek);
            }
        }
        return CreateTemplatedWeeklyStub(isoYear, isoWeek);
    }

    /// <summary>
    /// Writes content to the journal file and updates the search index.
    /// </summary>
    private string WriteJournalContent(string weeklyRelPath, string content, string successMessage)
    {
        try
        {
            _vault.WriteNote(weeklyRelPath, content, overwrite: true);
        }
        catch (Exception ex)
        {
            return $"Error writing journal content: {ex.Message}";
        }

        // Update the search index (non-critical operation)
        try
        {
            _index.UpdateFileByRel(weeklyRelPath);
        }
        catch
        {
            // Index update is non-critical; continue
        }

        return successMessage;
    }

    /// <summary>
    /// Inserts a new section with heading and content line into the document.
    /// If a title is found, inserts after it. Otherwise appends to the end.
    /// </summary>
    private static void InsertNewSection(List<string> lines, string sectionHeading, string contentLine)
    {
        // Find the main title (starts with #)
        int titleIndex = lines.FindIndex(l => TitleHeadingRegex.IsMatch(l));

        if (titleIndex >= 0)
        {
            // Insert after title and any blank lines
            int insertIndex = titleIndex + 1;
            while (insertIndex < lines.Count && string.IsNullOrWhiteSpace(lines[insertIndex]))
            {
                insertIndex++;
            }

            // Add blank line before section if previous line has content
            if (insertIndex > 0 && !string.IsNullOrWhiteSpace(lines[insertIndex - 1]))
            {
                lines.Insert(insertIndex, string.Empty);
                insertIndex++;
            }

            lines.Insert(insertIndex, sectionHeading);
            lines.Insert(insertIndex + 1, string.Empty);
            lines.Insert(insertIndex + 2, contentLine);
            lines.Insert(insertIndex + 3, string.Empty);
        }
        else
        {
            // No title found, append at end
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
            {
                lines.Add(string.Empty);
            }
            lines.Add(sectionHeading);
            lines.Add(string.Empty);
            lines.Add(contentLine);
        }
    }

    /// <summary>
    /// Creates a new weekly journal file using the template service.
    /// Generates day headings for all 7 days in the ISO week.
    /// </summary>
    private string CreateTemplatedWeeklyStub(int isoYear, int isoWeek)
    {
        try
        {
            // Get the Monday of this ISO week (first day)
            var monday = ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday);

            // Get the template
            var template = _template.GetDefaultJournalTemplate();


            // Build context with week information
            var context = new Dictionary<string, object>
            {
                ["week_number"] = isoWeek,
                ["year"] = isoYear,
                ["day_number"] = monday.Day,
                ["day_name"] = monday.ToString("dddd"),
                ["journal_day"] = monday,
                ["created_date"] = DateTime.Now,
                ["monday_iso_week"] = monday
            };
            
            // Render the template
            var renderedContent = _template.RenderTemplate(template, context);
            
            // Generate additional day headings for the remaining 6 days of the week
            var sb = new StringBuilder(renderedContent);
            
            for (int i = 1; i <= 6; i++)
            {
                var day = monday.AddDays(i);
                sb.AppendLine();
                sb.AppendLine($"## {day.Day} {day:dddd}");
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        catch (Exception)
        {
            // Fallback to minimal stub if templating fails
            return $"# Week {isoWeek} in {isoYear}\n\n";
        }
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

    // Regex patterns for day section headings (e.g., "## 14 Thursday")
    private static readonly Regex DaySectionHeaderRegex = new(
        @"^##\s+\d{1,2}\s+\w+\s*$",
        RegexOptions.Compiled);

    private static readonly Regex DaySectionHeaderWithCaptureRegex = new(
        @"^##\s+(\d{1,2})\s+\w+\s*$",
        RegexOptions.Compiled);

    // Regex patterns for markdown headings
    private static readonly Regex Level2HeadingRegex = new(
        @"^##\s+",
        RegexOptions.Compiled);

    private static readonly Regex TitleHeadingRegex = new(
        @"^#\s+",
        RegexOptions.Compiled);

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
