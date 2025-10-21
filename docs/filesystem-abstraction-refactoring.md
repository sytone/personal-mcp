# File System Abstraction Refactoring

**Date**: October 20, 2025  
**Issue**: Tools were using `System.IO` classes directly instead of `IFileSystem` abstraction  
**Resolution**: Refactored all tool classes to use `IFileSystem` for file operations

## Problem

Several tool classes in `src/Personal.Mcp/Tools/` were using direct `System.IO` calls (`Directory`, `Path`, `File`, `FileInfo`) instead of the `IFileSystem` abstraction provided by `System.IO.Abstractions`. This violated the project's architecture pattern and made the code:

- **Harder to test**: Direct I/O operations can't be mocked
- **Less consistent**: Some tools used abstraction, others didn't
- **Security risk**: Bypassed `VaultService` path validation in some cases

## Architecture Pattern

The project uses **Dependency Injection** with the following pattern:

1. **VaultService** manages vault-level operations and uses `IFileSystem` internally
2. **Tools** receive `IFileSystem` as an injected parameter (alongside `IVaultService`)
3. All file operations go through `IFileSystem` methods:
   - `fileSystem.Directory.*` instead of `Directory.*`
   - `fileSystem.Path.*` instead of `Path.*`
   - `fileSystem.File.*` instead of `File.*`
   - `fileSystem.FileInfo.New()` instead of `new FileInfo()`

## Files Refactored

### 1. OrganizationTools.cs
**Methods Updated**: `ListNotes`, `ListFolders`, `CreateFolder`

**Changes**:
- Added `IFileSystem fileSystem` parameter to all methods
- Replaced `Directory.EnumerateFiles` → `fileSystem.Directory.EnumerateFiles`
- Replaced `Directory.EnumerateDirectories` → `fileSystem.Directory.EnumerateDirectories`
- Replaced `Directory.CreateDirectory` → `fileSystem.Directory.CreateDirectory`
- Replaced `Path.GetRelativePath` → `fileSystem.Path.GetRelativePath`
- Replaced `Path.GetFileName` → `fileSystem.Path.GetFileName`
- Replaced `Path.Combine` → `fileSystem.Path.Combine`
- Replaced `File.Exists` → `fileSystem.File.Exists`
- Replaced `File.WriteAllText` → `fileSystem.File.WriteAllText`
- Replaced `new FileInfo()` → `fileSystem.FileInfo.New()`

### 2. LinkManagementTools.cs
**Methods Updated**: `FindBrokenLinks`

**Changes**:
- Added `IFileSystem fileSystem` parameter
- Replaced `Path.GetDirectoryName` → `fileSystem.Path.GetDirectoryName`
- Note: `GetBacklinks` didn't need changes (no direct I/O)

### 3. PropertyRegexDateTools.cs
**Methods Updated**: `SearchByRegex`, `SearchByDate`

**Changes**:
- Added `IFileSystem fileSystem` parameter to both methods
- Replaced `Directory.EnumerateFiles` → `fileSystem.Directory.EnumerateFiles`
- Replaced `Path.GetRelativePath` → `fileSystem.Path.GetRelativePath`
- Replaced `File.ReadAllText` → `fileSystem.File.ReadAllText`
- Replaced `new FileInfo()` → `fileSystem.FileInfo.New()`

### 4. MoveRenameTools.cs
**Methods Updated**: `MoveNote`, `RenameNote`, `MoveFolder`

**Changes**:
- Added `IFileSystem fileSystem` parameter to all methods
- Replaced `Directory.CreateDirectory` → `fileSystem.Directory.CreateDirectory`
- Replaced `Directory.EnumerateDirectories` → `fileSystem.Directory.EnumerateDirectories`
- Replaced `Directory.EnumerateFiles` → `fileSystem.Directory.EnumerateFiles`
- Replaced `Directory.Delete` → `fileSystem.Directory.Delete`
- Replaced `Path.GetDirectoryName` → `fileSystem.Path.GetDirectoryName`
- Replaced `File.Move` → `fileSystem.File.Move`
- Replaced `File.Copy` → `fileSystem.File.Copy`
- Changed parameter from `overwrite: true` to `true` (positional)

## Testing

All existing tests pass after refactoring:
- ✅ **52 tests passed**
- ✅ **0 tests failed**

The test suite uses `System.IO.Abstractions.TestingHelpers.MockFileSystem` to provide a testable file system, which now works correctly with all refactored tools.

## Benefits

1. **Testability**: All tools can now be tested with mock file systems
2. **Consistency**: All code follows the same pattern (Services + Tools use IFileSystem)
3. **Security**: All file operations can be validated through VaultService
4. **Maintainability**: Easier to add features like auditing, caching, or alternative storage backends

## Files Not Changed

The following files already used proper abstractions and didn't require changes:

- `VaultService.cs` - Already used `_fileSystem` internally ✅
- `NoteTools.cs` - Uses `VaultService` methods, no direct I/O ✅
- `SearchTools.cs` - Uses `IndexService`, no direct I/O ✅
- `EditTools.cs` - Uses `VaultService.ReadNoteRaw/WriteNote`, no direct I/O ✅
- `ImageTools.cs` - Uses `VaultService` methods ✅
- `JournalTools.cs` - Uses `VaultService` methods ✅
- `LinkTools.cs` - Uses `VaultService` methods ✅
- `TagTools.cs` - Uses `VaultService` methods ✅
- `PropertyTools.cs` - Uses `VaultService` methods ✅
- `DateUtilityTools.cs` - Pure date calculations, no I/O ✅

## Verification Commands

```powershell
# Check for any remaining direct System.IO usage (should only show VaultService)
dotnet build src/Personal.Mcp/Personal.Mcp.csproj --no-incremental

# Run tests to verify refactoring
dotnet test tests/Personal.Mcp.Tests/Personal.Mcp.Tests.csproj

# Search for any lingering System.IO direct usage in Tools
# (Should return no matches after refactoring)
grep -r "using System.IO;" src/Personal.Mcp/Tools/
```

## Related Documentation

- [Publishing to NuGet](./publishing-to-nuget.md) - Build and package process
- [Port Details](./port-details.md) - Overall architecture and implementation notes
- [Date Accuracy](./date-accuracy.md) - Date handling tools and patterns

## Future Work

- Consider adding more comprehensive integration tests for file operations
- Add logging/auditing to IFileSystem wrapper for debugging
- Document IFileSystem usage pattern in GitHub Copilot instructions
