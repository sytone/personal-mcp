---
title: Vault settings
order: 10
tags:
  - configuration
  - vault
ai_note: true
summary: How to configure vault-level settings for Personal MCP using a markdown file or environment variables.
lastUpdated: 2025-10-21
layout: doc
navbar: true
sidebar: true
editLink: false
---

## Overview

Personal MCP supports vault-level settings via a markdown file placed in the root of your Obsidian vault. The file must include YAML frontmatter. When present, the frontmatter keys override environment variables and built-in defaults.

## Precedence

- Vault root settings file (`mcp-settings.md`) - highest priority
- Environment variables prefixed with `PERSONAL_MCP_` (for example `PERSONAL_MCP_JOURNAL_PATH`)
- Built-in defaults (for example `1 Journal`)

## Example

Create a file named `mcp-settings.md` in the root of your vault with YAML frontmatter at the top. Example contents:

```yaml
---
journalPath: "1 Journal"
# defaultTemplate: "templates/daily.md"
---
```

## Notes

- Keys can use camelCase, snake_case, or kebab-case; the loader normalizes them.
- If the settings file is missing, the server will create a sample `mcp-settings.md` in the vault. The application ships a sample `assets/mcp-settings.md` which is used when creating the file.
