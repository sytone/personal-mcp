# Personal MCP Server – Status Log

Last updated: 2025-10-16

## Goal

- C# implementation of an MCP server for managing Obsidian vaults, providing comprehensive tools for note management, organization, search, and link maintenance.
- Originally inspired by the Python ObsidianPilot MCP server: <https://github.com/that0n3guy/ObsidianPilot>

Environment

- .NET: net9.0
- SDK/Packages: ModelContextProtocol 0.4.0-preview.2, Microsoft.Data.Sqlite 9.0.8, Microsoft.Extensions.* 9.0.8, SixLabors.ImageSharp 3.1.11, YamlDotNet 16.3.0
- Transport: stdio
- Index DB: `.obsidian/mcp-search-index.db`

Server setup

- Program configures MCP server with stdio transport, tool discovery via WithToolsFromAssembly, DI singletons for services.

Services implemented

- VaultService: path safety, note IO, frontmatter parse/write, tag extraction and CRUD, property helpers (get/set/list/remove/search), note info, image extraction and path resolve.
- IndexService: SQLite FTS5 index, build/update/search + reactive helpers (UpdateFileByRel, RemoveFileByRel, RenameFileByRel).
- TagService: tag enumeration with counts.
- LinkService: outgoing links, backlinks (basic), broken link detection (basic), and UpdateLinksForRename for wiki/markdown links including alias/heading/block preservation.

Tools implemented

- Notes: `read_note`, `create_note`, `update_note`, `edit_note_content`, `edit_note_section`, `delete_note`
- Organization: `list_notes`, `list_folders`, `create_folder`
- Move/Rename: `move_note` (calls link rewrite + reindex), `rename_note` (calls link rewrite + reindex), `move_folder` (no link rewrite yet)
- Tags: `list_tags`, `add_tags`, `update_tags`, `remove_tags`
- Search: `search_notes` (FTS5), `search_by_regex`, `search_by_date`
- Links: `get_outgoing_links`, `get_backlinks`, `find_broken_links`
- Images: `read_image`, `view_note_images`
- Properties/Frontmatter: `get_note_info`, `get_frontmatter`, `set_property`, `set_property_list`, `remove_property`, `search_by_property`

Recent improvements

- Link rewrite handles wiki form `[[target#heading^block|alias]]` and Markdown form `[label](path#heading)`, preserving alias/anchors while updating target title/path.
- Reactive index updates on create/edit/rename/delete; move/rename triggers rename in index and refresh (currently naive when many files change).
- Frontmatter parsing hardened for LF/CRLF.
- YAML serialization improved: quotes special keys, block scalars for multiline, quotes ambiguous scalars.

Known gaps vs. Python version

- Link rewrite:
  - Does not resolve all path/alias ambiguities across nested folders; matching is filename- and simple path-based.
  - Does not update embedded transclusions beyond links (non-image transclusions should be reviewed).
  - `move_folder` does not rewrite links or reindex moved tree.
- Backlinks/broken link fidelity: heuristic; needs alias-aware resolution, heading/block disambiguation, and better scope resolution.
- YAML fidelity: simple serializer does not preserve comments or original ordering; round-trip edits may reorder keys.
- Section editing: heading identification is exact-line match; range detection/basic; no fuzzy match by heading text regardless of level.
- Index updates after link rewrite: currently refreshes all notes when links are changed; could track which files changed to minimize work.
- Tests: none yet.

Next steps (suggested)

1) Enhance link resolver:
   - Alias-aware matching, folder-aware disambiguation, and safe path-based rewrites.
   - Implement link updates for `move_folder` and limit reindexing to changed files.
2) YAML round-trip improvements:
   - Consider YamlDotNet serializer with a node model to preserve quoting and support multiline more robustly; accept that comments/order may not be fully preserved.
3) Section editing:
   - Fuzzy heading targeting (ignore level), better section range capture, and options to insert at top/bottom of section.
4) Backlinks and broken links:
   - Improve resolution (aliases, headings/blocks, folder context) and add a confidence score.
5) Indexing:
   - Track changed files from link rewrite to reindex only those; consider file system watcher for background updates.
6) Add targeted unit tests for path safety, YAML round-trip, link rewrite, and indexing.

## How to Run

- Set `OBSIDIAN_VAULT_PATH` to the vault root.
- Build: `dotnet build`
- Run: `dotnet run --project src/Personal.Mcp`

## Notes

- All relative paths in tools are vault-relative using forward slashes.
- Index database is stored in the vault at `.obsidian/mcp-search-index.db`.
