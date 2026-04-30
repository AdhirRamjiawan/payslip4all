<!--
SYNC IMPACT REPORT
==================
Version change: 1.3.0 → 1.4.0 (MINOR — formally approved a repository-owned YARP
public HTTPS edge and added governed third-party dependency approval rules)

Modified principles:
  - II. Clean Architecture
    Clarified that public-edge/runtime adapters MUST stay inside the existing
    Web or Infrastructure projects and MUST NOT introduce a fifth solution
    project without amendment.
  - III. ASP.NET Core Blazor Web Application
    Clarified that a repository-owned public HTTPS gateway mode MAY run inside
    Payslip4All.Web only when it remains configuration-gated, fail-closed, and
    covered by startup/integration tests.

Added sections:
  - Technology Stack & Constraints: Public HTTPS Edge row approving
    `Yarp.ReverseProxy` 2.2.x inside `Payslip4All.Web`
  - Technology Stack & Constraints: governed third-party dependency approval rule
  - Governance: amendment requirements for governed third-party dependencies

Removed sections: None.

Placeholder token audit (2026-04-30):
  - [Authorize] → ASP.NET Core attribute, NOT a template token. Correct as-is.
  - No other template placeholder tokens remain unresolved.

Templates requiring updates:
  - ✅ .specify/templates/plan-template.md — UPDATED: added governed dependency
       gate and expanded Clean Architecture gate wording
  - ✅ .specify/templates/spec-template.md — UPDATED: added governed dependency
       alignment guidance
  - ✅ .specify/templates/tasks-template.md — UPDATED: added blocking task
       guidance for governed dependencies
  - ✅ .specify/templates/commands/ — directory not present in this repository;
       no command templates required syncing
  - ✅ README.md — reviewed; existing YARP deployment guidance already aligns
  - ✅ infra/yarp/README.md — reviewed; existing operator guidance already aligns

Deferred TODOs / Manual follow-up required: None.
-->

# Payslip4All Constitution

## Core Principles

### I. Test-Driven Development (NON-NEGOTIABLE)

All production code MUST be preceded by failing tests. The Red-Green-Refactor
cycle is strictly enforced across the entire codebase:

- Tests MUST be written and confirmed failing before any implementation begins.
- Unit tests MUST cover all domain entities and application service logic in
  isolation.
- Integration tests MUST cover cross-layer interactions (e.g., service →
  repository → database).
- Component tests (bUnit) MUST cover all Blazor page components with mock
  services.
- No feature is considered complete until all associated tests pass and CI is
  green.
- Test coverage on `Payslip4All.Domain` and `Payslip4All.Application` MUST NOT
  fall below 80%; coverage drops MUST block merge.
- Tests MUST be committed in the same PR as the code they exercise; deferred
  tests are PROHIBITED.

**Rationale**: Payslips are legal financial documents. Defects cause real
monetary harm to low-income employees. TDD is the primary quality gate: it
eliminates regressions, documents intent unambiguously, and makes financial
calculation logic (UIF, loans, net pay) auditable through executable
specifications.

### II. Clean Architecture

The codebase MUST maintain strict layer separation with inward-only
dependencies:

```
Payslip4All.Web          (Presentation — Blazor Server)
    ↓ depends on
Payslip4All.Application  (Use Cases, Interfaces, DTOs)
    ↓ depends on
Payslip4All.Domain       (Entities, Business Rules, Enums)
    ↑ implemented by
Payslip4All.Infrastructure (EF Core, External Services, Auth)
```

Rules that MUST be observed:

- `Payslip4All.Domain` MUST have zero external dependencies; EF Core attributes
  on domain entities are PROHIBITED.
- `Payslip4All.Application` MUST define service interfaces
  (`IAuthenticationService`, `ICompanyService`, `IEmployeeService`, etc.); it
  MUST NOT reference EF Core or Blazor.
- `Payslip4All.Infrastructure` implements Application interfaces and references
  EF Core; it MUST NOT reference `Payslip4All.Web`.
- `Payslip4All.Web` orchestrates UI and runtime hosting concerns; it MUST NOT
  contain payroll business logic, validation rules, or direct `DbContext`
  access.
- Cross-layer communication MUST use DTOs or view models; raw domain entities
  MUST NOT be passed directly to Razor components.
- The repository pattern is REQUIRED; direct `DbContext` usage in Application or
  Web layers is PROHIBITED.
- Public-edge/runtime adapters that terminate TLS, redirect HTTP, filter hosts,
  or proxy requests MUST live inside `Payslip4All.Web` or
  `Payslip4All.Infrastructure`; they MUST NOT justify a fifth solution project
  without a written amendment.
- Project count MUST NOT exceed four (Domain, Application, Infrastructure, Web)
  without a written, approved amendment.

**Rationale**: Clean Architecture keeps payroll rules independently testable
without a running database or Blazor runtime. It also ensures deployment-facing
capabilities such as a public HTTPS edge remain repository-owned without
fracturing the solution into ad-hoc projects that dilute boundaries and slow
reviews.

### III. ASP.NET Core Blazor Web Application

The user interface MUST be implemented as an ASP.NET Core Blazor Server
application targeting .NET 8:

- All UI MUST be built with Razor components (`.razor` files).
- Routable pages MUST use the `@page` directive; shared layout components live
  in `Shared/`.
- State management MUST use cascading parameters or scoped DI services; global
  static state is PROHIBITED.
- Components MUST remain presentation-focused; all business logic MUST be
  delegated to injected services from `Payslip4All.Application`.
- Async/await MUST be used for every I/O-bound operation; synchronous blocking
  calls on the Blazor Server render thread are PROHIBITED.
- Loading states and user-facing error messages MUST be implemented for all
  async component operations.
- Bootstrap CSS MUST be used for responsive layout; custom CSS MUST be scoped to
  components (`.razor.css` isolation files).
- A repository-owned public HTTPS gateway mode MAY run inside
  `Payslip4All.Web` only when it is configuration-gated, infrastructure-focused,
  and covered by automated startup and integration tests.
- Approved gateway behavior in that mode is limited to TLS termination,
  HTTP→HTTPS redirect, host allowlisting, forwarded-header handling, health
  exposure, and request proxying to an internal-only upstream.
- Gateway mode MUST fail closed when certificate or upstream prerequisites are
  missing.
- Client-side Blazor (WASM) is out of scope unless amended.

**Rationale**: Blazor Server provides a single C# stack, eliminating the Angular
+ API complexity of a prior iteration. Keeping any public-edge mode inside the
existing web host preserves the four-project limit while making public routing
behavior observable and testable through the same application runtime.

### IV. Basic Authentication (Cookie-Based)

User authentication MUST use ASP.NET Core cookie authentication with the
following rules:

- Passwords MUST be hashed using BCrypt or PBKDF2
  (`Microsoft.AspNetCore.Cryptography.KeyDerivation`); unsalted SHA-256 is
  PROHIBITED.
- Session cookies MUST be `HttpOnly`, `Secure` in production, and use a
  configurable maximum age (default: 30 days).
- `BlazorAuthenticationStateProvider` MUST be the single source of
  authentication state for the Blazor component tree; components MUST NOT hold
  independent auth state.
- Every page except `/Auth/Login` and `/Auth/Register` MUST carry `[Authorize]`.
- Role-based access MUST distinguish **SiteAdministrator** from
  **CompanyOwner**; admin-only pages and endpoints MUST enforce role checks.
- Service methods MUST filter data by the authenticated `UserId`; queries that
  could return another owner's companies or employees are a critical security
  defect.
- Authentication failures MUST return generic error messages; they MUST NOT
  reveal whether a username exists or which field is wrong.

**Rationale**: Cookie auth is well-suited to Blazor Server's persistent
WebSocket model. Ownership filtering is a non-negotiable security control:
Company Owners MUST never access another owner's data. Financial data exposure
is a legal liability.

### V. Database Support (Entity Framework Core)

All persistent data access MUST go through Entity Framework Core unless the
approved DynamoDB exception below is active:

- `PayslipDbContext` is the sole EF Core entry point; it MUST be registered as
  `Scoped`.
- LINQ-to-Entities is the only permitted relational query mechanism; raw SQL
  strings and `FromSqlRaw` are PROHIBITED unless wrapped in a named, reviewed EF
  Core migration.
- Every schema change for relational providers MUST be captured as an EF Core
  migration with a descriptive name (e.g., `AddEmployeeLoanTable`); manual
  `ALTER TABLE` scripts are PROHIBITED.
- `EnsureMigrated()` or `MigrateAsync()` MUST be called during application
  startup to apply pending relational migrations automatically.
- Entity relationships, cascade deletes, and foreign key constraints MUST be
  configured in `OnModelCreating`; data annotations on domain entities are
  PROHIBITED.
- Default backing store is SQLite for local development; the connection string
  MUST be provided via `appsettings.json` or environment variables.
- Migrating to SQL Server MUST require only a provider swap in `Program.cs`; no
  other code changes are permitted.

**Rationale**: EF Core abstracts the backing store, making a future SQLite →
SQL Server migration low-risk. Centralized migration management prevents schema
drift between environments and ensures reproducible deployments for every
developer and CI run.

#### DynamoDB Provider Exception (feature 006-dynamodb-persistence)

When `PERSISTENCE_PROVIDER=dynamodb` is set via environment variable, the
following rules apply as an approved deviation from the EF Core mandate above:

- The `Application` layer repository interfaces (`IUserRepository`,
  `ICompanyRepository`, `IEmployeeRepository`, `IPayslipRepository`,
  `ILoanRepository`) MUST remain unchanged. The DynamoDB implementation is a
  parallel Infrastructure concern only.
- A complete set of DynamoDB repository implementations MUST be provided in
  `Payslip4All.Infrastructure`, satisfying every existing Application-layer
  interface.
- `PayslipDbContext` and EF Core migrations are bypassed entirely when DynamoDB
  is active; `MigrateAsync()` MUST NOT be called for the DynamoDB provider path.
- Required DynamoDB tables MUST be created automatically at startup if they do
  not exist, and each creation MUST be logged.
- All DynamoDB runtime configuration (region, endpoint, table prefix) MUST be
  provided via environment variables. Credentials MUST come from environment
  variables or the AWS SDK standard credential chain; hardcoded production
  credentials are PROHIBITED.
- Ownership filtering (Company Owner data isolation) MUST be enforced in every
  DynamoDB repository query, matching the behavior of the EF Core
  implementations.
- The `AWSSDK.DynamoDBv2` package is the sole approved AWS SDK entry point for
  this provider; direct HTTP calls to the DynamoDB API are PROHIBITED.
- This exception applies only when `PERSISTENCE_PROVIDER=dynamodb`. All other
  provider values (`sqlite`, `mysql`, unset) MUST use the EF Core path without
  modification.
- Integration tests MUST cover the DynamoDB provider path using a local
  DynamoDB emulator; tests requiring a live AWS account are PROHIBITED in CI.

### VI. Manual Test Gate (NON-NEGOTIABLE)

After completing any implementation task, the agent MUST NOT automatically
execute `git commit`, `git merge`, `git push`, or any combined git operation.
Instead, the following gate protocol MUST be observed:

1. **Present the Manual Test Gate prompt** to the engineer. This prompt MUST:
   - Summarize what was implemented.
   - List the recommended manual test steps relevant to the change.
   - Ask for explicit approval before proceeding with any git operation.
2. **Await an explicit engineer response**:
   - If the engineer responds with `approve` or equivalent clear affirmative
     language, the agent MAY proceed with the requested git operation.
   - If the engineer responds with `decline` or equivalent language, the agent
     MUST leave all changes staged or unstaged but uncommitted.
3. **Scope** — This gate applies without exception to:
   - `git commit` (including `--amend`)
   - `git merge`
   - `git push` (including `--force`)
   - Any scripted or combined operation that performs one or more of the above
4. **No implicit approval** — Silence, a new instruction, or an ambiguous reply
   MUST NOT be treated as approval.
5. **No bypass** — This gate MUST NOT be skipped regardless of how trivial the
   change appears.

**Rationale**: Payslips are legal financial documents. Premature or incorrect
commits reaching shared branches can corrupt payroll history, trigger erroneous
CI deployments, and make rollback painful. The Manual Test Gate ensures the
engineer has consciously verified behavior before changes become part of the
auditable commit history.

## Technology Stack & Constraints

| Concern              | Mandated Choice |
|----------------------|-----------------|
| Runtime              | .NET 8 (LTS); upgrade requires amendment |
| UI Framework         | Blazor Server (ASP.NET Core 8) |
| Public HTTPS Edge    | `Yarp.ReverseProxy` 2.2.x inside `Payslip4All.Web`; a separate gateway project requires amendment |
| ORM                  | Entity Framework Core 8 |
| DB (development)     | SQLite |
| DB (production)      | SQLite, MySQL (`Pomelo.EntityFrameworkCore.MySql`), DynamoDB (`AWSSDK.DynamoDBv2`), or SQL Server (via config only) |
| Unit / Integration   | xUnit + Moq |
| Component testing    | bUnit |
| Password hashing     | BCrypt.Net-Next or PBKDF2 (`KeyDerivation`) |
| PDF export           | QuestPDF 2024.10.x |
| Logging              | `Serilog.AspNetCore` 10.x plus `Serilog.Enrichers.Environment` and `Serilog.Enrichers.Thread`; file sink at `logs/`, daily rolling, 31-day retention |
| CSS framework        | Bootstrap 5 |
| Source control       | Git; feature branches (`###-short-description`) |

Approved third-party libraries are limited to the mandated choices in this
table and their necessary framework companions. Any new package that introduces
or materially changes a governed concern — public edge, authentication,
persistence, messaging, payment processing, logging, or document generation —
MUST be approved by constitution amendment before implementation planning or
code changes begin. `Yarp.ReverseProxy` is approved solely for the
repository-owned public HTTPS edge role described above.

## Development Workflow & Quality Gates

### TDD Cycle (MANDATORY)

1. Write failing test(s) that encode the requirement.
2. Confirm with the team that the test accurately captures intent.
3. Implement the minimum code needed to make tests pass.
4. Refactor under green tests.
5. **Present Manual Test Gate prompt** (Principle VI) and await engineer
   approval before executing any `git commit`, `git merge`, or `git push`.
6. No implementation PR is valid without corresponding test coverage.

### Manual Test Gate Protocol

At the conclusion of every implementation task the agent MUST emit a prompt
structured as follows:

```text
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔍  MANUAL TEST GATE — awaiting engineer approval
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
What was implemented:
  • <concise summary of change>

Recommended manual tests:
  1. <specific action to verify>
  2. <additional scenario if relevant>

Pending git operations:
  • <list of commands ready to execute, e.g. git commit -m "...">

Please test the above and respond:
  ✅  "approve" — to proceed with the git operation(s)
  ❌  "decline" — to leave changes uncommitted for review/revert
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

The agent MUST NOT proceed until a clear affirmative is received. If declined,
the agent MUST acknowledge and confirm changes remain uncommitted.

### Branching & Review

- Feature branches MUST use `<###-short-description>`
  (e.g., `001-payslip-generation`).
- Pull requests MUST reference the associated `spec.md` and `plan.md`.
- All PRs MUST pass CI before merge.
- Changes to `Payslip4All.Domain` entities or authentication logic MUST receive
  at least one reviewer approval beyond the author.
- Plans or specs that introduce a new governed third-party dependency MUST cite
  the approving constitution section and version; otherwise the feature is
  blocked pending amendment.

### CI Quality Gates

| Gate                                     | Threshold |
|------------------------------------------|-----------|
| `dotnet build` (warnings as errors)      | Zero warnings |
| `dotnet test`                            | 100% pass |
| Coverage — Domain + Application layers   | ≥ 80% |
| Secrets / credentials in source          | PROHIBITED |
| DI registration startup integration test | MUST pass |

## Governance

This constitution supersedes all prior architecture documents
(`old_mds/ARCHITECTURE.md`, `old_mds/README.md`) and any ad-hoc verbal
decisions. Those documents are retained for historical reference only.

**Amendment procedure**:

1. Author a written proposal naming the principle or section being changed.
2. Describe the migration plan for any existing code that would become
   non-compliant.
3. For any governed third-party dependency, identify the concern, hosting
   project, approved package and version range, alternatives rejected, and the
   automated tests that will prove safe adoption.
4. Obtain team review and approval.
5. Increment the constitution version per the policy below and update
   `LAST_AMENDED_DATE`.

**Versioning policy**:

- **MAJOR**: Removal or fundamental redefinition of a principle; breaking
  governance change.
- **MINOR**: New principle, new mandatory section, or materially expanded
  guidance added.
- **PATCH**: Clarifications, wording corrections, or non-semantic refinements.

All PRs and code reviews MUST verify compliance with this constitution. Any
intentional deviation from a principle MUST be documented in the `plan.md`
Complexity Tracking table with justification before work begins. Planning MUST
stop when a governed third-party dependency lacks an approving constitution
entry.

**Version**: 1.4.0 | **Ratified**: 2026-03-14 | **Last Amended**: 2026-04-30
