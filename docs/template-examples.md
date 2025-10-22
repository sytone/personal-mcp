# Example Templates

This file contains example templates that you can use with Personal MCP's templating system.

## Meeting Notes Template

```liquid
---
title: {{meeting_title}}
date: {{date | iso_date: '%Y-%m-%d'}}
attendees: {{attendees}}
type: meeting
tags: [meeting]
---

# {{meeting_title}}

**Date**: {{date | iso_date: '%B %d, %Y'}}  
**Attendees**: {{attendees}}

## Agenda

1. 

## Notes


## Action Items

- [ ] 

## Next Steps

```

**Usage:**
```javascript
create_note({
  path: "Meetings/2025-10-21-standup.md",
  templateString: "...", // paste template above
  templateContext: JSON.stringify({
    meeting_title: "Weekly Standup",
    date: "2025-10-21",
    attendees: "Alice, Bob, Charlie"
  })
})
```

## Project Template

```liquid
---
title: {{project_name}}
status: {{status}}
start_date: {{start_date | iso_date: '%Y-%m-%d'}}
{% if end_date %}
end_date: {{end_date | iso_date: '%Y-%m-%d'}}
{% endif %}
tags: [project, {{status}}]
---

# {{project_name}}

**Status**: {{status}}  
**Start Date**: {{start_date | iso_date: '%B %d, %Y'}}
{% if end_date %}
**End Date**: {{end_date | iso_date: '%B %d, %Y'}}
{% endif %}

## Overview

{{description}}

## Team

{% for member in team_members %}
- {{member}}
{% endfor %}

## Milestones

{% for milestone in milestones %}
### {{milestone.name}}
- **Due Date**: {{milestone.date}}
- **Status**: {{milestone.status}}
{% endfor %}

## Tasks

- [ ] 

## Notes

```

**Usage:**
```javascript
create_note({
  path: "Projects/New Feature.md",
  templateString: "...",
  templateContext: JSON.stringify({
    project_name: "New Feature Development",
    status: "in-progress",
    start_date: "2025-10-01",
    description: "Implementing the new feature for customers",
    team_members: ["Alice", "Bob"],
    milestones: [
      { name: "Design Phase", date: "2025-10-15", status: "completed" },
      { name: "Development", date: "2025-11-01", status: "in-progress" }
    ]
  })
})
```

## Book Notes Template

```liquid
---
title: "{{book_title}}"
author: {{author}}
type: book-notes
status: {{status}}
rating: {{rating}}
tags: [book, {{genre}}]
---

# {{book_title}}

**Author**: {{author}}  
**Genre**: {{genre}}  
**Status**: {{status}}  
**Rating**: {{rating}}/5

## Summary

{{summary}}

## Key Takeaways

{% for takeaway in key_takeaways %}
- {{takeaway}}
{% endfor %}

## Quotes

{% if quotes %}
{% for quote in quotes %}
> {{quote}}

{% endfor %}
{% endif %}

## My Thoughts


## Related Books

```

## Daily Note Template

```liquid
---
title: {{date | iso_date: '%Y-%m-%d'}}
type: daily-note
tags: [daily]
---

# {{day_name}}, {{date | iso_date: '%B %d, %Y'}}

## Morning Reflection

**Goal for today**:

**Energy level**: 

## Tasks

- [ ] 

## Notes


## Evening Reflection

**Accomplishments**:

**What went well**:

**What to improve**:

**Gratitude**:

```

## Research Note Template

```liquid
---
title: {{title}}
topic: {{topic}}
type: research
date: {{date | iso_date: '%Y-%m-%d'}}
tags: [research, {{topic}}]
sources: {{sources}}
---

# {{title}}

**Topic**: {{topic}}  
**Date**: {{date | iso_date: '%B %d, %Y'}}

## Research Question

{{research_question}}

## Background


## Findings

{% for finding in findings %}
### {{finding.title}}

{{finding.description}}

**Source**: {{finding.source}}

{% endfor %}

## Analysis


## Conclusions


## Further Reading

{% if reading_list %}
{% for item in reading_list %}
- {{item}}
{% endfor %}
{% endif %}

## References

```

## Quick Note Template (Minimal)

```liquid
---
created: {{date | iso_date: '%Y-%m-%d'}}
tags: []
---

# {{title}}

{{content}}
```

## Tips for Creating Custom Templates

1. **Start Simple**: Begin with basic templates and add complexity as needed
2. **Use Conditionals**: Hide optional sections with `{% if variable %}...{% endif %}`
3. **Iterate Collections**: Use `{% for item in collection %}...{% endfor %}` for lists
4. **Default Values**: Provide sensible defaults in your code before rendering
5. **Test First**: Validate templates with `ValidateTemplate()` before using in production
6. **Document Variables**: Comment which variables your template expects
7. **Version Control**: Store templates in your vault for version control
8. **Date Formatting**: Always use the date filter for DateTime values: `{{date | date: '%Y-%m-%d'}}`

## Variable Naming Conventions

- Use **snake_case** for variable names: `meeting_title`, `start_date`
- Use descriptive names: `attendees` not `att`, `description` not `desc`
- DateTime variables should end with `_date`: `created_date`, `due_date`
- Boolean variables should be questions: `is_urgent`, `has_deadline`
- Collections should be plural: `tasks`, `team_members`, `milestones`

## Common Liquid Filters

```liquid
{{text | upcase}}              // UPPERCASE
{{text | downcase}}            // lowercase
{{text | capitalize}}          // Capitalize first letter
{{text | truncate: 100}}       // Truncate to 100 characters
{{array | join: ", "}}         // Join array with comma separator
{{number | round: 2}}          // Round to 2 decimal places
```

## Resources

- Full documentation: [docs/templating.md](../templating.md)
- Liquid syntax: https://shopify.github.io/liquid/
- Fluid library: https://github.com/sebastienros/fluid
