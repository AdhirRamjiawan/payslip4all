# Hosted Payment Provider Contract

## Purpose

This contract defines the provider-facing seam used by the Application layer to start a hosted wallet top-up and to translate raw provider/browser inputs into provider-neutral Payment Return Evidence. Providers do **not** choose the authoritative business outcome; they only supply validated evidence for the application-owned normalization policy.

## StartHostedTopUpAsync

- **Input**:
  - Internal matched-attempt identifier
  - Requested amount in ZAR
  - Owner-safe correlation metadata
  - Generic return URL (`/portal/wallet/top-ups/return`)
  - Optional cancel/back URL
- **Output**:
  - Hosted redirect URL
  - Provider session reference
  - Return correlation token
  - Optional hosted-page expiry metadata
- **Rules**:
  - The redirect target must be an externally hosted payment page.
  - The provider contract must never return, request, or persist raw cardholder data through Payslip4All.
  - Any provider-supplied expiry metadata is informational only; the authoritative abandonment rule remains `CreatedAt + 1 hour`.
  - The provider may include opaque references needed for later exact matching, but may not expose provider-specific business outcomes directly to Razor components.

## ReadPaymentReturnEvidenceAsync

- **Input**:
  - Raw browser/provider return payload
  - Optional request metadata required for validation
  - Matched-attempt context only after correlation data is checked by the application workflow
- **Output**:
  - Provider-neutral Payment Return Evidence envelope containing:
    - `ProviderKey`
    - `ProviderSessionReference` (nullable)
    - `ProviderPaymentReference` (nullable)
    - `ReturnCorrelationToken` (nullable)
    - `ClaimedOutcome` (`Completed`, `Cancelled`, `Expired`, `Unknown`, or null)
    - `TrustLevel` (`Trustworthy`, `LowConfidence`, `Untrusted`)
    - `ConfirmedChargedAmount` when trustworthy success exists
    - `EvidenceOccurredAt` when available
    - display-safe message seed
    - safe payload snapshot fields suitable for auditing
- **Rules**:
  - Browser payloads are untrusted until validation is complete.
  - The provider contract must expose correlation values cleanly enough for Application to decide exact match vs unmatched **before** final outcome classification.
  - The provider contract must never emit a business-only `Failed` state.
  - The provider contract must be able to represent unresolved evidence, low-confidence claimed final evidence, trustworthy final evidence, replayed evidence, and conflicting evidence.
  - Sensitive provider payload details and exceptions must not leak into owner-facing pages.

## Application-owned precedence boundary

- The provider contract is intentionally lower-level than the business policy.
- Providers may indicate trust level and claimed outcome, but only Application may decide:
  - whether evidence is unmatched,
  - the final Normalized outcome,
  - whether `Abandoned` applies at the exact threshold,
  - whether late trustworthy evidence supersedes `Abandoned`,
  - whether later conflicting evidence is audit-only because an authoritative final outcome already exists.

## Non-production simulator requirement

- A fake/simulator provider implementation is required for local development, automated testing, and demonstrations.
- The simulator must support at least:
  - trustworthy completed return,
  - trustworthy completed return with different confirmed amount,
  - trustworthy cancelled return,
  - trustworthy expired return,
  - unresolved pending return,
  - low-confidence claimed final return,
  - unmatched / uncorrelatable return,
  - no-return timeout path that reaches exact-threshold abandonment,
  - trustworthy late final evidence after abandonment,
  - conflicting evidence after an accepted authoritative final outcome,
  - replay of already-processed evidence.
- The simulator must remain clearly separate from the production card-entry journey.
- A future concrete provider may replace or supplement the simulator without changing the application-owned normalization policy or the terminology in this feature.
