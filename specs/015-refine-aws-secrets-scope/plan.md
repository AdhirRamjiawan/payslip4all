# Implementation Plan: AWS Secrets Scope Refinement

**Branch**: `015-refine-aws-secrets-scope` | **Date**: 2026-04-24 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/Users/adhirramjiawan/projects/payslip4all/specs/015-refine-aws-secrets-scope/spec.md`

## Summary

Refine the feature 014 AWS Secrets-backed custom configuration contract so only eligible repo-owned app settings remain in the rendered app-config artifact, while DynamoDB runtime keys and AWS credential keys are rejected as non-compliant. The design preserves the existing precedence `environment variables > rendered AWS-secret config > checked-in appsettings > code defaults`, adds explicit supported-versus-excluded scope validation at startup, and updates operator documentation so existing feature 014 deployments can migrate without losing AWS Secrets support for authentication and hosted-payment settings.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS), Bash, and AWS CloudFormation YAML  
**Primary Dependencies**: ASP.NET Core 8 Blazor Server, existing `awscli`/`jq` bootstrap flow under `/infra/aws/cloudformation/`, Serilog, xUnit + WebApplicationFactory, existing `AWSSDK.DynamoDBv2` runtime path  
**Storage**: N/A for business persistence; configuration resolves from checked-in JSON, an optional rendered AWS-secrets JSON artifact, and environment variables  
**Testing**: xUnit startup/integration/documentation tests in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests` plus focused infrastructure/unit tests for scope-catalog validation helpers  
**Target Platform**: Existing Linux/EC2 hosted AWS deployments plus unchanged local/dev environments for the ASP.NET Core web app  
**Project Type**: Infrastructure/runtime configuration refinement for an existing Blazor Server web application  
**Performance Goals**: Reject non-compliant AWS-secret artifacts before the deployment is considered healthy, preserve eligible-setting precedence behavior, and avoid regressions for deployments that already use only supported secret-backed keys  
**Constraints**: Preserve `env > AWS secrets > appsettings > code defaults`; explicitly exclude `DYNAMODB_*`, `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY` from the AWS app-config artifact; keep diagnostics secret-safe; do not add new NuGet packages or amend the constitution; keep the work within existing `infra/`, `src/Payslip4All.Web`, `src/Payslip4All.Infrastructure`, docs, and test projects  
**Scale/Scope**: One existing web app, one hosted AWS deployment path, 15 eligible secret-backed keys, 6 excluded keys, and repository-owned changes limited to existing `infra/`, `src/`, `tests/`, `README.md`, and feature-spec artifact paths

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

### Constitution Gate Notes

- **Pre-research gate**: Passed because the feature narrows an existing configuration contract and startup-validation path. No new UI surface, domain rule, application service contract, persistence schema, or constitution amendment is required.
- **Boundary gate**: Passed because DynamoDB runtime configuration and AWS credential handling remain infrastructure/runtime concerns, while the refined supported catalog stays within existing Web/Infrastructure configuration responsibilities.
- **TDD gate**: Passed because implementation will begin with failing startup/documentation tests for supported-scope resolution, excluded-scope rejection, precedence preservation, and secret-safe diagnostics.
- **Post-design re-check**: Passed because Phase 1 design keeps secret retrieval and deployment guidance in existing `infra/` documentation, keeps app-config scope validation in existing Web/Infrastructure code, and plans tests only inside existing test projects.

## Project Structure

### Documentation (this feature)

```text
specs/015-refine-aws-secrets-scope/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── aws-secrets-refined-scope-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
.
├── README.md
├── infra/
│   └── aws/
│       └── cloudformation/
│           ├── README.md
│           ├── payslip4all-web.yaml
│           └── user-data/
│               └── bootstrap-payslip4all.sh
├── src/
│   ├── Payslip4All.Infrastructure/
│   │   ├── Configuration/
│   │   │   ├── AwsSecretsConfigurationDefaults.cs
│   │   │   └── Payslip4AllCustomConfigurationKeys.cs
│   │   ├── HostedPayments/
│   │   │   └── PayFastHostedPaymentOptions.cs
│   │   └── Persistence/
│   │       └── DynamoDB/
│   │           └── DynamoDbConfigurationOptions.cs
│   └── Payslip4All.Web/
│       └── Program.cs
└── tests/
    ├── Payslip4All.Infrastructure.Tests/
    └── Payslip4All.Web.Tests/
        ├── Infrastructure/
        └── Startup/
```

**Structure Decision**: Keep AWS secret retrieval and rendered-artifact handling in the existing hosted AWS deployment path and documentation, implement refined scope validation in the existing Web/Infrastructure projects only, and verify the change through the repository’s established xUnit/WebApplicationFactory test structure without adding a new project or toolchain.

## Phase 0 Research Outcome

- The prior feature 014 catalog covered 21 keys. The constitution conflict is resolved by retaining only the 15 repo-owned app-setting keys (`PERSISTENCE_PROVIDER`, relational connection strings, `Auth:Cookie:ExpireDays`, and `HostedPayments:PayFast:*`) and excluding the 6 DynamoDB runtime/AWS credential keys from the AWS app-config artifact.
- The excluded catalog is: `DYNAMODB_REGION`, `DYNAMODB_ENDPOINT`, `DYNAMODB_TABLE_PREFIX`, `DYNAMODB_ENABLE_PITR`, `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY`.
- The safest implementation point is **before** the rendered secret values are merged into configuration: parse the JSON artifact, detect excluded keys, and fail fast with sanitized operator guidance rather than silently dropping keys or allowing non-compliant startup.
- The precedence contract remains unchanged for eligible keys: `environment variables > rendered AWS-secret config > checked-in appsettings > code defaults`.
- Migration guidance must explicitly help operators move excluded DynamoDB runtime and credential inputs out of the AWS app-config secret while leaving eligible settings in place.
- No new runtime dependencies are required; the existing hosted AWS bootstrap flow, `Program.cs` startup composition, and current test/doc patterns are sufficient.

## Phase 1 Design Output

- `research.md` documents the refined supported-versus-excluded scope, validation strategy, precedence preservation, migration guidance, and TDD approach.
- `data-model.md` captures the supported-setting catalog, excluded-setting catalog, scope-validation result, resolution stack, and migration guidance entities/state transitions.
- `contracts/aws-secrets-refined-scope-contract.md` defines the deployment-facing contract for the refined AWS app-config payload, excluded catalogs, validation behavior, precedence, and migration expectations.
- `quickstart.md` provides an operator-ready flow for auditing feature 014 payloads, removing excluded keys, supplying DynamoDB/AWS runtime inputs outside the app-config secret, and validating precedence plus failure behavior.
- `.github/agents/copilot-instructions.md` is refreshed by the repository helper so later `speckit.tasks` / implementation agents inherit the refined AWS Secrets scope context.

## Complexity Tracking

No constitutional exceptions are required for this feature.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | — | — |
