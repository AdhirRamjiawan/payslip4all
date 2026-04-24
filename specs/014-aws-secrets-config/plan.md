# Implementation Plan: AWS Secrets-Sourced Custom Configuration

**Branch**: `014-aws-secrets-config` | **Date**: 2026-04-23 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/Users/adhirramjiawan/projects/payslip4all/specs/014-aws-secrets-config/spec.md`

## Summary

Add first-class AWS Secrets Manager support for Payslip4All's repo-owned custom configuration by inventorying the full covered setting catalog, materializing an operator-supplied Secrets Manager payload into a deployment-owned intermediate config artifact, and explicitly rebuilding startup precedence as `environment variables > AWS-secret-backed config > checked-in appsettings > code defaults`. The design keeps AWS-specific secret retrieval inside the existing CloudFormation/bootstrap workflow, preserves current non-secret deployments, documents how each covered setting maps into the secret contract, and adds safe validation/diagnostics for missing, unreadable, incomplete, or malformed secret-backed configuration.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS), Bash, and AWS CloudFormation YAML  
**Primary Dependencies**: ASP.NET Core 8 Blazor Server, existing `awscli`/`jq` bootstrap flow under `/infra/aws/cloudformation/`, Serilog, xUnit + WebApplicationFactory, existing `AWSSDK.DynamoDBv2` runtime path  
**Storage**: N/A for business persistence; configuration resolves from checked-in JSON, deployment environment variables, and an optional bootstrap-rendered AWS-secrets JSON artifact  
**Testing**: xUnit startup/integration/documentation tests in `/tests/Payslip4All.Web.Tests` plus focused infrastructure tests for config mapping/validation behavior  
**Target Platform**: Existing Linux/EC2 deployment environments plus unchanged local/dev environments for the ASP.NET Core web app  
**Project Type**: Infrastructure/runtime configuration feature for an existing Blazor Server web application  
**Performance Goals**: Preserve SC-001 through SC-006 from the spec; secret-backed startup validation must surface actionable failure diagnostics within 30 seconds and must not regress normal startup for non-secret deployments  
**Constraints**: Precedence must remain `env > AWS secrets > appsettings`; only repo-owned custom configuration explicitly consumed by Payslip4All code/startup is in scope; no secret values or AWS credential details may appear in user-visible errors or logs; existing non-secret deployments must remain valid; no new third-party NuGet dependency or constitution amendment is required for the planned design  
**Scale/Scope**: One existing web application, one hosted AWS deployment path, 21 covered configuration keys across persistence/auth/hosted-payment/runtime groups, and repository-owned changes limited to existing `infra/`, `src/Payslip4All.Web`, `src/Payslip4All.Infrastructure`, and test/doc paths

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned/written before any implementation task begins? | PASS |
| II | Clean Architecture | Does the feature touch ≤ 4 projects (Domain/Application/Infrastructure/Web)? Does each layer only depend inward? | PASS |
| III | Blazor Web App | Are all new UI surfaces Razor components? Is business logic kept out of `.razor` files? | PASS |
| IV | Basic Authentication | Do new pages carry `[Authorize]`? Do new service methods filter by `UserId`? | PASS |
| V | Database Support | For EF Core providers (SQLite/MySQL): are all schema changes EF Core migrations? Is raw SQL avoided? For DynamoDB provider (`PERSISTENCE_PROVIDER=dynamodb`): do DynamoDB repositories implement all Application interfaces? Is ownership filtering enforced? | PASS |
| VI | Manual Test Gate | Is the Manual Test Gate prompt planned at the end of each implementation task, before any `git commit`, `git merge`, or `git push`? | PASS |

### Constitution Gate Notes

- **Pre-research gate**: Passed because the feature is a configuration-source/runtime concern. No new UI, domain entities, application service contracts, or persistence schema changes are required by the plan.
- **Dependency gate**: Passed because the design deliberately reuses the existing AWS CLI + bootstrap assets already owned in `infra/aws/cloudformation/` instead of adding a new AWS Secrets Manager NuGet package that would require a constitution tech-stack amendment.
- **Post-design re-check**: Passed because the Phase 1 design keeps responsibilities within existing boundaries: bootstrap/deployment secret retrieval in `infra/`, configuration ordering/validation in `Payslip4All.Web` and `Payslip4All.Infrastructure`, and TDD-first verification in existing test projects.

## Project Structure

### Documentation (this feature)

```text
specs/014-aws-secrets-config/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── aws-secrets-configuration-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
.
├── infra/
│   └── aws/
│       └── cloudformation/
│           ├── README.md
│           ├── payslip4all-web.yaml
│           └── user-data/
│               └── bootstrap-payslip4all.sh
├── src/
│   ├── Payslip4All.Infrastructure/
│   │   └── HostedPayments/
│   │       └── PayFastHostedPaymentOptions.cs
│   └── Payslip4All.Web/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── appsettings.Development.Private.json
└── tests/
    ├── Payslip4All.Infrastructure.Tests/
    └── Payslip4All.Web.Tests/
        ├── Integration/
        ├── Startup/
        └── Infrastructure/
```

**Structure Decision**: Keep AWS secret retrieval and secret-to-artifact rendering in the existing `/infra/aws/cloudformation/` deployment path, add effective-configuration ordering and validation in the current Web/Infrastructure projects only, and validate the feature through the repository's established xUnit/WebApplicationFactory test structure instead of introducing a new project or toolchain.

## Phase 0 Research Outcome

- The covered configuration scope is the repo-owned custom settings explicitly consumed by startup or feature code: persistence-provider keys, relational connection strings, auth cookie expiry, and the `HostedPayments:PayFast` operator-facing section. Framework-owned sections such as `AllowedHosts`, baseline `Logging`, and `Serilog` stay out of scope.
- The recommended implementation path is **bootstrap-mediated secret materialization**, not direct AWS SDK reads from the app. CloudFormation/bootstrap already owns Secrets Manager access for TLS and hosted-payment secrets, so this feature should extend that path by rendering a secret-backed config artifact and letting ASP.NET Core consume it with explicit precedence.
- The effective precedence contract should be rebuilt explicitly in `Program.cs` as `appsettings -> optional rendered AWS secrets file -> environment variables`, so operators can still use env vars as emergency overrides while AWS-secret-backed values cleanly outrank checked-in appsettings.
- A flat secret JSON object using standard ASP.NET Core configuration paths (for example `HostedPayments:PayFast:MerchantId`) is the least surprising operator contract and allows grouped sections to merge naturally without inventing a second mapping language.
- Validation splits into two layers: bootstrap must fail early when a referenced secret cannot be read, while the app must fail safely or block the affected flow when secret-backed values are missing, incomplete, or invalid after source resolution. Existing lazy PayFast validation is preserved unless the implementation explicitly upgrades that group to startup validation.
- Hosted AWS deployment changes are limited to an additional app-config secret parameter/output/IAM permission path plus bootstrap logic for rendering the intermediate config artifact; TLS certificate secret handling remains a separate concern.

## Phase 1 Design Output

- `research.md` records the decisions for in-scope setting inventory, secret contract shape, precedence ordering, bootstrap responsibilities, validation strategy, deployment/IAM changes, and testing approach.
- `data-model.md` captures the operator/runtime entities for covered settings, source sets, secret mappings, rendered secret artifacts, and validation failures/state transitions.
- `contracts/aws-secrets-configuration-contract.md` defines the deployment-facing contract for secret structure, covered keys, precedence, bootstrap materialization, and failure handling.
- `quickstart.md` provides the operator-ready validation flow for secret creation, CloudFormation/bootstrap wiring, mixed-source overrides, unchanged non-secret deployments, and safe failure scenarios.
- `.github/agents/copilot-instructions.md` is refreshed by the repository helper so subsequent `speckit.tasks` / implementation agents inherit the updated AWS-secret configuration context.

## Complexity Tracking

No constitutional exceptions are required for this feature.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | — | — |
