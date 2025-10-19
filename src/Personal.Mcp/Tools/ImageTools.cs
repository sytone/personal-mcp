using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class ImageTools
{
    [McpServerTool(Name = "read_image"), Description("Load and resize an image for viewing in MCP clients.")]
    public static ImageContentBlock ReadImage(IVaultService vault, [Description("Image path")] string path)
    {
        var abs = vault.GetAbsolutePath(path);
        using var img = Image.Load(abs);
        if (img.Width > 800)
        {
            var height = (int)(img.Height * (800.0 / img.Width));
            img.Mutate(x => x.Resize(800, height));
        }
        using var ms = new MemoryStream();
        img.Save(ms, new PngEncoder());
        var base64 = Convert.ToBase64String(ms.ToArray());
        return new ImageContentBlock
        {
            Type = "image",
            Data = base64,
            MimeType = "image/png"
        };
    }
}
