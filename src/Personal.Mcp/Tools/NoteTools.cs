using System.ComponentModel;
using ModelContextProtocol.Server;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tools;

[McpServerToolType]
public static class NoteTools
{
    [McpServerTool(Name = "read_note"), Description("Read the content and metadata of a specific note.")]
    public static object ReadNote(IVaultService vault, [Description("Path to the note, e.g., 'Daily/2024-01-15.md'")] string path)
    {
        var (content, metadata) = vault.ReadNote(path);
        return new { path, content, metadata };
    }

    [McpServerTool(Name = "create_note"), Description("Create a new note or update an existing one.")]
    public static object CreateNote(
        IVaultService vault,
        IndexService index,
        ITemplateService template,
        TimeProvider timeProvider,
        [Description("Path where the note should be created")] string path,
        [Description("Markdown content of the note")] string content,
        [Description("Whether to overwrite existing notes")] bool overwrite = false,
        [Description("Optional Liquid template string to use for generating the note. If provided, the content parameter is ignored and the template is rendered. Use variables like {{title}}, {{created_date}}, etc.")] string? templateString = null,
        [Description("Optional dictionary of template variables (JSON format) to use when rendering a template. Example: {\"title\": \"My Note\", \"tags\": [\"important\"]}")] string? templateContext = null)
    {
        string finalContent = content;

        // If a template is provided, render it with the context
        if (!string.IsNullOrWhiteSpace(templateString))
        {
            try
            {
                // Parse template context if provided, otherwise use default values
                var context = new Dictionary<string, object>();
                
                if (!string.IsNullOrWhiteSpace(templateContext))
                {
                    // Parse JSON context (basic support)
                    context = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(templateContext) 
                              ?? new Dictionary<string, object>();
                }
                
                // Add default values if not provided
                if (!context.ContainsKey("created_date"))
                {
                    context["created_date"] = timeProvider.GetLocalNow();
                }
                
                // Extract title from path if not provided
                if (!context.ContainsKey("title"))
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    context["title"] = fileName;
                }

                // Render the template
                finalContent = template.RenderTemplate(templateString, context);
            }
            catch (Exception ex)
            {
                return new { success = false, error = $"Template rendering failed: {ex.Message}" };
            }
        }

        try
        {
            vault.WriteNote(path, finalContent, overwrite);
            index.UpdateFileByRel(path);
            return new { success = true, path, usedTemplate = !string.IsNullOrWhiteSpace(templateString) };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }
}
