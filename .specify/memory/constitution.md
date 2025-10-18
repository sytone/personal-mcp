<!--
Sync Impact Report
- Version: 0.0.0 → 1.0.0
- Modified Principles:
	- (new) → Code Quality as a Release Gate
	- (new) → Test Protection for Every Change
	- (new) → Consistent End-User Experience
	- (new) → Performance Budgets for Vault Operations
- Added Sections:
	- Quality Gates & Metrics
	- Development Workflow & Compliance
- Removed Sections:
	- Placeholder principle slots
	- Untitled template sections
- Templates requiring updates:
	- ✅ .specify/templates/plan-template.md
	- ✅ .specify/templates/spec-template.md
	- ✅ .specify/templates/tasks-template.md
	- ⚠ pending .specify/templates/agent-file-template.md (auto-generated during plans)
- Follow-up TODOs:
	- None
-->

# Personal MCP Constitution

## Core Principles

### I. Code Quality as a Release Gate

- Every pull request MUST ship with readable, idiomatic C# that follows the repository style guides and removes dead code it touches.
- Static analysis, formatters, and linting MUST run cleanly on every change set before merge; failing checks block the release.
- Code reviews MUST include explicit verification that naming, comments, and structure make the intent obvious to a new contributor within five minutes.

**Rationale**: A small team iterating on a vault-focused MCP server cannot afford regressions caused by unclear or inconsistent code; high-quality code keeps maintenance predictable.

### II. Test Protection for Every Change

- New behavior MUST be guarded by automated tests written before or alongside implementation; tests MUST fail at least once to prove coverage.
- Touching an existing component requires updating or adding unit and integration tests so that coverage for changed files never drops below 85%.
- Contract tests for MCP tools and CLI commands MUST exercise both success and error paths relevant to the change.

**Rationale**: The server integrates with user vaults where data loss is unacceptable; disciplined testing prevents regressions and documents expected behavior.

### III. Consistent End-User Experience

- Tool names, arguments, and outputs MUST remain consistent across interfaces; any change requires documentation updates in the same pull request.
- User-facing errors MUST provide actionable guidance and reference remediation steps or logs.
- Accessibility defaults (plain text, JSON schemas, predictable ordering) MUST remain stable so that automation and screen readers can parse responses reliably.

**Rationale**: Consistency across tools builds trust for vault authors and downstream automations consuming MCP responses.

### IV. Performance Budgets for Vault Operations

- Core operations (note read/write, tag updates, link scans, search) MUST complete within 300 ms p95 for a 5k-note vault on reference hardware; falling outside this budget requires a tracked remediation task.
- Indexing or migration jobs MUST stream work in batches and avoid blocking the server main loop for more than 50 ms.
- Any change that increases memory usage above 200 MB RSS in steady state MUST document the justification and mitigations.

**Rationale**: Vaults scale quickly; predictable performance preserves responsiveness inside Obsidian and other MCP clients.

## Quality Gates & Metrics

1. **Static Quality**: dotnet format, analyzers, and security scanners MUST pass in CI before merge. Exceptions require a written risk assessment and time-bound fix.
2. **Test Enforcement**: CI MUST run unit, integration, and contract test suites; a pull request cannot merge without green runs and coverage confirmation ≥85% on touched files.
3. **UX Verification**: Each release MUST include an updated quickstart or changelog snippet describing UX-impacting changes, plus screenshots or JSON samples when relevant.
4. **Performance Monitoring**: Add or update benchmarks when touching performance-critical paths; attach p95 latency measurements in the PR description for verification.

## Development Workflow & Compliance

1. Start from a feature specification and implementation plan that map work to the principles above.
2. During development, maintain a running checklist that ties tasks to the principles they satisfy; unresolved conflicts block merge.
3. Code reviewers MUST confirm adherence to the four core principles and record the check in review comments.
4. After merge, update operational runbooks and automation scripts to stay aligned with UX and performance guarantees.

## Governance

- **Authority**: This constitution supersedes any prior informal practices for the Personal MCP project.
- **Amendments**: Proposed changes require agreement from two maintainers, documentation of rationale, and an accompanying update to affected templates within the same change set.
- **Versioning Policy**: Use semantic versioning—MAJOR for principle rewrites, MINOR for new principles or sections, PATCH for clarifications.
- **Compliance Reviews**: Maintainers MUST schedule a quarterly audit of active plans, specs, and tasks to ensure conformance with the constitution and to record remediation actions.

**Version**: 1.0.0 | **Ratified**: 2025-10-01 | **Last Amended**: 2025-10-16
