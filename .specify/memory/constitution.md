<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.2 → 1.1.0 (MINOR — new Principle VI added)
Bump rationale: A new Core Principle VI "Manual Test Gate" has been added to the
  Development Workflow section. This principle introduces a mandatory human-approval
  gate after every implementation task before any git operations (commit, merge, push)
  may be executed. This is a MINOR bump: new governance guidance added without
  removing or redefining existing principles.

Modified principles: None.

Added sections:
  - Core Principle VI: Manual Test Gate (NON-NEGOTIABLE)
  - New row VI in Development Workflow TDD Cycle
  - New sub-section "Manual Test Gate Protocol" under Development Workflow

Removed sections: None.

Placeholder token audit (2026-03-16):
  - [Authorize]  → ASP.NET Core attribute, NOT a template token. Correct as-is.
  - No other [ALL_CAPS_IDENTIFIER] template tokens remain unresolved.

Templates requiring updates:
  ✅ .specify/templates/plan-template.md   — Constitution Check table updated with
       row VI (Manual Test Gate).
  ✅ .specify/templates/tasks-template.md  — Notes and Implementation Strategy updated
       with Manual Test Gate reminder.
  ✅ .specify/templates/spec-template.md   — No changes required; principle is
       workflow-only and does not affect spec structure.
  ✅ .specify/templates/checklist-template.md — No changes required.
  ✅ .specify/templates/constitution-template.md — Source template; not modified
       (operates on memory/constitution.md only).
  ✅ .specify/templates/agent-file-template.md   — No changes required.

Deferred TODOs / Manual follow-up required: None.

Prior report (1.0.1 → 1.0.2):
  All four deferred items resolved 2026-03-15:
  specs/001-payslip-generation/contracts/http-endpoints.md,
  specs/001-payslip-generation/contracts/ui-contracts.md,
  specs/001-payslip-generation/quickstart.md,
  specs/001-payslip-generation/research.md
  — all corrected to use /Auth/Login, /Auth/Register, /Auth/Logout.
-->

# Payslip4All Constitution

## Core Principles

### I. Test-Driven Development (NON-NEGOTIABLE)

All production code MUST be preceded by failing tests. The Red-Green-Refactor
cycle is strictly enforced across the entire codebase:

- Tests MUST be written and confirmed failing before any implementation begins.
- Unit tests MUST cover all domain entities and application service logic in isolation.
- Integration tests MUST cover cross-layer interactions (e.g., service → repository → database).
- Component tests (bUnit) MUST cover all Blazor page components with mock services.
- No feature is considered complete until all associated tests pass and CI is green.
- Test coverage on `Payslip4All.Domain` and `Payslip4All.Application` MUST NOT fall
  below 80%; coverage drops MUST block merge.
- Tests MUST be committed in the same PR as the code they exercise — no deferred tests.

**Rationale**: Payslips are legal financial documents. Defects cause real monetary
harm to low-income employees. TDD is the primary quality gate: it eliminates regressions,
documents intent unambiguously, and makes financial calculation logic (UIF, loans,
net pay) auditable through executable specifications.

### II. Clean Architecture

The codebase MUST maintain strict layer separation with inward-only dependencies:

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

- `Payslip4All.Domain` MUST have zero external dependencies; no EF Core attributes
  on domain entities (configure relationships in `OnModelCreating`).
- `Payslip4All.Application` MUST define service interfaces (`IAuthenticationService`,
  `ICompanyService`, `IEmployeeService`, etc.); it MUST NOT reference EF Core or Blazor.
- `Payslip4All.Infrastructure` implements Application interfaces and references EF Core;
  it MUST NOT reference `Payslip4All.Web`.
- `Payslip4All.Web` orchestrates UI and injects services; it MUST NOT contain business
  logic, validation rules, or direct `DbContext` access.
- Cross-layer communication MUST use DTOs or view-models, not raw domain entities
  passed directly to Razor components.
- The repository pattern is REQUIRED; direct `DbContext` MUST NOT appear in Application
  or Web layers.
- Project count MUST NOT exceed four (Domain, Application, Infrastructure, Web) without
  a written, approved amendment.

**Rationale**: Clean Architecture ensures payroll business rules (UIF calculation,
loan deductions, net pay) remain independently testable without a running database or
Blazor runtime. It also allows the UI layer to evolve (e.g., adding a REST API later)
without touching domain logic.

### III. ASP.NET Core Blazor Web Application

The user interface MUST be implemented as an ASP.NET Core Blazor Server application
targeting .NET 8:

- All UI MUST be built with Razor components (`.razor` files).
- Routable pages MUST use the `@page` directive; shared layout components live in `Shared/`.
- State management MUST use cascading parameters or scoped DI services; global static
  state is PROHIBITED.
- Components MUST remain presentation-focused; all business logic MUST be delegated
  to injected services from `Payslip4All.Application`.
- Async/await MUST be used for every I/O-bound operation; synchronous blocking calls
  on the Blazor Server render thread are PROHIBITED.
- Loading states and user-facing error messages MUST be implemented for all async
  component operations.
- Bootstrap CSS MUST be used for responsive layout; custom CSS MUST be scoped to
  components (`.razor.css` isolation files).
- Client-side Blazor (WASM) is out of scope unless amended.

**Rationale**: Blazor Server provides a single C# stack, eliminating the Angular + API
complexity of a prior iteration. This reduces build complexity, accelerates feature
delivery, and enables direct domain model reuse without DTO duplication.

### IV. Basic Authentication (Cookie-Based)

User authentication MUST use ASP.NET Core cookie authentication with the following rules:

- Passwords MUST be hashed using BCrypt or PBKDF2 (`Microsoft.AspNetCore.Cryptography
  .KeyDerivation`); SHA-256 without salt is PROHIBITED.
- Session cookies MUST be `HttpOnly`, `Secure` (in production), and have a configurable
  maximum age (default: 30 days).
- `BlazorAuthenticationStateProvider` MUST be the single source of authentication state
  for the Blazor component tree; no component may hold its own auth state independently.
- Every page except `/Auth/Login` and `/Auth/Register` MUST carry `[Authorize]`.
- Role-based access MUST distinguish **SiteAdministrator** from **CompanyOwner**;
  endpoints and pages that serve admin-only functions MUST enforce role checks.
- Service methods MUST filter data by the authenticated `UserId`; queries that could
  return another owner's companies or employees are a critical security defect.
- Authentication failures MUST return generic error messages; they MUST NOT reveal
  whether a username exists or which field is wrong.

**Rationale**: Cookie auth is well-suited to Blazor Server's persistent WebSocket
model. Ownership filtering is a non-negotiable security control — Company Owners MUST
never access another owner's data. Financial data exposure is a legal liability.

### V. Database Support (Entity Framework Core)

All persistent data access MUST go through Entity Framework Core:

- `PayslipDbContext` is the sole EF Core entry point; it MUST be registered as `Scoped`.
- LINQ-to-Entities is the ONLY permitted query mechanism; raw SQL strings and
  `FromSqlRaw` are PROHIBITED unless wrapped in a named, reviewed EF Core migration.
- Every schema change MUST be captured as an EF Core migration with a descriptive name
  (e.g., `AddEmployeeLoanTable`); manual `ALTER TABLE` scripts are PROHIBITED.
- `EnsureMigrated()` (or `MigrateAsync()`) MUST be called during application startup
  to apply pending migrations automatically.
- Entity relationships, cascade deletes, and foreign key constraints MUST be configured
  in `OnModelCreating`; data annotations on domain entities are PROHIBITED.
- Default backing store is SQLite for local development; the connection string MUST be
  provided via `appsettings.json` or environment variables — no hardcoded paths.
- Migrating to SQL Server MUST require only a provider swap in `Program.cs`; no other
  code changes should be necessary.

**Rationale**: EF Core abstracts the backing store, making a future SQLite → SQL Server
migration low-risk. Centralized migration management prevents schema drift between
environments and ensures reproducible deployments for every developer and CI run.

### VI. Manual Test Gate (NON-NEGOTIABLE)

After completing any implementation task, the agent MUST NOT automatically execute
`git commit`, `git merge`, `git push`, or any combined git operation. Instead, the
following gate protocol MUST be observed:

1. **Present the Manual Test Gate prompt** to the engineer. This prompt MUST:
   - Summarise what was implemented.
   - List the recommended manual test steps relevant to the change.
   - Ask for explicit approval before proceeding with any git operation.

2. **Await an explicit engineer response**:
   - If the engineer responds with `approve` (or equivalent clear affirmative such
     as `yes`, `go ahead`, `lgtm`), the agent MAY proceed with the requested git
     operation.
   - If the engineer responds with `decline` (or equivalent: `no`, `wait`, `hold`),
     the agent MUST leave all changes staged or unstaged but uncommitted, so the
     engineer can inspect, adjust, or revert them without loss.

3. **Scope** — This gate applies without exception to:
   - `git commit` (including `--amend`)
   - `git merge`
   - `git push` (including `--force`)
   - Any scripted or combined operation that performs one or more of the above
     (e.g., CI trigger scripts, helper shell aliases).

4. **No implicit approval** — Silence, a new instruction, or an ambiguous reply
   MUST NOT be treated as approval. The agent MUST re-prompt or seek clarification.

5. **No bypass** — This gate MUST NOT be skipped regardless of how trivial the
   change appears (typo fix, comment update, etc.).

**Rationale**: Payslips are legal financial documents. Premature or incorrect commits
reaching shared branches can corrupt payroll history, trigger erroneous CI deployments,
and make rollback painful. The Manual Test Gate ensures the engineer has consciously
verified behaviour before changes become part of the auditable commit history. It also
preserves the engineer's agency over the codebase — the agent is a tool, not an
autonomous committer.

## Technology Stack & Constraints

| Concern              | Mandated Choice                                       |
|----------------------|-------------------------------------------------------|
| Runtime              | .NET 8 (LTS) — upgrade requires amendment             |
| UI Framework         | Blazor Server (ASP.NET Core 8)                        |
| ORM                  | Entity Framework Core 8                               |
| DB (development)     | SQLite                                                |
| DB (production)      | SQLite, MySQL (`Pomelo.EntityFrameworkCore.MySql`), or SQL Server (via config, no code change — see C2 deviation in feature-001 plan.md) |
| Unit / Integration   | xUnit + Moq                                           |
| Component testing    | bUnit                                                 |
| Password hashing     | BCrypt.Net-Next or PBKDF2 (KeyDerivation)             |
| PDF export (planned) | QuestPDF or equivalent — one named library only       |
| CSS framework        | Bootstrap 5                                           |
| Source control       | Git; feature branches (`###-short-description`)       |

Third-party libraries NOT in this table MUST be proposed via a written amendment
before being added to any project.

## Development Workflow & Quality Gates

### TDD Cycle (MANDATORY)

1. Write failing test(s) that encode the requirement.
2. Confirm with the team that the test accurately captures intent.
3. Implement the minimum code needed to make tests pass.
4. Refactor under green tests.
5. **Present Manual Test Gate prompt** (Principle VI) — await engineer approval
   before executing any `git commit`, `git merge`, or `git push`.
6. No implementation PR is valid without corresponding test coverage.

### Manual Test Gate Protocol

At the conclusion of every implementation task the agent MUST emit a prompt
structured as follows (adapt wording to context):

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔍  MANUAL TEST GATE — awaiting engineer approval
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
What was implemented:
  • [concise summary of change]

Recommended manual tests:
  1. [specific action to verify]
  2. [additional scenario if relevant]

Pending git operations:
  • [list of commands ready to execute, e.g. git commit -m "..."]

Please test the above and respond:
  ✅  "approve" — to proceed with the git operation(s)
  ❌  "decline" — to leave changes uncommitted for review/revert
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

The agent MUST NOT proceed until a clear affirmative is received. If declined, the
agent MUST acknowledge and confirm changes remain uncommitted.

### Branching & Review

- Feature branches: `###-short-description` (e.g., `001-payslip-generation`).
- Pull Requests MUST reference the associated `spec.md` and `plan.md`.
- All PRs MUST pass CI before merge (build + tests + coverage threshold).
- Changes to `Payslip4All.Domain` entities or authentication logic MUST receive
  at least one additional reviewer approval beyond the author.

### CI Quality Gates

| Gate                                    | Threshold          |
|-----------------------------------------|--------------------|
| `dotnet build` (warnings as errors)     | Zero warnings      |
| `dotnet test`                           | 100% pass          |
| Coverage — Domain + Application layers  | ≥ 80%              |
| Secrets / credentials in source         | PROHIBITED         |
| DI registration startup integration test| MUST pass         |

## Governance

This constitution supersedes all prior architecture documents (`old_mds/ARCHITECTURE.md`,
`old_mds/README.md`) and any ad-hoc verbal decisions. Those documents are retained for
historical reference only.

**Amendment procedure**:

1. Author a written proposal naming the principle and section being changed.
2. Describe the migration plan for any existing code that would be non-compliant.
3. Obtain team review and approval.
4. Increment the constitution version per the policy below and update `LAST_AMENDED_DATE`.

**Versioning policy**:

- **MAJOR**: Removal or fundamental redefinition of a principle; breaking governance change.
- **MINOR**: New principle, new mandatory section, or materially expanded guidance added.
- **PATCH**: Clarifications, wording corrections, or non-semantic refinements.

All PRs and code reviews MUST verify compliance with this constitution. Any intentional
deviation from a principle MUST be documented in the `plan.md` Complexity Tracking
table with justification before work begins.

**Version**: 1.1.0 | **Ratified**: 2026-03-14 | **Last Amended**: 2026-03-16
