# Implementation Plan: Document Payment Gateway Setup

**Branch**: `010-payment-gateway-readme` | **Date**: 2026-04-05 | **Spec**: `specs/010-payment-gateway-readme/spec.md`  
**Input**: Feature specification from `specs/010-payment-gateway-readme/spec.md`

## Summary

Add a README setup guide for the existing PayFast-hosted wallet top-up flow so developers and operators can configure sandbox or live payments, supply credentials safely, expose a valid public notify callback, and verify the payment journey without reading source code. The plan keeps the feature documentation-only while grounding it in the current runtime configuration and owner-safe payment confirmation flow.

## Technical Context

**Language/Version**: Markdown documentation in a .NET 8 / ASP.NET Core 8 repository  
**Primary Dependencies**: `README.md`, PayFast hosted-payment configuration under `HostedPayments:PayFast`, existing wallet top-up flow, appsettings/environment-variable configuration conventions  
**Storage**: N/A for new feature data; documentation describes existing configuration sources such as `appsettings*.json` and environment variables  
**Testing**: Specification-driven manual walkthroughs from `quickstart.md`; if implementation expands beyond documentation, use existing `dotnet test` suites for regression safety  
**Target Platform**: Developers and operators configuring the ASP.NET Core web app locally or in a deployed environment with a public HTTPS callback endpoint  
**Project Type**: Documentation change for a Clean Architecture web application  
**Performance Goals**: Enable a maintainer to reach the first hosted wallet-top-up screen within 15 minutes using the guide alone, include every required setup input and callback prerequisite before first verification, and make the most common setup failures diagnosable in one pass without undocumented troubleshooting steps  
**Constraints**: No committed live secrets; README must stay aligned with the existing hosted PayFast card-funded wallet top-up flow; notify callback must be public HTTPS and not localhost; browser return remains informational while server-side notify drives trustworthy confirmation; no unsupported payment methods may be documented  
**Scale/Scope**: Planning covers README content plus supporting design artifacts in `specs/010-payment-gateway-readme/`; implementation is expected to touch documentation and possibly agent context only, while referencing existing Infrastructure/Web payment files as sources of truth

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

- **Pre-research gate**: Passed because this feature is documentation-only and does not propose new runtime layers, UI surfaces, authentication changes, or persistence changes.
- **Post-design re-check**: Passed because the design artifacts keep the feature scoped to README guidance while explicitly preserving the current Blazor, authentication, and payment-confirmation boundaries that implementation must not contradict.

## Project Structure

### Documentation (this feature)

```text
specs/010-payment-gateway-readme/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── readme-payment-gateway-setup-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
.
├── README.md
├── src/
│   ├── Payslip4All.Infrastructure/
│   │   └── HostedPayments/
│   │       ├── PayFastHostedPaymentOptions.cs
│   │       ├── PayFastHostedPaymentProvider.cs
│   │       └── PayFastSignatureVerifier.cs
│   └── Payslip4All.Web/
│       ├── Endpoints/
│       │   └── PayFastNotifyEndpoint.cs
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── appsettings.Development.Private.json
└── specs/
    └── 010-payment-gateway-readme/
```

**Structure Decision**: Keep the feature inside the existing documentation and configuration surface. `README.md` is the primary deliverable, while the PayFast Infrastructure/Web files above remain the source-of-truth references the documentation must match.

## Phase 0 Research Outcome

- Confirmed that PayFast is the currently supported hosted payment gateway and that the relevant runtime configuration surface is `HostedPayments:PayFast`.
- Confirmed that the required documented inputs are `ProviderKey`, `UseSandbox`, `MerchantId`, `MerchantKey`, `Passphrase`, and `PublicNotifyUrl`, with the existing application configuration split across shared and private/deployment-specific settings.
- Confirmed that payment confirmation depends on a public HTTPS `notify_url` pointing to `/api/payments/payfast/notify`, and that `localhost` or PayFast-owned hosts are invalid callback targets.
- Confirmed that browser returns are informational only; README verification must center on hosted checkout start plus server-side callback reachability and safe troubleshooting.
- Confirmed that the guide must preserve the existing hosted PayFast card-funded wallet top-up flow and should not document unsupported payment methods.

## Phase 1 Design Output

- `research.md` captures the PayFast setup decisions, secret-handling approach, callback rules, and verification/troubleshooting guidance that the README must reflect.
- `data-model.md` defines the conceptual documentation entities for the setup guide: configuration values, callback requirements, verification scenarios, and troubleshooting cases.
- `contracts/readme-payment-gateway-setup-contract.md` defines the required README sections, content obligations, and acceptance conditions for the payment gateway setup guide.
- `quickstart.md` defines the manual validation path for confirming the README implementation is sufficient for first-time gateway setup and verification.

## Complexity Tracking

No constitutional exceptions are required for this feature.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | — | — |
