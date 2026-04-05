# Wallet PayFast Top-Up Application Contract

## Start PayFast top-up

- **Actor**: Authenticated `CompanyOwner`
- **Input**:
  - `UserId`
  - `RequestedAmount`
  - absolute `return_url`
  - absolute `cancel_url`
- **Output**:
  - `WalletTopUpAttemptId`
  - hosted redirect/form target
  - `Status = Pending`
  - `HostedPageDeadline`
  - `NextReconciliationDueAt`
- **Rules**:
  - Only the authenticated owner may start a top-up for their own wallet.
  - `RequestedAmount` must be between R50 and R1000 inclusive.
  - The pending attempt must persist before redirecting to PayFast.
  - The provider request must enforce card-only checkout and include a publicly reachable `notify_url`.
  - Gateway misconfiguration or initiation failure must not create false success or wallet credit.

## Process browser return

- **Actor**: Returning authenticated `CompanyOwner`
- **Input**:
  - `UserId`
  - untrusted browser return payload
- **Output**:
  - matched-attempt redirect/result, or
  - generic owner-safe not-confirmed result
- **Rules**:
  - Browser return is informational only.
  - Browser return may record evidence and influence owner-safe messaging, but may not authorize wallet credit.
  - A matched result may be shown only after exact owner-safe correlation succeeds.
  - Unmatched, foreign-owner, duplicate-finalized, and otherwise unsafe-to-disclose cases must route to the same `Top-up not confirmed` result.
  - Browser-return processing must never expose raw gateway diagnostics.

## Process PayFast authoritative confirmation

- **Actor**: PayFast server callback
- **Input**:
  - server-side notification payload
  - request metadata required for validation
- **Output**:
  - persisted evidence
  - normalization decision
  - authoritative attempt update
  - wallet credit only when trustworthy success is accepted
- **Rules**:
  - Validate local signature first, then validate source, amount, payment method, environment mode, and PayFast step-4 server confirmation before treating the payload as trustworthy.
  - A valid success or cancellation match exists only when `m_payment_id` maps to exactly one eligible attempt in `Pending`, `Expired`, `Abandoned`, or `NotConfirmed` for the same owner and currency and the confirmed amount is within R50-R1000 when success is claimed.
  - Use the confirmed charged amount for wallet credit.
  - Commit the accepted attempt update, normalization decision, wallet activity, wallet balance, and history-source fields in one unit of work so owner balance and history become visible within 1 minute of callback receipt.
  - Reject browser-only, unmatched, foreign-owner, duplicate-finalized, already-completed, already-cancelled, non-credit-card, non-live, callback-invalid, callback-missing, and unsafe late-finalized evidence from settlement.
  - Replays must be idempotent.
  - Absence of trustworthy callback evidence keeps the flow non-crediting and owner-safe.

## Reconcile timed-out or unresolved attempts

- **Actor**: Application reconciliation workflow
- **Trigger mechanism**:
  - scheduled sweep running every 1 minute, and
  - read-through reconciliation invoked by owner history/result reads when `NextReconciliationDueAt <= now`
- **Input**:
  - pending or not-confirmed attempt
  - current time
  - any follow-up evidence/status-check result
- **Output**:
  - updated authoritative status
  - reconciliation decision audit
- **Rules**:
  - When the hosted PayFast session deadline passes without trustworthy completion or cancellation, the first due reconciliation records `Expired`.
  - After `Expired`, a later due reconciliation with no trustworthy completion or cancellation records `Abandoned`.
  - `Expired` and `Abandoned` are both non-crediting.
  - Later validated server-side evidence may still change `Expired`, `Abandoned`, or `NotConfirmed` to `Completed` or `Cancelled` for the same exact-matched attempt.
  - Later callbacks after already authoritative `Completed` or `Cancelled` outcomes are persisted for audit only and do not reopen settlement.

## Get owner top-up result

- **Actor**: Authenticated `CompanyOwner`
- **Input**:
  - `UserId`
  - `WalletTopUpAttemptId`
- **Output**:
  - owner-safe result DTO containing requested amount, confirmed amount when available, current authoritative status, wallet-credit indicator, and display message
- **Rules**:
  - Return only attempts owned by the authenticated user.
  - If the attempt is overdue for reconciliation, invoke read-through reconciliation before returning the result.
  - Browser-return success without authoritative server confirmation must not render as settled success.
  - Refreshing the result page must never create another credit.
  - Unsafe/disallowed evidence must not change an accepted final outcome.
  - Owner-visible unsafe cases must use the same generic `Top-up not confirmed` family.

## List owner top-up history

- **Actor**: Authenticated `CompanyOwner`
- **Input**:
  - `UserId`
- **Output**:
  - newest-first list of the owner’s top-up attempts
- **Rules**:
  - Show only the owner’s attempts.
  - If any listed attempt is due for reconciliation, invoke read-through reconciliation before returning the list.
  - Visible statuses for this feature are `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, and `NotConfirmed`.
  - Do not surface unmatched return records as synthetic attempts.
  - Successful authoritative callbacks must become visible in both balance and history within 1 minute for SC-002 measurement.

## Get Site Administrator payment review data

- **Actor**: Authenticated `SiteAdministrator`
- **Input**:
  - optional attempt identifier
  - optional Payment Confirmation Record identifier
  - optional date range / outcome filters
- **Output**:
  - privacy-minimized review DTO containing only non-sensitive evidence and audit linkage
- **Rules**:
  - Non-admin callers must be denied.
  - The review DTO must expose only safe audit fields.
  - The review path is read-only and must not mutate attempts, evidence, or wallet balances.
  - Conflicting later evidence and unmatched return records must remain reviewable without changing the accepted authoritative outcome.

## Cross-provider rules

- Relational and DynamoDB implementations must apply the same owner filtering, evidence persistence, reconciliation behavior, and exactly-once settlement semantics.
- A repository implementation is non-compliant if it settles from browser-return-only evidence, misses SC-002 history visibility, or stores prohibited sensitive payment data.
- Relational and DynamoDB implementations must expose the same safe admin-review data needed for internal reconciliation.
