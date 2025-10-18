# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → If not found: ERROR "No implementation plan found"
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Extract decisions → setup tasks
   → plan.md Experience & Performance Guardrails → UX/perf tasks
3. Generate tasks by category:
   → Setup: project init, dependencies, linting, analyzers
   → Tests: contract tests, integration tests, coverage enforcement
   → Core: models, services, CLI commands
   → UX Consistency: docs, error copy, sample payload updates
   → Performance: benchmarks, profiling, performance remediation
   → Integration: DB, middleware, logging
   → Polish: unit tests, observability, final docs
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD) with failing proof
   → Include linting/format and coverage verification tasks for every feature branch
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have tests?
   → All entities have models?
   → All endpoints implemented?
9. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure

## Phase 3.1: Setup
- [ ] T001 Create project structure per implementation plan
- [ ] T002 Initialize [language] project with [framework] dependencies
- [ ] T003 [P] Configure linting, formatting, and static analyzers (dotnet format, analyzers)
- [ ] T004 Enable CI coverage reporting for touched projects

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**
- [ ] T005 [P] Contract test POST /api/users in tests/contract/test_users_post.py
- [ ] T006 [P] Contract test GET /api/users/{id} in tests/contract/test_users_get.py
- [ ] T007 [P] Integration test user registration in tests/integration/test_registration.py
- [ ] T008 [P] Integration test auth flow in tests/integration/test_auth.py
- [ ] T009 Ensure coverage on touched files stays ≥85% (fail build if lower)

## Phase 3.3: Core Implementation (ONLY after tests are failing)
- [ ] T010 [P] User model in src/models/user.py
- [ ] T011 [P] UserService CRUD in src/services/user_service.py
- [ ] T012 [P] CLI --create-user in src/cli/user_commands.py
- [ ] T013 POST /api/users endpoint
- [ ] T014 GET /api/users/{id} endpoint
- [ ] T015 Input validation
- [ ] T016 Error handling and logging with actionable messages

## Phase 3.4: Integration
- [ ] T017 Connect UserService to DB
- [ ] T018 Auth middleware
- [ ] T019 Request/response logging (structured JSON)
- [ ] T020 CORS and security headers
- [ ] T021 [P] Update UX samples in docs/quickstart.md and README snippets

## Phase 3.5: Polish
- [ ] T022 [P] Unit tests for validation in tests/unit/test_validation.py
- [ ] T023 Performance benchmark (<300 ms p95, 200 MB RSS) with report in docs/perf.md
- [ ] T024 [P] UX regression checklist in docs/ux.md (tool signatures, error copy)
- [ ] T025 Remove duplication
- [ ] T026 Run manual-testing.md and attach results to PR
- [ ] T027 Confirm static analysis + coverage reports uploaded in CI

## Dependencies
- Tests (T004-T007) before implementation (T008-T014)
- T008 blocks T009, T015
- T016 blocks T018
- Implementation before polish (T019-T023)

## Parallel Example
```
# Launch T004-T007 together:
Task: "Contract test POST /api/users in tests/contract/test_users_post.py"
Task: "Contract test GET /api/users/{id} in tests/contract/test_users_get.py"
Task: "Integration test registration in tests/integration/test_registration.py"
Task: "Integration test auth in tests/integration/test_auth.py"
```

## Notes
- [P] tasks = different files, no dependencies
- Verify tests fail before implementing
- Commit after each task
- Avoid: vague tasks, same file conflicts

## Task Generation Rules
*Applied during main() execution*

1. **From Contracts**:
   - Each contract file → contract test task [P]
   - Each endpoint → implementation task
   
2. **From Data Model**:
   - Each entity → model creation task [P]
   - Relationships → service layer tasks
   
3. **From User Stories**:
   - Each story → integration test [P]
   - Quickstart scenarios → validation tasks

4. **Ordering**:
   - Setup → Tests → Models → Services → Endpoints → Polish
   - Dependencies block parallel execution

## Validation Checklist
*GATE: Checked by main() before returning*

- [ ] All contracts have corresponding tests
- [ ] All entities have model tasks
- [ ] All tests come before implementation
- [ ] Parallel tasks truly independent
- [ ] Each task specifies exact file path
- [ ] No task modifies same file as another [P] task
- [ ] Coverage enforcement, UX, and performance tasks present