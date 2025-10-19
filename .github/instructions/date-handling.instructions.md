---
applyTo: "**"
---

# Date Calculation Guidelines for GitHub Copilot

## Critical Rule: Always Verify Relative Dates

When a user mentions relative dates like "last Wednesday", "next Friday", "two weeks ago", etc., you **MUST**:

1. **Use `calculate_relative_date` tool first** to compute the exact date
2. **Display the calculated date WITH day of week** to the user for verification
3. **Only then** proceed with the requested action

## Why This Matters

**Problem**: Natural language date interpretation is ambiguous and error-prone. Copilot may miscalculate dates.

**Example Error**:
- User (on Sunday, Oct 19, 2025): "Add entry for last Wednesday"
- Wrong calculation: October 16, 2025 (Thursday) ❌  
- Correct calculation: October 15, 2025 (Wednesday) ✅

## Correct Workflow

### ❌ WRONG - Direct Calculation
```
User: "Add journal entry for last Wednesday..."
Copilot: [calculates internally: maybe Oct 16?]
Copilot: add_journal_entry("Test entry", date="2025-10-16")
Result: Entry appears under "## 16 Thursday" (WRONG DAY!)
```

### ✅ CORRECT - Use Date Tools
```
User: "Add journal entry for last Wednesday..."

Copilot Step 1 - Verify date:
  calculate_relative_date("last Wednesday", "2025-10-19")
  → Returns: {"calculatedDate": "2025-10-15", "calculatedDayOfWeek": "Wednesday"}

Copilot Step 2 - Confirm with user:
  "I'll add an entry for last Wednesday, October 15, 2025."

Copilot Step 3 - Execute:
  add_journal_entry("Test entry", date="2025-10-15")
  → Entry correctly appears under "## 15 Wednesday"
```

## Available Date Tools

### 1. calculate_relative_date
**When to use**: User mentions "last X", "next Y", "X days/weeks ago"

**Examples**:
```
calculate_relative_date("last Wednesday")
calculate_relative_date("next Friday", "2025-10-19")  
calculate_relative_date("2 weeks ago")
```

### 2. get_date_info
**When to use**: Need to verify what day of week a specific date is

**Example**:
```
get_date_info("2025-10-15")
→ Returns day of week, ISO week, relative description
```

### 3. get_week_dates
**When to use**: User wants to plan multiple entries or see the whole week

**Example**:
```
get_week_dates("2025-10-15")
→ Returns all 7 days in that ISO week
```

## Response Format Rules

### Always Include Day of Week

When mentioning dates in responses:
- ❌ "I'll create an entry for October 15"
- ✅ "I'll create an entry for Wednesday, October 15, 2025"

### Show Verification

When using date tools:
```
"Let me verify that date... [calls calculate_relative_date]
Last Wednesday from today (Sunday, October 19) is Wednesday, October 15.
I'll add your entry for October 15."
```

## Algorithm Reference

### "last [dayname]" Logic
Correctly implemented in `DateUtilityTools.GetLastDayOfWeek`:

```
1. Start from yesterday (reference date - 1)
2. Loop backwards day by day
3. Return first occurrence of target day
```

**Example**: "last Wednesday" from Sunday, October 19
- Oct 18 (Sat) → No
- Oct 17 (Fri) → No  
- Oct 16 (Thu) → No
- Oct 15 (Wed) → **YES!** ✅

### "next [dayname]" Logic
```
1. Start from tomorrow (reference date + 1)
2. Loop forward day by day
3. Return first occurrence of target day
```

## Edge Cases to Handle

### 1. Ambiguous References
If user says "Wednesday" without "last" or "next":
- If today is Sunday and they say "Wednesday"
- Ask: "Do you mean last Wednesday (Oct 15) or next Wednesday (Oct 22)?"

### 2. Current Day
If user says "last Wednesday" and today IS Wednesday:
- Correct: Previous Wednesday (7 days ago)
- NOT today

### 3. ISO Week Boundaries
- Week starts Monday (ISO 8601)
- December 31 might be in Week 1 of next year
- Use `isoYear` and `isoWeek` from tool results

## Testing Your Date Calculations

Before making journal entries, mentally verify:
1. What is today's date and day?
2. What day of the week is the target date?
3. Does the calculation make sense?

**Quick check**: If today is Sunday Oct 19 and you're looking for "last Wednesday":
- Count back: Sat (1), Fri (2), Thu (3), Wed (4) 
- Answer: 4 days back = Oct 15 ✅

## Documentation

Full details: `docs/date-accuracy.md`
Tool implementation: `src/Personal.Mcp/Tools/DateUtilityTools.cs`
Tests: `tests/Personal.Mcp.Tests/Tools/DateUtilityToolsTests.cs`
