# Migrate DateTime to DateTimeOffset - Universal Prompt

Use this prompt to systematically migrate any C# project from `DateTime` to `DateTimeOffset` for unambiguous, cross-platform time handling.

---

## What This Prompt Does

When you run this prompt, the AI will:

1. **Analyze the codebase** - Find all DateTime usage and categorize it
2. **Check for DI usage** - Determine if project uses dependency injection
3. **Create a migration plan** - Generate a project-specific plan with checkboxes in `.copilot-tracking/changes/datetime-migration-plan.md`
4. **Add TimeProvider** - Set up TimeProvider in DI for testability (if DI is used)
5. **Update/create instructions** - Update `.github/instructions/csharp-datetime.instructions.md` to enforce DateTimeOffset and TimeProvider
6. **Execute migration** - Systematically convert DateTime to DateTimeOffset + TimeProvider with test-driven approach
7. **Update tests** - Modify tests to use FakeTimeProvider for deterministic, timezone-independent testing
8. **Track progress** - Update the plan file with checkboxes as work completes
9. **Validate results** - Ensure all tests pass and no behavior changes

## Benefits of This Migration

### DateTimeOffset Benefits
- ✅ **Unambiguous**: Represents a single point in time with UTC offset
- ✅ **Cross-platform**: Same behavior on Windows, Linux, macOS
- ✅ **Timezone-aware**: Eliminates timezone confusion

### TimeProvider Benefits
- ✅ **Testable**: Mock time in tests with FakeTimeProvider
- ✅ **Deterministic**: Tests no longer depend on system clock
- ✅ **Timezone-independent**: Tests work in any timezone
- ✅ **Scenario testing**: Easily test edge cases (midnight, DST transitions, specific dates)

---

## How to Use

### Simple Invocation

```
Migrate this C# project from DateTime to DateTimeOffset.

Use the prompt in: .github/prompts/migrate-datetime-to-datetimeoffset.md
```

### With Specific Phase

```
Continue the DateTime migration, starting from Phase 3.

Check .copilot-tracking/changes/datetime-migration-plan.md for current progress.
```

---

## Prompt Instructions for AI

When this prompt is invoked, follow these steps:

### Step 1: Initial Discovery (15 minutes)

1. **Search for DateTime usage** across the entire codebase:
   - Use `grep_search` with pattern `**/*.cs` to find ALL C# files
   - No assumptions about project structure - discover it dynamically
   
   - Categorize usage into:
     - Direct instantiation (DateTime.Now, DateTime.UtcNow, new DateTime())
     - Method parameters and return types
     - Field and property declarations
     - Parse/TryParse operations
     - Third-party library interactions (e.g., ISOWeek.ToDateTime)

2. **Identify project type and structure**:
   - Analyze the actual files found to determine project organization
   - Check for DI usage (look for `Program.cs`, `Startup.cs`, `builder.Services`, `IServiceCollection`)
   - Separate test files from application code (files containing `[Fact]`, `[Test]`, `[TestMethod]`, or in test-related folders)
   - Group remaining files logically based on their actual folder/namespace structure

3. **Analyze test coverage**:
   - Run `dotnet test` to establish baseline (record total/pass/fail counts)
   - Identify methods using DateTime that lack test coverage
   - Note any timezone-sensitive operations

4. **Identify external dependencies**:
   - Check for libraries that require DateTime (SQLite, JSON serializers, etc.)
   - Plan compatibility shims if needed

### Step 2: Create Project-Specific Plan

Create a new file: `.copilot-tracking/changes/datetime-migration-plan.md`

The plan should include:

```markdown
# DateTime to DateTimeOffset Migration Plan

**Project**: [Project Name]
**Created**: [Date]
**Baseline Tests**: [X total, Y passed, Z failed]

## Analysis Summary

### Project Structure
- Uses DI: [Yes/No - detected by presence of IServiceCollection, Program.cs with builder.Services, etc.]
- Organization: [Describe the actual folder/namespace structure discovered]

### DateTime Usage Inventory
[Organize by the actual structure found in the project]

**Application/Library Files:**
[Group logically based on discovered structure - could be by folder, namespace, or flat list]
- Path/To/File1.cs (X occurrences)
- Path/To/File2.cs (X occurrences)
- Path/To/File3.cs (X occurrences)

**Test Files:**
[Files identified as tests]
- Path/To/Test1.cs (X occurrences)
- Path/To/Test2.cs (X occurrences)

**Total occurrences:** [N]

### External Dependencies
- [List any libraries requiring DateTime]
- [Note any special handling needed]

## Migration Phases

### Phase 1: Setup ✅ / ⏳ / ⬜
- [ ] Add `TimeProvider` to DI container (if project uses DI)
- [ ] Add `Microsoft.Extensions.TimeProvider.Testing` to test project(s)

### Phase 2: Application/Library Code ✅ / ⏳ / ⬜
[List all non-test C# files with DateTime usage, grouped logically if structure is apparent]

- [ ] `Path/To/File1.cs` (X occurrences)
- [ ] `Path/To/File2.cs` (X occurrences)
- [ ] `Path/To/File3.cs` (X occurrences)
[Continue for all application/library files]

### Phase 3: Test Files ✅ / ⏳ / ⬜
- [ ] Update tests to inject `FakeTimeProvider` instead of relying on real time
- [ ] `Path/To/Test1.cs` (X occurrences)
- [ ] `Path/To/Test2.cs` (X occurrences)
[List all test files - identified by test attributes/assertions or being in test projects]

### Phase 4: Validation ✅ / ⏳ / ⬜
- [ ] All tests pass (baseline: X)
- [ ] Build succeeds in all configurations
- [ ] No unexpected DateTime references remain
- [ ] Documentation updated

## Migration Log

[AI updates this section as work progresses]

### [Date] - Phase 1 Started
- Migrated: ServiceFile1.cs
- Tests: ✅ All passing (X/X)
- Notes: [Any observations]

[Continue logging each change]
```

### Step 3: Add TimeProvider to Dependency Injection

**Check if the project uses DI** (look for one of these patterns):

- `Program.cs` with `builder.Services` (modern .NET)
- `Startup.cs` with `ConfigureServices` method (older ASP.NET Core)
- `IServiceCollection` extensions
- Manual DI container setup (Autofac, etc.)

**If DI is used**, add `TimeProvider` to the service collection:

**In `Program.cs` (modern .NET 6+)**:
```csharp
var builder = WebApplication.CreateBuilder(args); // or Host.CreateApplicationBuilder(args)
// Add TimeProvider as singleton for testable time operations
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
```

**In `Startup.cs` (older ASP.NET Core)**:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add TimeProvider as singleton for testable time operations
    services.AddSingleton<TimeProvider>(TimeProvider.System);
    // ... other services
}
```

**For console apps or services**:
```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        // ... other services
    })
    .Build();
```

**Update classes to use TimeProvider via DI**:
```csharp
// Before: Direct DateTimeOffset.Now usage
public class OrderService
{
    public void ProcessOrder()
    {
        var now = DateTimeOffset.Now;  // Hard to test
    }
}

// After: TimeProvider injected via constructor (primary constructor)
public class OrderService(TimeProvider timeProvider)
{
    public void ProcessOrder()
    {
        var now = timeProvider.GetUtcNow();  // Easily testable
    }
}

// After: TimeProvider injected via constructor (traditional)
public class OrderService
{
    private readonly TimeProvider _timeProvider;
    
    public OrderService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void ProcessOrder()
    {
        var now = _timeProvider.GetUtcNow();
    }
}

// For ASP.NET Controllers
public class OrdersController(TimeProvider timeProvider) : ControllerBase
{
    [HttpPost]
    public IActionResult CreateOrder()
    {
        var timestamp = timeProvider.GetUtcNow();
        // ...
    }
}
```

**Add to test project** (if not already present):
- Add NuGet package: `Microsoft.Extensions.TimeProvider.Testing`
- Use `FakeTimeProvider` in tests to control time

**Example test with FakeTimeProvider**:
```csharp
[Fact]
public void ProcessOrder_OnSpecificDate_ReturnsExpectedResult()
{
    // Arrange
    var fakeTime = new FakeTimeProvider();
    fakeTime.SetUtcNow(new DateTimeOffset(2025, 10, 24, 10, 30, 0, TimeSpan.Zero));
    var service = new OrderService(fakeTime);
    
    // Act
    var result = service.ProcessOrder();
    
    // Assert
    result.Timestamp.Should().Be(fakeTime.GetUtcNow());
}
```

**If DI is NOT used** (simple console app, utilities, static helpers):
- Document that TimeProvider could be added later for better testability
- Use DateTimeOffset.Now/UtcNow directly for now
- Add comment: `// TODO: Consider adding DI and TimeProvider for testability`
- Focus on DateTime → DateTimeOffset conversion only

### Step 4: Update/Create Instructions File

**Check if exists**: `.github/instructions/csharp-datetime.instructions.md`

**If exists**: Read the file and update it to include DateTimeOffset and TimeProvider guidance
**If not exists**: Create it with this content:

```markdown
---
applyTo: "**/*.cs"
---

# C# Date and Time Handling Guidelines

## Critical Rules

1. **Use `DateTimeOffset`**, not `DateTime`
2. **Use `TimeProvider`** for dependency injection and testability

---

## TimeProvider Pattern (Preferred)

**For classes using DI**, inject `TimeProvider` instead of calling `DateTimeOffset.Now` directly:

```csharp
// ✅ BEST - Testable with TimeProvider
public class MyService(TimeProvider timeProvider)
{
    public void DoWork()
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset local = timeProvider.GetLocalNow();
    }
}

// ❌ AVOID - Hard to test
public class MyService
{
    public void DoWork()
    {
        var now = DateTimeOffset.Now;  // Can't control in tests
    }
}
```

**Register in DI** (`Program.cs`):
```csharp
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
```

**Test with FakeTimeProvider** (requires `Microsoft.Extensions.TimeProvider.Testing`):
```csharp
[Fact]
public void Operation_OnSpecificDate_ReturnsExpectedResult()
{
    var fakeTime = new FakeTimeProvider();
    fakeTime.SetUtcNow(new DateTimeOffset(2025, 10, 24, 10, 30, 0, TimeSpan.Zero));
    var service = new MyService(fakeTime);
    
    var result = service.DoWork();
    
    result.Should().Be(expectedValue);
}
```

---

## DateTimeOffset Patterns

### When DI Is Available (Services, Tools with DI)

| Operation | Code |
|-----------|------|
| Get current UTC time | `timeProvider.GetUtcNow()` |
| Get current local time | `timeProvider.GetLocalNow()` |
| Get timestamp | `timeProvider.GetTimestamp()` |

### When DI Is NOT Available (Utilities, Static Methods)

| Operation | Code |
|-----------|------|
| Current time | `DateTimeOffset.Now` |
| UTC time | `DateTimeOffset.UtcNow` |
| Today's date | `DateTimeOffset.Now.LocalDateTime.Date` |

### General Operations (Same Everywhere)

| Operation | Code |
|-----------|------|
| Create specific date | `new DateTimeOffset(2025, 10, 24, 0, 0, 0, TimeSpan.Zero)` |
| Parse string | `DateTimeOffset.Parse(str)` |
| Try parse | `DateTimeOffset.TryParse(str, out var result)` |
| Format | `dto.ToString("yyyy-MM-dd")` |
| ISO 8601 | `dto.ToString("o")` |
| Add time | `dto.AddDays(7)` |
| Compare | `dto1 > dto2` |

---

## Special Cases

### ISO Week (returns DateTime)
```csharp
var dt = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
var dto = new DateTimeOffset(dt, TimeSpan.Zero);
```

### Date-Only Operations
```csharp
var dateOnly = DateTimeOffset.Now.LocalDateTime.Date;
```

### File System Dates
```csharp
var fileInfo = new FileInfo(path);
var lastModified = new DateTimeOffset(fileInfo.LastWriteTime, DateTimeOffset.Now.Offset);
```

### External APIs Requiring DateTime
```csharp
// REQUIRED: ExternalApi needs DateTime
var result = api.Process(dateTimeOffset.DateTime);
```

---

## When DateTime Is Acceptable

**ONLY** use `DateTime` when:
1. External library explicitly requires it (add comment)
2. Framework method returns it (convert to DateTimeOffset immediately)

---

## Migration Resources

- Full migration guide: `.github/prompts/migrate-datetime-to-datetimeoffset.md`
- TimeProvider docs: https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview

---

**Last Updated**: October 24, 2025
```

### Step 4: Execute Migration (Test-Driven)

**Important**: Prioritize using `TimeProvider` via DI for better testability.

For each file identified in the plan:

1. **Before changing the file**:
   - Run relevant tests: `dotnet test --filter "ClassName"`
   - Ensure tests pass

2. **Make the changes**:
   
   **If class uses DI** (has constructor injection):
   - Add `TimeProvider` parameter to constructor
   - Replace `DateTimeOffset.Now` → `timeProvider.GetUtcNow()`
   - Replace `DateTimeOffset.UtcNow` → `timeProvider.GetUtcNow()`
   - Replace `DateTimeOffset.Now` (local) → `timeProvider.GetLocalNow()`
   
   **If class doesn't use DI** (static methods, utilities):
   - Replace `DateTime` → `DateTimeOffset` following patterns
   - Add comment: `// TODO: Consider TimeProvider for testability`
   
   **All classes**:
   - Handle special cases (ISO week, date-only, external APIs)
   - Preserve string output formats (yyyy-MM-dd remains unchanged)

3. **Update corresponding tests**:
   - Inject `FakeTimeProvider` instead of real TimeProvider
   - Set specific time: `fakeTime.SetUtcNow(new DateTimeOffset(2025, 10, 24, 10, 0, 0, TimeSpan.Zero))`
   - This makes tests deterministic and timezone-independent

4. **After changing the file**:
   - Run tests again: `dotnet test --filter "ClassName"`
   - Verify tests still pass (now with controlled time)
   - Update plan with `[x]` checkbox
   - Add entry to Migration Log

5. **If tests fail**:
   - Analyze the failure
   - Determine if it's expected (timezone handling change) or a bug
   - Fix the issue or revert the change
   - Document the issue in the plan

### Step 5: Validation

After all files migrated:

1. **Run full test suite**: `dotnet test`
   - Compare to baseline (should be same or more tests passing)

2. **Build all configurations**:
   - `dotnet build -c Debug`
   - `dotnet build -c Release`
   - Any custom configurations

3. **Search for remaining DateTime**:
   - Use grep_search to find any remaining usage
   - Verify each is intentional/documented

4. **Update documentation**:
   - Update README if it mentions date formats
   - Update CHANGELOG.md
   - Update any docs/ files referencing DateTime

### Step 6: Completion

Mark all Phase 4 checkboxes complete and add final summary to Migration Log:

```markdown
## Migration Complete ✅

**Completion Date**: [Date]
**Files Changed**: [N]
**Test Results**: [X/X passing]
**Build Status**: ✅ All configurations
**Remaining DateTime**: [Only documented exceptions]

### Summary
- Total DateTime occurrences replaced: [N]
- Test coverage added: [N new tests]
- Known exceptions: [List files with documented DateTime usage]
```

---

## Replacement Patterns

The AI should use these patterns during migration:

### Setup: Add TimeProvider to DI

**In `Program.cs` or startup**:
```csharp
// Add this line to service registration
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
```

**In test project `.csproj`**:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="9.0.0" />
</ItemGroup>
```

### Current Time (Preferred: TimeProvider)

**For classes with DI**:
```csharp
// ❌ Before - Hard to test
public class ReportGenerator
{
    public void GenerateReport()
    {
        var now = DateTime.Now;
        // ...
    }
}

// ✅ After - Testable with TimeProvider (primary constructor syntax)
public class ReportGenerator(TimeProvider timeProvider)
{
    public void GenerateReport()
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        // or timeProvider.GetLocalNow() for local time
        // ...
    }
}

// ✅ After - Testable with TimeProvider (traditional constructor)
public class ReportGenerator
{
    private readonly TimeProvider _timeProvider;
    
    public ReportGenerator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void GenerateReport()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        // ...
    }
}
```

**For static utilities or simple cases**:
```csharp
// ❌ Before
var now = DateTime.Now;
var utc = DateTime.UtcNow;

// ✅ After (when TimeProvider isn't available)
var now = DateTimeOffset.Now;
var utc = DateTimeOffset.UtcNow;

// Consider adding comment:
// TODO: Consider using TimeProvider via DI for better testability
```

### Creating Specific Dates
```csharp
// ❌ Before
var date = new DateTime(2025, 10, 24);
var created = DateTime.Now;

// ✅ After
var date = new DateTimeOffset(2025, 10, 24, 0, 0, 0, TimeSpan.Zero);
var created = DateTimeOffset.Now;
```

### Parsing Strings
```csharp
// ❌ Before
DateTime.TryParse(input, out var result);
DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, 
    DateTimeStyles.None, out var date);

// ✅ After
DateTimeOffset.TryParse(input, out var result);
DateTimeOffset.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture,
    DateTimeStyles.None, out var date);
```

### Date-Only Operations
```csharp
// ❌ Before
var today = DateTime.Today;
var date = someDateTime.Date;

// ✅ After
var today = DateTimeOffset.Now.LocalDateTime.Date;
var date = someDateTimeOffset.LocalDateTime.Date;
```

### ISO Week Calculations
```csharp
// ❌ Before
var monday = ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday);

// ✅ After
var mondayDT = ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday);
var monday = new DateTimeOffset(mondayDT, TimeSpan.Zero);
```

### Time Arithmetic
```csharp
// ❌ Before
var tomorrow = DateTime.Now.AddDays(1);
var lastWeek = DateTime.Now.AddDays(-7);

// ✅ After (with TimeProvider)
var now = timeProvider.GetUtcNow();
var tomorrow = now.AddDays(1);
var lastWeek = now.AddDays(-7);

// ✅ After (without TimeProvider)
var tomorrow = DateTimeOffset.Now.AddDays(1);
var lastWeek = DateTimeOffset.Now.AddDays(-7);
```

### Test Patterns with FakeTimeProvider

```csharp
// ✅ Test with controlled time
[Fact]
public void GenerateReport_CreatesReportWithCurrentTimestamp()
{
    // Arrange
    var fakeTime = new FakeTimeProvider();
    var expectedTime = new DateTimeOffset(2025, 10, 24, 14, 30, 0, TimeSpan.Zero);
    fakeTime.SetUtcNow(expectedTime);
    var generator = new ReportGenerator(fakeTime);
    
    // Act
    var report = generator.GenerateReport();
    
    // Assert
    report.Timestamp.Should().Be(expectedTime);
}

[Theory]
[InlineData("2025-10-20")] // Monday
[InlineData("2025-10-25")] // Saturday
public void Operation_OnDifferentDays_BehavesCorrectly(string dateString)
{
    // Arrange - Test different dates easily
    var fakeTime = new FakeTimeProvider();
    fakeTime.SetUtcNow(DateTimeOffset.Parse(dateString));
    var service = new MyService(fakeTime);
    
    // Act & Assert
    // Test date-specific behavior
}
```

### Formatting (Usually Unchanged)
```csharp
// ✅ Both work the same
var formatted = dateTimeOffset.ToString("yyyy-MM-dd");
var iso = dateTimeOffset.ToString("o");  // ISO 8601 with offset
```

### Nullable Dates
```csharp
// ❌ Before
DateTime? optional = null;

// ✅ After
DateTimeOffset? optional = null;
```

---

## Critical Guidelines for AI

### Test-Driven Approach
- **NEVER** skip running tests after a change
- **ALWAYS** compare test results to baseline
- **STOP** immediately if tests fail unexpectedly
- **DOCUMENT** any test changes needed

### Progress Tracking
- **UPDATE** the plan file after each file migration
- **MARK** checkboxes as `[x]` when complete
- **LOG** each change in the Migration Log section
- **NOTE** any issues or observations

### Safety First
- **ONE FILE** at a time
- **TEST** after each file
- **COMMIT** frequently (suggest to user after each phase)
- **REVERT** if problems occur

### Communication
- **SHOW** the user what you're doing
- **EXPLAIN** why certain changes are needed
- **ASK** if unsure about edge cases
- **CONFIRM** before proceeding to next phase

---

## Edge Cases to Handle

### 1. File System Dates
```csharp
// FileInfo.LastWriteTime returns DateTime
var fileInfo = new FileInfo(path);
var lastModified = new DateTimeOffset(fileInfo.LastWriteTime, DateTimeOffset.Now.Offset);
```

### 2. Database Storage
```csharp
// Store as ISO 8601 string
var dbValue = dateTimeOffset.ToString("o");

// Retrieve
if (DateTimeOffset.TryParse(dbValue, out var parsed))
{
    // Use parsed
}
```

### 3. JSON Serialization
```csharp
// System.Text.Json handles DateTimeOffset automatically
var json = JsonSerializer.Serialize(new { date = DateTimeOffset.Now });
// {"date":"2025-10-24T10:30:00+00:00"}
```

### 4. Third-Party Libraries
```csharp
// If library requires DateTime, document it
// REQUIRED: OldLibrary.Process requires DateTime
var result = OldLibrary.Process(dateTimeOffset.DateTime);
```

---

## When to Stop and Ask

The AI should stop and ask the user if:

1. **Tests fail** and it's unclear if the failure is expected
2. **External API** requires DateTime but purpose is unclear
3. **Performance concerns** - massive number of DateTime uses
4. **Ambiguous timezone** - unclear if UTC or local is intended
5. **Breaking changes** - migration would change public API

---

## Success Criteria

Migration is complete when:

- ✅ All checkboxes in plan marked `[x]`
- ✅ All tests passing (same count or more than baseline)
- ✅ All builds successful
- ✅ Only documented DateTime exceptions remain
- ✅ Instructions file created/updated
- ✅ Migration log complete

---

## Example Usage Session

**User**: 
```
Migrate this C# project from DateTime to DateTimeOffset.
Use .github/prompts/migrate-datetime-to-datetimeoffset.md
```

**AI Response**:
```
I'll migrate this project from DateTime to DateTimeOffset with TimeProvider for testability.

[Searches for DateTime usage in **/*.cs...]

Found DateTime usage across the codebase:
- Application/Library files: 14 files (37 occurrences)
- Test files: 6 files (15 occurrences)
Total: 20 files, 52 occurrences

Project structure discovered:
- Controllers folder (3 files)
- Services folder (5 files)
- Models folder (2 files)
- Middleware folder (1 file)
- Several test files

[Runs baseline tests...]

Baseline test results: 87 total, 87 passed, 0 failed

[Checks for DI usage...]

Project uses dependency injection (found Program.cs with builder.Services).
Will add TimeProvider to DI container for better testability.

[Creates migration plan at .copilot-tracking/changes/datetime-migration-plan.md]

I've created a migration plan organized by project layers:
- Phase 1: Setup (add TimeProvider to DI)
- Phase 2: Application code (Controllers, Services, Models, Middleware)
- Phase 3: Tests (update to use FakeTimeProvider)
- Phase 4: Validation

Would you like me to proceed with Phase 1?
```

**User**: "Yes, proceed"

**AI**:
```
Phase 1: Setup

Step 1: Adding TimeProvider to DI
- Modified Program.cs to add: builder.Services.AddSingleton<TimeProvider>(TimeProvider.System)
- Added Microsoft.Extensions.TimeProvider.Testing to test project
✅ Complete

Phase 2: Application Code

Step 1: Migrating Controllers/OrdersController.cs (3 DateTime occurrences)...
- Added TimeProvider parameter to constructor
- Replaced DateTimeOffset.Now with timeProvider.GetUtcNow()
Running tests: dotnet test --filter "OrdersController"
✅ All tests pass (5/5)
[x] OrdersController.cs ✅

Step 2: Migrating Services/OrderService.cs (8 DateTime occurrences)...
- Added TimeProvider to constructor
- Updated timestamp generation
Running tests: dotnet test --filter "OrderService"
✅ All tests pass (12/12)
[x] OrderService.cs ✅

Step 3: Updating OrderServiceTests.cs...
- Injected FakeTimeProvider in test setup
- Set specific time for deterministic testing
✅ All tests pass (12/12)
[x] OrderServiceTests.cs ✅

[Continues through all files organized by folder/namespace...]

Phase 2 complete. All application code updated.
All tests passing (87/87).

Proceed to Phase 3 (remaining test updates)?
```

---

## Iterative Refinement

After running this prompt on a project, update this file with:
- New edge cases discovered
- Better patterns found
- Common issues encountered
- Improved instructions

This makes the prompt better for future projects.

---

**Last Updated**: October 24, 2025
**Use On**: Any C# project with DateTime usage
