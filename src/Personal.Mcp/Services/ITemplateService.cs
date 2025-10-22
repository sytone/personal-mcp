using System.Globalization;

namespace Personal.Mcp.Services;

/// <summary>
/// Service for rendering Liquid templates with context data.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Renders a Liquid template string with the provided context data.
    /// </summary>
    /// <param name="template">The Liquid template string to render.</param>
    /// <param name="context">Dictionary of key-value pairs to use as template variables.</param>
    /// <returns>The rendered template as a string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the template syntax is invalid or rendering fails.</exception>
    string RenderTemplate(string template, Dictionary<string, object> context);

    /// <summary>
    /// Validates that a Liquid template string has valid syntax.
    /// </summary>
    /// <param name="template">The Liquid template string to validate.</param>
    /// <returns>True if the template is valid, false otherwise.</returns>
    bool ValidateTemplate(string template);

    /// <summary>
    /// Sets the culture info to use for date and number formatting in templates.
    /// </summary>
    /// <param name="cultureInfo">The culture info to set.</param>
    void SetCultureInfo(CultureInfo cultureInfo);
    
    /// <summary>
    /// Sets the time zone to use for date and time formatting in templates.
    /// </summary>
    /// <param name="timeZoneInfo">The time zone info to set.</param>
    void SetTimeZone(TimeZoneInfo timeZoneInfo);

    /// <summary>
    /// Gets the default template for journal entries.
    /// </summary>
    /// <returns>The default journal template string.</returns>
    string GetDefaultJournalTemplate();

    /// <summary>
    /// Gets the default template for new notes.
    /// </summary>
    /// <returns>The default note template string.</returns>
    string GetDefaultNoteTemplate();
}
