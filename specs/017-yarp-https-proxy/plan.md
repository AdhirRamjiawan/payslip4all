# Implementation Plan: YARP HTTPS Reverse Proxy Migration

**Branch**: `017-yarp-https-proxy` | **Date**: 2026-04-30 | **Spec**: `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/spec.md`  
**Input**: Feature specification from `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/spec.md`

## Summary

Refresh the repository-owned YARP public-edge plan so `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web` remains the only public HTTPS gateway host, uses the constitution-approved `Yarp.ReverseProxy` 2.2.x dependency, keeps the hosted backend on `http://127.0.0.1:8080`, treats `https://payslip4all.co.za/health` as the single readiness smoke-check, rejects wrong hosts with `421`, returns the exact generic `503 Service temporarily unavailable.` failure body within 10 seconds, and fails closed on certificate activation with the exact operator-visible error required by the refreshed spec.

## Technical Context

**Language/Version**: C# 12 on .NET 8 SDK/runtime (`8.0.0`, roll-forward latest feature)  
**Primary Dependencies**: ASP.NET Core 8 Blazor Server, `Yarp.ReverseProxy` 2.2.0 in `Payslip4All.Web`, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, Serilog.AspNetCore 10.x  
**Storage**: SQLite/MySQL via EF Core, or DynamoDB when `PERSISTENCE_PROVIDER=dynamodb`; no new storage schema for this feature  
**Testing**: xUnit, Moq, bUnit, `Microsoft.AspNetCore.Mvc.Testing` integration tests, startup/documentation contract tests in `Payslip4All.Web.Tests`  
**Target Platform**: Linux-hosted ASP.NET Core 8 web app and YARP gateway on the existing AWS single-EC2 deployment path  
**Project Type**: Clean Architecture Blazor Server web application with infrastructure-focused gateway mode inside the existing web host  
**Performance Goals**: `/health` readiness succeeds within 5 seconds; HTTP→HTTPS redirect happens in one navigation step; upstream failures surface `503 Service Unavailable` with generic body within 10 seconds  
**Constraints**: Must stay within the existing four projects; YARP-first scope only; hosted default upstream remains `http://127.0.0.1:8080`; wrong-host requests return `421`; certificate activation fails closed with the exact spec error; no insecure fallback traffic; no nginx-defined new behavior  
**Scale/Scope**: Single production public host (`payslip4all.co.za`), one hosted AWS EC2 instance plus Elastic IP, two runtime services from the same web binary, repository-owned deployment and operator guidance only

## Constitution Check

*GATE: Passed before Phase 0 research. Re-checked after Phase 1 design and still passing.*

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned before implementation tasks begin for startup, proxy behavior, deployment docs, and contract enforcement? | ✅ Planned in Phase 2 handoff and reinforced in research/quickstart |
| II | Clean Architecture | Does the feature stay within the existing four projects and keep the public edge inside `Payslip4All.Web`/repo-owned infra? | ✅ Yes; no fifth project required |
| III | Blazor Web App | Are new behaviors infrastructure-focused, with no new UI surface or business logic added to `.razor` files? | ✅ Yes; gateway mode stays in `Program.cs` and runtime wiring |
| IV | Basic Authentication | Does the feature avoid changing auth page/service scope while preserving secure public-host behavior for cookies and redirects? | ✅ Yes; no new auth scope, only forwarded context preservation |
| V | Database Support | Does the feature avoid schema changes/raw SQL and keep the hosted DynamoDB path untouched except for deployment/runtime hosting? | ✅ Yes; no persistence contract changes |
| VI | Manual Test Gate | Is the Manual Test Gate still required at the end of each future implementation task before git operations? | ✅ Yes; required for all implementation follow-up |

**Governed dependency gate**: PASS. The refreshed spec explicitly cites Payslip4All Constitution v1.4.0 for governed public-edge usage, and `.specify/memory/constitution.md` approves `Yarp.ReverseProxy` 2.2.x inside `Payslip4All.Web` under **Technology Stack & Constraints → Public HTTPS Edge** with branch/review requirements under **Development Workflow & Quality Gates → Branching & Review**.

### Post-design Constitution Re-check

- **Result**: PASS
- **Why**: The refreshed design artifacts keep YARP inside `Payslip4All.Web`, preserve the four-project boundary, introduce no new UI/auth/persistence deviations, and keep all implementation work TDD-first with manual test gate enforcement.

## Project Structure

### Documentation (this feature)

```text
/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── yarp-gateway-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
/Users/adhirramjiawan/projects/payslip4all/src/
├── Payslip4All.Domain/
├── Payslip4All.Application/
├── Payslip4All.Infrastructure/
└── Payslip4All.Web/
    ├── Program.cs
    ├── ReverseProxyModeOptions.cs
    └── Endpoints/
        └── HealthEndpoint.cs

/Users/adhirramjiawan/projects/payslip4all/tests/
├── Payslip4All.Domain.Tests/
├── Payslip4All.Application.Tests/
├── Payslip4All.Infrastructure.Tests/
└── Payslip4All.Web.Tests/
    ├── Integration/
    ├── Infrastructure/
    └── Startup/

/Users/adhirramjiawan/projects/payslip4all/infra/
├── aws/
│   └── cloudformation/
└── yarp/
```

**Structure Decision**: Use the existing four-project Clean Architecture solution. Runtime gateway logic stays in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web`, deployment artifacts stay under `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation` and `/Users/adhirramjiawan/projects/payslip4all/infra/yarp`, and verification coverage stays in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests`.

## Phase 0 Research Outcome

All clarifications from the refreshed spec are resolved in `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/research.md`, including:

- constitution v1.4.0 governance for YARP in `Payslip4All.Web`
- `/health` as the single readiness smoke-check
- FR-006 forwarding guarantees for redirects, Blazor/SignalR, and form/navigation preservation
- fail-closed certificate activation with the exact operator-visible error
- YARP-first hosted AWS alignment with default upstream `http://127.0.0.1:8080`

## Phase 1 Design Output

- `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/data-model.md`
- `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md`
- `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md`

These artifacts now align with the refreshed requirement split across FR-001, FR-009, and FR-011 while preserving the already-agreed YARP-first behavior, hosted default upstream, wrong-host `421`, exact `503` body, 10-second failure bound, and fail-closed certificate activation.

## Complexity Tracking

No constitution violations or justified exceptions are required for this plan refresh.
