# Templating Support in Personal MCP

## Overview

Personal MCP now includes powerful templating capabilities using the Liquid template syntax (via the Fluid library). Templates allow you to create standardized, dynamic content for notes and journal entries with variable substitution, conditionals, loops, and more.

## Features

- **Liquid Syntax**: Full support for Liquid templating language
- **Dynamic Variables**: Pass context data to templates for customization
- **Default Templates**: Pre-configured templates for journals and notes
- **Extensible**: Easy to customize and extend templates
- **Type-Safe**: DateTime automatic formatting to ISO 8601 (yyyy-MM-dd)

## Core Components

### TemplateService

The `TemplateService` is the core service that handles template rendering:

- **Location**: `src/Personal.Mcp/Services/TemplateService.cs`
- **Interface**: `ITemplateService`
- **Registration**: Singleton service in DI container

#### Methods

```csharp
// Render a template with context variables
string RenderTemplate(string template, Dictionary<string, object> context);

// Validate template syntax
bool ValidateTemplate(string template);

// Get default templates
string GetDefaultJournalTemplate();
string GetDefaultNoteTemplate();
```

## Default Templates

### Journal Template

The default journal template creates structured weekly journal entries:

```liquid
# Week {{week_number}} in {{year}}

## {{day_number}} {{day_name}}

```

**Variables:**
- `week_number`: ISO week number (1-53)
- `year`: Year
- `day_number`: Day of month (1-31)
- `day_name`: Day name (Monday, Tuesday, etc.)

### Note Template

The default note template creates notes with frontmatter:

```liquid
---
title: {{title}}
created: {{created_date | iso_date: '%Y-%m-%d'}}
tags: []
---

# {{title}}

```

**Variables:**
- `title`: Note title (extracted from filename if not provided)
- `created_date`: Creation date (DateTime object, formatted using Liquid's date filter)

## Usage Examples

### 1. Using Templates in Journal Creation

When you create a new journal entry using `add_journal_entry`, if the weekly file doesn't exist, it will be automatically created using the default journal template:

```javascript
// The JournalTools automatically uses templates
add_journal_entry({
  entryContent: "My journal entry",
  date: "2025-10-21"
})

// Creates: 1 Journal/2025/2025-W43.md with:
// # Week 43 in 2025
//
// ## 21 Tuesday
//
// - 14:30 - My journal entry
```

### 2. Using Templates with create_note

You can now use custom templates when creating notes:

```javascript
// Use the default note template
create_note({
  path: "Notes/My Note.md",
  content: "", // ignored when using template
  templateString: "---\ntitle: {{title}}\ncreated: {{created_date}}\ntags: [{{tags}}]\n---\n\n# {{title}}\n\n{{content}}",
  templateContext: JSON.stringify({
    title: "My Custom Note",
    tags: "important, work",
    content: "This is the note content"
  })
})
```

### 3. Simple Template Example

```javascript
// Create a meeting notes template
create_note({
  path: "Meetings/2025-10-21.md",
  content: "",
  templateString: `---
title: {{meeting_title}}
date: {{date}}
attendees: {{attendees}}
---

# {{meeting_title}}

**Date**: {{date}}
**Attendees**: {{attendees}}

## Agenda


## Notes


## Action Items

`,
  templateContext: JSON.stringify({
    meeting_title: "Weekly Standup",
    date: "2025-10-21",
    attendees: "Alice, Bob, Charlie"
  })
})
```

### 4. Using Conditionals

```liquid
---
title: {{title}}
{% if urgent %}
priority: high
{% endif %}
---

# {{title}}

{% if description %}
{{description}}
{% endif %}

{% if tasks %}
## Tasks
{% for task in tasks %}
- [ ] {{task}}
{% endfor %}
{% endif %}
```

### 5. Using Loops

```liquid
# Project: {{project_name}}

## Team Members
{% for member in team_members %}
- {{member}}
{% endfor %}

## Milestones
{% for milestone in milestones %}
### {{milestone.name}}
Due: {{milestone.date}}
{% endfor %}
```

## Liquid Syntax Reference

### Variables

```liquid
{{variable_name}}
{{ variable_name }}  // whitespace is flexible
```

### Conditionals

```liquid
{% if condition %}
  content
{% endif %}

{% if condition %}
  content
{% else %}
  alternative
{% endif %}

{% if condition1 %}
  content1
{% elsif condition2 %}
  content2
{% else %}
  content3
{% endif %}
```

### Loops

```liquid
{% for item in collection %}
  {{item}}
{% endfor %}

{% for item in collection %}
  {{item.property}}
{% endfor %}
```

### Filters

Liquid supports many built-in filters:

```liquid
{{name | upcase}}           // UPPERCASE
{{name | downcase}}         // lowercase
{{name | capitalize}}       // Capitalize
{{text | truncate: 50}}     // Truncate to 50 chars
{{array | join: ", "}}      // Join array elements
```

## Advanced Features

### Date Formatting with Liquid Filters

Instead of relying on automatic conversions, use Liquid's `iso_date` filter for flexible date formatting:

```liquid
{{created_date | iso_date: '%Y-%m-%d'}}      // 2025-10-21
{{created_date | iso_date: '%B %d, %Y'}}     // October 21, 2025
{{created_date | iso_date: '%A, %B %d'}}     // Monday, October 21
{{created_date | iso_date: '%Y-%m-%d %H:%M'}} // 2025-10-21 14:30
```

**Common date format codes:**
- `%Y` - Year (4 digits)
- `%m` - Month (01-12)
- `%d` - Day (01-31)
- `%B` - Full month name
- `%b` - Abbreviated month name
- `%A` - Full weekday name
- `%a` - Abbreviated weekday name
- `%H` - Hour (00-23)
- `%M` - Minute (00-59)
- `%S` - Second (00-59)

See [Liquid date filter documentation](https://shopify.github.io/liquid/filters/date/) for all format codes.

### Complex Objects

You can access nested properties:

```liquid
Name: {{user.name}}
Email: {{user.email}}
```

### Error Handling

The template service handles errors gracefully:

- Invalid syntax: Returns `InvalidOperationException` with detailed error message
- Missing variables: Renders as empty string
- Rendering errors: Returns error in response object

## Testing

The templating system includes comprehensive unit tests:

- **Location**: `tests/Personal.Mcp.Tests/Tools/TemplateServiceTests.cs`
- **Coverage**: 35 test cases covering:
  - Basic rendering
  - Multiple variables
  - DateTime formatting
  - Conditionals
  - Loops
  - Filters
  - Edge cases
  - Error handling
  - Default templates

Run tests:
```powershell
dotnet test --filter "FullyQualifiedName~TemplateServiceTests"
```

## Integration Points

### JournalTools

- **Method**: `LoadOrCreateWeeklyContent`
- **Behavior**: Automatically uses template service when creating new weekly journal files
- **Template**: Uses `GetDefaultJournalTemplate()` and generates all 7 day headings

### NoteTools

- **Method**: `CreateNote`
- **New Parameters**:
  - `templateString`: Optional Liquid template
  - `templateContext`: Optional JSON context dictionary
- **Behavior**: If template is provided, renders it; otherwise uses provided content

## Configuration

Templates can be customized by:

1. **Passing custom template strings** to `create_note`
2. **Modifying default templates** in `TemplateService.cs`
3. **Creating template files** in your vault and reading them as needed

## Best Practices

1. **Validate Templates**: Use `ValidateTemplate()` before rendering to catch syntax errors
2. **Provide Defaults**: Always provide fallback values in template context
3. **Handle Errors**: Wrap template rendering in try-catch blocks
4. **Keep Templates Simple**: Complex logic should be in code, not templates
5. **Use Meaningful Variable Names**: Makes templates self-documenting
6. **Test Templates**: Create unit tests for custom templates
7. **Document Variables**: Comment which variables your templates expect

## Future Enhancements

Potential improvements for future versions:

- [ ] Template storage in vault (e.g., `_templates/` folder)
- [ ] Template discovery and listing tools
- [ ] More default templates (daily notes, meeting notes, project templates)
- [ ] Template inheritance/includes
- [ ] Custom filters and functions
- [ ] Template validation tool
- [ ] VS Code extension for template editing with IntelliSense

## Resources

- **Fluid Library**: https://github.com/sebastienros/fluid
- **Liquid Syntax**: https://shopify.github.io/liquid/
- **Project Documentation**: `docs/README.md`
- **API Documentation**: See inline code comments in `ITemplateService.cs`

## Troubleshooting

### Template Not Rendering

**Problem**: Template returns empty or unexpected output

**Solutions**:
- Verify template syntax with `ValidateTemplate()`
- Check variable names match context keys (case-sensitive)
- Ensure context values are not null
- Check for typos in variable names

### Invalid Template Syntax

**Problem**: `InvalidOperationException` thrown

**Solutions**:
- Verify all `{{` have matching `}}`
- Verify all `{%` have matching `%}`
- Check for unclosed loops or conditionals
- Validate template before use

### DateTime Not Formatting

**Problem**: DateTime shows full timestamp instead of desired format

**Solutions**:
- Use Liquid's iso_date filter: `{{created_date | iso_date: '%Y-%m-%d'}}`
- See [date filter documentation](https://shopify.github.io/liquid/filters/date/) for format codes
- Pass pre-formatted strings if you need custom formats not supported by date filter

## Support

For issues, questions, or contributions:

- **GitHub Issues**: https://github.com/sytone/personal-mcp/issues
- **Discussions**: https://github.com/sytone/personal-mcp/discussions
- **Documentation**: `docs/` folder in repository
