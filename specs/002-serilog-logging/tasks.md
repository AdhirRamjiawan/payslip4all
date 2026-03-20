---
description: "Task list for 002-serilog-logging"
---

# Tasks: Serilog File Logging & Global Exception Handling

**Input**: Design documents from `/specs/002-serilog-logging/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for all features in
this project (xUnit for unit/integration, bUnit for Blazor components). Tests MUST be
written and confirmed failing before implementation tasks begin.

**Scope**: Changes confined to `Payslip4All.Web` and `Payslip4All.Web.Tests` only.
No Domain, Application, or Infrastructure changes required.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add Serilog NuGet dependency to the Web project.

- [X] T001 Add `Serilog.AspNetCore` 8.x NuGet package to `src/Payslip4All.Web/Payslip4All.Web.csproj` via `dotnet add package Serilog.AspNetCore`

**Checkpoint**: `dotnet build` succeeds with Serilog reference resolved.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Bootstrap Serilog as the application-wide logging provider. All three user stories depend on this foundation being in place before any test or implementation work begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 Add `Serilog` configuration section (file sink, enrichers, minimum levels, rolling interval, retention, output template) to `src/Payslip4All.Web/appsettings.json`
- [X] T003 [P] Add `Serilog:MinimumLevel:Default: Debug` override to `src/Payslip4All.Web/appsettings.Development.json`
- [X] T004 Bootstrap Serilog in `src/Payslip4All.Web/Program.cs`: call `builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext())` before `builder.Build()`; enable `Serilog.Debugging.SelfLog` to `Console.Error`

**Checkpoint**: Application starts, logs appear at `logs/payslip4all-[date].log`. Existing 152 tests still pass.

---

## Phase 3: User Story 1 — Exception Logged to File with Full Context (Priority: P1) 🎯 MVP

**Goal**: All unhandled exceptions are intercepted by `GlobalExceptionMiddleware`, enriched with request context and user identity, logged at `Error` level, and a user-friendly 500 response is returned without exposing the stack trace.

**Independent Test**: Trigger a deliberate exception via a test endpoint; assert the log file contains the exception type, stack trace, request path, HTTP method, and UserId.

### Tests for User Story 1 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation (T006–T008)**

- [X] T005 [US1] Write failing unit tests for `GlobalExceptionMiddleware` in `tests/Payslip4All.Web.Tests/Middleware/GlobalExceptionMiddlewareTests.cs` covering:
  - (1) Unhandled exception → `ILogger.LogError` called once with the exception, request path and method
  - (2) Authenticated user → `UserId` claim value appears in log scope properties
  - (3) Unauthenticated request → `"anonymous"` appears in log scope properties, no secondary exception thrown
  - (4) Response not yet started → middleware writes HTTP 500 status and does NOT propagate the exception to the caller
  - (5) Response already started (Blazor circuit streaming) → exception is rethrown so Blazor's error boundary handles it

### Implementation for User Story 1

- [X] T006 [US1] Implement `GlobalExceptionMiddleware` in `src/Payslip4All.Web/Middleware/GlobalExceptionMiddleware.cs`:
  - Constructor: `ILogger<GlobalExceptionMiddleware>`, `RequestDelegate _next`
  - `InvokeAsync`: wrap `await _next(context)` in try/catch
  - On exception: read `UserId` from `context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous"`; push `UserId`, `RequestPath`, `RequestMethod` via `LogContext.PushProperty`; call `_logger.LogError(ex, "Unhandled exception on {Method} {Path}", method, path)`
  - If `!context.Response.HasStarted`: set status 500, write user-friendly response (redirect to `/Error` for `text/html` Accept, or JSON `{"error":"An unexpected error occurred."}` for API paths starting with `/payslips`)
  - If `context.Response.HasStarted`: rethrow
- [X] T007 [US1] Create `UseGlobalExceptionHandler()` extension method in `src/Payslip4All.Web/Extensions/ApplicationBuilderExtensions.cs` that registers `GlobalExceptionMiddleware`
- [X] T008 [US1] Register middleware in `src/Payslip4All.Web/Program.cs`: call `app.UseGlobalExceptionHandler()` after `app.UseStaticFiles()` and before `app.UseRouting()` — remove or keep `app.UseExceptionHandler("/Error")` only for non-Blazor routes

**Checkpoint**: US1 fully functional — trigger an exception, observe structured log entry in file with correct fields. All T005 tests pass.

---

## Phase 4: User Story 2 — Rolling Daily Log Files (Priority: P2)

**Goal**: Log files rotate daily with date-stamped names; old files are auto-deleted after the configured retention period; the `logs/` directory is created automatically on first write.

**Independent Test**: Using a temp log path in `WebApplicationFactory<Program>`, trigger any log write, assert a file exists containing the current UTC date in its name. Inspect `retainedFileCountLimit` configuration is present.

### Tests for User Story 2 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation (T010–T011)**

- [X] T009 [US2] Write failing integration test in `tests/Payslip4All.Web.Tests/Integration/LoggingIntegrationTests.cs`:
  - Override `Serilog` config via `WebApplicationFactory` to write to a temp directory
  - Trigger a request (e.g., `GET /portal`) to produce at least one log entry
  - Assert a log file is created whose name contains today's UTC date in `yyyyMMdd` format (e.g., `payslip4all-20260319.log`)
  - Assert the log file is non-empty
  - Clean up temp directory in test teardown

### Implementation for User Story 2

- [X] T010 [US2] Verify `appsettings.json` file sink `Args` include `rollingInterval: "Day"`, `retainedFileCountLimit: 31`, and `path: "logs/payslip4all-.log"` (Serilog.Sinks.File appends the date before the extension automatically); adjust if missing from T002
- [X] T011 [US2] Confirm `Serilog.Debugging.SelfLog` is routed to `Console.Error` in `src/Payslip4All.Web/Program.cs` (bootstrapped in T004) so permission-denied and other sink-level errors surface without crashing the application

**Checkpoint**: US2 fully functional — daily rolling confirmed via integration test; `logs/` created automatically; retention configured. All T009 tests pass.

---

## Phase 5: User Story 3 — Configurable Log Level via appsettings (Priority: P3)

**Goal**: `Serilog:MinimumLevel:Default` in `appsettings.json` (or any environment-specific override) controls what reaches the file sink at runtime without recompilation.

**Independent Test**: Override `Serilog:MinimumLevel:Default` to `Warning` in the test host configuration and emit an `Information`-level log; assert it does NOT appear in the log file.

### Tests for User Story 3 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation (T013)**

- [X] T012 [US3] Extend `tests/Payslip4All.Web.Tests/Integration/LoggingIntegrationTests.cs` with a test that:
  - Overrides `Serilog:MinimumLevel:Default` to `Warning` via `WebApplicationFactory` config override
  - Emits an `ILogger.LogInformation(...)` call
  - Asserts the log file does NOT contain the information-level message
  - Also asserts that a `LogWarning(...)` IS written (confirming the sink is active)

### Implementation for User Story 3

- [X] T013 [US3] Confirm `appsettings.json` contains per-namespace level overrides under `Serilog:MinimumLevel:Override` for `Microsoft: Warning`, `Microsoft.Hosting.Lifetime: Information`, and `System: Warning` to suppress framework noise at default log level; adjust if missing from T002

**Checkpoint**: US3 fully functional — log level is runtime-configurable. All T012 tests pass. All 3 user stories working end-to-end.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validation, documentation, and ensuring pre-existing tests are unaffected.

- [X] T014 [P] Run full test suite (`dotnet test`) to confirm all pre-existing 152 tests plus all new tests pass with zero failures
- [X] T015 [P] Update `README.md` to document: log file location (`logs/`), how to change the minimum log level, and where to find the rolling file configuration in `appsettings.json`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **BLOCKS all user stories**
- **US1 (Phase 3)**: Depends on Phase 2 — no dependency on US2 or US3
- **US2 (Phase 4)**: Depends on Phase 2 — no dependency on US1 or US3
- **US3 (Phase 5)**: Depends on Phase 2 — no dependency on US1 or US2
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start immediately after Phase 2 ✅
- **US2 (P2)**: Can start immediately after Phase 2 ✅ (independent of US1)
- **US3 (P3)**: Can start immediately after Phase 2 ✅ (independent of US1/US2)

### Within Each User Story

1. Write tests (T005 / T009 / T012) — confirm **FAIL**
2. Implement feature code (T006–T008 / T010–T011 / T013)
3. Confirm tests **PASS**
4. **Manual Test Gate (Principle VI)**: Present gate prompt, await `approve`
5. Proceed with `git commit` only after approval

### Parallel Opportunities

- T002 and T003 can run in parallel (different files)
- After Phase 2: US1, US2, and US3 phases can all be started in parallel (different files, independent tests)
- T014 and T015 can run in parallel

---

## Parallel Example: Phase 2

```
# Run simultaneously (different files):
Task T002: "Configure Serilog section in appsettings.json"
Task T003: "Add Development override in appsettings.Development.json"
```

## Parallel Example: After Foundational

```
# Can run simultaneously (different stories, different files):
Task T005: "Write failing tests for GlobalExceptionMiddleware (US1)"
Task T009: "Write failing integration test for rolling files (US2)"
Task T012: "Write failing test for log level config (US3)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Install Serilog package
2. Complete Phase 2: Bootstrap Serilog (CRITICAL — blocks all stories)
3. Write T005 tests → confirm FAIL
4. Complete Phase 3: GlobalExceptionMiddleware (T006–T008)
5. Confirm T005 tests PASS
6. **Present Manual Test Gate prompt** — await engineer `approve`
7. `git commit` only after approval

### Incremental Delivery

1. Phase 1 + Phase 2 → Serilog logging live for all existing log calls
2. Phase 3 (US1) → Global exception middleware active; exceptions logged to file
3. Phase 4 (US2) → Rolling/retention confirmed and tested
4. Phase 5 (US3) → Log level fully configurable
5. Phase 6 → Full validation; README updated

---

## Notes

- [P] tasks = different files, no dependencies between them
- [Story] label maps task to user story for traceability
- No Domain, Application, or Infrastructure code changes — Web layer only
- Serilog.AspNetCore 8.x transitively includes Serilog.Sinks.File — no separate package reference needed
- **Manual Test Gate (Principle VI — NON-NEGOTIABLE)**: After every implementation task, present the gate prompt and await explicit `approve` before any `git commit`, `git merge`, or `git push`. Leave changes uncommitted if engineer responds `decline`.
