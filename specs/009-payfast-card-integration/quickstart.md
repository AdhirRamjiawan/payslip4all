# Quickstart: PayFast Card Integration

## Purpose

Validate the refreshed PayFast design, constitutional DynamoDB requirements, internal review constraints, and release evidence plan before implementation tasks begin.

## Prerequisites

- Payslip4All running locally from `/Users/adhirramjiawan/projects/payslip4all`
- Authenticated `CompanyOwner` and `SiteAdministrator` test accounts
- Configured PayFast merchant credentials for the intended environment (sandbox and/or live as appropriate)
- Publicly reachable HTTPS `notify_url`
- Ability to inspect persisted top-up attempts, Payment Confirmation Records, safe payment evidence, normalization decisions, unmatched return records, wallet activities, and startup logs
- Ability to run the same scenarios against relational persistence and `PERSISTENCE_PROVIDER=dynamodb`

## Scenario 1: Amount bounds are enforced before redirect

1. Sign in as a `CompanyOwner`.
2. Open `/portal/wallet`.
3. Try starting top-ups with `49.00`, `50.00`, `1000.00`, and `1001.00`.
4. Confirm:
   - R50 and R1000 are accepted,
   - out-of-range values show user-friendly validation,
   - no invalid attempt is persisted for out-of-range values.

## Scenario 2: Checkout start is card-only

1. Enter a valid amount such as `100.00`.
2. Start the hosted top-up.
3. Confirm:
   - a pending attempt is stored before redirect,
   - the outbound PayFast request includes explicit card-only restriction,
   - the outbound request signature matches PayFast-compatible field normalization and encoding rules,
   - Payslip4All does not collect or store card details,
   - the hosted payment journey does not offer non-card methods.

## Scenario 3: Browser return is informational only

1. Complete checkout until the browser returns to Payslip4All.
2. Prevent, delay, or invalidate the authoritative PayFast callback.
3. Confirm:
   - no wallet credit is created,
   - the browser flow does not mark authoritative success,
   - the owner sees only an owner-safe pending/not-confirmed result,
   - later valid callback evidence is still required before credit may occur.

## Scenario 4: Authoritative callback updates both balance and history within 1 minute

1. Start a fresh top-up for `100.00`.
2. Complete the hosted PayFast card flow.
3. Capture the timestamp when the valid server-side callback reaches Payslip4All.
4. Confirm within 1 minute:
   - signature, source, amount, owner, currency, card-only, and merchant-context validation checks succeed,
   - PayFast step-4 server confirmation succeeds after local signature verification,
   - the attempt becomes `Completed`,
   - wallet balance increases exactly once by the confirmed charged amount,
   - the same completed attempt is visible in owner history with the confirmed amount and completed status,
   - audit links connect attempt, Payment Confirmation Record, normalization decision, and wallet credit.
5. Record this as SC-002 evidence, not only as balance evidence, and record the sample in `checklists/sc-004-uat-sign-off-pack.md`.

## Scenario 5: Exact-match failures never settle

1. Replay or simulate each failure condition:
   - unknown `m_payment_id`,
   - `m_payment_id` matching multiple attempts,
   - foreign-owner attempt,
   - wrong currency,
   - confirmed amount outside R50-R1000.
2. Confirm for each:
   - no wallet credit is created,
   - the owner-visible outcome remains generic and safe,
   - safe evidence and decisions are persisted for audit.

## Scenario 6: Callback delivery failure or merchant misconfiguration stays non-crediting

1. Simulate each operational failure:
   - unreachable or invalid `notify_url`,
   - callback validation failure,
   - merchant credentials or merchant account status preventing hosted checkout start,
   - sandbox/live environment mismatch for the intended run mode.
2. Confirm:
   - no wallet credit is created,
   - no false success state is shown,
   - owners receive only user-friendly safe messaging,
   - operational evidence is sufficient for reconciliation without exposing raw gateway diagnostics.

## Scenario 7: Sandbox acceptance is explicit and production settlement remains live-only

1. Run the feature once with PayFast sandbox mode enabled and once with production/live mode enabled.
2. Confirm:
   - sandbox mode can start the hosted flow and validate callback evidence for non-production verification,
   - the correct PayFast process and validation hosts are selected for each mode,
   - production settlement rejects non-live evidence even if the payload otherwise looks successful.

## Scenario 8: DynamoDB startup provisions or verifies payment tables and logs the result

1. Run with `PERSISTENCE_PROVIDER=dynamodb`.
2. Start the app against an empty or partially provisioned DynamoDB environment.
3. Confirm startup verifies or creates and logs each required table:
   - `{prefix}_wallet_topup_attempts`
   - `{prefix}_payment_return_evidences`
   - `{prefix}_outcome_normalization_decisions`
   - `{prefix}_unmatched_payment_return_records`
   - dependent wallet tables used by settlement/history
4. Restart against an already provisioned environment.
5. Confirm logs distinguish created tables from already-confirmed tables.

## Scenario 9: Expired and abandoned are driven by explicit reconciliation triggers

1. Start a top-up and let the hosted deadline pass without trustworthy callback or cancellation.
2. Confirm the first due scheduled or read-through reconciliation records `Expired`.
3. Let the next due reconciliation run with the attempt still unresolved.
4. Confirm:
   - the status becomes `Abandoned`,
   - no wallet credit is created,
   - the reconciliation trigger is auditable in normalization decisions,
   - owner history reflects the later `Abandoned` state after reconciliation runs.

## Scenario 10: Late authoritative evidence may upgrade interim non-crediting states

1. Put a known attempt into `Expired`, `Abandoned`, or `NotConfirmed`.
2. Deliver later validated server-side PayFast evidence for the same owner-safe exact-matched attempt.
3. Confirm:
   - `Completed` or `Cancelled` may replace the interim non-crediting state when proved by the later authoritative callback,
   - completed/cancelled outcomes remain traceable to the Payment Confirmation Record,
   - no duplicate wallet credit is created.

## Scenario 11: “Top-up not confirmed” stays generic and owner-safe

1. Produce each unsafe-to-disclose case:
   - unmatched return,
   - foreign-owner return,
   - duplicate-finalized return,
   - callback missing or otherwise ambiguous return.
2. Confirm:
   - every case shows the same owner-safe `Top-up not confirmed` outcome,
   - no page reveals whether another owner’s attempt exists,
   - no page reveals prior finalization details or raw PayFast diagnostics.

## Scenario 12: Duplicate and replayed confirmations remain idempotent

1. Complete a successful top-up.
2. Replay the same authoritative callback multiple times.
3. Replay conflicting or late-finalized payloads afterward.
4. Confirm:
   - only one wallet credit exists,
   - the accepted final outcome does not change,
   - later evidence is audit-only unless it is a valid late upgrade from an interim non-crediting state.

## Scenario 13: Owner filtering holds for pages, history, and returns

1. Start or complete a top-up as one `CompanyOwner`.
2. Sign in as another `CompanyOwner`.
3. Attempt to access the first owner’s result route, wallet history, and generic return flows.
4. Confirm:
   - foreign data is not returned,
   - unsafe cases collapse to the same generic owner-safe outcome,
   - owner history remains filtered by `UserId`.

## Scenario 14: Site Administrator-only internal review stays privacy-safe

1. Sign in as a non-admin user and attempt to access `/portal/admin/wallet-topups/review`.
2. Confirm access is denied.
3. Sign in as a `SiteAdministrator`.
4. Open the same review surface for a successful credit, a conflicting late-evidence case, and an unmatched return case.
5. Confirm:
   - only minimum non-sensitive evidence is shown,
   - Payment Confirmation Record linkage is visible,
   - no PAN, CVV, expiry, raw gateway diagnostics, or secrets are rendered,
   - the surface is read-only.

## Scenario 15: Persistence parity holds across relational and DynamoDB providers

1. Run equivalent start, callback, duplicate, unsafe-return, deadline, late-upgrade, admin-review, and history scenarios once with relational persistence and once with `PERSISTENCE_PROVIDER=dynamodb`.
2. Confirm:
   - owner filtering behavior matches,
   - the same safe evidence fields are persisted,
   - lifecycle transitions match,
   - startup table provisioning/logging exists only where required but payment behavior matches,
   - exactly-once wallet credit behavior matches,
   - admin-review data remains privacy-equivalent.

## Release evidence plan

- **SC-001**: Automated acceptance tests cover checkout start and successful settlement path for the release candidate.
- **SC-002**: Automated/integration evidence measures time from authoritative callback receipt to both wallet balance visibility and history visibility. Record one row per successful sampled callback in `checklists/sc-004-uat-sign-off-pack.md` and require at least 95% of those rows to show both views within 1 minute.
- **SC-004**: UAT samples 20 completed owner attempts with no manual operator action required to start, confirm, or reconcile payment.
- **SC-004**: Record all 20 attempts in `checklists/sc-004-uat-sign-off-pack.md`, then obtain final reviewer approval on the completed pack.
- **SC-005**: Operational review inspects every successful wallet credit in the release test window and traces it from `WalletActivity.ReferenceId` to the originating `WalletTopUpAttempt`, then through `AuthoritativeEvidenceId` to the Payment Confirmation Record (`ServerConfirmed = true`, `SignatureVerified = true`). Record the trace rows in `checklists/sc-004-uat-sign-off-pack.md`.
- **Ops**: Capture PayFast `notify_url` reachability evidence, reconciliation-trigger evidence (`Expired` then `Abandoned`), late authoritative upgrade evidence, DynamoDB created-versus-confirmed startup logs for the release candidate, FR-026 log evidence showing gateway-unavailability versus merchant-misconfiguration events, and admin-review privacy checks in `checklists/sc-004-uat-sign-off-pack.md`.

## Internal operator review path

- Use the authorized `SiteAdministrator` review surface or equivalent admin-safe operational tooling to review `OutcomeNormalizationDecision` rows with `ConflictWithAcceptedFinalOutcome = true`.
- Review unmatched-return audits through `UnmatchedPaymentReturnRecord` and the linked Payment Confirmation Record / `PaymentReturnEvidence` chain.
- Do not surface these records on owner-facing pages; they are for FR-013 / FR-024 internal review only.

## Manual Test Gate evidence

All 15 scenarios in this quickstart, together with the completed SC-004 sign-off pack, constitute the Manual Test Gate evidence bundle for the release candidate.

## Planned automated coverage

- **Domain/Application**: amount bounds, exact-match criteria, lifecycle transitions, `NotConfirmed` normalization, explicit `Expired` then `Abandoned` reconciliation, late authoritative upgrade from interim non-crediting states, exactly-once settlement, and owner filtering
- **Infrastructure**: PayFast request construction, callback validation, card-only enforcement, safe evidence persistence, Payment Confirmation Record linkage, public callback handling, sandbox/live gating, DynamoDB startup provisioning/logging, and relational/DynamoDB parity
- **Web/bUnit/integration**: wallet top-up form validation, generic return routing, matched result display, not-confirmed page copy, Site Administrator review authorization, privacy-minimized admin rendering, and wallet balance/history freshness

## Verification commands once implementation starts

```bash
dotnet test --filter "Category!=Integration"
dotnet test --collect:"XPlat Code Coverage"
dotnet test --filter "FullyQualifiedName~PayFastHostedPaymentProviderTests|FullyQualifiedName~PayslipDbContextMigrationTests"
```
