# Wallet Card Top-Up Application Contract

## Start hosted top-up

- **Actor**: `CompanyOwner`
- **Input**:
  - Authenticated `UserId`
  - Positive rand `RequestedAmount`
  - Generic hosted-return URL information supplied by the Web layer
  - Optional cancel/back URL
- **Output**:
  - `WalletTopUpAttemptId`
  - Hosted `RedirectUrl`
  - Initial attempt status (`Pending`)
  - `AbandonAfterUtc` set to exactly 1 hour after initiation
- **Rules**:
  - Reject zero or negative amounts.
  - Persist the pending top-up attempt before returning the redirect URL.
  - Do not accept or emit card number, CVV, or expiry data.
  - Store only opaque provider references and correlation values required for later evidence handling.
  - All hosted returns must enter a generic inbound route first; no provider return may depend on an attempt-specific URL.

## Handle generic hosted return

- **Actor**: `CompanyOwner` returning from the hosted payment page
- **Input**:
  - Authenticated `UserId`
  - Browser/provider return payload treated as untrusted input
  - Request metadata needed for provider-safe validation
- **Output**:
  - Either a matched-attempt resolution:
    - `ResultType = MatchedAttempt`
    - `WalletTopUpAttemptId`
    - authoritative matched status
    - confirmed charged amount when `Completed`
    - wallet credit linkage when present
    - owner-safe display message
  - Or a generic unmatched resolution:
    - `ResultType = Unmatched`
    - generic not-confirmed message
    - internal `UnmatchedPaymentReturnRecordId`
- **Rules**:
  - Must persist the incoming Payment Return Evidence before finalizing the result.
  - Must apply one deterministic precedence policy across providers.
  - Must decide exact match vs unmatched **before** evaluating claimed payment outcome.
  - If correlation yields zero matches, multiple matches, or missing/invalid/conflicting match data, create an Unmatched Payment Return Record, do not change any attempt status, do not credit any wallet, and return the generic unmatched result.
  - If exactly one attempt is matched and trustworthy evidence confirms success, accept authoritative `Completed`, settle the wallet exactly once using the confirmed charged amount, and persist wallet-credit linkage in the audit trail.
  - If exactly one attempt is matched and trustworthy evidence confirms cancellation or expiry, persist authoritative `Cancelled` or `Expired` and do not credit the wallet.
  - If exactly one attempt is matched, no trustworthy final evidence exists, and `currentTime < AbandonAfterUtc`, return `Pending` or `Unverified` according to whether a final outcome is being claimed/implied without enough trust.
  - If `currentTime >= AbandonAfterUtc` and no trustworthy final outcome has been accepted, `Abandoned` becomes the authoritative baseline.
  - If trustworthy matched final evidence arrives at or after the abandonment threshold, it may supersede `Abandoned`.
  - Low-confidence, unresolved, or unmatched late evidence must not reopen an `Abandoned` attempt.
  - Once a trustworthy final outcome has been accepted, later conflicting evidence must be audited but must not change the authoritative outcome or wallet effect.
  - No distinct `Failed` status or result may be emitted by this contract.
  - Deny access when a matched attempt does not belong to the authenticated owner.

## Get matched attempt result

- **Actor**: `CompanyOwner`
- **Input**:
  - Authenticated `UserId`
  - `WalletTopUpAttemptId`
- **Output**:
  - Authoritative matched status
  - Requested amount
  - Confirmed charged amount when available
  - wallet credited / not credited indicator
  - linked wallet activity reference when present
  - owner-safe display message
- **Rules**:
  - Return only attempts owned by the authenticated user.
  - This contract is for matched attempts only.
  - Unmatched returns must never be projected into this contract as a synthetic attempt.
  - Refresh/revisit must be idempotent and must not create a second wallet credit.

## Resolve abandoned pending or unverified attempts

- **Actor**: System reconciliation process or service-driven timeout handler
- **Input**:
  - Current UTC timestamp
  - Optional batch/filter information for unresolved attempts
- **Output**:
  - Count or list of attempts transitioned to `Abandoned`
- **Rules**:
  - Only `Pending` or `Unverified` attempts with no accepted trustworthy final outcome may become `Abandoned`.
  - The transition point is exact: `currentTime >= AbandonAfterUtc`.
  - No wallet credit or balance change may occur during abandonment processing.
  - The abandonment decision must create a persistent Normalized outcome audit record.
  - Re-running abandonment processing must be idempotent.

## List owner top-up history

- **Actor**: `CompanyOwner`
- **Input**:
  - Authenticated `UserId`
- **Output**:
  - Owner-scoped matched attempts ordered newest first
  - Requested amount
  - Confirmed charged amount when available
  - authoritative status
  - timestamps
  - linked wallet activity reference for completed attempts
- **Rules**:
  - Return only the authenticated owner’s matched attempts.
  - `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, and `Unverified` remain visible even when no wallet credit exists.
  - Unmatched Payment Return Records are not attempt history items.
  - History must remain reconcilable with wallet activity and audit decisions.
