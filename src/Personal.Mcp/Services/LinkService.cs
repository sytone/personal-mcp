using System.Text.RegularExpressions;

namespace Personal.Mcp.Services;

public partial class LinkService(VaultService vaultService)
{
    private readonly VaultService _vault = vaultService;

    // Very basic wiki-link extraction for now
    public IReadOnlyList<(string target, int line, string context)> GetOutgoingLinks(string relativePath, int contextLength = 100)
    {
        var abs = _vault.GetAbsolutePath(relativePath);
        var lines = File.ReadAllLines(abs);
        var results = new List<(string target, int line, string context)>();
        var re = new Regex(@"\[\[([^\]|]+)(?:\|[^\]]+)?\]\]", RegexOptions.Compiled);
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (Match m in re.Matches(lines[i]))
            {
                var target = m.Groups[1].Value.Trim();
                var ctx = lines[i];
                if (ctx.Length > contextLength) ctx = ctx[..Math.Min(ctx.Length, contextLength)];
                results.Add((target, i + 1, ctx));
            }
        }
        return results;
    }

    public IReadOnlyList<(string sourcePath, string target, string context)> GetBacklinks(string targetRelativePath, int contextLength = 100)
    {
        // Backlinks: scan all notes for [[Target Note]] where Target Note name matches the file name (without extension)
        var targetName = Path.GetFileNameWithoutExtension(targetRelativePath.Replace("\\", "/"));
        var re = new Regex(@"\[\[([^\]|]+)(?:\|[^\]]+)?\]\]", RegexOptions.Compiled);
        var list = new List<(string sourcePath, string target, string context)>();
        foreach (var (abs, rel) in _vault.EnumerateMarkdownFiles())
        {
            var text = File.ReadAllText(abs);
            foreach (Match m in re.Matches(text))
            {
                var linkTarget = m.Groups[1].Value.Trim();
                if (string.Equals(NormalizeName(linkTarget), NormalizeName(targetName), StringComparison.OrdinalIgnoreCase))
                {
                    var idx = Math.Max(0, m.Index - contextLength);
                    var end = Math.Min(text.Length, m.Index + m.Length + contextLength);
                    var ctx = text.Substring(idx, end - idx);
                    list.Add((rel, linkTarget, ctx));
                }
            }
        }
        return list;
    }

    public IReadOnlyList<(string sourcePath, string brokenLink, string displayText)> FindBrokenLinks(string? directory = null)
    {
        var re = new Regex(@"\[\[([^\]|]+)(?:\|([^\]]+))?\]\]", RegexOptions.Compiled);
        var nameToRel = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, rel) in _vault.EnumerateMarkdownFiles())
        {
            var name = Path.GetFileNameWithoutExtension(rel);
            if (!nameToRel.TryGetValue(name, out var list)) nameToRel[name] = list = new List<string>();
            list.Add(rel);
        }

        var broken = new List<(string sourcePath, string brokenLink, string displayText)>();
        foreach (var (abs, rel) in _vault.EnumerateMarkdownFiles(directory))
        {
            var text = File.ReadAllText(abs);
            foreach (Match m in re.Matches(text))
            {
                var target = m.Groups[1].Value.Trim();
                var display = m.Groups[2].Success ? m.Groups[2].Value : target;
                var normalized = NormalizeName(Path.GetFileNameWithoutExtension(target));
                if (!nameToRel.ContainsKey(normalized))
                {
                    broken.Add((rel, target, display));
                }
            }
        }
        return broken;
    }

    public (int filesUpdated, int replacements) UpdateLinksForRename(string oldRelativePath, string newRelativePath)
    {
        var oldRel = oldRelativePath.Replace("\\", "/");
        var newRel = newRelativePath.Replace("\\", "/");
        var oldAbs = _vault.GetAbsolutePath(oldRel);
        var newAbs = _vault.GetAbsolutePath(newRel);
        var oldTitle = Path.GetFileNameWithoutExtension(oldRel);
        var newTitle = Path.GetFileNameWithoutExtension(newRel);
        var oldRelNoExt = oldRel.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? oldRel[..^3] : oldRel;
        var newRelNoExt = newRel.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? newRel[..^3] : newRel;

        var filesUpdated = 0;
        var replacements = 0;

        // Wiki link pattern: [[target#heading^block|alias]]
        var reWiki = new Regex(@"\[\[([^\]|#^]+)(?:#([^\]^|]+))?(?:\^([^\]|]+))?(?:\|([^\]]+))?\]\]", RegexOptions.Compiled);
        // Markdown link: [label](path#heading)
        var reMd = new Regex(@"\[([^\]]*)\]\(([^)\s#]+)(#[^)]+)?\)", RegexOptions.Compiled);

        foreach (var (abs, rel) in _vault.EnumerateMarkdownFiles())
        {
            var noteDir = Path.GetDirectoryName(abs)!;
            var text = File.ReadAllText(abs);
            var newText = text;
            var changed = false;

            newText = reWiki.Replace(newText, m =>
            {
                var target = (m.Groups[1].Value ?? string.Empty).Trim().Replace("\\", "/");
                var heading = m.Groups[2].Success ? m.Groups[2].Value : null;
                var block = m.Groups[3].Success ? m.Groups[3].Value : null;
                var alias = m.Groups[4].Success ? m.Groups[4].Value : null;

                string? newTarget = null;
                // Match by title or by relative path (without extension)
                if (string.Equals(target, oldTitle, StringComparison.OrdinalIgnoreCase))
                {
                    newTarget = newTitle;
                }
                else if (string.Equals(target.TrimStart('/'), oldRelNoExt.TrimStart('/'), StringComparison.OrdinalIgnoreCase) || target.EndsWith("/" + oldTitle, StringComparison.OrdinalIgnoreCase))
                {
                    newTarget = newRelNoExt;
                }
                if (newTarget is null) return m.Value;

                changed = true; replacements++;
                var rebuilt = "[[" + newTarget;
                if (!string.IsNullOrEmpty(heading)) rebuilt += "#" + heading;
                if (!string.IsNullOrEmpty(block)) rebuilt += "^" + block;
                if (!string.IsNullOrEmpty(alias)) rebuilt += "|" + alias;
                rebuilt += "]]";
                return rebuilt;
            });

            newText = reMd.Replace(newText, m =>
            {
                var label = m.Groups[1].Value;
                var url = (m.Groups[2].Value ?? string.Empty).Trim();
                var anchor = m.Groups[3].Success ? m.Groups[3].Value : string.Empty;

                string normUrl = url.Replace("\\", "/");
                string candidateAbs = normUrl.StartsWith('/')
                    ? Path.GetFullPath(Path.Combine(_vault.VaultPath, normUrl.TrimStart('/')))
                    : Path.GetFullPath(Path.Combine(noteDir, normUrl));
                string candidateAbsWithExt = candidateAbs.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? candidateAbs : candidateAbs + ".md";

                if (!string.Equals(candidateAbs, oldAbs, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(candidateAbsWithExt, oldAbs, StringComparison.OrdinalIgnoreCase))
                {
                    return m.Value;
                }

                string newUrl;
                if (normUrl.StartsWith('/'))
                {
                    var newFromRoot = newRel.Replace("\\", "/");
                    newUrl = "/" + newFromRoot.TrimStart('/');
                }
                else
                {
                    var relative = Path.GetRelativePath(noteDir, newAbs).Replace("\\", "/");
                    newUrl = relative;
                }
                changed = true; replacements++;
                return $"[{label}]({newUrl}{anchor})";
            });

            if (changed && !newText.Equals(text, StringComparison.Ordinal))
            {
                File.WriteAllText(abs, newText);
                filesUpdated++;
            }
        }
        return (filesUpdated, replacements);
    }

    private static string NormalizeName(string s) => s.Replace("\\", "/").Trim();
}
