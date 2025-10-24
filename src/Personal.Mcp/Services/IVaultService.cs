namespace Personal.Mcp.Services;

/// <summary>
/// Interface for vault operations, enabling testability through mocking
/// </summary>
public interface IVaultService
{
    /// <summary>
    /// Gets the absolute path to the vault root directory
    /// </summary>
    string VaultPath { get; }

    /// <summary>
    /// Converts a vault-relative path to an absolute file system path
    /// </summary>
    string GetAbsolutePath(string relativePath);

    /// <summary>
    /// Reads a note and returns its content with metadata extracted from frontmatter
    /// </summary>
    (string content, IDictionary<string, object> metadata) ReadNote(string relativePath);

    /// <summary>
    /// Reads the raw content of a note including frontmatter
    /// </summary>
    string ReadNoteRaw(string relativePath);

    /// <summary>
    /// Gets file statistics for a note
    /// </summary>
        /// <summary>
    /// Get file statistics (existence, last modified time, size).
    /// </summary>
    (bool exists, DateTimeOffset lastModified, long size) GetFileStats(string relativePath);

    /// <summary>
    /// Checks if a directory exists in the vault
    /// </summary>
    bool DirectoryExists(string relativePath);

    /// <summary>
    /// Gets the filename from a relative path
    /// </summary>
    string GetFileName(string relativePath);

    /// <summary>
    /// Writes content to a note file
    /// </summary>
    void WriteNote(string relativePath, string content, bool overwrite);

    /// <summary>
    /// Enumerates all markdown files in the vault or a subdirectory
    /// </summary>
    IEnumerable<(string abs, string rel)> EnumerateMarkdownFiles(string? directory = null);

    /// <summary>
    /// Gets comprehensive information about a note including existence, metadata, and statistics
    /// </summary>
    (bool exists, IDictionary<string, object> metadata, (long size, int words) stats) GetNoteInfo(string relativePath);

    /// <summary>
    /// Adds tags to a note's frontmatter
    /// </summary>
    bool AddTags(string relativePath, IEnumerable<string> tags);

    /// <summary>
    /// Updates tags in a note's frontmatter
    /// </summary>
    bool UpdateTags(string relativePath, IEnumerable<string> tags, bool merge);

    /// <summary>
    /// Removes tags from a note's frontmatter
    /// </summary>
    bool RemoveTags(string relativePath, IEnumerable<string> tags);

    /// <summary>
    /// Gets the frontmatter dictionary from a note
    /// </summary>
    IDictionary<string, object> GetFrontmatter(string relativePath);

    /// <summary>
    /// Sets a property in a note's frontmatter
    /// </summary>
    bool SetProperty(string relativePath, string key, string? value);

    /// <summary>
    /// Sets a list property in a note's frontmatter
    /// </summary>
    bool SetPropertyList(string relativePath, string key, IEnumerable<string> values);

    /// <summary>
    /// Removes a property from a note's frontmatter
    /// </summary>
    bool RemoveProperty(string relativePath, string key);

    /// <summary>
    /// Searches for notes by frontmatter property
    /// </summary>
    IEnumerable<(string path, object? value)> SearchByProperty(string key, string? value = null, bool contains = false, bool caseInsensitive = true);

    /// <summary>
    /// Gets all images referenced in a note
    /// </summary>
    IReadOnlyList<(string raw, string? resolvedPath, bool exists)> GetNoteImages(string relativePath);

    /// <summary>
    /// Reads a markdown settings file from the vault root (if present) and returns its frontmatter as a key/value dictionary.
    /// </summary>
    IDictionary<string, object> GetVaultSettings();

    /// <summary>
    /// Gets a single vault setting value. Checks the vault root settings file first then environment variables prefixed with PERSONAL_MCP_ (e.g. PERSONAL_MCP_JOURNAL_PATH).
    /// </summary>
    string? GetVaultSetting(string key);
}
