# Research: Document Payment Gateway Setup

## Decision 1: Scope the guide to the existing PayFast hosted wallet top-up flow

- **Decision**: Document only the currently supported PayFast-hosted wallet top-up flow and identify it explicitly as the payment gateway setup covered by the README update.
- **Rationale**: The current runtime wiring, application settings, and provider implementation are all centered on PayFast. Limiting the guide to the active flow avoids documentation drift and prevents maintainers from assuming unsupported gateway options exist.
- **Alternatives considered**:
  - **Document payment gateways generically**: Rejected because the repository currently exposes one concrete gateway setup surface.
  - **Document both fake and real providers equally**: Rejected because the feature is specifically about setting up the real payment gateway.

## Decision 2: Document the concrete PayFast configuration inputs already used by the app

- **Decision**: The README setup guide will cover `HostedPayments:PayFast:ProviderKey`, `UseSandbox`, `MerchantId`, `MerchantKey`, `Passphrase`, and `PublicNotifyUrl`.
- **Rationale**: These are the configuration values present in the app settings and runtime option binding for the real hosted-payment provider. Listing the exact values and explaining them is the clearest way to satisfy the setup-focused requirements.
- **Alternatives considered**:
  - **Describe configuration vaguely without naming keys**: Rejected because maintainers would still need to inspect source code.
  - **Document only the secret values**: Rejected because environment mode and callback settings are equally required for a working setup.

## Decision 3: Keep secrets out of committed examples and direct readers to private or deployment-specific configuration

- **Decision**: The guide will instruct maintainers to place merchant secrets in private local settings or deployment-time configuration, not in committed shared files.
- **Rationale**: The repository already separates default settings from private local overrides, and the spec explicitly forbids committing live secrets. The README must reinforce that operational boundary.
- **Alternatives considered**:
  - **Show full live credential examples in README**: Rejected because it would normalize unsafe handling of payment secrets.
  - **Skip secret-handling guidance entirely**: Rejected because that leaves a critical setup and security decision undocumented.

## Decision 4: Make the public HTTPS notify callback a first-class setup requirement

- **Decision**: The guide will document that `PublicNotifyUrl` must be a public HTTPS address pointing to the Payslip4All notify route and must not use `localhost` or a PayFast-owned URL.
- **Rationale**: The current startup validation rejects invalid notify URLs before hosted checkout starts, so this is a setup prerequisite rather than an optional operational note. Treating it as first-class documentation reduces the most likely misconfiguration path.
- **Alternatives considered**:
  - **Treat callback configuration as advanced troubleshooting only**: Rejected because it blocks trustworthy payment confirmation from the start.
  - **Document the browser return URL instead of the server-side notify path**: Rejected because browser returns are not the authoritative confirmation path.

## Decision 5: Explain sandbox and live mode as operational choices, not code changes

- **Decision**: The guide will present sandbox versus live mode as a configuration choice driven by `UseSandbox` and merchant-account context, with clear guidance on when each mode is appropriate.
- **Rationale**: The current option model already switches both the hosted-payment and validation endpoints based on `UseSandbox`. Documentation should map that operational choice clearly without forcing readers into source-level reasoning.
- **Alternatives considered**:
  - **Assume all setups use sandbox**: Rejected because the README must also support production readiness.
  - **Assume readers will infer environment behavior from the merchant account alone**: Rejected because the mode switch is explicit in application configuration.

## Decision 6: Verification guidance must start with hosted checkout and end with trustworthy callback handling

- **Decision**: The guide will include a short verification path that confirms the app can start a hosted wallet top-up, redirect to PayFast, and rely on the documented notify callback for trustworthy confirmation.
- **Rationale**: Setup documentation is incomplete if it stops at configuration entry. Maintainers need a concrete way to tell whether the gateway is working or whether callback prerequisites are still broken.
- **Alternatives considered**:
  - **Stop the guide after listing configuration values**: Rejected because it does not let maintainers prove the setup works.
  - **Use browser return as the success signal in the guide**: Rejected because the runtime treats that path as informational only.

## Decision 7: Troubleshooting guidance should focus on the most likely setup failures

- **Decision**: The README should call out missing credentials, invalid or private notify URLs, and wrong sandbox/live mode as the primary setup failures to diagnose first.
- **Rationale**: These are the failure conditions directly visible from the current configuration surface and provider validation behavior. Focusing on them keeps the guide concise while materially helping first-time setup.
- **Alternatives considered**:
  - **Document every possible payment failure**: Rejected because the README would become noisy and implementation-heavy.
  - **Avoid troubleshooting to keep the guide short**: Rejected because the spec requires one-pass diagnosability for common failures.
