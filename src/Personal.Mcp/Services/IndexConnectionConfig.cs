namespace Personal.Mcp.Services;

/// <summary>
/// Configuration for SQLite database connection used by IndexService.
/// </summary>
public class IndexConnectionConfig(string dataSource, bool? inMemory = false)
{
    /// <summary>
    /// Data source path or ":memory:" for in-memory database.
    /// </summary>
    public string DataSource { get; } = dataSource;

    /// <summary>
    /// Indicates if this is an in-memory database that should maintain a persistent connection.
    /// </summary>
    public bool IsInMemory { get; } = inMemory ?? false;

    /// <summary>
    /// Builds the connection string from the configuration properties.
    /// </summary>
    public string BuildConnectionString()
    {
        var parts = new List<string> { $"Data Source={DataSource}" };

        if (IsInMemory)
        {
            parts.Add($"Mode=Memory");
            parts.Add($"Cache=Shared");
        }

        return string.Join(";", parts);
    }
}
