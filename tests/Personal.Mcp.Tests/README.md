# Personal MCP Testing Documentation

## Test Structure

The test project `Personal.Mcp.Tests` provides comprehensive unit tests for the Personal MCP server, focusing initially on the JournalTools functionality.

### Test Organization

Tests are organized into the following categories:

- **ReadJournalEntriesTests**: Tests for reading and filtering journal entries
- **AddJournalEntryTests**: Tests for adding entries to weekly journal files
- **EdgeCaseTests**: Tests for error handling and boundary conditions
- **IntegrationTests**: End-to-end workflow tests

### Test Fixtures

The `TestVaultFixture` class creates isolated test environments:

- Creates temporary vault directories for each test run
- Sets up realistic test data (weekly journals, daily entries, notes)
- Manages environment variables for VaultService initialization
- Automatically cleans up after tests complete

### Key Test Coverage

#### JournalTools.ReadJournalEntries
- ✅ Default parameter handling
- ✅ Custom journal path validation
- ✅ Date range filtering
- ✅ Content inclusion/exclusion options
- ✅ Maximum entry limits
- ✅ Error handling for invalid paths

#### JournalTools.AddJournalEntry
- ✅ Adding to existing weekly files
- ✅ Creating new weekly files
- ✅ Correct day heading placement
- ✅ Multiple entries per day
- ✅ Timestamp inclusion
- ✅ Date parsing and ISO week calculation
- ✅ Custom journal path support
- ✅ Input validation and error handling

#### Edge Cases
- ✅ Year boundary transitions (ISO week handling)
- ✅ Invalid date handling with fallback
- ✅ Malformed file handling
- ✅ Missing vault path scenarios

#### Integration Tests
- ✅ End-to-end add/read workflows
- ✅ Multi-week consistency
- ✅ Cross-component interactions

### Running Tests

```powershell
# Run all tests
dotnet test tests/Personal.Mcp.Tests/Personal.Mcp.Tests.csproj

# Run with verbose output
dotnet test tests/Personal.Mcp.Tests/Personal.Mcp.Tests.csproj --verbosity normal

# Run specific test category
dotnet test --filter "AddJournalEntryTests"

# Run single test
dotnet test --filter "AddJournalEntry_WithValidContent_AddsEntryToExistingWeeklyFile"
```

### Test Statistics

- **Total Tests**: 29
- **Passing**: 27
- **Known Issues**: 2 (related to fixture isolation)

### Known Test Issues

1. **Current Date Test**: Test assumes specific weekly file exists, needs dynamic date handling
2. **Custom Path Test**: Fixture isolation causes directory creation issues between test instances

### Future Test Additions

Additional test classes can be added for:

- VaultService tests
- IndexService tests  
- LinkService tests
- TagService tests
- Other tool classes (NoteTools, SearchTools, etc.)

### Test Data

The test fixture creates realistic test data:

- Weekly journal files with ISO week naming (2025-W42.md)
- Daily journal entries with proper frontmatter
- Sample notes with tags and metadata
- Proper directory structure mimicking real Obsidian vaults

## Adding New Tests

To add tests for other components:

1. Create new test class in `tests/Personal.Mcp.Tests/Tools/` or `tests/Personal.Mcp.Tests/Services/`
2. Use the existing `TestVaultFixture` or create specialized fixtures
3. Follow the existing pattern of nested test classes for organization
4. Use FluentAssertions for readable test assertions
5. Include comprehensive edge case and error handling tests

Example structure for new tool tests:

```csharp
public class NewToolTests : IClassFixture<TestVaultFixture>
{
    private readonly TestVaultFixture _fixture;
    private readonly NewTool _newTool;

    public NewToolTests(TestVaultFixture fixture)
    {
        _fixture = fixture;
        _newTool = new NewTool(_fixture.VaultService, _fixture.IndexService);
    }

    public class FeatureTests : NewToolTests
    {
        public FeatureTests(TestVaultFixture fixture) : base(fixture) { }

        [Fact]
        public void Method_WithValidInput_ReturnsExpectedResult()
        {
            // Arrange, Act, Assert
        }
    }
}
```