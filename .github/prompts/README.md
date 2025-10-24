# DateTime to DateTimeOffset Migration

This directory contains a reusable prompt for migrating C# projects from `DateTime` to `DateTimeOffset`.

## Files

1. **`migrate-datetime-to-datetimeoffset.md`** - The main prompt
   - Instructions for AI on how to execute the migration
   - Creates project-specific plan in `.copilot-tracking/changes/`
   - Updates/creates C# instructions file
   - Test-driven, phase-by-phase approach

2. **`../.instructions/csharp-datetime.instructions.md`** - Automatic enforcement
   - Applies to all `*.cs` files
   - Ensures future code uses DateTimeOffset
   - Quick reference for patterns

## How to Use

```
Migrate this C# project from DateTime to DateTimeOffset.
Use .github/prompts/migrate-datetime-to-datetimeoffset.md
```

The AI will:
1. Analyze the codebase
2. Create a migration plan at `.copilot-tracking/changes/datetime-migration-plan.md`
3. Execute the migration with test-driven approach
4. Track progress with checkboxes
5. Update/create the instructions file

## What Gets Created

After running the prompt:
- `.copilot-tracking/changes/datetime-migration-plan.md` - Project-specific plan with checkboxes
- `.github/instructions/csharp-datetime.instructions.md` - Enforces DateTimeOffset in future code (if doesn't exist)

## Reusable Across Projects

This prompt works on any C# project. Each project gets its own customized migration plan.
