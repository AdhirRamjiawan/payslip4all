# Implementation Plan: PayFast Card Integration

**Branch**: `009-payfast-card-integration` | **Date**: 2026-04-05 | **Spec**: `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/spec.md`  
**Input**: Feature specification from `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/spec.md`

## Summary

Implement real PayFast-hosted wallet top-ups for `CompanyOwner` users with explicit card-only checkout, authoritative server-side notify validation, owner-safe browser returns, standardized Payment Confirmation Record terminology, and `SiteAdministrator`-only internal review that exposes only minimum non-sensitive evidence. The design preserves Clean Architecture, keeps Razor pages presentation-only, and requires parity across EF Core and DynamoDB persistence paths including startup table verification for DynamoDB.

## Technical Context

**Language/Version**: C# 12 on .NET 8 / ASP.NET Core 8 Blazor Web App  
**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, Serilog, xUnit, bUnit, Moq, PayFast hosted-payment integration  
**Storage**: SQLite/MySQL via EF Core migrations; DynamoDB via repository implementations and startup table verification when `PERSISTENCE_PROVIDER=dynamodb`  
**Testing**: xUnit, Moq, bUnit, WebApplicationFactory-style integration coverage, DynamoDB parity/infrastructure tests  
**Target Platform**: ASP.NET Core 8 web application on server-hosted Linux/macOS/Windows environments with public HTTPS callback reachability  
**Project Type**: Clean Architecture web application  
**Performance Goals**: Meet SC-002 by making authoritative successful callbacks visible in wallet balance and owner history within 1 minute for at least 95% of successful top-ups; preserve exactly-once settlement under replay  
**Constraints**: Card-only settlement; browser return is informational only; wallet credit requires validated server-side notify plus PayFast confirmation; sandbox allowed only in non-production; no PAN/CVV/expiry/raw gateway diagnostics; owner-safe `Top-up not confirmed` messaging; `SiteAdministrator`-only internal review with minimal non-sensitive evidence; no more than four production projects  
**Scale/Scope**: Touches Domain, Application, Infrastructure, and Web plus corresponding test projects; affects wallet pages, PayFast notify endpoint, audit persistence, reconciliation flows, and one internal admin review surface

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned/written before any implementation task begins? | PASS |
| II | Clean Architecture | Does the feature touch ≤ 4 projects (Domain/Application/Infrastructure/Web)? Does each layer only depend inward? | PASS |
| III | Blazor Web App | Are all new UI surfaces Razor components? Is business logic kept out of `.razor` files? | PASS |
| IV | Basic Authentication | Do new pages carry `[Authorize]`? Do new service methods filter by `UserId`? | PASS |
| V | Database Support | For EF Core providers (SQLite/MySQL): are all schema changes EF Core migrations? Is raw SQL avoided? For DynamoDB provider (`PERSISTENCE_PROVIDER=dynamodb`): do DynamoDB repositories implement all Application interfaces? Is ownership filtering enforced? | PASS |
| VI | Manual Test Gate | Is the Manual Test Gate prompt planned at the end of each implementation task, before any `git commit`, `git merge`, or `git push`? | PASS |

### Constitution Gate Notes

- **Pre-research gate**: Passed after confirming the feature remains inside the existing four-project architecture and the spec already demands failing tests for wallet page, owner return pages, generic not-confirmed page, and any internal review page.
- **Post-design re-check**: Passed after Phase 1 artifacts were aligned to authoritative notify-only settlement, component-test expectations for affected Blazor pages, Site Administrator-only internal review, EF Core migration discipline for relational changes, and DynamoDB startup/provider parity requirements.

## Project Structure

### Documentation (this feature)

```text
/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── dynamodb-payment-storage-contract.md
│   ├── payfast-hosted-payment-contract.md
│   ├── site-admin-payment-review-contract.md
│   ├── wallet-payfast-topup-application-contract.md
│   └── wallet-payfast-topup-ui-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
/Users/adhirramjiawan/projects/payslip4all/
├── src/
│   ├── Payslip4All.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Constants/
│   ├── Payslip4All.Application/
│   │   ├── DTOs/Wallet/
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── Payslip4All.Infrastructure/
│   │   ├── HostedPayments/
│   │   ├── Persistence/Repositories/
│   │   ├── Persistence/DynamoDB/Repositories/
│   │   ├── Services/
│   │   └── Migrations/
│   └── Payslip4All.Web/
│       ├── Pages/
│       ├── Endpoints/
│       └── Components/
└── tests/
    ├── Payslip4All.Domain.Tests/
    ├── Payslip4All.Application.Tests/
    ├── Payslip4All.Infrastructure.Tests/
    └── Payslip4All.Web.Tests/Pages/
```

**Structure Decision**: Use the repository’s existing four-project Clean Architecture layout. The feature remains centered on existing wallet/payment seams: Domain entities and enums define lifecycle rules, Application owns owner filtering and settlement orchestration, Infrastructure owns PayFast protocol handling plus EF/DynamoDB persistence, and Web exposes Razor pages plus the notify endpoint.

## Phase 0 Research Outcome

- Resolved all clarifications in favor of authoritative server-side notify processing backed by PayFast confirmation.
- Standardized planning terminology on **Payment Confirmation Record** for the trustworthy settlement artifact while preserving the implementation mapping to validated `PaymentReturnEvidence`.
- Corrected lifecycle planning so `Completed` and `Cancelled` remain terminal authoritative outcomes, while `Expired`, `Abandoned`, and `NotConfirmed` remain interim non-crediting states that may be superseded only by later validated server-side evidence for the same attempt.
- Added explicit design guidance for component-test coverage on affected Blazor pages and a `SiteAdministrator`-only internal review surface with minimum non-sensitive evidence exposure.

## Phase 1 Design Output

- `research.md` refreshed to reflect late authoritative upgrades, Payment Confirmation Record terminology, Site Administrator-only internal review, and bUnit expectations for affected pages.
- `data-model.md` refreshed to align exact-match and lifecycle rules with the refined spec and to define an internal review read model.
- `contracts/` refreshed for owner UI, application behavior, PayFast integration, DynamoDB parity, and the admin review surface.
- `quickstart.md` refreshed with explicit validation scenarios for browser-return informational behavior, server-side notify authority, sandbox restrictions, unified owner-safe messaging, internal review access, and component-test expectations.

## Complexity Tracking

No constitutional exceptions are required for this feature.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | — | — |
