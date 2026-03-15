<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.1 → 1.0.2 (PATCH — path example drift correction)
Bump rationale: Principle IV referenced `/login` and `/register` as the anonymous-access
  exception paths in the [Authorize] rule. The implemented Razor Pages routing places
  these pages under the `/Auth/` folder, producing canonical routes `/Auth/Login` and
  `/Auth/Register`. This PATCH corrects the literal path strings in Principle IV to match
  the canonical implementation. No principle semantics were changed.

Modified principles:
  - Principle IV "Basic Authentication (Cookie-Based)":
      OLD: Every page except `/login` and `/register` MUST carry [Authorize].
      NEW: Every page except `/Auth/Login` and `/Auth/Register` MUST carry [Authorize].

Added sections: None.
Removed sections: None.

Placeholder token audit (2026-03-15):
  - [Authorize]  → ASP.NET Core attribute, NOT a template token. Correct as-is.
  - No other [ALL_CAPS_IDENTIFIER] template tokens remain unresolved.

Templates requiring updates:
  ✅ .specify/templates/plan-template.md   — No path references. No edit required.
  ✅ .specify/templates/spec-template.md   — No path references. No edit required.
  ✅ .specify/templates/tasks-template.md  — No path references. No edit required.
  ✅ .specify/templates/checklist-template.md — No path references. No edit required.
  ✅ .specify/templates/constitution-template.md — Template only; no edit required.
  ✅ .specify/templates/agent-file-template.md   — No path references. No edit required.

Deferred TODOs / Manual follow-up required:
  ✅ specs/001-payslip-generation/contracts/http-endpoints.md — Resolved 2026-03-15.
  ✅ specs/001-payslip-generation/contracts/ui-contracts.md — Resolved 2026-03-15.
  ✅ specs/001-payslip-generation/quickstart.md — Resolved 2026-03-15.
  ✅ specs/001-payslip-generation/research.md — Resolved 2026-03-15.
  All four deferred items were corrected to use /Auth/Login, /Auth/Register, /Auth/Logout.
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
4. Refactor under green tests; commit only when fully green.
5. No implementation PR is valid without corresponding test coverage.

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

**Version**: 1.0.2 | **Ratified**: 2026-03-14 | **Last Amended**: 2026-03-15
