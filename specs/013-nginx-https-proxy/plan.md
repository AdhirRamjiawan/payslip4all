# Implementation Plan: nginx HTTPS Reverse Proxy

**Branch**: `013-nginx-https-proxy` | **Date**: 2026-04-19 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/013-nginx-https-proxy/spec.md`

## Summary

Add a source-controlled nginx gateway definition under `infra/` that serves `payslip4all.co.za` over HTTPS, redirects HTTP to HTTPS, and reverse-proxies traffic to the existing ASP.NET Core Blazor Server app. The implementation plan assumes nginx terminates TLS on the host, forwards scheme/host/client-IP and Blazor WebSocket headers to Kestrel, restricts serving to the intended production hostname, and integrates with the repository's existing AWS CloudFormation/bootstrap/docs/test patterns instead of introducing a separate deployment stack.

## Technical Context

**Language/Version**: nginx configuration, Bash bootstrap scripting, and existing C# 12 / .NET 8 application runtime  
**Primary Dependencies**: nginx, ASP.NET Core 8 reverse-proxy support, AWS CloudFormation/EC2/Secrets Manager deployment assets, Serilog request logging, xUnit + WebApplicationFactory  
**Storage**: N/A (no application persistence or schema changes)  
**Testing**: xUnit infrastructure/documentation tests, WebApplicationFactory reverse-proxy behavior tests, and deployment smoke checks for HTTPS redirect, `/health`, host restriction, and upstream failure handling  
**Target Platform**: Linux server or EC2 host running nginx in front of the existing Kestrel-hosted Payslip4All app  
**Project Type**: Infrastructure feature for an existing Blazor Server web application  
**Performance Goals**: Preserve SC-002 through SC-005 from the spec: single-step HTTP→HTTPS redirects, first complete HTTPS page load within 5 seconds for at least 95% of smoke-test requests, and unavailable responses within 10 seconds when the upstream app is down  
**Constraints**: `payslip4all.co.za` remains the fixed public host; TLS certificate/key material must stay external to source control; nginx must preserve forwarded scheme/host/client IP plus WebSocket upgrade headers for Blazor Server; the app must no longer be directly internet-accessible on its internal port; implementation must remain consistent with `infra/aws/cloudformation/` deployment guidance and existing .NET test conventions  
**Scale/Scope**: One production host/domain, one nginx gateway config, one internal app endpoint (planned as `127.0.0.1:8080`), repository-owned infra/docs/tests only

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

- **Pre-research gate**: Passed because the feature is an infrastructure/runtime-edge change. No new domain rules, service interfaces, auth boundaries, or schema changes are introduced.
- **Post-design re-check**: Passed because the design keeps changes inside existing repository boundaries: `infra/` deployment assets, `Payslip4All.Web` startup/runtime expectations if needed, and `Payslip4All.Web.Tests` validation. TDD-first implementation and the Manual Test Gate remain explicit implementation requirements.

## Project Structure

### Documentation (this feature)

```text
specs/013-nginx-https-proxy/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── nginx-gateway-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
.
├── infra/
│   ├── nginx/
│   │   ├── payslip4all.conf
│   │   └── README.md
│   └── aws/
│       └── cloudformation/
│           ├── payslip4all-web.yaml
│           ├── README.md
│           └── user-data/
│               └── bootstrap-payslip4all.sh
├── src/
│   └── Payslip4All.Web/
│       └── Program.cs
└── tests/
    └── Payslip4All.Web.Tests/
        ├── Infrastructure/
        │   ├── AwsDeploymentDocumentationTests.cs
        │   └── NginxGatewayConfigTests.cs
        ├── Integration/
        │   └── ReverseProxyForwardingTests.cs
        └── Startup/
            └── AwsDeploymentStartupTests.cs
```

**Structure Decision**: Keep the new gateway assets under `infra/nginx/` as repository-owned deployment configuration, update the existing AWS CloudFormation/bootstrap/docs flow only where required to activate nginx on the current EC2-hosted path, and validate behavior through the existing `Payslip4All.Web.Tests` xUnit/WebApplicationFactory patterns.

## Phase 0 Research Outcome

- nginx should terminate TLS and own ports `80`/`443`, while the ASP.NET Core app moves behind it on a local-only upstream endpoint (`127.0.0.1:8080`).
- Certificate and key material must remain externally supplied at deployment time; the plan assumes file paths under `/etc/nginx/certs/` populated from an operator-managed source such as AWS Secrets Manager or equivalent host provisioning.
- The reverse proxy must forward `Host`, `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`, and WebSocket upgrade headers so the existing Blazor Server app preserves secure redirects, cookie security, and interactive SignalR behavior.
- nginx should enforce HTTP→HTTPS redirect before proxying, restrict serving to `payslip4all.co.za`, and provide a generic unavailable response when the upstream app is unreachable without exposing internal endpoint details.
- Existing deployment touchpoints are `infra/aws/cloudformation/payslip4all-web.yaml`, `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`, `infra/aws/cloudformation/README.md`, and repository-owned .NET tests; a separate platform stack is not required.

## Phase 1 Design Output

- `research.md` records the implementation decisions for TLS termination, upstream porting, forwarded headers, Blazor WebSocket support, host restriction, failure handling, and AWS deployment integration.
- `data-model.md` captures the operator-facing entities for the nginx gateway, certificate binding, upstream endpoint, header policy, prerequisites, and verification lifecycle.
- `contracts/nginx-gateway-contract.md` defines the public gateway contract: fixed host, HTTPS behavior, redirect policy, upstream proxying, forwarded-header requirements, host filtering, and failure-response rules.
- `quickstart.md` defines the operator-ready validation flow for prerequisites, gateway activation, HTTPS redirect verification, proxied app behavior, wrong-host rejection, upstream-failure behavior, and certificate rotation/reload checks.
- `.github/agents/copilot-instructions.md` will be refreshed by the repository helper so future implementation agents inherit the nginx/AWS reverse-proxy planning context.

## Complexity Tracking

No constitutional exceptions are required for this feature.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | — | — |
