# Implementation Plan: Customer Wallet Credits

**Branch**: `007-wallet-credits` | **Date**: 2026-04-01 | **Spec**: [/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/spec.md](/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/spec.md)  
**Input**: Feature specification from `/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/spec.md`

## Summary

Add a wallet feature that gives each company owner a rand-denominated credit balance, debits that balance whenever a payslip is generated successfully, and exposes wallet balance, wallet activity, configurable per-payslip pricing, and public pricing/wallet messaging on the home page. The implementation will extend the existing Clean Architecture seams by adding wallet and pricing entities, application services, EF Core plus DynamoDB repository support, protected CompanyOwner and SiteAdministrator Razor pages, and a public landing-page surface for wallet pricing visibility.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core 8, AWSSDK.DynamoDBv2, xUnit, Moq, bUnit, QuestPDF, Serilog  
**Storage**: SQLite/MySQL via EF Core migrations by default, DynamoDB via `PERSISTENCE_PROVIDER=dynamodb` exception path  
**Testing**: xUnit, Moq, bUnit, repository integration tests, DynamoDB LocalStack integration tests  
**Target Platform**: ASP.NET Core Blazor Server web application running on .NET 8  
**Project Type**: Four-project Clean Architecture web application (`Domain`, `Application`, `Infrastructure`, `Web`)  
**Performance Goals**: Wallet balance and current payslip price should load within the existing interactive page load budget; successful top-ups and payslip debits should be visible to the same owner immediately after request completion; pricing checks must happen inside the payslip generation request path before success is returned; the public landing page should show wallet pricing information without exposing private data  
**Constraints**: Preserve ownership filtering by authenticated `UserId`; support EF Core and DynamoDB providers without changing Application interfaces; keep business rules out of `.razor` files; use EF Core migrations for relational providers; maintain auditable wallet history; expose only public pricing and wallet messaging on the landing page; follow manual test gate before any future git operation  
**Scale/Scope**: One wallet per company owner account, site-wide configurable payslip price, owner-visible balance plus recent activity, pricing enforcement on every successful payslip generation, and signed-out visitor visibility of the wallet model on the home page

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify compliance with each Payslip4All constitution principle before proceeding:

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned/written before any implementation task begins? | PASS |
| II | Clean Architecture | Does the feature touch ≤ 4 projects (Domain/Application/Infrastructure/Web)? Does each layer only depend inward? | PASS |
| III | Blazor Web App | Are all new UI surfaces Razor components? Is business logic kept out of `.razor` files? | PASS |
| IV | Basic Authentication | Do new pages carry `[Authorize]`? Do new service methods filter by `UserId`? | PASS |
| V | Database Support | For EF Core providers (SQLite/MySQL): are all schema changes EF Core migrations? Is raw SQL avoided? For DynamoDB provider (`PERSISTENCE_PROVIDER=dynamodb`): do DynamoDB repositories implement all Application interfaces? Is ownership filtering enforced? | PASS |
| VI | Manual Test Gate | Is the Manual Test Gate prompt planned at the end of each implementation task, before any `git commit`, `git merge`, or `git push`? | PASS |

Post-design re-check: PASS. The design keeps all work inside the four existing projects, preserves inward dependencies, adds EF migrations for relational storage, plans DynamoDB repository parity, and keeps UI concerns inside authorized Razor pages plus a public landing-page surface with service-driven pricing reads and no private wallet data.

## Project Structure

### Documentation (this feature)

```text
/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── wallet-application-contract.md
│   └── wallet-ui-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
/Users/adhirramjiawan/projects/payslip4all/src/
├── Payslip4All.Domain/
│   ├── Entities/
│   └── Enums/
├── Payslip4All.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   │   └── Repositories/
│   └── Services/
├── Payslip4All.Infrastructure/
│   ├── Migrations/
│   ├── Persistence/
│   │   ├── DynamoDB/
│   │   │   └── Repositories/
│   │   └── Repositories/
│   └── Services/
└── Payslip4All.Web/
    ├── Pages/
    │   ├── Admin/
    │   ├── Payslips/
    │   ├── Portal/
    │   └── Home/
    └── Shared/

/Users/adhirramjiawan/projects/payslip4all/tests/
├── Payslip4All.Domain.Tests/
├── Payslip4All.Application.Tests/
├── Payslip4All.Infrastructure.Tests/
└── Payslip4All.Web.Tests/
```

**Structure Decision**: Use the existing four-project Clean Architecture layout. Wallet balance rules and ledger entities belong in `Domain`; service contracts, DTOs, and orchestration belong in `Application`; EF Core repositories, DynamoDB repositories, and migrations belong in `Infrastructure`; CompanyOwner pages, SiteAdministrator pages, and the public landing-page wallet messaging belong in `Web`.
