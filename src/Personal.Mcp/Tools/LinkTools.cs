using System.ComponentModel;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class LinkTools
{
    [McpServerTool(Name = "get_outgoing_links"), Description("List all links from a specific note.")]
    public static object GetOutgoingLinks(LinkService links, [Description("Path to the note")] string path)
    {
        var list = links.GetOutgoingLinks(path).Select(l => new { target = l.target, line = l.line, context = l.context }).ToList();
        return new { source_note = path, link_count = list.Count, links = list };
    }
}
