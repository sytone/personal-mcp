# List Notes Date Filtering

## Overview

The `list_notes` tool has been enhanced to support filtering notes by creation or modification date. This allows you to find notes that were created or modified on a specific date or within a date range.

## Parameters

### New Parameters

- **`date_filter`** (string, optional): Filter by date or date range
  - Single date format: `YYYY-MM-DD` (e.g., `2025-10-20`)
  - Date range format: `YYYY-MM-DD:YYYY-MM-DD` (e.g., `2025-10-01:2025-10-31`)
  
- **`date_property`** (string, default: `"modified"`): Which date property to filter on
  - `"modified"`: Filter by last modification date
  - `"created"`: Filter by creation date
  - `"both"`: Match notes that meet the criteria on either created OR modified date

### Existing Parameters

- **`directory`** (string, optional): Directory to list (e.g., `"Daily"`)
- **`recursive`** (bool, default: `true`): Whether to recurse into subfolders

## Usage Examples

### Find Notes Modified Today

```json
{
  "date_filter": "2025-10-20",
  "date_property": "modified"
}
```

### Find Notes Created This Week

```json
{
  "date_filter": "2025-10-14:2025-10-20",
  "date_property": "created"
}
```

### Find Notes Created or Modified in October 2025

```json
{
  "date_filter": "2025-10-01:2025-10-31",
  "date_property": "both"
}
```

### Find All Recent Notes in a Specific Directory

```json
{
  "directory": "1 Journal/2025",
  "date_filter": "2025-10-01:2025-10-20",
  "date_property": "modified",
  "recursive": true
}
```

### Find Notes Modified After a Specific Date

When using a date range, you can leave one end open by using an appropriate range:

```json
{
  "date_filter": "2025-10-01:2025-12-31",
  "date_property": "modified"
}
```

## Response Format

The response includes additional date information for each note:

```json
{
  "directory": "/",
  "recursive": true,
  "date_filter": "2025-10-20",
  "date_property": "modified",
  "start_date": "2025-10-20",
  "end_date": "2025-10-20",
  "count": 5,
  "notes": [
    {
      "path": "1 Journal/2025/2025-W43.md",
      "name": "2025-W43.md",
      "created": "2025-10-14 08:00:00",
      "modified": "2025-10-20 15:30:45"
    },
    {
      "path": "2 Projects/My Project/notes.md",
      "name": "notes.md",
      "created": "2025-10-20 09:15:22",
      "modified": "2025-10-20 14:20:10"
    }
  ]
}
```

## Date Filtering Logic

### Single Date

When a single date is provided (e.g., `2025-10-20`):
- Start date: Beginning of day (`2025-10-20 00:00:00`)
- End date: End of day (`2025-10-20 23:59:59.9999999`)

This matches all notes created or modified anytime during that day.

### Date Range

When a date range is provided (e.g., `2025-10-01:2025-10-31`):
- Start date: Beginning of first day (`2025-10-01 00:00:00`)
- End date: End of last day (`2025-10-31 23:59:59.9999999`)

This matches all notes created or modified between these dates (inclusive).

### Date Property Filtering

- **`modified`**: Filters based on the file's last write time
- **`created`**: Filters based on the file's creation time
- **`both`**: Matches notes where either the creation date OR modification date falls within the range

## Common Use Cases

### Daily Review

Find all notes you worked on today:

```json
{
  "date_filter": "2025-10-20",
  "date_property": "modified"
}
```

### Weekly Review

Find all notes created or modified this week:

```json
{
  "date_filter": "2025-10-14:2025-10-20",
  "date_property": "both"
}
```

### Monthly Archive

Find all notes from a specific month:

```json
{
  "date_filter": "2025-09-01:2025-09-30",
  "date_property": "both"
}
```

### Recent Activity in Project

Find recent activity in a specific project folder:

```json
{
  "directory": "2 Projects/My Project",
  "date_filter": "2025-10-01:2025-10-20",
  "date_property": "modified",
  "recursive": true
}
```

## Notes

- All dates use ISO 8601 format (`YYYY-MM-DD`)
- Date filtering is based on file system metadata (creation and modification times)
- The `both` option uses OR logic: notes matching either created OR modified criteria will be included
- Times are based on the local file system time zone
- Date ranges are inclusive on both ends

## Backward Compatibility

The new parameters are optional. Existing calls to `list_notes` without date filtering will continue to work as before, returning all notes in the specified directory.

## Performance Considerations

Date filtering happens after retrieving the file list, so it still enumerates all files in the directory structure. For large vaults, consider:
- Using a more specific `directory` parameter to limit the search scope
- Setting `recursive: false` when you only need files in a single directory
- Combining with other tools for more complex filtering requirements
