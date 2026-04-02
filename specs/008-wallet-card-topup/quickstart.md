# Quickstart: Generic Wallet Card Top-Up

## Prerequisites

- Application running locally from `/Users/adhirramjiawan/projects/payslip4all`
- Authenticated `CompanyOwner` test account
- Persistence configured for both:
  - the default EF Core-backed path, and
  - `PERSISTENCE_PROVIDER=dynamodb` for parity verification
- Non-production hosted-payment simulator enabled for local/manual validation
- Stopwatch or timestamp capture capability for SC-001, SC-004, and SC-006 checks
- Ability to inspect persisted audit records for:
  - Payment Return Evidence
  - Normalized outcome decisions
  - Unmatched Payment Return Records
  - linked wallet activity

## Scenario 1: Measure the top-up start hand-off journey (SC-001)

1. Sign in as a `CompanyOwner`.
2. Navigate to `/portal/wallet`.
3. Start timing when the wallet funding form is ready to use.
4. Enter a valid positive amount such as `100.00` and submit the hosted top-up request.
5. Stop timing when the browser is handed off to the external hosted payment page or non-production simulator.
6. Confirm:
   - the elapsed time measures only the start-to-hand-off journey,
   - Payslip4All creates a pending top-up attempt before redirecting,
   - no card PAN, CVV, or expiry data is collected or stored,
   - the wallet balance remains unchanged.

## Scenario 2: Trustworthy matched success credits the wallet exactly once

1. Start a new top-up attempt from `/portal/wallet`.
2. Complete the hosted flow using the simulator’s trustworthy **Completed** outcome.
3. Return through `/portal/wallet/top-ups/return`.
4. Confirm:
   - the flow resolves to the matched-attempt result route,
   - the attempt is `Completed`,
   - the wallet is credited exactly once,
   - the credited amount equals the confirmed charged amount,
   - the resulting wallet activity is linked back to the matched attempt and audit trail.

## Scenario 3: Confirmed charged amount differs from requested amount

1. Start a top-up attempt with requested amount `100.00`.
2. Use the simulator’s trustworthy **Completed with different amount** action returning `95.00`.
3. Confirm:
   - the matched attempt is `Completed`,
   - the wallet is credited by `95.00`, not `100.00`,
   - the attempt, audit records, and wallet activity all show the authoritative charged amount consistently.

## Scenario 4: Trustworthy cancelled and expired outcomes do not credit the wallet

1. Start two fresh top-up attempts.
2. Use the simulator’s trustworthy **Cancelled** outcome for the first attempt.
3. Use the simulator’s trustworthy **Expired** outcome for the second attempt.
4. Confirm for each:
   - the matched attempt shows the correct explicit status,
   - the wallet remains unchanged,
   - the result page explains that no wallet credit was created.

## Scenario 5: Distinguish pre-threshold `Pending` from `Unverified`

1. Start one top-up attempt and use the simulator’s **Pending / unresolved** action.
2. Confirm:
   - the attempt remains `Pending`,
   - no wallet credit is created,
   - the result explains that the final outcome is still unknown.
3. Start a second top-up attempt and use the simulator’s **Low-confidence final claim** action before the 1-hour threshold.
4. Confirm:
   - the second attempt becomes `Unverified`,
   - no wallet credit is created,
   - the result explains that the wallet was not credited because the payment could not be confirmed.

## Scenario 6: The exact 1-hour threshold produces `Abandoned`

1. Start a fresh top-up attempt and do not complete the hosted flow.
2. Advance time to just before `AbandonAfterUtc`; confirm the attempt is not yet `Abandoned`.
3. Advance time to exactly `AbandonAfterUtc`.
4. Confirm:
   - the attempt is now `Abandoned`,
   - no wallet credit exists,
   - audit history shows an abandonment decision at the threshold,
   - the owner-visible result reflects that the top-up was not completed.

## Scenario 7: Trustworthy late evidence may replace `Abandoned`

1. Create a top-up attempt and let it become `Abandoned` at the exact threshold.
2. After the threshold, deliver trustworthy matched **Completed**, **Cancelled**, or **Expired** Payment Return Evidence for that same attempt.
3. Confirm:
   - the authoritative outcome changes from `Abandoned` to the trustworthy final outcome,
   - the earlier abandonment remains visible in the audit trail as superseded,
   - wallet credit occurs only for trustworthy late `Completed`,
   - any resulting wallet credit is linked to the accepted late evidence and matched attempt,
   - the update occurs within the SC-006 timing target.

## Scenario 8: Low-confidence late evidence cannot reopen `Abandoned`

1. Create a top-up attempt and let it become `Abandoned`.
2. After the threshold, send low-confidence or unresolved evidence that claims or implies a final outcome.
3. Confirm:
   - the attempt remains `Abandoned`,
   - no wallet credit is created,
   - the late evidence is recorded for audit,
   - no owner-visible transition back to `Pending` or `Unverified` occurs.

## Scenario 9: Conflicting evidence after an accepted trustworthy final outcome is audit-only

1. Complete a matched attempt with trustworthy final evidence so it becomes authoritative.
2. Replay later conflicting evidence for the same attempt.
3. Confirm:
   - the authoritative matched outcome does not change,
   - no additional or reversed wallet credit is created,
   - the conflicting evidence is still present in Payment Return Evidence and decision audit records,
   - the owner-visible result remains stable.

## Scenario 10: Unmatched returns stay generic and privacy-safe (SC-005)

1. Send a return with missing, invalid, conflicting, or multi-match correlation data to `/portal/wallet/top-ups/return` and confirm the user is redirected to `/portal/wallet/top-ups/return/not-confirmed`.
2. Confirm:
   - the system creates an Unmatched Payment Return Record,
   - the user lands on the generic not-confirmed result flow,
   - the result reveals no guessed attempt identifier,
   - the result reveals no owner identity, wallet details, or wallet-credit confirmation,
   - no matched attempt status changes and no wallet credit is created.

## Scenario 11: Replays remain idempotent

1. Complete a successful matched top-up attempt.
2. Refresh the matched result page or replay the same trustworthy success evidence multiple times.
3. Confirm:
   - only one wallet credit exists,
   - the same authoritative `Completed` outcome remains visible,
   - the audit trail shows repeated evidence handling without duplicate settlement.

## Scenario 12: Matched-attempt history visibility meets SC-004

1. Start fresh matched attempts that end as `Completed`, `Cancelled`, `Expired`, `Abandoned`, and `Unverified`.
2. Record the timestamp when Payslip4All determines each authoritative matched outcome.
3. Refresh `/portal/wallet` history.
4. Confirm:
   - the same normalized matched statuses appear for the owning user,
   - each status becomes visible within 5 minutes of outcome determination,
   - unmatched returns are not shown as synthetic attempt statuses.

## Scenario 13: Owner scope and unmatched privacy both hold

1. Create top-up attempts for one `CompanyOwner`.
2. Sign in as a different `CompanyOwner`.
3. Attempt to access:
   - a matched result route for the first owner,
   - the generic unmatched result flow triggered by an unmatched return.
4. Confirm:
   - matched attempt access is denied or returns no foreign payment data,
   - the unmatched generic result still discloses no foreign owner or wallet information,
   - each owner sees only their own matched attempt history and wallet credits.

## Scenario 14: Audit and reconciliation trail is financially credible

1. Execute one trustworthy matched success, one superseded abandonment, one low-confidence late evidence case, one conflicting-after-final case, and one unmatched case.
2. Inspect stored records.
3. Confirm:
   - each Payment Return Evidence item is persisted,
   - each authoritative or ignored decision has a Normalized outcome decision record,
   - superseded abandonment remains traceable,
   - each Unmatched Payment Return Record is separate from attempt history,
   - any wallet credit can be traced to the matched attempt, accepted evidence, and decision record that authorized it.

## Scenario 15: Persistence parity across EF Core and DynamoDB

1. Run Scenarios 2 through 14 with the default EF Core-backed provider path.
2. Repeat the same scenarios with `PERSISTENCE_PROVIDER=dynamodb`.
3. Confirm:
   - both persistence paths produce the same authoritative statuses,
   - both preserve the same precedence decisions and privacy rules,
   - both prevent duplicate settlement,
   - both preserve the same audit trail concepts and unmatched behavior.
