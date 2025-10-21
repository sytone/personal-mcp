# IndexService Configuration Guide

## Overview

The `IndexService` has been updated to support flexible SQLite connection configuration through the `IndexConnectionConfig` class. This enables scenarios like in-memory databases for testing or custom connection settings for production use.

## Connection Configuration

### IndexConnectionConfig Properties

- **DataSource** (required): The SQLite database file path or `:memory:` for in-memory databases
- **Mode** (optional): Connection mode such as `ReadWriteCreate`, `ReadWrite`, or `ReadOnly`
- **Cache** (optional): Cache mode like `Shared` or `Private`
- **IsInMemory** (bool): When `true`, maintains a persistent connection throughout the service lifetime

## Usage Examples

### Default File-Based Database

The default behavior (no configuration) creates a file-based database in the vault's `.obsidian` folder:

```csharp
// Program.cs - Default configuration
builder.Services.AddSingleton<IndexService>();
```

This creates a database at `{VaultPath}/.obsidian/mcp-search-index.db`.

### In-Memory Database (Testing)

For unit tests or temporary data that doesn't need persistence:

```csharp
var config = new IndexConnectionConfig
{
    DataSource = ":memory:",
    IsInMemory = true
};

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<IndexService>();
```

**Important**: When `IsInMemory = true`, the service maintains a persistent connection that's only closed when the service is disposed. This keeps the in-memory database alive.

### Custom File-Based Database

For custom database locations or connection settings:

```csharp
var config = new IndexConnectionConfig
{
    DataSource = "C:/custom/path/search-index.db",
    Mode = "ReadWriteCreate",
    Cache = "Shared",
    IsInMemory = false
};

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<IndexService>();
```

### Read-Only Database

For scenarios where the index should only be queried, not modified:

```csharp
var config = new IndexConnectionConfig
{
    DataSource = "path/to/readonly-index.db",
    Mode = "ReadOnly",
    IsInMemory = false
};

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<IndexService>();
```

## Connection Management

### File-Based Databases

For standard file-based databases (`IsInMemory = false`):
- Each operation opens a new connection
- Connections are disposed immediately after use
- Supports concurrent access with proper locking

### In-Memory Databases

For in-memory databases (`IsInMemory = true`):
- A single connection is opened during construction
- The connection remains open for the service lifetime
- Connection is closed only when `Dispose()` is called
- **Critical**: The in-memory database exists only while the connection is open

## Disposal

The `IndexService` implements `IDisposable`. When registered as a singleton, ASP.NET Core/Host automatically disposes it on application shutdown. For manual lifetime management:

```csharp
using var indexService = new IndexService(vaultService, fileSystem, config);
// Use the service...
// Connection automatically closed on disposal
```

## Testing Example

```csharp
[TestClass]
public class IndexServiceTests
{
    private IVaultService _vaultService;
    private IFileSystem _fileSystem;
    private IndexService _indexService;

    [TestInitialize]
    public void Setup()
    {
        _vaultService = Mock.Of<IVaultService>();
        _fileSystem = new MockFileSystem();
        
        var config = new IndexConnectionConfig
        {
            DataSource = ":memory:",
            IsInMemory = true
        };
        
        _indexService = new IndexService(_vaultService, _fileSystem, config);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _indexService.Dispose();
    }

    [TestMethod]
    public void TestSearch()
    {
        // Test with in-memory database...
    }
}
```

## Connection String Format

The configuration builds SQLite connection strings in this format:

```
Data Source={DataSource}[;Mode={Mode}][;Cache={Cache}]
```

Examples:
- `Data Source=:memory:` (in-memory)
- `Data Source=index.db;Mode=ReadWriteCreate;Cache=Shared` (file-based with options)
- `Data Source=/path/to/db.sqlite;Mode=ReadOnly` (read-only file)

## Migration Notes

### Breaking Changes

None - the default behavior remains unchanged. Existing code continues to work without modification.

### Backward Compatibility

- The constructor with no `IndexConnectionConfig` parameter creates a default file-based configuration
- Existing registrations in `Program.cs` work without changes
- The default database location remains `{VaultPath}/.obsidian/mcp-search-index.db`

## Performance Considerations

- **In-Memory**: Fastest performance, but data is lost on disposal/restart
- **File-Based with WAL**: Good performance with persistence, supports concurrent readers
- **Read-Only Mode**: Prevents accidental modifications, useful for distributed read scenarios

## Troubleshooting

### In-Memory Database Loses Data

**Symptom**: Data disappears after operations complete.

**Cause**: The connection was closed, destroying the in-memory database.

**Solution**: Ensure `IsInMemory = true` in the configuration to maintain the persistent connection.

### File Access Errors

**Symptom**: "Database is locked" or file permission errors.

**Cause**: Multiple processes accessing the same database file, or insufficient permissions.

**Solution**: 
- Use different database files for different processes
- Ensure the application has write permissions to the database directory
- Consider using WAL mode (enabled by default)

### Connection Not Disposed

**Symptom**: File handles remain open after application shutdown.

**Cause**: `IndexService` not properly disposed.

**Solution**: Ensure the service is registered as a singleton and let the DI container handle disposal, or manually dispose in custom lifetime scenarios.
