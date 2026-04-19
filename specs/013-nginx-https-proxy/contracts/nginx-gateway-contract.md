# Contract: nginx Gateway for Payslip4All

## Purpose

Define the operator-facing contract for the nginx gateway assets that expose Payslip4All at `payslip4all.co.za` over HTTPS and reverse-proxy traffic to the existing ASP.NET Core app.

## Audience

- Operators deploying or updating the public gateway
- Maintainers reviewing nginx or AWS deployment changes

## Scope

This contract covers the repository-owned nginx config, its activation prerequisites, the required proxy behavior, and the observable verification conditions. It does not define new business-domain behavior inside Payslip4All.

## Required Inputs

The gateway assets MUST accept or document operator-supplied values for:

| Input | Purpose |
|-------|---------|
| TLS certificate reference | Supplies the public certificate chain for `payslip4all.co.za` |
| TLS private key reference | Supplies the HTTPS private key without committing it to source control |
| Upstream application address | Identifies the internal Payslip4All endpoint nginx should proxy to |
| Host bootstrap path | Describes how the config and certificate files are placed on the target host |
| DNS prerequisite | Ensures `payslip4all.co.za` resolves to the nginx host |

The domain name itself is fixed by this feature and MUST remain `payslip4all.co.za`.

## Required Gateway Behavior

The gateway assets MUST:

1. serve `https://payslip4all.co.za` over TLS,
2. redirect `http://payslip4all.co.za` to the equivalent HTTPS URL,
3. reverse-proxy public requests to the configured internal Payslip4All endpoint,
4. preserve the original request context needed by the app by forwarding `Host`, `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host`,
5. forward WebSocket/upgrade headers required for Blazor Server interactivity,
6. expose the app's `/health` behavior through the public gateway,
7. return an unavailable non-success response when the upstream app is down,
8. avoid exposing the internal app endpoint in user-facing failure responses,
9. restrict production serving to `payslip4all.co.za` rather than treating arbitrary hostnames as valid production entry points.

## Security Rules

The implementation MUST:

- keep reusable certificate or secret material out of version-controlled plaintext files,
- make nginx the intended public edge while the app stays on an internal-only endpoint,
- preserve HTTPS as the normal user path,
- avoid weakening the existing ASP.NET Core forwarded-header behavior,
- support certificate replacement without redefining the overall host-routing behavior.

The implementation MUST NOT:

- commit reusable certificate private keys into the repo,
- expose the internal app port as the public production endpoint,
- silently serve the production app for unrelated hostnames.

## Deployment & Documentation Rules

The implementation MUST:

- store the primary gateway artifact under `infra/`,
- document operator prerequisites and activation inputs,
- stay consistent with the repository's existing `infra/aws/cloudformation/` deployment guidance when hosted on AWS,
- define smoke-verifiable checks for redirect behavior, HTTPS access, `/health`, host restriction, and upstream failure handling.

## Acceptance Conditions

This contract is satisfied when:

1. an operator can locate the nginx gateway artifact in `infra/`,
2. the operator can identify the required certificate and upstream activation inputs quickly,
3. end users reach Payslip4All successfully at `https://payslip4all.co.za`,
4. insecure requests redirect to HTTPS in one step,
5. interactive Blazor behavior still works through the proxy,
6. unrelated hostnames are not treated as the production entry point,
7. upstream outages return a user-safe unavailable response without leaking internal endpoint details.
