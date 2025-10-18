# Personal MCP Server

A C# implementation of an MCP (Model Context Protocol) server for interacting with Obsidian vaults. Built with the official ModelContextProtocol C# SDK, this server provides comprehensive tools for managing notes, links, tags, and more through AI assistants.

**Original Inspiration:** https://github.com/that0n3guy/ObsidianPilot

## Quick Start

- Set OBSIDIAN_VAULT_PATH to your vault directory
- Run: `dotnet run --project src/Personal.Mcp`
- Add to your MCP client as a stdio server command

```powershell
$env:OBSIDIAN_VAULT_PATH = "C:\path\to\your\vault"
dotnet run --project src/Personal.Mcp
```

## Adding to Visual Studio Code

To use this MCP server with GitHub Copilot in VS Code:

1. **Build the project** (optional, for better performance):
   ```powershell
   dotnet build src/Personal.Mcp/Personal.Mcp.csproj
   ```

2. **Add to VS Code settings**:
   - Open VS Code settings (File → Preferences → Settings or `Ctrl+,`)
   - Search for "MCP" or navigate to Extensions → GitHub Copilot → Model Context Protocol Servers
   - Click "Edit in settings.json"

3. **Add the server configuration**:
   ```json
   {
     "github.copilot.chat.mcp.servers": {
       "personal-mcp": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "${workspaceFolder}\\src\\Personal.Mcp\\Personal.Mcp.csproj"
         ],
         "env": {
           "OBSIDIAN_VAULT_PATH": "C:\\path\\to\\your\\vault"
         }
       }
     }
   }
   ```

   Or if you built the project, use the compiled executable for faster startup:
   ```json
   {
     "github.copilot.chat.mcp.servers": {
       "personal-mcp": {
         "command": "dotnet",
         "args": [
           "${workspaceFolder}\\src\\Personal.Mcp\\bin\\Debug\\net9.0\\Personal.Mcp.dll"
         ],
         "env": {
           "OBSIDIAN_VAULT_PATH": "C:\\path\\to\\your\\vault"
         }
       }
     }
   }
   ```

4. **Restart VS Code** to load the MCP server if you have issues.

5. **Verify**: Open GitHub Copilot Chat and try using Obsidian-related commands. The server tools should now be available.

**Note**: Replace the paths with your actual repository and vault locations.

## Available Tools

- **Notes**: read_note, create_note, update_note, edit_note_content, edit_note_section, delete_note
- **Organization**: list_notes, list_folders, create_folder
- **Move/Rename**: move_note (basic wiki link update), rename_note (basic wiki link update), move_folder
- **Tags**: list_tags, add_tags, update_tags, remove_tags
- **Search**: search_notes (FTS5), search_by_regex, search_by_date
- **Links**: get_outgoing_links, get_backlinks, find_broken_links
- **Images**: read_image, view_note_images
- **Properties**: get_note_info, get_frontmatter, set_property, set_property_list, remove_property, search_by_property

## Implementation Notes

- Link rewrite currently updates [[Title]] links by filename only; it does not rewrite path links or handle aliases comprehensively.
- YAML writing uses a simple serializer; quoting/multi-line/ordering/comments are not preserved.
- Backlink and broken link detection are heuristic and filename-based.
- Index updates happen when search is invoked; not yet reactive to file edits/moves.
