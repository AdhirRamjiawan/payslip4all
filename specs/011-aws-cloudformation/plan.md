# Implementation Plan: AWS CloudFormation Deployment

**Branch**: `011-aws-cloudformation` | **Date**: 2026-04-07 | **Spec**: `specs/011-aws-cloudformation/spec.md`  
**Input**: Feature specification from `specs/011-aws-cloudformation/spec.md`

## Summary

Deliver a CloudFormation-first AWS deployment path for Payslip4All that runs the web app on one EC2 instance behind an internet-facing ALB, publishes `payslip4all.co.za` through Route 53 and ACM, uses the existing DynamoDB provider path, and enables automated DynamoDB point-in-time recovery. The refined plan makes the operator-visible signal set explicit, keeps reusable secrets out of committed assets, and constrains the manual pre-launch workflow to five actions so success criteria remain testable.

## Technical Context

**Language/Version**: YAML infrastructure-as-code plus existing C# 12 / .NET 8 application configuration  
**Primary Dependencies**: AWS CloudFormation, EC2, Application Load Balancer, Route 53, ACM, IAM, CloudWatch-linked operational signals, existing `PERSISTENCE_PROVIDER=dynamodb` runtime path, existing DynamoDB table provisioner, new backup-protection hosted service  
**Storage**: DynamoDB with the repository's multi-table auto-provisioning model and hosted AWS point-in-time recovery enabled for regular backups  
**Testing**: Existing `dotnet test` suites plus xUnit/WebApplicationFactory coverage for CloudFormation contract checks, deployment documentation, startup health endpoint registration, DynamoDB backup protection, and focused validation of the five-step operator workflow  
**Target Platform**: AWS single-environment deployment running the ASP.NET Core Blazor Server app on Linux EC2 behind an internet-facing load balancer  
**Project Type**: Infrastructure feature for an existing Clean Architecture web application  
**Performance Goals**: Support stack launch within 45 minutes after prerequisites are ready, first page load through `payslip4all.co.za` within 10 seconds in smoke tests, DynamoDB recovery drills within 60 minutes, and expose operator-visible signals through stack outputs, ALB target health, `/health`, and deployment documentation  
**Constraints**: Must use one EC2 instance for the web app, DynamoDB for persistence, `payslip4all.co.za` as the public load-balancer address, payslip4all.co.za-derived instance identification, secure HTTPS access, health-based routing, no hardcoded reusable secrets, no more than five manual pre-launch setup actions, and the lowest practical ongoing cost; ALB and Route 53 remain expected paid services  
**Scale/Scope**: One low-traffic environment, one replaceable web instance, application-owned DynamoDB tables auto-created at startup, explicit operator signals limited to stack outputs plus documented runtime health checks, and implementation scoped to infrastructure assets, startup configuration glue, docs, and tests inside the four-project repository

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

- **Pre-research gate**: Passed because the feature adds deployment assets and startup configuration only. It does not introduce new business rules, change auth boundaries, or add projects outside the existing four-project constitution limit.
- **Post-design re-check**: Passed because the refined design keeps the DynamoDB provider logic in Infrastructure, the health endpoint in Web, and all verification within existing xUnit/WebApplicationFactory patterns. The plan also explicitly preserves TDD-first implementation and the Manual Test Gate before any git operations.

## Project Structure

### Documentation (this feature)

```text
specs/011-aws-cloudformation/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── cloudformation-deployment-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
.
├── infra/
│   └── aws/
│       └── cloudformation/
│           ├── payslip4all-web.yaml
│           ├── README.md
│           └── user-data/
│               └── bootstrap-payslip4all.sh
├── src/
│   ├── Payslip4All.Infrastructure/
│   │   └── Persistence/
│   │       └── DynamoDB/
│   │           ├── DynamoDbBackupProtectionHostedService.cs
│   │           ├── DynamoDbServiceExtensions.cs
│   │           └── DynamoDbTableProvisioner.cs
│   └── Payslip4All.Web/
│       ├── Endpoints/
│       │   └── HealthEndpoint.cs
│       └── Program.cs
└── tests/
    ├── Payslip4All.Infrastructure.Tests/
    │   └── Persistence/
    │       └── DynamoDB/
    │           └── DynamoDbBackupProtectionTests.cs
    └── Payslip4All.Web.Tests/
        ├── Infrastructure/
        │   ├── AwsCloudFormationTemplateTests.cs
        │   └── AwsDeploymentDocumentationTests.cs
        └── Startup/
            └── AwsDeploymentStartupTests.cs
```

**Structure Decision**: Keep deployable infrastructure assets under `infra/aws/cloudformation/`, keep runtime startup and backup-protection logic inside the existing Web and Infrastructure projects, and validate all deployment behavior through the existing .NET test projects. This structure makes the actual implementation surface explicit instead of treating the source tree as a partial placeholder.

## Phase 0 Research Outcome

- The lowest-cost architecture that still satisfies the spec is a single EC2 instance in a public subnet behind an internet-facing ALB, with Route 53 aliasing `payslip4all.co.za` to the ALB and payslip4all.co.za-derived instance tagging for operator identification.
- Required operator-visible signals are now fixed as: CloudFormation outputs for `ApplicationUrl`, `InstanceId`, `LoadBalancerArn`, security-group identifiers, and backup mode; ALB target health; the public `/health` endpoint; and deployment runbook guidance that ties those signals together.
- Reusable secrets must stay out of committed template values, bootstrap scripts, and docs; the supported patterns are IAM instance profiles plus external secret references such as AWS Secrets Manager.
- The pre-launch workflow is constrained to five manual actions: publish the app artifact, confirm ACM issuance, confirm Route 53 authority, gather secret references, and launch the CloudFormation stack with parameters.
- The repository's DynamoDB path still requires `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, and a table prefix, and the AWS SDK credential chain remains the preferred hosted-AWS credential strategy.
- Point-in-time recovery remains the default interpretation of "regularly backed up" because it provides continuous recoverability with less operator and cost overhead than AWS Backup across every application table.

## Phase 1 Design Output

- `research.md` captures the architecture, signal-set, secret-handling, backup, and cost decisions that resolve the analysis gaps.
- `data-model.md` defines the operator-facing deployment entities, explicit signal set, external secret handling boundary, and five-step launch workflow constraints.
- `contracts/cloudformation-deployment-contract.md` defines the CloudFormation contract including required inputs, outputs, operator-visible signals, security expectations, backup behavior, and launch-step limits.
- `quickstart.md` defines the verification flow for launch, public-domain reachability, signal inspection, DynamoDB startup, replaceable compute, and restore readiness while staying within the documented operator workflow.
- `.github/agents/copilot-instructions.md` will be refreshed by the repository helper script so future implementation or review agents inherit the tightened AWS deployment context.

## Complexity Tracking

No constitutional exceptions are required for this feature.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | — | — |
