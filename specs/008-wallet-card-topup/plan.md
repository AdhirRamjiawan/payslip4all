# Implementation Plan: Generic Wallet Card Top-Up

**Branch**: `008-wallet-card-topup` | **Date**: 2026-04-02 | **Spec**: `/Users/adhirramjiawan/projects/payslip4all/specs/008-wallet-card-topup/spec.md`  
**Input**: Feature specification from `/Users/adhirramjiawan/projects/payslip4all/specs/008-wallet-card-topup/spec.md`

## Summary

Re-plan the existing wallet card top-up design so Payslip4All uses one provider-agnostic normalization policy for Payment Return Evidence, enforces explicit evidence-precedence and exact 1-hour abandonment behavior, allows matched trustworthy late evidence to supersede `Abandoned`, keeps low-confidence or conflicting late evidence non-authoritative, preserves unmatched returns as separate auditable records, and links any one-time wallet credit to the accepted authoritative outcome.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core 8, AWSSDK.DynamoDBv2, Serilog, xUnit, Moq, bUnit  
**Storage**: SQLite for local EF Core development, MySQL / SQL Server via EF Core configuration, DynamoDB via approved provider path  
**Testing**: xUnit + Moq for Domain/Application/Infrastructure, bUnit + xUnit + Moq for Web  
**Target Platform**: ASP.NET Core Blazor Server application on server-hosted environments  
**Project Type**: Clean Architecture web application  
**Performance Goals**: SC-001 hosted-payment hand-off in under 1 minute for 95% of valid starts; SC-004 and SC-006 visibility / reclassification within 5 minutes of outcome determination  
**Constraints**: Provider-agnostic design; no card PAN/CVV/expiry storage; abandonment is exactly 1 hour after initiation; `failed` is not a valid attempt outcome; unmatched returns stay separate from attempt statuses; generic unmatched flow must reveal no guessed attempt ID, owner identity, wallet details, or wallet-credit confirmation; wallet credit occurs only after validated success using the confirmed charged amount; trustworthy matched final evidence is authoritative over pending/unverified/abandoned; later conflicting evidence is auditable but non-authoritative once a trustworthy final outcome is accepted  
**Scale/Scope**: Touches Domain, Application, Infrastructure, and Web plus all four matching test projects; extends existing wallet, wallet activity, hosted payment, and persistence abstractions rather than introducing new runtime services

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify compliance with each Payslip4All constitution principle before proceeding:

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned/written before any implementation task begins? | ☑ |
| II | Clean Architecture | Does the feature touch ≤ 4 projects (Domain/Application/Infrastructure/Web)? Does each layer only depend inward? | ☑ |
| III | Blazor Web App | Are all new UI surfaces Razor components? Is business logic kept out of `.razor` files? | ☑ |
| IV | Basic Authentication | Do new pages carry `[Authorize]`? Do new service methods filter by `UserId`? | ☑ |
| V | Database Support | For EF Core providers (SQLite/MySQL): are all schema changes EF Core migrations? Is raw SQL avoided? For DynamoDB provider (`PERSISTENCE_PROVIDER=dynamodb`): do DynamoDB repositories implement all Application interfaces? Is ownership filtering enforced? | ☑ |
| VI | Manual Test Gate | Is the Manual Test Gate prompt planned at the end of each implementation task, before any `git commit`, `git merge`, or `git push`? | ☑ |

**Gate Result**: PASS. The design stays within the existing four-project architecture, keeps wallet/payment rules in Domain and Application, keeps provider parsing in Infrastructure, and limits Web changes to owner-scoped Razor pages and routing.

## Project Structure

### Documentation (this feature)

```text
specs/008-wallet-card-topup/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── hosted-payment-provider-contract.md
│   ├── wallet-card-topup-application-contract.md
│   └── wallet-card-topup-ui-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── Payslip4All.Domain/
│   ├── Entities/
│   ├── Enums/
│   └── Services/
├── Payslip4All.Application/
│   ├── DTOs/Wallet/
│   ├── Interfaces/
│   ├── Interfaces/Repositories/
│   └── Services/
├── Payslip4All.Infrastructure/
│   ├── HostedPayments/
│   ├── Persistence/
│   │   ├── DynamoDB/Repositories/
│   │   ├── Repositories/
│   │   └── PayslipDbContext.cs
│   └── Migrations/
└── Payslip4All.Web/
    └── Pages/

tests/
├── Payslip4All.Domain.Tests/
├── Payslip4All.Application.Tests/
├── Payslip4All.Infrastructure.Tests/
└── Payslip4All.Web.Tests/
```

**Structure Decision**: Reuse the existing four-project Clean Architecture layout. Domain owns attempt state rules and audit invariants, Application owns the deterministic normalization and orchestration policy, Infrastructure owns EF Core / DynamoDB persistence plus provider evidence parsing, and Web owns only owner-scoped Razor routes for start flow, matched results, and the generic unmatched result.

## Design Focus

### Phase 0: Research outcomes to carry into implementation

- Define one application-owned precedence policy for Payment Return Evidence so providers cannot alter business semantics.
- Make the abandonment threshold explicit and deterministic at the exact 1-hour mark.
- Define how trustworthy late evidence may supersede `Abandoned` while low-confidence late evidence may not reopen it.
- Strengthen the audit model so matched trustworthy evidence, unmatched returns, superseded abandonment, normalization decisions, and wallet-credit linkage remain financially credible and traceable.

### Phase 1: Design commitments

- Replace any `Failed`-centric wallet top-up design with explicit matched attempt outcomes: `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, and `Unverified`.
- Introduce persistent Payment Return Evidence and Normalized outcome decision records so every authoritative outcome and ignored/conflicting late signal is auditable.
- Route provider/browser returns through a generic inbound path first, then branch into either a matched attempt result page or a generic unmatched result page.
- Preserve exactly-once wallet crediting on both EF Core and DynamoDB paths using the accepted trustworthy successful evidence and its confirmed charged amount.
- Keep accepted trustworthy final outcomes authoritative; later conflicting evidence is recorded for audit only and cannot reverse, duplicate, or replace the authoritative outcome inside this feature.

### Planned test coverage before implementation

- **Domain tests**: attempt state transitions, exact-threshold abandonment, late trustworthy replacement of `Abandoned`, immutable authoritative finals, and invalid-status rejection.
- **Application tests**: normalization precedence, unmatched-before-outcome handling, owner scoping, idempotent settlement, low-confidence late evidence handling, conflicting-evidence auditing, and generic unmatched privacy-safe result selection.
- **Infrastructure tests**: EF Core migration-backed persistence, DynamoDB parity, settlement idempotency, provider evidence mapping, and audit-record persistence.
- **Web tests**: wallet page behavior, generic return route, matched result rendering, unmatched generic result privacy, and owner authorization.

## Post-Design Constitution Check

| # | Principle | Post-Design Status |
|---|-----------|--------------------|
| I | TDD | PASS — design maps each rule to explicit failing tests across Domain/Application/Infrastructure/Web before implementation |
| II | Clean Architecture | PASS — business rules remain in Domain/Application; providers and persistence stay in Infrastructure; UI stays orchestration-only |
| III | Blazor Web App | PASS — new surfaces remain Razor pages/components and do not own normalization logic |
| IV | Basic Authentication | PASS — all routes remain `CompanyOwner`-scoped and matched data is filtered by `UserId` |
| V | Database Support | PASS — persistence changes remain repository-backed with EF Core migration parity and DynamoDB repository parity |
| VI | Manual Test Gate | PASS — implementation tasks must conclude with the constitution-mandated manual test gate prompt |

## Complexity Tracking

No constitution violations or justified exceptions are required for this feature plan.
