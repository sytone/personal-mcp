using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using YamlDotNet.RepresentationModel;

namespace Personal.Mcp.Services;

public sealed class VaultService
{
    private readonly string _vaultPath;
    public string VaultPath => _vaultPath;

    public VaultService()
    {
        _vaultPath = Environment.GetEnvironmentVariable("OBSIDIAN_VAULT_PATH")
                    ?? throw new InvalidOperationException("OBSIDIAN_VAULT_PATH is not set.");
        if (!Directory.Exists(_vaultPath))
            throw new DirectoryNotFoundException($"Vault not found: {_vaultPath}");
    }

    public string GetAbsolutePath(string relativePath)
    {
        relativePath = relativePath.Replace("\\", "/");
        if (relativePath.StartsWith("/")) throw new ArgumentException("Path cannot start with '/'");
        if (relativePath.Contains("..")) throw new ArgumentException("Path cannot contain '..'");
        var abs = Path.GetFullPath(Path.Combine(_vaultPath, relativePath));
        if (!abs.StartsWith(Path.GetFullPath(_vaultPath), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path escapes vault");
        return abs;
    }

    public (string content, IDictionary<string, object> metadata) ReadNote(string relativePath)
    {
        var abs = GetAbsolutePath(relativePath);
        if (!File.Exists(abs)) throw new FileNotFoundException("Note not found", abs);
        var text = File.ReadAllText(abs);
        var (frontmatter, body) = SplitFrontmatter(text);
        var tags = ExtractTags(frontmatter, body);
        return (body, new Dictionary<string, object>{ ["tags"] = tags, ["frontmatter"] = frontmatter });
    }

    public string ReadNoteRaw(string relativePath)
    {
        var abs = GetAbsolutePath(relativePath);
        if (!File.Exists(abs)) throw new FileNotFoundException("Note not found", abs);
        return File.ReadAllText(abs);
    }

    public (bool exists, DateTime lastModified, long size) GetFileStats(string relativePath)
    {
        var abs = GetAbsolutePath(relativePath);
        if (!File.Exists(abs))
            return (false, DateTime.MinValue, 0);
        var info = new FileInfo(abs);
        return (true, info.LastWriteTime, info.Length);
    }

    public bool DirectoryExists(string relativePath)
    {
        try
        {
            var abs = GetAbsolutePath(relativePath);
            return Directory.Exists(abs);
        }
        catch
        {
            return false;
        }
    }

    public string GetFileName(string relativePath)
    {
        return Path.GetFileName(relativePath);
    }

    public void WriteNote(string relativePath, string content, bool overwrite)
    {
        var abs = GetAbsolutePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
        if (File.Exists(abs) && !overwrite) throw new IOException("File exists; set overwrite=true to replace");
        File.WriteAllText(abs, content);
    }

    public static (IDictionary<string, object> frontmatter, string body) SplitFrontmatter(string text)
    {
        if (string.IsNullOrEmpty(text)) return (new Dictionary<string, object>(), string.Empty);
        using var reader = new StringReader(text);
        var first = reader.ReadLine();
        if (first is null || !first.Trim().Equals("---", StringComparison.Ordinal))
            return (new Dictionary<string, object>(), text);

        // Capture YAML lines until a line with only '---'
        var yamlSb = new StringBuilder();
        string? line;
        bool foundEnd = false;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Trim().Equals("---", StringComparison.Ordinal)) { foundEnd = true; break; }
            yamlSb.AppendLine(line);
        }
        if (!foundEnd)
        {
            // No closing fence; treat as no frontmatter
            return (new Dictionary<string, object>(), text);
        }
        var yaml = yamlSb.ToString();
        var body = reader.ReadToEnd() ?? string.Empty;
        var fm = ParseYaml(yaml);
        return (fm, body);
    }

    private static IDictionary<string, object> ParseYaml(string yaml)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yaml));
            if (stream.Documents.Count > 0 && stream.Documents[0].RootNode is YamlMappingNode map)
            {
                foreach (var kv in map.Children)
                {
                    var key = ((YamlScalarNode)kv.Key).Value ?? string.Empty;
                    dict[key] = ToDotNet(kv.Value);
                }
            }
        }
        catch
        {
            // ignore malformed frontmatter
        }
        return dict;
    }

    private static object ToDotNet(YamlNode node)
    {
        return node switch
        {
            YamlScalarNode s => s.Value ?? string.Empty,
            YamlSequenceNode seq => seq.Children.Select(ToDotNet).ToList(),
            YamlMappingNode map => map.Children.ToDictionary(k => ((YamlScalarNode)k.Key).Value ?? string.Empty, v => ToDotNet(v.Value)),
            _ => string.Empty
        };
    }

    private static IReadOnlyList<string> ExtractTags(IDictionary<string, object> frontmatter, string body)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (frontmatter.TryGetValue("tags", out var t))
        {
            switch (t)
            {
                case string s when !string.IsNullOrWhiteSpace(s): tags.Add(s.TrimStart('#')); break;
                case IEnumerable<object> list:
                    foreach (var item in list)
                        if (item is string tag) tags.Add(tag.TrimStart('#'));
                    break;
            }
        }
        foreach (Match m in Regex.Matches(body, @"(^|\s)#([\w\-/]+)", RegexOptions.Multiline))
            tags.Add(m.Groups[2].Value);
        return tags.ToList();
    }

    public IEnumerable<(string abs, string rel)> EnumerateMarkdownFiles(string? directory = null)
    {
        var baseDir = string.IsNullOrWhiteSpace(directory) ? _vaultPath : GetAbsolutePath(directory);
        foreach (var abs in Directory.EnumerateFiles(baseDir, "*.md", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(_vaultPath, abs).Replace("\\", "/");
            yield return (abs, rel);
        }
    }

    public (bool exists, IDictionary<string, object> metadata, (long size, int words) stats) GetNoteInfo(string relativePath)
    {
        var abs = GetAbsolutePath(relativePath);
        if (!File.Exists(abs))
            return (false, new Dictionary<string, object>(), (0, 0));
        var text = File.ReadAllText(abs);
        var (fm, body) = SplitFrontmatter(text);
        var tags = ExtractTags(fm, body);
        var words = string.IsNullOrWhiteSpace(body) ? 0 : Regex.Matches(body, "\\b\\w+\\b").Count;
        return (true, new Dictionary<string, object> { ["tags"] = tags, ["frontmatter"] = fm }, (new FileInfo(abs).Length, words));
    }

    // --- Tag operations ---
    public bool AddTags(string relativePath, IEnumerable<string> tags)
    {
        var (fm, body, abs) = ReadNoteFull(relativePath);
        var set = GetFrontmatterTagSet(fm);
        foreach (var t in tags) set.Add(NormalizeTag(t));
        fm["tags"] = set.ToList();
        WriteFrontmatterAndBody(abs, fm, body);
        return true;
    }

    public bool UpdateTags(string relativePath, IEnumerable<string> tags, bool merge)
    {
        var (fm, body, abs) = ReadNoteFull(relativePath);
        if (merge)
        {
            var set = GetFrontmatterTagSet(fm);
            foreach (var t in tags) set.Add(NormalizeTag(t));
            fm["tags"] = set.ToList();
        }
        else
        {
            fm["tags"] = tags.Select(NormalizeTag).ToList();
        }
        WriteFrontmatterAndBody(abs, fm, body);
        return true;
    }

    public bool RemoveTags(string relativePath, IEnumerable<string> tags)
    {
        var (fm, body, abs) = ReadNoteFull(relativePath);
        var set = GetFrontmatterTagSet(fm);
        foreach (var t in tags) set.Remove(NormalizeTag(t));
        fm["tags"] = set.ToList();
        WriteFrontmatterAndBody(abs, fm, body);
        return true;
    }

    // --- Frontmatter property helpers ---
    public IDictionary<string, object> GetFrontmatter(string relativePath)
    {
        var abs = GetAbsolutePath(relativePath);
        var text = File.Exists(abs) ? File.ReadAllText(abs) : string.Empty;
        var (fm, _) = SplitFrontmatter(text);
        return new Dictionary<string, object>(fm, StringComparer.OrdinalIgnoreCase);
    }

    public bool SetProperty(string relativePath, string key, string? value)
    {
        var (fm, body, abs) = ReadNoteFull(relativePath);
        if (value is null)
            fm.Remove(key);
        else
            fm[key] = value;
        WriteFrontmatterAndBody(abs, fm, body);
        return true;
    }

    public bool SetPropertyList(string relativePath, string key, IEnumerable<string> values)
    {
        var (fm, body, abs) = ReadNoteFull(relativePath);
        fm[key] = values.ToList();
        WriteFrontmatterAndBody(abs, fm, body);
        return true;
    }

    public bool RemoveProperty(string relativePath, string key)
    {
        var (fm, body, abs) = ReadNoteFull(relativePath);
        var changed = fm.Remove(key);
        if (changed) WriteFrontmatterAndBody(abs, fm, body);
        return changed;
    }

    public IEnumerable<(string path, object? value)> SearchByProperty(string key, string? value = null, bool contains = false, bool caseInsensitive = true)
    {
        var list = new List<(string path, object? value)>();
        var comp = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        foreach (var (abs, rel) in EnumerateMarkdownFiles())
        {
            var text = File.ReadAllText(abs);
            var (fm, _) = SplitFrontmatter(text);
            if (fm.TryGetValue(key, out var v))
            {
                if (value is null)
                {
                    list.Add((rel, v));
                    continue;
                }
                switch (v)
                {
                    case string s:
                        if ((contains && s?.IndexOf(value, comp) >= 0) || (!contains && string.Equals(s, value, comp)))
                            list.Add((rel, v));
                        break;
                    case IEnumerable<object> seq:
                        foreach (var item in seq)
                        {
                            if (item is string si && ((contains && si.IndexOf(value, comp) >= 0) || (!contains && string.Equals(si, value, comp))))
                            { list.Add((rel, v)); break; }
                        }
                        break;
                    default:
                        var sv = v?.ToString() ?? string.Empty;
                        if ((contains && sv.IndexOf(value, comp) >= 0) || (!contains && string.Equals(sv, value, comp)))
                            list.Add((rel, v));
                        break;
                }
            }
        }
        return list;
    }

    // --- Image extraction from notes ---
    public IReadOnlyList<(string raw, string? resolvedPath, bool exists)> GetNoteImages(string relativePath)
    {
        var abs = GetAbsolutePath(relativePath);
        if (!File.Exists(abs)) return Array.Empty<(string, string?, bool)>();
        var text = File.ReadAllText(abs);
        var dir = Path.GetDirectoryName(abs)!;
        var results = new List<(string raw, string? resolvedPath, bool exists)>();

        // Obsidian embedded image: ![[path or name.ext]] optionally with | dimension
        var reEmbed = new Regex(@"!\[\[([^\]|]+)(?:\|[^\]]+)?\]\]", RegexOptions.Compiled);
        foreach (Match m in reEmbed.Matches(text))
        {
            var target = m.Groups[1].Value.Trim();
            var pathNoNorm = ResolveAssetPath(target, dir);
            var rel = pathNoNorm is null ? null : Path.GetRelativePath(_vaultPath, pathNoNorm).Replace("\\", "/");
            results.Add((m.Value, rel, pathNoNorm is not null && File.Exists(pathNoNorm)));
        }

        // Markdown image: ![alt](path)
        var reMd = new Regex(@"!\[[^\]]*\]\(([^)]+)\)", RegexOptions.Compiled);
        foreach (Match m in reMd.Matches(text))
        {
            var target = m.Groups[1].Value.Trim();
            var pathNoNorm = ResolveAssetPath(target, dir);
            var rel = pathNoNorm is null ? null : Path.GetRelativePath(_vaultPath, pathNoNorm).Replace("\\", "/");
            results.Add((m.Value, rel, pathNoNorm is not null && File.Exists(pathNoNorm)));
        }

        return results;
    }

    private string? ResolveAssetPath(string target, string noteDirAbs)
    {
        // If absolute-like, combine with vault
        var norm = target.Replace("\\", "/");
        // If path is relative, resolve against note directory first, then vault root
        string try1 = Path.GetFullPath(Path.Combine(noteDirAbs, norm));
        if (File.Exists(try1)) return try1;
        string try2 = Path.GetFullPath(Path.Combine(_vaultPath, norm));
        if (File.Exists(try2)) return try2;
        // Also try searching by basename across vault
        var name = Path.GetFileName(norm);
        var found = Directory.EnumerateFiles(_vaultPath, name, SearchOption.AllDirectories).FirstOrDefault();
        return found;
    }

    private static string NormalizeTag(string t) => (t ?? string.Empty).Trim().TrimStart('#');

    private static HashSet<string> GetFrontmatterTagSet(IDictionary<string, object> fm)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (fm.TryGetValue("tags", out var v))
        {
            if (v is IEnumerable<object> list)
            {
                foreach (var item in list)
                    if (item is string s && !string.IsNullOrWhiteSpace(s)) set.Add(NormalizeTag(s));
            }
            else if (v is string s && !string.IsNullOrWhiteSpace(s)) set.Add(NormalizeTag(s));
        }
        return set;
    }

    private (IDictionary<string, object> fm, string body, string abs) ReadNoteFull(string relativePath)
    {
        var abs = GetAbsolutePath(relativePath);
        var text = File.Exists(abs) ? File.ReadAllText(abs) : string.Empty;
        var (fm, body) = SplitFrontmatter(text);
        return (fm, body, abs);
    }

    private static void WriteFrontmatterAndBody(string abs, IDictionary<string, object> fm, string body)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        WriteYamlMapping(sb, fm, 0);
        sb.AppendLine("---");
        if (!string.IsNullOrEmpty(body)) sb.Append(body);
        File.WriteAllText(abs, sb.ToString());
    }

    private static void WriteYamlMapping(StringBuilder sb, IDictionary<string, object> map, int indent)
    {
        string pad = new(' ', indent);
        foreach (var kv in map)
        {
            switch (kv.Value)
            {
                case IEnumerable<object> list when kv.Value is not string:
                    sb.AppendLine($"{pad}{QuoteIfNeeded(kv.Key)}:");
                    foreach (var item in list)
                    {
                        if (item is IDictionary<string, object> child)
                        {
                            sb.AppendLine($"{pad}-");
                            WriteYamlMapping(sb, child, indent + 2);
                        }
                        else
                        {
                            sb.AppendLine($"{pad}- {FormatScalar(item)}");
                        }
                    }
                    break;
                case IDictionary<string, object> childMap:
                    sb.AppendLine($"{pad}{QuoteIfNeeded(kv.Key)}:");
                    WriteYamlMapping(sb, childMap, indent + 2);
                    break;
                default:
                    sb.AppendLine($"{pad}{QuoteIfNeeded(kv.Key)}: {FormatScalar(kv.Value)}");
                    break;
            }
        }
    }

    private static string QuoteIfNeeded(string key)
    {
        // quote keys with special chars
        return Regex.IsMatch(key, @"[^A-Za-z0-9_\-]") ? $"\"{key.Replace("\"", "\\\"")}\"" : key;
    }

    private static string FormatScalar(object? value)
    {
        if (value is null) return "~"; // YAML null
        var s = value is string str ? str : value.ToString() ?? string.Empty;
        if (s.Contains('\n'))
        {
            // Use block scalar for multiline
            var lines = s.Replace("\r\n", "\n").Split('\n');
            var indented = string.Join("\n  ", lines);
            return $"|\n  {indented}";
        }
        // Quote when needed: leading/trailing spaces, special YAML chars, or looks like number/boolean
        var needsQuote = string.IsNullOrEmpty(s)
            || char.IsWhiteSpace(s[0]) || char.IsWhiteSpace(s[^1])
            || Regex.IsMatch(s, @"[:#\-?*&!|>'%{}@`,]\s?")
            || Regex.IsMatch(s, @"^(true|false|null|~|0[boxX][0-9A-Fa-f_]+|[-+]?[0-9][0-9_]*(\.[0-9_]+)?([eE][-+]?[0-9_]+)?)$");
        if (needsQuote)
        {
            var esc = s.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{esc}\"";
        }
        return s;
    }
}
