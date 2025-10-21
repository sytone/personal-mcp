using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Personal.Mcp.Services;

/// <summary>
/// Simple in-memory search index using dictionary-based text matching.
/// This is a lightweight alternative to SQLite FTS5 for basic search functionality.
/// </summary>
public class IndexService : IDisposable
{
    private readonly IVaultService _vault;
    private readonly IFileSystem _fileSystem;
    private readonly object _gate = new();
    private readonly Dictionary<string, (long mtime, long size, string content)> _index = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public IndexService(IVaultService vault, IFileSystem fileSystem, IndexConnectionConfig connectionConfig)
    {
        _vault = vault;
        _fileSystem = fileSystem;
        // connectionConfig is ignored in this simple implementation but kept for compatibility
    }

    public void BuildOrUpdateIndex(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            foreach (var file in _fileSystem.Directory.EnumerateFiles(_vault.VaultPath, "*.md", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rel = _fileSystem.Path.GetRelativePath(_vault.VaultPath, file).Replace("\\", "/");
                var info = _fileSystem.FileInfo.New(file);
                var mtime = info.LastWriteTimeUtc.Ticks;
                var size = info.Length;

                // Only update if file changed or not in index
                if (!_index.TryGetValue(rel, out var existing) || 
                    existing.mtime != mtime || 
                    existing.size != size)
                {
                    var content = _fileSystem.File.ReadAllText(file);
                    _index[rel] = (mtime, size, content);
                }
            }

            // Remove deleted files from index
            var currentFiles = _fileSystem.Directory.EnumerateFiles(_vault.VaultPath, "*.md", SearchOption.AllDirectories)
                .Select(f => _fileSystem.Path.GetRelativePath(_vault.VaultPath, f).Replace("\\", "/"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toRemove = _index.Keys.Where(k => !currentFiles.Contains(k)).ToList();
            foreach (var key in toRemove)
            {
                _index.Remove(key);
            }
        }
    }

    public void UpdateFileByRel(string relativePath)
    {
        var rel = relativePath.Replace("\\", "/");
        var abs = _vault.GetAbsolutePath(rel);
        
        if (!_fileSystem.File.Exists(abs))
        {
            RemoveFileByRel(rel);
            return;
        }

        var info = _fileSystem.FileInfo.New(abs);
        var mtime = info.LastWriteTimeUtc.Ticks;
        var size = info.Length;
        var content = _fileSystem.File.ReadAllText(abs);

        lock (_gate)
        {
            _index[rel] = (mtime, size, content);
        }
    }

    public void RemoveFileByRel(string relativePath)
    {
        var rel = relativePath.Replace("\\", "/");
        lock (_gate)
        {
            _index.Remove(rel);
        }
    }

    public void RenameFileByRel(string oldRelativePath, string newRelativePath)
    {
        RemoveFileByRel(oldRelativePath);
        UpdateFileByRel(newRelativePath);
    }

    public IEnumerable<(string path, string snippet)> Search(string query, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            yield break;
        }

        lock (_gate)
        {
            var results = new List<(string path, string snippet, int score)>();
            var queryTerms = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            if (queryTerms.Count == 0)
            {
                yield break;
            }

            foreach (var (path, (_, _, content)) in _index)
            {
                var contentLower = content.ToLowerInvariant();
                var score = 0;
                var matchPositions = new List<int>();

                // Calculate relevance score based on term matches
                foreach (var term in queryTerms)
                {
                    var index = 0;
                    while ((index = contentLower.IndexOf(term, index, StringComparison.Ordinal)) != -1)
                    {
                        score++;
                        matchPositions.Add(index);
                        index += term.Length;
                    }
                }

                if (score > 0)
                {
                    // Extract snippet around first match
                    var snippet = ExtractSnippet(content, matchPositions.Min(), queryTerms);
                    results.Add((path, snippet, score));
                }
            }

            // Return results sorted by relevance score, then by path
            foreach (var result in results.OrderByDescending(r => r.score).ThenBy(r => r.path).Take(limit))
            {
                yield return (result.path, result.snippet);
            }
        }
    }

    private string ExtractSnippet(string content, int matchPosition, List<string> terms)
    {
        const int contextLength = 100;
        var start = Math.Max(0, matchPosition - contextLength / 2);
        var end = Math.Min(content.Length, matchPosition + contextLength);

        // Adjust to word boundaries
        while (start > 0 && !char.IsWhiteSpace(content[start - 1]))
        {
            start--;
            if (matchPosition - start > contextLength) break;
        }

        while (end < content.Length && !char.IsWhiteSpace(content[end]))
        {
            end++;
            if (end - matchPosition > contextLength) break;
        }

        var snippet = content.Substring(start, Math.Min(end - start, content.Length - start));
        
        // Highlight matching terms
        foreach (var term in terms.OrderByDescending(t => t.Length))
        {
            snippet = Regex.Replace(snippet, Regex.Escape(term), m => $"[{m.Value}]", RegexOptions.IgnoreCase);
        }

        var prefix = start > 0 ? "… " : "";
        var suffix = end < content.Length ? " …" : "";
        
        return $"{prefix}{snippet.Trim()}{suffix}";
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            lock (_gate)
            {
                _index.Clear();
            }
        }

        _disposed = true;
    }
}
