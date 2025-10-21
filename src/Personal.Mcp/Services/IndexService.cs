using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace Personal.Mcp.Services;

public class IndexService : IDisposable
{
    private readonly IVaultService _vault;
    private readonly IFileSystem _fileSystem;
    private readonly IndexConnectionConfig _connectionConfig;
    private readonly object _gate = new();
    private SqliteConnection? _persistentConnection;
    private bool _disposed;

    public IndexService(IVaultService vault, IFileSystem fileSystem, IndexConnectionConfig connectionConfig)
    {
        _vault = vault;
        _fileSystem = fileSystem;
        _connectionConfig = connectionConfig;

        // For in-memory databases, create and keep connection open
        if (_connectionConfig.IsInMemory)
        {
            _persistentConnection = new SqliteConnection(_connectionConfig.BuildConnectionString());
            _persistentConnection.Open();
        }

        EnsureSchema();
    }

    private SqliteConnection Open()
    {
        // For in-memory databases, return the persistent connection
        if (_connectionConfig.IsInMemory && _persistentConnection != null)
        {
            return _persistentConnection;
        }

        // For file-based databases, create a new connection
        return new SqliteConnection(_connectionConfig.BuildConnectionString());
    }

    private void EnsureSchema()
    {
        lock (_gate)
        {
            var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                PRAGMA journal_mode=WAL;
                CREATE TABLE IF NOT EXISTS files(
                    path TEXT PRIMARY KEY,
                    mtime INTEGER NOT NULL,
                    size INTEGER NOT NULL
                );
                CREATE VIRTUAL TABLE IF NOT EXISTS notes_fts USING fts5(
                    path,
                    content,
                    tokenize = 'porter'
                );
            ";
            cmd.ExecuteNonQuery();
        }
    }

    public void BuildOrUpdateIndex(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var conn = Open();


            using var tx = conn.BeginTransaction();

            var existing = new Dictionary<string, (long mtime, long size)>(StringComparer.OrdinalIgnoreCase);
            using (var cmdSel = conn.CreateCommand())
            {
                cmdSel.CommandText = "SELECT path, mtime, size FROM files";
                using var r = cmdSel.ExecuteReader();
                while (r.Read()) existing[r.GetString(0)] = (r.GetInt64(1), r.GetInt64(2));
            }

            foreach (var file in _fileSystem.Directory.EnumerateFiles(_vault.VaultPath, "*.md", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rel = _fileSystem.Path.GetRelativePath(_vault.VaultPath, file).Replace("\\", "/");
                var info = _fileSystem.FileInfo.New(file);
                var mtime = info.LastWriteTimeUtc.Ticks;
                var size = info.Length;
                if (!existing.TryGetValue(rel, out var meta) || meta.mtime != mtime || meta.size != size)
                {
                    var content = _fileSystem.File.ReadAllText(file);
                    using var del = conn.CreateCommand();
                    del.CommandText = "DELETE FROM notes_fts WHERE path=$p";
                    del.Parameters.AddWithValue("$p", rel);
                    del.ExecuteNonQuery();

                    using var ins = conn.CreateCommand();
                    ins.CommandText = "INSERT INTO notes_fts(path, content) VALUES($p,$c)";
                    ins.Parameters.AddWithValue("$p", rel);
                    ins.Parameters.AddWithValue("$c", content);
                    ins.ExecuteNonQuery();

                    using var up = conn.CreateCommand();
                    up.CommandText = "INSERT INTO files(path, mtime, size) VALUES($p,$m,$s) ON CONFLICT(path) DO UPDATE SET mtime=excluded.mtime, size=excluded.size";
                    up.Parameters.AddWithValue("$p", rel);
                    up.Parameters.AddWithValue("$m", mtime);
                    up.Parameters.AddWithValue("$s", size);
                    up.ExecuteNonQuery();
                }
            }

            tx.Commit();

        }
    }

    public void UpdateFileByRel(string relativePath)
    {
        var rel = relativePath.Replace("\\", "/");
        var abs = _vault.GetAbsolutePath(rel);
        if (!_fileSystem.File.Exists(abs)) { RemoveFileByRel(rel); return; }
        var info = _fileSystem.FileInfo.New(abs);
        var mtime = info.LastWriteTimeUtc.Ticks;
        var size = info.Length;
        var content = _fileSystem.File.ReadAllText(abs);
        lock (_gate)
        {
            var conn = Open();


            using var tx = conn.BeginTransaction();
            using (var del = conn.CreateCommand())
            {
                del.CommandText = "DELETE FROM notes_fts WHERE path=$p";
                del.Parameters.AddWithValue("$p", rel);
                del.ExecuteNonQuery();
            }
            using (var ins = conn.CreateCommand())
            {
                ins.CommandText = "INSERT INTO notes_fts(path, content) VALUES($p,$c)";
                ins.Parameters.AddWithValue("$p", rel);
                ins.Parameters.AddWithValue("$c", content);
                ins.ExecuteNonQuery();
            }
            using (var up = conn.CreateCommand())
            {
                up.CommandText = "INSERT INTO files(path, mtime, size) VALUES($p,$m,$s) ON CONFLICT(path) DO UPDATE SET mtime=excluded.mtime, size=excluded.size";
                up.Parameters.AddWithValue("$p", rel);
                up.Parameters.AddWithValue("$m", mtime);
                up.Parameters.AddWithValue("$s", size);
                up.ExecuteNonQuery();
            }
            tx.Commit();

        }
    }

    public void RemoveFileByRel(string relativePath)
    {
        var rel = relativePath.Replace("\\", "/");
        lock (_gate)
        {
            var conn = Open();


            using var tx = conn.BeginTransaction();
            using (var del1 = conn.CreateCommand())
            {
                del1.CommandText = "DELETE FROM notes_fts WHERE path=$p";
                del1.Parameters.AddWithValue("$p", rel);
                del1.ExecuteNonQuery();
            }
            using (var del2 = conn.CreateCommand())
            {
                del2.CommandText = "DELETE FROM files WHERE path=$p";
                del2.Parameters.AddWithValue("$p", rel);
                del2.ExecuteNonQuery();
            }
            tx.Commit();

        }
    }

    public void RenameFileByRel(string oldRelativePath, string newRelativePath)
    {
        // delete old index and add new
        RemoveFileByRel(oldRelativePath);
        UpdateFileByRel(newRelativePath);
    }

    public IEnumerable<(string path, string snippet)> Search(string query, int limit = 50)
    {
        lock (_gate)
        {
            var conn = Open();


            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT path, snippet(notes_fts, 1, '[', ']', ' … ', 8)
                FROM notes_fts WHERE notes_fts MATCH $q LIMIT $lim;
            ";
            cmd.Parameters.AddWithValue("$q", query);
            cmd.Parameters.AddWithValue("$lim", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                yield return (r.GetString(0), r.GetString(1));
            }

        }
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
            // Dispose managed resources
            if (_persistentConnection != null)
            {
                _persistentConnection.Dispose();
                _persistentConnection = null;
            }
        }

        _disposed = true;
    }
}
