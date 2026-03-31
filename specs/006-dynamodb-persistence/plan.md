# Implementation Plan: AWS DynamoDB Persistence Option

**Branch**: `006-dynamodb-persistence` | **Date**: 2026-03-29 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/006-dynamodb-persistence/spec.md`

---

## Summary

Add DynamoDB as a third persistence provider selected entirely by environment variables,
without changing any `Application`-layer repository interfaces or the existing SQLite/MySQL
paths. The DynamoDB design uses a parallel Infrastructure implementation: `Program.cs`
switches on `PERSISTENCE_PROVIDER`, registers DynamoDB repositories plus a startup table
provisioner, bypasses `PayslipDbContext` and EF Core migrations when DynamoDB is active,
and keeps ownership filtering aligned with the existing relational behavior.

AWS authentication follows the approved operator contract:

1. use `AWS_ACCESS_KEY_ID` + `AWS_SECRET_ACCESS_KEY` when both are explicitly supplied,
2. use standard dummy credentials when `DYNAMODB_ENDPOINT` points at a local emulator and
   explicit credentials are absent,
3. otherwise allow the AWS SDK for .NET default credential chain / hosted identity
   (for example IAM roles, container credentials, instance metadata, or configured profiles)
   to resolve credentials for real AWS.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS)  
**Primary Dependencies**: `AWSSDK.DynamoDBv2`, Entity Framework Core 8 (retained for SQLite/MySQL), Serilog, xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing`  
**Storage**: SQLite (default), MySQL, or AWS DynamoDB selected by `PERSISTENCE_PROVIDER`; DynamoDB uses six auto-provisioned tables with optional `DYNAMODB_TABLE_PREFIX`  
**Testing**: xUnit + Moq + WebApplicationFactory integration tests; DynamoDB repository/infrastructure tests run against DynamoDB Local via fixture-backed test infrastructure  
**Target Platform**: ASP.NET Core 8 Blazor Server app running locally, in CI, or on hosted AWS infrastructure  
**Project Type**: Clean Architecture web application; feature implementation is primarily Infrastructure + Web startup wiring  
**Performance Goals**: Startup validation errors surface within 5 seconds; table provisioning completes before first request; normal repository operations preserve existing user-facing responsiveness for employee, payslip, and loan workflows  
**Constraints**: Keep `Application` repository interfaces unchanged; touch no more than Web + Infrastructure in production code; enforce `UserId` ownership filtering on every DynamoDB query; bypass `PayslipDbContext` and EF migrations when DynamoDB is active; use environment variables only for DynamoDB runtime configuration; support explicit credentials, local-emulator dummy credentials, and the AWS SDK credential chain for real AWS; no live AWS account required in CI  
**Scale/Scope**: 5 repository interfaces, 6 DynamoDB tables, 2 production projects (`Payslip4All.Infrastructure`, `Payslip4All.Web`), 2 test projects (`Payslip4All.Infrastructure.Tests`, `Payslip4All.Web.Tests`)

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify compliance with each Payslip4All constitution principle before proceeding:

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | DynamoDB provider-switching, client-factory, table-provisioning, repository, and multi-tenant ownership tests are planned to fail first before implementation tasks begin. | ✅ |
| II | Clean Architecture | Production changes remain within `Payslip4All.Infrastructure` and `Payslip4All.Web`; `Application` interfaces and `Domain` entities stay unchanged; all dependencies still point inward. | ✅ |
| III | Blazor Web App | No new UI surfaces are introduced; existing Razor components/pages remain the only UI surface and business logic stays outside `.razor` files. | ✅ |
| IV | Basic Authentication | No new auth pages are required; DynamoDB repositories and existing services continue to enforce `UserId`-scoped reads/writes for company-owner isolation. | ✅ |
| V | Database Support | DynamoDB work stays within the constitution's approved provider exception: all existing repository interfaces are implemented, EF Core startup is bypassed only for `PERSISTENCE_PROVIDER=dynamodb`, and ownership filtering is enforced on every DynamoDB path. | ✅ |
| VI | Manual Test Gate | Implementation tasks must end with a Manual Test Gate prompt before any `git commit`, `git merge`, or `git push`; this will be preserved in `tasks.md` generation. | ✅ |

**Gate Result**: PASS — no constitution violations or unresolved clarifications block Phase 0.

**Post-Design Re-check**: PASS — Phase 1 design keeps the feature within the same approved
scope: operator-facing configuration is documented, DynamoDB stays an Infrastructure concern,
and no new cross-layer dependency or UI/auth deviation is introduced.

---

## Project Structure

### Documentation (this feature)

```text
specs/006-dynamodb-persistence/
├── plan.md                           # This file
├── research.md                       # Phase 0 output
├── data-model.md                     # Phase 1 output
├── quickstart.md                     # Phase 1 output
├── contracts/
│   └── persistence-provider-contract.md
└── tasks.md                          # Phase 2 output (/speckit.tasks)
```

### Source Code

```text
src/
├── Payslip4All.Application/
│   └── Interfaces/Repositories/
│       ├── IUserRepository.cs
│       ├── ICompanyRepository.cs
│       ├── IEmployeeRepository.cs
│       ├── ILoanRepository.cs
│       └── IPayslipRepository.cs
├── Payslip4All.Domain/
│   └── Entities/
│       ├── User.cs
│       ├── Company.cs
│       ├── Employee.cs
│       ├── EmployeeLoan.cs
│       ├── Payslip.cs
│       └── PayslipLoanDeduction.cs
├── Payslip4All.Infrastructure/
│   └── Persistence/
│       ├── PayslipDbContext.cs
│       ├── Repositories/                           # Existing EF Core repositories for sqlite/mysql
│       └── DynamoDB/
│           ├── DynamoDbClientFactory.cs
│           ├── DynamoDbServiceExtensions.cs
│           ├── DynamoDbTableProvisioner.cs
│           ├── DynamoDbUnitOfWork.cs
│           └── Repositories/
│               ├── DynamoDbUserRepository.cs
│               ├── DynamoDbCompanyRepository.cs
│               ├── DynamoDbEmployeeRepository.cs
│               ├── DynamoDbLoanRepository.cs
│               └── DynamoDbPayslipRepository.cs
└── Payslip4All.Web/
    ├── Program.cs
    ├── Middleware/GlobalExceptionMiddleware.cs
    └── appsettings.json

tests/
├── Payslip4All.Infrastructure.Tests/
│   └── DynamoDB/
│       ├── DynamoDbClientFactoryTests.cs
│       ├── DynamoDbTableProvisionerTests.cs
│       ├── DynamoDbUnitOfWorkTests.cs
│       ├── DynamoDbTestFixture.cs
│       └── Repositories/
│           ├── DynamoDbUserRepositoryTests.cs
│           ├── DynamoDbCompanyRepositoryTests.cs
│           ├── DynamoDbEmployeeRepositoryTests.cs
│           ├── DynamoDbLoanRepositoryTests.cs
│           └── DynamoDbPayslipRepositoryTests.cs
└── Payslip4All.Web.Tests/
    └── DynamoDbProviderSwitchingTests.cs
```

**Structure Decision**: Use the existing Clean Architecture web-application structure.
Production changes are constrained to Infrastructure (DynamoDB implementation) and Web
(`Program.cs` provider switching and startup behavior). Application and Domain remain
stable contracts that both relational and DynamoDB providers implement.

---

## Complexity Tracking

> No constitution violations — table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| — | — | — |
