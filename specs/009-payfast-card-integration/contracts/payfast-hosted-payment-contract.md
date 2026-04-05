# PayFast Hosted Payment Contract

## Purpose

Define the Infrastructure-layer contract between Payslip4All and PayFast for starting hosted wallet top-ups and validating server-side PayFast evidence without exposing sensitive card data to Payslip4All.

## Outbound hosted checkout request

- **Target**: PayFast hosted/custom integration checkout
- **Required fields**:
  - `merchant_id`
  - `merchant_key`
  - `amount`
  - `item_name`
  - `m_payment_id`
  - `return_url`
  - `cancel_url`
  - `notify_url` = public HTTPS callback endpoint owned by Payslip4All (planned route: `POST /api/payments/payfast/notify`)
  - explicit card-only payment restriction (`payment_method=cc`)
  - `signature`
- **Optional fields**:
  - approved pass-through values such as `custom_str1..5` when needed for safe correlation
- **Rules**:
  - Card-only must be explicit on every outbound request.
  - `amount` must already satisfy Payslip4All’s R50-R1000 rule before request generation.
  - `m_payment_id` must correlate to exactly one business-owned pending attempt.
  - Checkout signing must use PayFast-compatible form encoding over trimmed non-empty non-signature fields, with passphrase inclusion only when configured.
  - Sandbox/live host selection must be configuration-driven.
  - Sandbox acceptance is allowed only when the environment is explicitly configured for sandbox validation; production settlement remains live-only.
  - If merchant configuration is invalid or hosted checkout cannot be created, Payslip4All must fail safely and non-creditingly.
  - Payslip4All must never collect, store, display, or log PAN, CVV, expiry, or equivalent cardholder data.

## Browser return payload

- **Source**: PayFast redirect to `return_url` or `cancel_url`
- **Expected usage**:
  - return the owner to Payslip4All,
  - capture low-trust evidence for audit/correlation,
  - redirect either to an owner-scoped matched result or the generic not-confirmed page.
- **Rules**:
  - Browser return data is informational only.
  - Browser return may not authorize wallet credit, even when it appears successful.
  - Unknown, foreign-owner, duplicate-finalized, or otherwise unsafe browser returns must resolve to the same owner-safe `Top-up not confirmed` outcome family.
  - Browser return handling must not expose raw gateway diagnostics.

## PayFast notify / authoritative confirmation payload

- **Source**: PayFast POST to `notify_url`
- **Expected fields**:
  - `m_payment_id`
  - `pf_payment_id`
  - `payment_status`
  - `amount_gross`
  - `signature`
  - any configured correlation/pass-through values
- **Validation rules**:
  - Treat the server-side notification path as the only settlement-authority candidate.
  - Validate signature locally before any trust decision.
  - Build the notify verification parameter string with the same PayFast-compatible normalization rules used for signature verification.
  - Validate source, amount, payment method, environment mode, and PayFast step-4 server confirmation before treating a payload as trustworthy.
  - Successful settlement may proceed only when:
    - `m_payment_id` maps to exactly one eligible attempt in `Pending`, `Expired`, `Abandoned`, or `NotConfirmed`,
    - the attempt belongs to the same owner and currency scope being evaluated,
    - confirmed amount is within R50-R1000,
    - the evidence proves card-only payment,
    - the evidence is from the permitted live path for production settlement.
  - Callback delivery failure, callback validation failure, or merchant misconfiguration must leave the attempt non-crediting and owner-safe.
- **Settlement rules**:
  - Successful confirmation may create wallet credit only when all validation checks pass.
  - Successful confirmation must update the attempt result, normalization decision, wallet credit, wallet balance, and history source data inside the same unit of work so balance and history can meet SC-002.
  - Cancelled, expired, not-confirmed, unmatched, non-card, non-live, invalid, duplicate-finalized, or late-finalized notifications must never create wallet credit.
  - Duplicate notifications must be idempotent.
  - `Abandoned` is not a direct callback outcome; it is recorded later by Payslip4All reconciliation after an `Expired` deadline.

## Mapping into Payslip4All evidence

- **Payment Confirmation Record** = validated `PaymentReturnEvidence` from `SourceChannel = PayFastNotify` with successful trust checks and PayFast confirmation
- `ProviderKey` = `payfast`
- `SourceChannel` = `BrowserReturn` for redirects, `PayFastNotify` for authoritative server confirmations
- `ProviderPaymentReference` = `pf_payment_id`
- `MerchantPaymentReference` = `m_payment_id`
- `ClaimedOutcome` maps to `Completed | Cancelled | Expired | Unknown`
- `TrustLevel` maps to `Trustworthy | LowConfidence | Untrusted`
- `SafePayloadSnapshot` contains only non-sensitive evidence needed for audit and reconciliation
- Unsafe owner-visible cases map to the single generic outcome message `Top-up not confirmed`

## Persistence parity requirements

- The same safe evidence fields, correlation rules, history freshness rules, and exactly-once settlement behavior must be preserved in relational and DynamoDB persistence paths.
- Any new relational fields required by this contract must be added via EF Core migration and mirrored in DynamoDB repository serialization.

## Security boundary

- PayFast-specific signing, payload parsing, remote confirmation, and callback request handling stay in Infrastructure/Web startup boundaries.
- Application owns owner filtering, exact-match rules, normalized status assignment, accepted final outcomes, reconciliation transitions, and wallet settlement.
