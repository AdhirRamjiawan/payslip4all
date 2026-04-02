# Wallet Card Top-Up UI Contract

## CompanyOwner wallet page

- **Route**: `/portal/wallet`
- **Authorization**: `CompanyOwner`
- **Displays**:
  - Current wallet balance
  - Top-up initiation form
  - Recent wallet activity
  - Matched top-up attempt history with statuses, requested amount, confirmed amount where available, and wallet-credit linkage
- **Interactions**:
  - Submit a positive hosted card top-up amount
  - Receive validation feedback for invalid amounts
  - Leave Payslip4All by redirecting to the hosted payment page
  - View only `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, and `Unverified` matched attempts
  - See clearly whether a wallet credit was created for each matched attempt

## Generic hosted return entry

- **Route**: `/portal/wallet/top-ups/return`
- **Authorization**: `CompanyOwner`
- **Purpose**: Entry point for all hosted-payment returns before the application knows whether the evidence matches exactly one known attempt.
- **Displays / Behavior**:
  - Processes incoming Payment Return Evidence on first load
  - Resolves to either:
    - a matched-attempt result flow, or
    - a generic unmatched not-confirmed flow
  - Never exposes guessed attempt identifiers during intake
  - Never reveals whether a guessed attempt exists

## Matched attempt result page

- **Route**: `/portal/wallet/top-ups/{attemptId:guid}/return`
- **Authorization**: `CompanyOwner`
- **Displays**:
  - Current authoritative matched status
  - Requested amount
  - Confirmed charged amount when `Completed`
  - Wallet credited / not credited message
  - Link back to wallet history
- **Interactions**:
  - Show stable results when revisited or refreshed
  - Show explicit messaging for `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, and `Unverified`
  - Show `Abandoned` until trustworthy late final evidence supersedes it
  - Never represent unmatched data as if it were this attempt

## Generic unmatched not-confirmed page

- **Route**: `/portal/wallet/top-ups/return/not-confirmed`
- **Authorization**: `CompanyOwner`
- **Displays**:
  - Generic message that the payment could not be matched to a confirmed top-up result
  - No guessed attempt identifier
  - No guessed-attempt existence hint
  - No owner identity details
  - No wallet details
  - No wallet-credit confirmation
  - Link back to `/portal/wallet`
- **Interactions**:
  - Support refresh/revisit without revealing additional correlation details
  - Confirm only that the payment was not matched to a confirmed top-up result

## Non-production simulator surface

- **Route**: `/hosted-payments/fake`
- **Authorization**: non-production only as configured by environment
- **Purpose**: Development / test / demo hosted-payment simulator
- **Rules**:
  - Must remain clearly separate from the production customer journey
  - Must not accept or imply real in-app production card entry
  - Must support deterministic test cases for exact-threshold abandonment, trustworthy late evidence, low-confidence late evidence, unmatched returns, and conflicting evidence after accepted final outcome

## Security and UX rules

- Razor components must never collect, display, or log card details.
- All business decisions come from injected services, not inline Razor logic.
- All wallet top-up pages must load owner-scoped data only.
- Async operations must show loading and error states consistent with existing wallet UX.
- The unmatched generic result must reveal no guessed attempt ID, guessed-attempt existence, owner identity, wallet details, or wallet-credit confirmation.
- No UI surface may present `Failed` as a distinct card top-up status or outcome for this feature.
