# Date Utility Tools for Journal Accuracy

## Problem Summary

When using natural language date references like "last Wednesday", there was a discrepancy between:
- **My (Copilot's) calculation**: Sometimes incorrect due to off-by-one errors or ambiguous interpretation
- **The actual date**: What the calendar says

For example:
- User prompt: "Add a journal entry for last Wednesday" (spoken on Sunday, October 19, 2025)
- Expected: October 15, 2025 (Wednesday)
- Copilot miscalculated: October 16, 2025 (Thursday)

## Root Cause

The issue is **NOT** with the MCP tool (`AddJournalEntry`). The tool correctly:
1. Accepts a date string in `YYYY-MM-DD` format
2. Calculates the day of the week  
3. Creates the heading like `## 16 Thursday`

The problem is with **Copilot's date calculation** when interpreting natural language like:
- "last Wednesday"
- "next Friday"
- "two weeks ago"

## Solution: Date Utility Tools

Three new MCP tools have been added to `src/Personal.Mcp/Tools/DateUtilityTools.cs`:

### 1. `calculate_relative_date`

**Purpose**: Convert natural language dates to exact dates with verification

**Usage**:
```
calculate_relative_date "last Wednesday" from "2025-10-19"
```

**Returns**:
```json
{
  "success": true,
  "inputDescription": "last Wednesday",
  "referenceDate": "2025-10-19",
  "referenceDayOfWeek": "Sunday",
  "calculatedDate": "2025-10-15",
  "calculatedDayOfWeek": "Wednesday",
  "daysFromReference": -4,
  "isoWeek": 42,
  "isoYear": 2025
}
```

**Supported patterns**:
- `today`, `yesterday`, `tomorrow`
- `last [dayname]`, `previous [dayname]`
- `next [dayname]`
- `X days ago`, `X weeks ago`

### 2. `get_date_info`

**Purpose**: Get complete information about a specific date

**Usage**:
```
get_date_info "2025-10-15"
```

**Returns**:
```json
{
  "success": true,
  "date": "2025-10-15",
  "dayOfWeek": "Wednesday",
  "dayOfMonth": 15,
  "month": "October",
  "year": 2025,
  "isoWeek": 42,
  "isoYear": 2025,
  "dayOfYear": 288,
  "relativeToToday": "4 days ago",
  "daysFromToday": -4,
  "today": "2025-10-19"
}
```

### 3. `get_week_dates`

**Purpose**: Get all 7 dates in a week with their day names

**Usage**:
```
get_week_dates "2025-10-15"
```

**Returns**:
```json
{
  "success": true,
  "isoWeek": 42,
  "isoYear": 2025,
  "weekDescription": "Week 42 of 2025",
  "dates": [
    { "date": "2025-10-13", "dayOfWeek": "Monday", "dayNumber": 13, "isToday": false },
    { "date": "2025-10-14", "dayOfWeek": "Tuesday", "dayNumber": 14, "isToday": false },
    { "date": "2025-10-15", "dayOfWeek": "Wednesday", "dayNumber": 15, "isToday": false },
    ...
  ]
}
```

## Workflow for Accurate Journal Entries

### Before (Error-Prone)
```
User: "Add a journal entry for last Wednesday..."
Copilot: [calculates date internally, might be wrong]
Copilot: [calls add_journal_entry with potentially wrong date]
Result: Entry appears under wrong day heading
```

### After (Accurate)
```
User: "Add a journal entry for last Wednesday..."
Copilot: Let me verify the date first...
Copilot: [calls calculate_relative_date "last Wednesday"]
Result: {"calculatedDate": "2025-10-15", "calculatedDayOfWeek": "Wednesday"}
Copilot: Confirms this is Wednesday, October 15, 2025
Copilot: [calls add_journal_entry with date: "2025-10-15"]
Result: Entry correctly appears under "## 15 Wednesday"
```

## Best Practices for Copilot

### 1. Always Verify Relative Dates

When the user says "last X" or "next Y":
1. Call `calculate_relative_date` first
2. Show the user the calculated date and day of week
3. Ask for confirmation if needed
4. Then call `add_journal_entry`

### 2. Show Day of Week in Responses

Always include the day of the week when mentioning dates:
- ❌ "I'll add an entry for October 15"  
- ✅ "I'll add an entry for Wednesday, October 15"

### 3. Use get_week_dates for Planning

When the user wants to add multiple entries or plan a week:
```
get_week_dates  # Gets current week
```

Shows all 7 days with their dates, making it easy to reference specific dates.

## Algorithm for "last [dayname]"

The correct algorithm implemented in `GetLastDayOfWeek`:

```
1. Start from YESTERDAY (not today)
2. Go back day by day
3. Stop when you find the target day of the week
```

Example: Finding "last Wednesday" from Sunday, October 19:
- Start: Saturday, October 18 (yesterday)
- Check: Saturday? No
- Go back: Friday, October 17
- Check: Friday? No
- Go back: Thursday, October 16  
- Check: Thursday? No
- Go back: **Wednesday, October 15** ✅
- **Result: October 15, 2025**

## Testing

Comprehensive tests in `tests/Personal.Mcp.Tests/Tools/DateUtilityToolsTests.cs`:

- ✅ All day-of-week calculations from Sunday, October 19
- ✅ "last Wednesday" correctly returns October 15
- ✅ Week boundaries and ISO week handling
- ✅ Error handling for invalid input

Run tests:
```powershell
dotnet test tests/Personal.Mcp.Tests/Personal.Mcp.Tests.csproj --filter "FullyQualifiedName~DateUtilityToolsTests"
```

## Summary

**The MCP tool was always correct**. The issue was Copilot's interpretation of natural language dates. The new `DateUtilityTools` provide explicit verification to ensure date accuracy, especially for relative date references like "last Wednesday".

Always use these tools when dealing with relative dates to ensure accurate journal entries!
