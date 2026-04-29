# Implementation Plan: Local DynamoDB Development Environment

**Branch**: `016-localstack-dynamodb-dockerfile` | **Date**: 2026-04-24 | **Spec**: `/Users/adhirramjiawan/projects/payslip4all/specs/016-localstack-dynamodb-dockerfile/spec.md`
**Input**: Feature specification from `/Users/adhirramjiawan/projects/payslip4all/specs/016-localstack-dynamodb-dockerfile/spec.md`

## Summary

Align the repository's LocalStack-based DynamoDB development environment with the current DynamoDB provider path by refining the source-controlled Docker image contract, preserving the existing `http://localhost:8000` integration-test workflow, and tightening documentation and verification so contributors can build, run, and troubleshoot the local emulator without using live AWS resources.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS), Dockerfile, Markdown documentation  
**Primary Dependencies**: ASP.NET Core Blazor Server, `AWSSDK.DynamoDBv2`, LocalStack Docker image, xUnit, `Microsoft.AspNetCore.Mvc.Testing`  
**Storage**: DynamoDB via LocalStack emulator for local development; no production or schema-model changes  
**Testing**: xUnit integration and infrastructure configuration tests in `tests/Payslip4All.Web.Tests`  
**Target Platform**: Docker-capable developer workstation running the app and DynamoDB emulator locally on `http://localhost:8000`  
**Project Type**: Web application with supporting infrastructure artifact and developer documentation  
**Performance Goals**: Contributors can start and verify the local emulator within the success criteria defined by the spec; startup remains predictable for integration tests and local smoke testing  
**Constraints**: No live AWS dependency, preserve existing `PERSISTENCE_PROVIDER=dynamodb` startup contract, keep the setup source-controlled and reproducible, avoid changes to business logic or persistence abstractions  
**Scale/Scope**: One LocalStack Docker artifact, related documentation, and verification coverage across existing web/integration test surfaces

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

**Pre-design assessment**:
- TDD remains mandatory even though the feature centers on infrastructure and docs; implementation must start with failing tests around the LocalStack artifact and any updated runtime expectations.
- The feature stays within existing repository boundaries: `infra/localstack`, `README.md`, and `tests/Payslip4All.Web.Tests`, with possible limited updates to existing DynamoDB startup wiring only if tests prove necessary.
- No constitution deviations or exceptions are required.

**Post-design re-check**:
- Phase 0 and Phase 1 artifacts keep the feature inside the existing DynamoDB provider path and avoid new architectural or authentication surface area.
- No unresolved clarifications remain, and no constitution gate is blocked by the current design.

## Project Structure

### Documentation (this feature)

```text
specs/016-localstack-dynamodb-dockerfile/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── local-dynamodb-runtime.md
└── tasks.md
```

### Source Code (repository root)

```text
infra/
└── localstack/
    ├── Dockerfile
    └── README.md

src/
├── Payslip4All.Infrastructure/
│   └── Persistence/
│       └── DynamoDB/
│           ├── DynamoDbClientFactory.cs
│           ├── DynamoDbConfigurationOptions.cs
│           ├── DynamoDbServiceExtensions.cs
│           └── DynamoDbTableProvisioner.cs
└── Payslip4All.Web/
    └── Program.cs

tests/
└── Payslip4All.Web.Tests/
    ├── Infrastructure/
    │   └── LocalStackIntegrationConfigTests.cs
    ├── Integration/
    │   └── DynamoDbLocalStartupTests.cs
    └── Startup/
        └── DynamoDbConfigurationValidationTests.cs

README.md
```

**Structure Decision**: Keep the feature scoped to the existing infrastructure asset, web startup contract, and web test suite. No new project, package, or deployment surface is needed; implementation should refine `infra/localstack` and the existing DynamoDB startup/test workflow already used by the repository.

## Complexity Tracking

No constitution violations or justified exceptions are required for this feature.
