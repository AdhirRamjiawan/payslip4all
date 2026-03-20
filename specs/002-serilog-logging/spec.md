# Feature Specification: Serilog File Logging & Global Exception Handling

**Feature Branch**: `002-serilog-logging`  
**Created**: 2026-03-19  
**Status**: Draft  
**Input**: User description: "add file logging using serilog for global exception handling"

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

- **I. TDD**: All middleware and configuration code is covered by unit + integration tests before implementation.
- **II. Clean Architecture**: Logging infrastructure stays in `Payslip4All.Web` (middleware, bootstrap) and `Payslip4All.Infrastructure` (if log sink configuration is extracted). No Domain or Application changes.
- **III. Blazor**: No new UI surfaces. Existing Blazor error pages updated to surface a correlation ID only.
- **IV. Authentication**: Exception context includes authenticated `UserId` for traceability; no new auth surfaces.
- **V. Database**: No schema changes; no EF Core migrations required.
- **VI. Manual Test Gate**: Gate prompt issued after every implementation task before any git operation.

---

### User Story 1 — Exception Logged to File with Full Context (Priority: P1)

As a developer or system administrator, when an unhandled exception occurs anywhere in the
Payslip4All application, I want it captured in a structured log file on disk so I can
diagnose the root cause without needing a running debugger.

**Why this priority**: Without file logging, production errors are invisible after the
process restarts. This is the baseline observability requirement.

**Independent Test**: Trigger a deliberate unhandled exception in the test host and
assert a log entry is written to the file sink containing the exception type, message,
stack trace, and request path.

**Acceptance Scenarios**:

1. **Given** an unhandled exception is thrown during an HTTP request,  
   **When** the global exception middleware intercepts it,  
   **Then** a log entry at `Error` level is written to the configured log file containing: exception type, message, stack trace, request path, and UTC timestamp.

2. **Given** an authenticated user triggers an exception,  
   **When** the middleware logs the error,  
   **Then** the log entry includes the authenticated `UserId` as a structured property.

3. **Given** an unauthenticated request triggers an exception,  
   **When** the middleware logs the error,  
   **Then** the log entry records `UserId = anonymous` without throwing a secondary exception.

4. **Given** an exception occurs,  
   **When** the middleware handles it,  
   **Then** the HTTP response returns a user-friendly error page (non-JSON routes) or a generic error JSON payload (API routes like `/payslips/{id}/download`), and does NOT expose stack trace details to the client.

---

### User Story 2 — Rolling Daily Log Files (Priority: P2)

As an operator, I want log files to rotate daily and be retained for a configurable
number of days so disk usage stays bounded.

**Why this priority**: Without rotation, the log file grows unbounded and will eventually
exhaust disk space.

**Independent Test**: Configure retention to 1 day in tests, write a log entry, and
assert the file name contains the current date and that older files are not created.

**Acceptance Scenarios**:

1. **Given** the application is running across midnight,  
   **When** a new log entry is written,  
   **Then** it is written to a new file named with the current date (e.g., `logs/payslip4all-20260320.log`).

2. **Given** the log retention period is configured (default: 31 days),  
   **When** files older than the retention limit exist,  
   **Then** Serilog automatically deletes them on next write.

3. **Given** a `logs/` directory does not yet exist at startup,  
   **When** the application starts,  
   **Then** the directory is created automatically without manual intervention.

---

### User Story 3 — Configurable Log Level via appsettings (Priority: P3)

As a developer, I want to control the minimum log level for file output via
`appsettings.json` (and `appsettings.Development.json`) without recompiling.

**Why this priority**: Allows verbose debug logging in development and quiet production
logging without code changes.

**Independent Test**: Set `Serilog:MinimumLevel:Default` to `Warning` in test config
and assert that `Information`-level log entries are NOT written to the file sink.

**Acceptance Scenarios**:

1. **Given** `Serilog:MinimumLevel:Default` is set to `Warning` in configuration,  
   **When** an `Information`-level log is emitted,  
   **Then** it does NOT appear in the log file.

2. **Given** `Serilog:MinimumLevel:Default` is set to `Debug` in `appsettings.Development.json`,  
   **When** any log is emitted at `Debug` or above,  
   **Then** it is written to the file.

---

### Edge Cases

- What happens when the `logs/` directory is not writable (permissions denied)?  
  → Serilog self-log should surface the error to the console; the application MUST continue to run (logging failure is non-fatal).
- What happens if an exception occurs inside the middleware itself?  
  → A fallback `try/catch` returns a 500 response and writes to Serilog's self-log.
- What happens if multiple threads throw simultaneously?  
  → Serilog's file sink is thread-safe; no additional locking needed in middleware.

## Requirements

### Functional Requirements

- **FR-001**: System MUST capture all unhandled exceptions via a global middleware registered early in the ASP.NET Core pipeline.
- **FR-002**: Each exception log entry MUST include: exception type, message, full stack trace, HTTP request path, HTTP method, UTC timestamp, and authenticated `UserId` (or `anonymous`).
- **FR-003**: Log files MUST be written to a configurable path (default: `logs/payslip4all-.log`) with daily rolling and configurable retention (default: 31 days).
- **FR-004**: Minimum log level MUST be configurable via `appsettings.json` without recompilation.
- **FR-005**: The middleware MUST return a user-friendly response to the client and MUST NOT expose stack trace details in HTTP responses.
- **FR-006**: Serilog MUST be the sole logging provider; the default ASP.NET Core console logger MAY be replaced or supplemented.
- **FR-007**: Serilog self-log MUST be directed to the console so infrastructure-level errors (e.g., file permission denied) are visible without breaking the app.

### Non-Functional Requirements

- **NFR-001**: Logging failure MUST NOT crash or halt the application.
- **NFR-002**: Log writes MUST be asynchronous or buffered to avoid blocking the Blazor Server render thread.
- **NFR-003**: Log file path and retention MUST be configurable via environment variables or `appsettings.json`; no hardcoded paths in source code.

### Key Entities

No new database entities. This feature is infrastructure-only.

## Success Criteria

### Measurable Outcomes

- **SC-001**: After triggering a deliberate exception, a `Error`-level entry containing the stack trace appears in `logs/payslip4all-[date].log` within 1 second.
- **SC-002**: Log files rotate daily; no single file exceeds one day of entries.
- **SC-003**: Changing `Serilog:MinimumLevel:Default` in `appsettings.json` and restarting the app changes the observed log output without any code change.
- **SC-004**: All 152 pre-existing tests continue to pass after this feature is implemented.
