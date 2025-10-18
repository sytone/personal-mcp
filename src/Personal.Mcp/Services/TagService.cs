using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Personal.Mcp.Services;

public sealed class TagService
{
    private readonly VaultService _vault;
    public TagService(VaultService vault) => _vault = vault;

    public IReadOnlyDictionary<string, int> ListTags()
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(_vault.VaultPath, "*.md", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(_vault.VaultPath, file).Replace("\\", "/");
            var text = File.ReadAllText(file);
            var (fm, body) = VaultService.SplitFrontmatter(text);
            foreach (var tag in ExtractTags(fm, body))
                counts[tag] = counts.TryGetValue(tag, out var c) ? c + 1 : 1;
        }
        return counts;
    }

    public static IReadOnlyList<string> ExtractTags(IDictionary<string, object> frontmatter, string body)
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
}
