using Personal.Mcp.Services;
using System.Text;
using System.Globalization;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Personal.Mcp.Tests.TestHelpers;

/// <summary>
/// Test fixture that creates an in-memory vault with test data for testing
/// </summary>
public class TestVaultFixture : IDisposable
{
    public string VaultPath { get; }
    public IFileSystem FileSystem { get; }
    public IVaultService VaultService { get; }
    public IndexService IndexService { get; }

    public TestVaultFixture()
    {
        // Use a deterministic path for the mock vault
        VaultPath = @"C:\TestVault";

        // Create mock file system with pre-populated files
        var mockFiles = new Dictionary<string, MockFileData>();
        
        // Create vault root directory
        mockFiles[@"C:\TestVault"] = new MockDirectoryData();
        
        // Populate test files
        PopulateVirtualFileSystem(mockFiles);
        
        // Initialize mock file system with all files
        FileSystem = new MockFileSystem(mockFiles);

        // Set environment variable for VaultService
        Environment.SetEnvironmentVariable("OBSIDIAN_VAULT_PATH", VaultPath);

        // Initialize services with mock file system
        VaultService = new VaultService(FileSystem);
        IndexService = new IndexService(VaultService, FileSystem);
    }

    private void PopulateVirtualFileSystem(Dictionary<string, MockFileData> mockFiles)
    {
        // Create directory structure (directories need to be explicitly added to MockFileSystem)
        var directories = new[]
        {
            @"C:\TestVault\1 Journal",
            @"C:\TestVault\1 Journal\2025",
            @"C:\TestVault\1 Journal\2024",
            @"C:\TestVault\Notes",
            @"C:\TestVault\Projects",
            @"C:\TestVault\.obsidian"
        };

        foreach (var dir in directories)
        {
            mockFiles[dir] = new MockDirectoryData();
        }

        // Create test journal files
        AddMockFile(mockFiles, @"C:\TestVault\1 Journal\2025\2025-W42.md", CreateWeeklyJournalContent(2025, 42));
        AddMockFile(mockFiles, @"C:\TestVault\1 Journal\2025\2025-W41.md", CreateWeeklyJournalContent(2025, 41));
        AddMockFile(mockFiles, @"C:\TestVault\1 Journal\2024\2024-12-31.md", CreateDailyJournalContent(new DateTime(2024, 12, 31)));

        // Create test note files
        AddMockFile(mockFiles, @"C:\TestVault\Notes\Test Note.md", CreateTestNoteContent("Test Note", new[] { "test", "example" }));
        AddMockFile(mockFiles, @"C:\TestVault\Projects\Personal MCP.md", CreateTestNoteContent("Personal MCP Project", new[] { "project", "mcp", "csharp" }));
    }

    private void AddMockFile(Dictionary<string, MockFileData> mockFiles, string fullPath, string content)
    {
        mockFiles[fullPath] = new MockFileData(content);
    }

    private static string CreateWeeklyJournalContent(int year, int week)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"tags:");
        sb.AppendLine($"  - \"journal/weekly/{year}-W{week:00}\"");
        sb.AppendLine("notetype: weekly");
        sb.AppendLine("noteVersion: 5");
        sb.AppendLine("category: weekly");
        sb.AppendLine($"created: {DateTime.Now:yyyy-MM-ddTHH:mm}");
        sb.AppendLine("---");
        sb.AppendLine($"# Week {week} in {year}");
        sb.AppendLine();

        // Add some sample daily entries
        var monday = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        for (int i = 0; i < 7; i++)
        {
            var day = monday.AddDays(i);
            sb.AppendLine($"## {day.Day} {day:dddd}");
            sb.AppendLine();
            sb.AppendLine($"- 09:00 - Sample entry for {day:dddd}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string CreateDailyJournalContent(DateTime date)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"date: {date:yyyy-MM-dd}");
        sb.AppendLine("tags:");
        sb.AppendLine("  - journal/daily");
        sb.AppendLine("---");
        sb.AppendLine($"# {date:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine($"Daily journal entry for {date:MMMM d, yyyy}");
        sb.AppendLine();
        sb.AppendLine("## Tasks");
        sb.AppendLine("- [x] Sample completed task");
        sb.AppendLine("- [ ] Sample pending task");
        sb.AppendLine();
        sb.AppendLine("## Notes");
        sb.AppendLine("Sample journal content for testing");
        return sb.ToString();
    }

    private static string CreateTestNoteContent(string title, string[] tags)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("tags:");
        foreach (var tag in tags)
        {
            sb.AppendLine($"  - {tag}");
        }
        sb.AppendLine($"created: {DateTime.Now:yyyy-MM-ddTHH:mm}");
        sb.AppendLine("---");
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"This is test content for {title}.");
        sb.AppendLine();
        sb.AppendLine("## Section 1");
        sb.AppendLine("Sample content in section 1.");
        sb.AppendLine();
        sb.AppendLine("## Section 2");
        sb.AppendLine("Sample content in section 2.");
        return sb.ToString();
    }

    public void Dispose()
    {
        // With MockFileSystem, no need to clean up physical directories
        // Just clean up environment variable
        Environment.SetEnvironmentVariable("OBSIDIAN_VAULT_PATH", null);
    }
}