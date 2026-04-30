# Phase 0 Research: YARP HTTPS Reverse Proxy Migration

All refreshed-spec clarifications for `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/plan.md` are resolved below. Historical nginx artifacts remain reference-only and do not govern this branch.

## Decision 1: Keep the governed public edge inside `Payslip4All.Web`

**Decision**: Continue hosting the public HTTPS edge as a configuration-gated YARP mode inside `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`, using the constitution-approved `Yarp.ReverseProxy` 2.2.x dependency in `Payslip4All.Web`.

**Rationale**: Payslip4All Constitution v1.4.0 explicitly approves `Yarp.ReverseProxy` 2.2.x for the repository-owned public HTTPS edge inside `Payslip4All.Web` and also forbids adding a fifth project for runtime edge concerns. This keeps the feature within the four-project Clean Architecture boundary while matching the current code and tests.

**Alternatives considered**:
- A fifth standalone gateway project — rejected because it violates the constitution without an amendment.
- Reusing nginx as the defining public edge — rejected because the refreshed spec and current repo scope are explicitly YARP-first.

## Decision 2: Keep the hosted topology as two services on one host

**Decision**: Preserve the hosted AWS topology where the backend app listens on `http://127.0.0.1:8080` and a separate gateway process from the same binary listens publicly on `http://0.0.0.0:80;https://0.0.0.0:443`.

**Rationale**: This keeps the backend internal-only, matches the repository-owned CloudFormation/bootstrap assets, and preserves the already-agreed hosted default upstream contract required by the refreshed spec.

**Alternatives considered**:
- Expose the backend app directly on `443` — rejected because the spec requires a separate public-edge responsibility.
- Split gateway and app onto different hosts — rejected because the existing hosted path is intentionally single-host and simpler to operate.

## Decision 3: Treat `https://payslip4all.co.za/health` as the single readiness smoke-check

**Decision**: Use `/health` as the only readiness smoke-check contract for the public edge, and validate it over the production HTTPS host.

**Rationale**: The refreshed spec now makes `/health` the singular readiness verification endpoint, with a 5-second success expectation and repeatable smoke-test target. The existing health endpoint and deployment docs already align with that contract.

**Alternatives considered**:
- Separate gateway-specific readiness and application readiness paths — rejected because the refreshed spec intentionally standardizes on one smoke-check contract.
- Use root-page availability as the readiness signal — rejected because it is less deterministic and mixes app rendering concerns into edge readiness.

## Decision 4: Preserve forwarded public request context for redirects, Blazor/SignalR, and forms

**Decision**: Continue forwarding original host, HTTPS scheme, and client context so backend-generated redirects stay on `https://payslip4all.co.za`, Blazor/SignalR interactions remain bound to the public host, and form submissions plus follow-up navigation do not switch to the internal upstream address.

**Rationale**: FR-006 is now explicit about redirect preservation, Blazor/SignalR behavior, and form/navigation continuity. Existing forwarded-header configuration and integration tests already prove the underlying YARP behavior that the refreshed plan must preserve.

**Alternatives considered**:
- Rely on default backend URL generation without forwarded host/scheme preservation — rejected because redirects and real-time flows could leak or switch to the internal upstream address.
- Scope forwarding only to basic page loads — rejected because the refreshed spec now explicitly includes interactive Blazor and form scenarios.

## Decision 5: Enforce the public-edge contract with explicit wrong-host and upstream-failure behavior

**Decision**: Requests for hosts other than `payslip4all.co.za` return `421 Misdirected Request`, and upstream 502/503/504 conditions collapse to the exact generic user-facing body `Service temporarily unavailable.` with an HTTP `503 Service Unavailable` status inside the 10-second failure bound.

**Rationale**: The refreshed spec preserves the same fail-safe behavior but more clearly separates repository contract scope (FR-001), operator documentation scope (FR-009), and hosted deployment scope (FR-011). Existing Program.cs behavior and integration tests already support the exact `421` and `503` contract.

**Alternatives considered**:
- Allow multiple hosts or wildcard fallback — rejected because this feature governs one production hostname only.
- Surface detailed upstream diagnostics to end users — rejected because the spec requires generic failure behavior with no internal endpoint disclosure.

## Decision 6: Fail closed on certificate activation with the exact operator-visible error

**Decision**: Certificate activation remains an external-input bootstrap/runtime prerequisite. If certificate material is missing, invalid, or unreadable, activation must stop before serving traffic and surface the exact error: `HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.`

**Rationale**: The refreshed spec now elevates the exact operator-visible activation error into the governed feature contract. The repository already stages certificate material externally and converts it into a Kestrel-consumable `.pfx`, which fits a fail-closed startup model.

**Alternatives considered**:
- Serve HTTP-only traffic until the certificate is fixed — rejected because the feature must fail closed and avoid insecure fallback traffic.
- Emit a generic startup failure without the exact operator-visible message — rejected because the refreshed spec now requires that exact activation error string.

## Decision 7: Keep deployment guidance repository-owned and operator-discoverable

**Decision**: Continue documenting the YARP edge through `/Users/adhirramjiawan/projects/payslip4all/infra/yarp/README.md`, `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/README.md`, `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/payslip4all-web.yaml`, and `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`.

**Rationale**: The refreshed spec separates repository contract, hosted deployment defaults, and operator guidance, but still requires them to stay discoverable and repository-owned. The current docs already provide the core paths and inputs that the quickstart can reference.

**Alternatives considered**:
- Leave deployment knowledge implicit in code/tests only — rejected because operators need a reviewable deployment contract.
- Move edge guidance to external wiki/runbook only — rejected because the spec requires repository ownership and versioned discoverability.

## Decision 8: Drive implementation tasks from existing web test seams

**Decision**: Future implementation work should begin with failing tests in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests` that cover startup gating, proxy request behavior, hosted deployment artifacts, and operator-facing documentation/contract alignment.

**Rationale**: This directly satisfies Constitution Principle I and avoids inventing a new test project for an already-established test surface. The repo already has integration, infrastructure, and startup tests around the YARP path.

**Alternatives considered**:
- Depend on manual smoke testing first — rejected because the constitution requires TDD before implementation.
- Add a separate dedicated YARP test project — rejected because the existing web test project already covers the needed seams.
