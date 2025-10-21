# GitHub Copilot Instructions for obsidian-mcp

**Project**: Personal MCP Server - C# MCP (Model Context Protocol) implementation for Obsidian vault management  
**.NET Version**: net9.0 | **Key Pattern**: Dependency Injection + Attribute-based Tool Discovery

## Architecture Overview

This is an **MCP server** built on the official ModelContextProtocol SDK. The architecture separates concerns into three layers:

### Core Layers

1. **Services** (`src/Personal.Mcp/Services/`) - Singleton DI services managing vault state
   - `VaultService`: File I/O, path validation, YAML frontmatter parsing (vault-relative paths enforced)
   - `IndexService`: SQLite FTS5 full-text search, reactive index updates on file changes
   - `LinkService`: Wiki link parsing `[[target#heading^block|alias]]`, backlink resolution, broken link detection
   - `TagService`: Tag enumeration with counts from frontmatter and inline tags

2. **Tools** (`src/Personal.Mcp/Tools/`) - MCP-exposed tool classes using attributes
   - Each file is a `[McpServerToolType]` static class with multiple `[McpServerTool]` methods
   - Tools receive injected services (VaultService, IndexService, etc.) as parameters
   - Return types must be serializable objects (use anonymous types)
   - Examples: `NoteTools.cs` (read/create/update), `SearchTools.cs` (FTS5 search), `LinkManagementTools.cs` (rename with link rewrite)

3. **Transport** (`Program.cs`) - Flexible stdio/HTTP server setup
   - Default: **stdio transport** for CLI/MCP client use
   - Alt: HTTP transport via `--http` flag for web scenarios
   - Both use same DI/tool discovery pipeline

## Key Patterns & Conventions

### Path Handling (Critical!)
- **All paths are vault-relative using forward slashes** (e.g., `Folder/note.md`)
- `VaultService.GetAbsolutePath()` validates every path:
  - Rejects paths starting with `/` or containing `..`
  - Ensures resolved path stays within vault bounds (prevents directory traversal)
- When iterating files: use `VaultService.EnumerateMarkdownFiles()` for vault-relative paths

### Tool Definition Pattern
```csharp
[McpServerToolType]  // Marks this class as containing MCP tools
public static class FeatureTools
{
    [McpServerTool(Name = "tool_name"), Description("...")]
    public static object MethodName(
        VaultService vault,           // Injected service
        IndexService index,           // Optional, injected
        [Description("...")] string param1,  // Parameters are MCP tool inputs
        [Description("...")] int param2 = 50)
    {
        // Implementation
        return new { success = true, result = "..." };  // Must be serializable
    }
}
```

### Frontmatter Handling
- YAML frontmatter is between `---` delimiters at file start
- `VaultService.SplitFrontmatter()` returns `(frontmatter_dict, body_text)`
- Frontmatter is parsed into `IDictionary<string, object>` (see `PropertyTools.cs` for examples)
- Tag extraction: checks both `tags:` frontmatter field AND inline `#tag` syntax in body

### Index/Search Workflow
- `IndexService.BuildOrUpdateIndex()` scans vault for `*.md` files
- Uses SQLite FTS5 with Porter tokenizer (case-insensitive, stemming)
- **Not reactive** - index updates on explicit search invocation (not file-system watchers)
- Tracks mtime + size to detect changes; rebuilds entries on mismatch
- Thread-safe via lock in `_gate` object

### Link Rewriting (Complex!)
- `LinkService.UpdateLinksForRename()` rewrites all wiki/markdown links when notes are renamed/moved
- Handles:
  - Wiki form: `[[Note#heading^block|alias]]` → preserves alias, heading, block refs
  - Markdown form: `[label](path#heading)` → preserves label, heading/block
  - **Limitations**: Filename-based matching (doesn't resolve full path ambiguities), no transclusion updates
- Called by `move_note` and `rename_note` tools; triggers index reindex of all notes

### Configuration & Startup
- Environment variable: `OBSIDIAN_VAULT_PATH` (required, points to vault root directory)
- Serilog logging to console (stderr) and daily file logs in `logs/` folder
- Build configurations: `Debug` | `Release` | `McpServer` (self-contained, single-file for NuGet publishing)

## Common Development Tasks

### Running Locally
```powershell
$env:OBSIDIAN_VAULT_PATH = "C:\path\to\vault"
dotnet run --project src/Personal.Mcp
```

### Building for NuGet Package
```powershell
dotnet build src/Personal.Mcp/Personal.Mcp.csproj -c Release
dotnet pack src/Personal.Mcp/Personal.Mcp.csproj -c Release -o ./nuget
# Inspect: unzip .nupkg to verify no sensitive files, then publish to nuget.org
```

### Adding a New Tool
1. Create file in `src/Personal.Mcp/Tools/` (e.g., `MyTools.cs`)
2. Add `[McpServerToolType]` class with static `[McpServerTool]` methods
3. Inject `VaultService`, `IndexService` as needed via parameters
4. Program.cs already calls `.WithToolsFromAssembly()` - no registration needed
5. Return serializable object (anonymous type preferred)
6. **CRITICAL: Create comprehensive unit tests** in `tests/Personal.Mcp.Tests/Tools/` (see Testing Guidelines below)

### Testing Guidelines (MANDATORY)

**Every new tool, feature, or modification MUST include unit tests.** Follow these rules:

1. **Test File Location**: `tests/Personal.Mcp.Tests/Tools/[FeatureName]Tests.cs`
2. **Test Class Structure**: Use nested classes for organization (see `JournalToolsTests.cs` as reference)
3. **Required Test Coverage**:
   - ✅ **Happy path**: Valid inputs produce expected outputs
   - ✅ **Edge cases**: Boundary conditions, empty inputs, null values
   - ✅ **Error handling**: Invalid paths, malformed data, permission errors
   - ✅ **Integration**: Multi-step workflows combining multiple operations
   - ✅ **Configuration**: Test with custom vault settings and defaults
   - ✅ **Data validation**: Verify file content, structure, and formatting

4. **Test Naming Convention**: `MethodName_Scenario_ExpectedResult`
   - Example: `AddJournalTask_WithValidTask_AddsToTasksSection`
   - Example: `AddJournalTask_WithEmptyDescription_ReturnsError`

5. **Use TestVaultFixture**: Inherit from `IClassFixture<TestVaultFixture>` for isolated test vault
6. **FluentAssertions**: Use `.Should()` syntax for readable assertions
7. **Theory Tests**: Use `[Theory]` with `[InlineData]` for testing multiple scenarios

**Example Test Structure**:
```csharp
public class FeatureToolsTests : IClassFixture<TestVaultFixture>
{
    private readonly TestVaultFixture _fixture;
    private readonly FeatureTools _featureTools;

    public FeatureToolsTests(TestVaultFixture fixture)
    {
        _fixture = fixture;
        _featureTools = new FeatureTools(_fixture.VaultService, _fixture.IndexService);
    }

    public class MethodNameTests : FeatureToolsTests
    {
        public MethodNameTests(TestVaultFixture fixture) : base(fixture) { }

        [Fact]
        public void MethodName_WithValidInput_ReturnsExpectedResult()
        {
            // Arrange
            var input = "test input";

            // Act
            var result = _featureTools.MethodName(input);

            // Assert
            result.Should().Contain("expected");
        }

        [Theory]
        [InlineData("case1")]
        [InlineData("case2")]
        public void MethodName_WithVariousInputs_HandlesCorrectly(string input)
        {
            // Arrange, Act, Assert
        }
    }
}
```

**Before Committing**:
- ✅ All tests pass: `dotnet test`
- ✅ New code has >80% coverage
- ✅ Edge cases documented in test names
- ✅ No manual testing only - automated tests required

### Debugging Links & Broken References
- Link extraction uses regex: `@"\[\[([^\]|]+)(?:\|[^\]]+)?\]\]"` (captures wiki link target before `|` if present)
- Backlink detection: normalized filename comparison (case-insensitive)
- Run `search_notes` before expecting results (triggers index build)

### Known Limitations & Future Work
- **Link resolution**: Filename-based only; doesn't disambiguate same filename in different folders
- **YAML serialization**: Simple serializer; doesn't preserve comments or key ordering on round-trip
- **Reactive indexing**: Not implemented; index updates on explicit search or tool calls (consider file-system watcher)
- **Section editing**: Line-based heading match; no fuzzy matching across heading levels
- **move_folder**: Doesn't rewrite links or reindex moved tree (TODO)

## Key Files Reference

| File | Purpose |
|------|---------|
| `Program.cs` | MCP server setup, DI registration, transport configuration |
| `Services/VaultService.cs` | Path safety, file I/O, YAML parsing, tag extraction |
| `Services/IndexService.cs` | SQLite FTS5 index, search, reactive update helpers |
| `Services/LinkService.cs` | Link parsing, backlink discovery, link rewriting |
| `Tools/NoteTools.cs` | read_note, create_note, update_note, delete_note |
| `Tools/SearchTools.cs` | search_notes (FTS5), search_by_regex, search_by_date |
| `Tools/LinkManagementTools.cs` | move_note, rename_note (with link rewrite) |
| `Personal.Mcp.csproj` | Package metadata, NuGet config, build configurations |
| `docs/port-details.md` | Implementation status, services inventory, known gaps |

## Security & Best Practices

- **Path validation**: Always use `VaultService.GetAbsolutePath()` - never construct file paths manually
- **No hardcoded secrets**: Use environment variables for configuration
- **Relative paths**: All MCP tool inputs/outputs use vault-relative forward-slash paths
- **Locking**: `IndexService` uses `_gate` lock for thread-safe SQLite access
- **OBSIDIAN_VAULT_PATH**: Must be set before VaultService instantiation; throws if missing

## Testing & Validation

- **Test Framework**: xUnit with FluentAssertions for readable assertions
- **Test Location**: `tests/Personal.Mcp.Tests/` with structure mirroring `src/Personal.Mcp/`
- **Test Execution**: `dotnet test` or `.\scripts\test.ps1`
- **Coverage Goal**: >80% code coverage for all new features
- **Test Vault**: `TestVaultFixture` provides isolated test environment with in-memory filesystem
- **Continuous Testing**: Tests run automatically in CI/CD pipeline
- **Manual Testing**: Use MCP client (VS Code + GitHub Copilot) for integration validation after automated tests pass

**See Testing Guidelines section above for mandatory testing requirements.**



## Public Repository Guidelines

This is a **public repository**. When contributing or modifying files, follow these guidelines:

### Documentation Standards
- **Location**: All documentation files MUST be placed in the `docs/` folder
- **README Exception**: The main `README.md` file is the only documentation file that can exist at the root level
- **No duplicate docs**: Do not create documentation files at the root level or in other directories
- **Always Use Markdown Formatting**: Always use Markdown formatting in the docs files, do not underline headings or use other formatting approaches. Markdown is the only allowed format. 

### File Naming Conventions
- **Format**: Use lowercase letters with dashes (kebab-case) for all filenames
- **Examples**: `port-details.md`, `publishing-to-nuget.md`, `getting-started.md`
- **Exception**: `README.md` is the only file that uses PascalCase at the root level

### Security Requirements
- **No Secrets**: Never commit API keys, passwords, tokens, or credentials
- **No Personal Paths**: Avoid hardcoded absolute paths; use placeholders like `~/repos/` or `C:\path\to\`
- **Environment Variables**: Use environment variables for configuration
- **Sensitive Files**: Add sensitive configuration files to `.gitignore` (e.g., `.vscode/mcp.json` with local vault paths)

### Directory Structure
```
docs/                          # All documentation here
├── README.md                  # Documentation index
├── getting-started.md         # Getting started guide
├── publishing-to-nuget.md     # Publishing guide
└── ...

.github/
├── instructions/              # Development guidelines
│   └── haystack.instructions.md
└── workflows/                 # CI/CD pipelines

src/                           # Source code
├── Personal.Mcp/
└── ...

README.md                       # Main README (root only)
.gitignore
Personal.Mcp.sln
```

---

**Last Updated**: October 18, 2025  
For detailed implementation notes, see `docs/port-details.md`  
