# Implementation Plan: AWS DynamoDB Persistence Option

**Branch**: `006-dynamodb-persistence` | **Date**: 2026-03-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-dynamodb-persistence/spec.md`

## Summary

Add AWS DynamoDB as a third configurable persistence backend alongside the existing SQLite
and MySQL providers. Provider selection is driven entirely by the `PERSISTENCE_PROVIDER`
environment variable (`sqlite` / `mysql` / `dynamodb`; default: `sqlite`). The existing
Application-layer repository interfaces are unchanged; a parallel set of DynamoDB
repository implementations is added to `Payslip4All.Infrastructure`. The existing EF
Core path and all its tests are preserved and unaffected.

A key complication is that the existing `Program.cs` uses `DatabaseProvider` as its
configuration key. This feature standardises the key to `PERSISTENCE_PROVIDER` across all
three providers; the old `DatabaseProvider` key is retired and must be updated in
`appsettings.json`, deployment documentation, and any CI/CD configuration.

## Technical Context

**Language/Version**: C# / .NET 8 (LTS)
**Primary Dependencies**: `AWSSDK.DynamoDBv2` (approved in constitution amendment v1.3.0);
  existing: EF Core 8, xUnit, Moq, bUnit
**Storage**: DynamoDB (multi-table design; 6 tables); SQLite and MySQL paths unchanged
**Testing**: xUnit + Moq (unit), DynamoDB-local Docker image (integration)
**Target Platform**: ASP.NET Core 8 Blazor Server (Linux/Windows server)
**Project Type**: Web application
**Performance Goals**: Standard web app expectations (p95 < 2 s for list operations)
**Constraints**: No live AWS account permitted in CI; all DynamoDB integration tests
  MUST run against the local emulator (`amazon/dynamodb-local` Docker image)
**Scale/Scope**: Same as existing providers (single-company deployments to
  small-medium business payroll)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned/written before any implementation task begins? | ✅ Yes — test tasks are defined first in every phase |
| II | Clean Architecture | Does the feature touch ≤ 4 projects (Domain/Application/Infrastructure/Web)? Does each layer only depend inward? | ✅ Yes — Domain and Application are untouched; changes in Infrastructure and Web (Program.cs) only |
| III | Blazor Web App | Are all new UI surfaces Razor components? Is business logic kept out of `.razor` files? | ✅ N/A — no new UI surfaces |
| IV | Basic Authentication | Do new pages carry `[Authorize]`? Do new service methods filter by `UserId`? | ✅ N/A for new pages; DynamoDB repositories enforce ownership filtering (see C1 below) |
| V | Database Support | For EF Core providers (SQLite/MySQL): are all schema changes EF Core migrations? Is raw SQL avoided? For DynamoDB provider (`PERSISTENCE_PROVIDER=dynamodb`): do DynamoDB repositories implement all Application interfaces? Is ownership filtering enforced? | ✅ EF Core path unchanged; DynamoDB exception formally approved in constitution v1.3.0 |
| VI | Manual Test Gate | Is the Manual Test Gate prompt planned at the end of each implementation task, before any `git commit`, `git merge`, or `git push`? | ✅ Yes — gate is mandatory at the end of each phase |

> **All gates pass.** Justified deviations documented in Complexity Tracking below.

## Project Structure

### Documentation (this feature)

```text
specs/006-dynamodb-persistence/
├── spec.md
├── plan.md                   (this file)
├── research.md               (Phase 0 output)
├── data-model.md             (Phase 1 output)
├── quickstart.md             (Phase 1 output)
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/
├── Payslip4All.Domain/           (no changes)
├── Payslip4All.Application/      (no changes)
├── Payslip4All.Infrastructure/
│   └── Persistence/
│       ├── PayslipDbContext.cs               (unchanged)
│       ├── Repositories/                     (existing EF Core — unchanged)
│       └── DynamoDB/
│           ├── DynamoDbClientFactory.cs      (new)
│           ├── DynamoDbTableProvisioner.cs   (new — auto-creates tables)
│           ├── DynamoDbUnitOfWork.cs         (new — no-op IUnitOfWork)
│           └── Repositories/
│               ├── DynamoDbUserRepository.cs
│               ├── DynamoDbCompanyRepository.cs
│               ├── DynamoDbEmployeeRepository.cs
│               ├── DynamoDbLoanRepository.cs
│               ├── DynamoDbPayslipRepository.cs
│               └── DynamoDbPayslipLoanDeductionStore.cs
└── Payslip4All.Web/
    └── Program.cs              (provider switching logic updated)

tests/
├── Payslip4All.Infrastructure.Tests/
│   └── DynamoDB/
│       ├── DynamoDbUserRepositoryTests.cs
│       ├── DynamoDbCompanyRepositoryTests.cs
│       ├── DynamoDbEmployeeRepositoryTests.cs
│       ├── DynamoDbLoanRepositoryTests.cs
│       ├── DynamoDbPayslipRepositoryTests.cs
│       └── DynamoDbTableProvisionerTests.cs
└── Payslip4All.Web.Tests/
    └── DynamoDbProviderSwitchingTests.cs   (startup integration tests)
```

**Structure Decision**: All DynamoDB infrastructure lives under
`Payslip4All.Infrastructure/Persistence/DynamoDB/` — a peer namespace to the existing
EF Core `Repositories/` directory. This keeps the parallel nature explicit without
polluting the existing namespace.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| C1 — DynamoDB repos bypass EF Core | DynamoDB cannot use EF Core as its ORM; constitution v1.3.0 explicitly approves this deviation | Using EF Core with a custom DynamoDB provider is not supported and would require maintaining a full EF Core provider — disproportionate complexity |
| C2 — `PERSISTENCE_PROVIDER` replaces `DatabaseProvider` | Standardises the env var name as specified in the approved feature spec; prevents two competing keys | Keeping `DatabaseProvider` would require supporting two keys indefinitely, complicating startup logic and operator documentation |
| C3 — `DynamoDbUnitOfWork` is a no-op | DynamoDB has no ambient transaction context; each repo method commits immediately (mirroring the existing EF Core repo behaviour where `SaveChangesAsync` is called inside each method) | Implementing full DynamoDB TransactWriteItems for the entire unit of work is disproportionate; the payslip generation service already calls `SaveChangesAsync` as a second call on an already-saved EF context |
