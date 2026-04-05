# Feature Specification: PayFast Card Integration

**Feature Branch**: `009-payfast-card-integration`  
**Created**: 2026-04-02  
**Status**: Ready for Implementation  
**Input**: User description: "I want to create a real world integration of a South African payment gateway. The developer docs for the APIs can be found here https://developers.payfast.co.za/docs. Payslip4All should only accept credit card payments and no other payment methods."

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

- **Domain**: Define the business rules for card-only wallet top-up attempts, authoritative payment confirmation, owner-safe outcome handling, and when wallet value may or may not be credited.
- **Application**: Expose use cases to start a PayFast wallet top-up, evaluate authoritative payment evidence, reconcile non-completed attempts, and present owner-scoped results and history.
- **Infrastructure**: Connect Payslip4All to the configured PayFast merchant account, capture non-sensitive payment evidence, distinguish live processing from non-production sandbox behavior, and support consistent persistence across supported providers.
- **Web**: Let a Company Owner start a wallet top-up from the wallet experience, see informative return states that do not overstate payment success, and ensure any internal review surface remains restricted to authorized internal staff.
- **TDD expectation**: Each requirement below must be covered by acceptance scenarios that can be translated into failing tests before implementation starts. The wallet top-up page, owner return pages, generic "Top-up not confirmed" view, and any internal review view introduced for this feature must each have component-level acceptance behavior that can be exercised through Blazor component tests with mocked services.

## Clarifications

### Session 2026-04-03

- Q: Which settlement signal may authorize wallet credit? → A: Only validated server-side PayFast confirmation may authorize wallet credit; browser returns are informational only.
- Q: What top-up amount range is allowed? → A: Minimum R50 and maximum R1000 per top-up.
- Q: What should users see for unmatched or foreign returns? → A: Show one generic owner-safe message such as "Top-up not confirmed" for unmatched, foreign-owner, duplicate-finalized, or otherwise unsafe-to-disclose returns.
- Q: What makes a PayFast confirmation a valid match to a pending top-up attempt? → A: A confirmation matches only when `m_payment_id` maps to one pending attempt, the same owner, the same currency, and a confirmed charged amount within the allowed R50-R1000 range.
- Q: How should non-success outcome states be assigned? → A: `cancelled` = customer or gateway cancellation, `expired` = hosted PayFast session deadline passing, `abandoned` = owner non-completion recorded later during follow-up reconciliation or status checks after that deadline, and `not-confirmed` = browser-only, unmatched, foreign-owner, duplicate-finalized, or otherwise unsafe-to-credit outcomes.
- Q: How should `expired` and `abandoned` be distinguished for silent non-completion? → A: `expired` applies when the hosted PayFast session deadline passes; `abandoned` applies when the owner never completes the flow before that deadline and the system later records that non-completion during follow-up reconciliation or status checks.
- Q: How should SC-001, SC-004, and SC-005 be measured? → A: SC-001 is measured by automated checkout and settlement-path tests, SC-004 by a UAT sample of 20 completed owner attempts with no manual operator intervention, and SC-005 by operational review of all successful wallet credits in the release test window.
- Q: What payment-related data may Payslip4All store, display, or log? → A: Payslip4All may store only non-sensitive PayFast evidence needed for audit and reconciliation, and it must never store, display, or log PAN, CVV, expiry, or raw gateway diagnostics.
- Q: Must persistence behavior match across relational and DynamoDB providers? → A: Yes. Relational and DynamoDB persistence paths must enforce the same owner filtering, payment-evidence persistence, and exactly-once settlement rules.
- Q: What operational callback requirements must be explicit in the spec? → A: The spec requires a publicly reachable `notify_url`, validated server-side callback evidence for settlement, and a non-crediting owner-safe outcome whenever callback delivery or merchant misconfiguration prevents trustworthy confirmation.

### Session 2026-04-05

- Q: What makes a PayFast notify result authoritative enough to settle a wallet top-up? → A: It must arrive through the public server-side notify path, pass authenticity checks, match the configured merchant context, confirm card settlement, pass PayFast's authoritative confirmation step, and correlate to exactly one eligible top-up attempt.
- Q: When may sandbox behavior be used? → A: Sandbox behavior is allowed only in non-production contexts and must never act as the production settlement path.
- Q: Who may review operator-facing payment evidence created by this feature? → A: Only Site Administrators may access operator-facing review records, and they may see only the minimum non-sensitive evidence needed for reconciliation and troubleshooting.

### User Story 1 - Start a wallet top-up with a credit card (Priority: P1)

As a Company Owner, I want to add money to my wallet through PayFast using a credit card so that I can continue paying for payroll activity without leaving Payslip4All for manual support.

**Why this priority**: This is the primary value of the feature. Without a reliable way to start a real hosted card payment, the integration delivers no usable business outcome.

**Independent Test**: Can be fully tested by signing in as a Company Owner, entering a top-up amount on the wallet page, starting the hosted checkout, and confirming that only a credit card payment option is offered and no invalid request creates an attempt.

**Acceptance Scenarios**:

1. **Given** an authenticated Company Owner is viewing their wallet, **When** they choose to top up and enter an amount between R50 and R1000 inclusive, **Then** Payslip4All starts a PayFast-hosted payment for that owner and records the attempt as pending.
2. **Given** a Company Owner starts a top-up, **When** the hosted payment page is shown, **Then** the payment journey permits credit card payment only and does not offer alternative payment methods.
3. **Given** a user is not authenticated as the owning Company Owner, **When** they attempt to start a wallet top-up, **Then** the request is denied and no payment attempt is created for another owner's wallet.
4. **Given** a Company Owner enters an amount that is non-numeric, zero, negative, outside R50-R1000 inclusive, or uses more than two decimal places in South African rand, **When** they attempt to start the top-up, **Then** Payslip4All rejects the entry with a user-friendly validation message and does not create a payment attempt.

---

### User Story 2 - Receive a trustworthy payment result (Priority: P2)

As a Company Owner, I want my wallet to update only after a trustworthy PayFast result is received so that my available balance always matches real money collected.

**Why this priority**: Starting a payment is not enough; the system must safely decide whether to credit the wallet and communicate the outcome clearly.

**Independent Test**: Can be fully tested by completing, cancelling, abandoning, replaying, and mismatching payment evidence and then verifying that wallet balance, result pages, and top-up history reflect only authoritative outcomes.

**Acceptance Scenarios**:

1. **Given** a pending top-up attempt, **When** Payslip4All receives a server-side PayFast notify result that passes authenticity validation, matches the configured merchant context, passes PayFast's authoritative confirmation step, confirms card settlement, and whose `m_payment_id` maps to exactly one eligible attempt for the same owner and currency with a confirmed charged amount within R50-R1000 inclusive and no more than two decimal places, **Then** the wallet is credited once using the confirmed charged amount and the attempt is marked completed.
2. **Given** a pending top-up attempt, **When** Payslip4All receives an authoritative successful confirmation whose charged amount is lower or higher than the originally requested amount but still meets the same currency, range, and precision rules, **Then** the wallet is credited once for the confirmed charged amount, the owner sees the actual credited amount, and both requested and confirmed amounts are retained for audit and reconciliation.
3. **Given** a pending top-up attempt, **When** Payslip4All receives server-side payment evidence that fails authenticity checks, fails PayFast's authoritative confirmation step, does not match the configured merchant context, references a non-card payment method, or cannot be matched confidently to exactly one eligible owner attempt, **Then** no wallet credit occurs and the owner sees only the generic owner-safe "Top-up not confirmed" message family.
4. **Given** a pending top-up attempt, **When** Payslip4All receives validated server-side PayFast evidence that the payment was cancelled by the customer or gateway, **Then** the wallet is not credited, the attempt is marked cancelled, and the owner can see that cancelled outcome in their own top-up history and result views.
5. **Given** a pending top-up attempt reaches its hosted payment deadline without authoritative completion or cancellation evidence, **When** Payslip4All later reconciles the attempt during scheduled follow-up or owner-triggered status checks, **Then** the wallet is not credited and the attempt is marked expired first and may later be marked abandoned if the owner's silent non-completion is still unresolved.
6. **Given** an attempt is already completed and the same successful confirmation is received again, **When** Payslip4All processes the repeated evidence, **Then** no additional wallet credit occurs and the previously completed result remains unchanged.
7. **Given** Payslip4All has already recorded a final authoritative completed or cancelled outcome for an attempt, **When** conflicting later evidence for the same attempt arrives, **Then** Payslip4All preserves the existing authoritative outcome, does not reverse or duplicate wallet movement within this feature, and retains the conflicting evidence for internal review.
8. **Given** an attempt is in expired, abandoned, or not-confirmed because authoritative evidence was missing earlier, **When** later validated server-side evidence for the same attempt arrives, **Then** Payslip4All may replace that interim non-crediting outcome with the authoritative completed or cancelled outcome that the later evidence proves.
9. **Given** a browser return indicates apparent payment success, **When** no authoritative server-side confirmation has been received, **Then** the wallet is not credited and the browser return is treated as informational only.
10. **Given** settlement processing has received authoritative payment evidence but cannot finish linking the Payment Confirmation Record, wallet credit, and audit trail, **When** Payslip4All detects the partial failure, **Then** the attempt is not presented as completed, no orphaned wallet credit is allowed to remain without traceable evidence, and the attempt stays or returns to a recoverable non-completed state until reconciliation finishes.

---

### User Story 3 - Run live gateway payments safely (Priority: P3)

As a Site Administrator, I want Payslip4All to run live PayFast wallet top-ups with safe environment controls and restricted internal review access so that production wallet balances remain trustworthy and payment troubleshooting stays privacy-safe.

**Why this priority**: Operational trust matters for a financial feature. Live payments must not fall back to simulated flows, unsupported payment types, or loosely controlled internal access.

**Independent Test**: Can be fully tested by enabling the live PayFast path, attempting non-production or non-card settlement paths, exercising owner-safe failure states, verifying Site Administrator-only internal review access, and confirming startup validation of required payment tables.

**Acceptance Scenarios**:

1. **Given** live wallet top-ups are enabled for production use, **When** a Company Owner starts a top-up, **Then** the payment is routed through the configured real PayFast merchant account rather than a simulated payment journey.
2. **Given** Payslip4All is running in a non-production context, **When** sandbox behavior is enabled for verification purposes, **Then** the feature may use sandbox behavior without changing the production-only rule that live production wallet credits require the real live payment path.
3. **Given** Payslip4All is running in production, **When** a sandbox, test-only, or other non-live payment path is presented or returns a result, **Then** the wallet is not credited and the attempt is recorded as not successfully confirmed.
4. **Given** the PayFast gateway is temporarily unavailable, **When** a Company Owner attempts to start a top-up, **Then** Payslip4All does not create a false success state, shows the owner-safe message "Payment could not be started", and records the event as an operator-visible gateway-availability issue.
5. **Given** the PayFast merchant account is misconfigured or no longer allowed to create hosted payments, **When** a Company Owner attempts to start a top-up, **Then** Payslip4All does not create a false success state, shows the same owner-safe message "Payment could not be started", and records the event as an operator-visible merchant-configuration issue without exposing that detail to the owner.
6. **Given** payment evidence or reconciliation details require internal review, **When** a user who is not a Site Administrator attempts to access that review information, **Then** access is denied; **And Given** a Site Administrator opens the same review information, **When** access is granted, **Then** only the minimum non-sensitive evidence needed for reconciliation or troubleshooting is shown.
7. **Given** Payslip4All is configured with `PERSISTENCE_PROVIDER=dynamodb`, **When** the application starts, **Then** it verifies that all payment-related DynamoDB tables needed for wallet top-up attempts, payment confirmation records, normalization decisions, and unmatched return records are available or creates any missing ones before accepting payment processing, and it records for each table whether it was created or already confirmed.

---

### Edge Cases

- A Company Owner submits a top-up amount below R50 or above R1000.
- A Company Owner submits a top-up amount that is not a valid South African rand amount because it is non-numeric, zero, negative, or uses more than two decimal places.
- PayFast reports success for an amount that differs from the originally requested top-up amount, including overpayment and underpayment that still fall within the permitted currency and amount rules.
- PayFast reports a confirmed amount that is zero, negative, outside R50-R1000 inclusive, or more precise than two decimal places.
- The same successful authoritative confirmation is received more than once for the same attempt.
- A completed or cancelled attempt later receives conflicting evidence with the opposite status.
- A user closes the browser after leaving for PayFast and returns later to check their wallet.
- A pending attempt reaches its hosted PayFast session deadline without authoritative confirmation or cancellation evidence; it becomes expired at that deadline, and later follow-up checks may record the owner's silent non-completion as abandoned.
- A payment return arrives after the attempt has already been cancelled, expired, abandoned, completed, or otherwise finalized.
- A browser return suggests success or cancellation, but no authoritative server-side evidence has been received yet.
- A server-side notify result arrives but fails authenticity checks, fails PayFast's authoritative confirmation step, or cannot be tied to the configured merchant context.
- A payment return references an unknown or already-settled top-up attempt.
- A payment return includes an `m_payment_id` that does not map to exactly one eligible unsettled attempt for the same owner and currency.
- A payment return belongs to another owner or otherwise cannot be disclosed safely; the owner must see only the generic "Top-up not confirmed" outcome.
- A non-credit-card or non-live PayFast payment route is attempted despite the card-only and live-path business rules.
- Sandbox behavior is accidentally enabled in production.
- Trustworthy payment evidence is received, but the system cannot finish persisting the Payment Confirmation Record, the wallet credit link, or the wallet state update in the same settlement flow.
- PayFast is reachable, but the hosted payment cannot be created because merchant credentials or account status are invalid.
- A non-Site-Administrator attempts to access operator-facing payment review records.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow an authenticated Company Owner to initiate a wallet top-up for their own wallet only.
- **FR-002**: The system MUST start wallet top-ups through PayFast as the real hosted payment gateway for this feature.
- **FR-003**: The system MUST restrict this payment journey to credit card payments only and MUST prevent other PayFast payment methods from being accepted for wallet top-ups.
- **FR-004**: The system MUST record each top-up attempt with the initiating owner, requested amount, gateway reference, initiation time, hosted-session deadline, and current outcome state.
- **FR-005**: The system MUST credit wallet value only after it has received a server-side PayFast notify result that is validated as authentic, matched to the configured merchant context, passed through PayFast's authoritative confirmation step, and matched to exactly one eligible top-up attempt; browser returns are informational only and MUST NOT authorize wallet credit.
- **FR-006**: The system MUST use the confirmed charged amount for the wallet credit when an authoritative successful payment result is accepted, even if that amount differs from the amount originally requested, provided the confirmed amount is in South African rand, within R50-R1000 inclusive, uses no more than two decimal places, and represents a credit-card settlement.
- **FR-007**: The system MUST NOT credit the wallet for cancelled, expired, abandoned, not-confirmed, unmatched, duplicate, invalid-amount, unverified, non-credit-card, sandbox/test-only, or otherwise non-authoritative payment results.
- **FR-008**: The system MUST process duplicate return messages idempotently so that a successful top-up can affect the wallet at most once, and repeated delivery of the same successful evidence MUST leave the existing completed outcome unchanged.
- **FR-009**: The system MUST present the Company Owner with a clear outcome for each known top-up attempt, including pending, completed, cancelled, expired, abandoned, and not-confirmed states.
- **FR-010**: The system MUST preserve an auditable Payment Confirmation Record as the authoritative payment evidence artifact that links each wallet credit to the corresponding verified PayFast payment result and originating top-up attempt for settlement and audit correlation.
- **FR-011**: The system MUST permit sandbox or other non-live payment behavior only in non-production contexts, and production wallet top-ups MUST use the live payment path only.
- **FR-012**: The system MUST show only the owner-safe message "Payment could not be started" when the hosted top-up cannot be created or initiated, and it MUST NOT expose raw gateway diagnostics, merchant-configuration details, or other sensitive payment troubleshooting detail to end users.
- **FR-013**: The system MUST ensure that all wallet top-up views, history, and result pages are filtered to the authenticated Company Owner's own data, and any support-facing or operator-facing review created by this feature MUST be accessible only to Site Administrators and reveal only the minimum non-sensitive payment evidence needed for reconciliation or troubleshooting.
- **FR-014**: The system MUST accept wallet top-up amounts from R50 to R1000 inclusive in South African rand with no more than two decimal places, and MUST reject non-numeric, zero, negative, out-of-range, or over-precision amounts with a user-friendly validation message before any payment attempt is created.
- **FR-015**: The system MUST use the generic owner-safe "Top-up not confirmed" message family for browser-only, unmatched, foreign-owner, duplicate-finalized, invalid, unverified, callback-failed, or otherwise unsafe-to-disclose non-confirmed result cases, and it MUST NOT reveal whether any specific attempt exists.
- **FR-016**: The system MUST treat a PayFast confirmation as a valid match only when its `m_payment_id` maps to exactly one eligible top-up attempt for the same owner and currency, where eligible means pending, expired, abandoned, or not-confirmed but not already completed or cancelled, and its confirmed charged amount is within the allowed R50-R1000 range with no more than two decimal places; otherwise the result MUST be treated as unmatched and must not credit the wallet.
- **FR-017**: The system MUST assign non-success top-up attempt outcomes as follows: `cancelled` for customer or gateway cancellation proved by authoritative server-side evidence, `expired` when the hosted PayFast session deadline passes before authoritative evidence is available, `abandoned` when the owner never completes the flow before that deadline and the system later records that silent non-completion during follow-up reconciliation or status checks, and `not-confirmed` for a known top-up attempt whose current evidence is browser-only, invalid, or otherwise insufficient to authorize credit.
- **FR-018**: The system MUST store only non-sensitive PayFast payment evidence needed for audit and reconciliation, and it MUST NOT store, display, or log PAN, CVV, expiry, or raw gateway diagnostics anywhere in Payslip4All.
- **FR-019**: The system MUST enforce the same owner filtering, Payment Confirmation Record persistence, outcome-normalization and unmatched-return auditing, and exactly-once wallet-settlement behavior across supported relational persistence providers and the DynamoDB persistence provider; whenever new payment-audit fields or records are introduced for the relational path, the relational data model MUST be updated through migration-backed schema changes so the stored audit evidence remains consistent and reviewable.
- **FR-020**: The system MUST expose a publicly reachable PayFast `notify_url`, and it MUST treat callback evidence as authoritative only when that callback arrives through the notify path, passes authenticity verification, matches the configured merchant context, confirms card settlement, and passes PayFast's authoritative confirmation step; whenever callback delivery failure or any of those validation steps prevents trustworthy confirmation, the outcome MUST remain non-crediting and owner-safe.
- **FR-021**: When `PERSISTENCE_PROVIDER=dynamodb`, the system MUST verify at startup that the payment-related DynamoDB tables for wallet top-up attempts, payment confirmation records, outcome normalization decisions, and unmatched return records are available or create any that are missing before payment processing begins, and it MUST log for each table whether it was created or already confirmed.
- **FR-022**: When authoritative successful evidence shows an overpayment or underpayment that still satisfies the allowed currency, range, and precision rules, the system MUST preserve both requested and confirmed amounts for audit and reconciliation and show the owner the actual credited amount in top-up history.
- **FR-023**: When evidence shows a zero, negative, out-of-range, or over-precision confirmed amount, the system MUST treat the attempt as not-confirmed, MUST NOT credit the wallet, and MUST preserve the evidence for audit and reconciliation.
- **FR-024**: If conflicting evidence is received after an attempt has already reached a final authoritative completed or cancelled outcome, the system MUST preserve the first authoritative final outcome, MUST NOT reverse or duplicate wallet movement within this feature, and MUST retain the conflicting evidence for Site Administrator review.
- **FR-025**: The system MUST complete settlement only when the Payment Confirmation Record, the wallet credit record, and their audit linkage are all persisted together; if any part of that settlement persistence fails, the owner-visible result MUST remain or return to a non-completed state and no orphaned or untraceable wallet credit may remain.
- **FR-026**: The system MUST distinguish operator-facing initiation failures caused by temporary gateway unavailability from those caused by merchant credential or account misconfiguration, while still showing the same owner-safe start-failure message to the Company Owner in both cases.
- **FR-027**: The system MUST treat completed and cancelled as terminal authoritative outcomes, while expired, abandoned, and not-confirmed are interim non-crediting outcomes that may change only to completed or cancelled if later validated server-side evidence for the same top-up attempt arrives.
- **FR-028**: The phrase "Top-up not confirmed" MUST be treated as a user-facing message family and, for known top-up attempts, as the owner-visible label for the `not-confirmed` lifecycle state; unmatched or foreign-owner returns may use the same message family without creating or changing any owner's recorded attempt state.

### Key Entities *(include if feature involves data)*

- **Wallet Top-Up Attempt**: A record of a requested wallet funding action, including the owner, requested amount, gateway reference, lifecycle state, hosted session deadline, and timestamps needed to track the payment journey. Its lifecycle states are pending, completed, cancelled, expired, abandoned, and not-confirmed. Completed and cancelled are terminal authoritative outcomes; expired, abandoned, and not-confirmed are non-crediting states that may later be replaced only by authoritative completed or cancelled evidence for the same attempt.
- **Payment Confirmation Record**: The authoritative payment evidence artifact for settlement and audit correlation, storing the trustworthy PayFast result used to decide whether the wallet may be credited, including the charged amount, payment method classification, `m_payment_id`, confirmation status, and correlation to exactly one top-up attempt.
- **Wallet Credit Record**: The auditable wallet balance change created only when a top-up succeeds, linked back to the corresponding Payment Confirmation Record.
- **Outcome Normalization Decision**: An audit record of how Payslip4All interprets a payment return, timeout, or reconciliation event into a final owner-visible outcome such as completed, cancelled, expired, abandoned, or not-confirmed, including whether wallet credit is allowed.
- **Unmatched Return Record**: A privacy-safe record of a payment return that cannot be matched or disclosed as a specific owner's top-up result and therefore must resolve to the generic owner-safe "Top-up not confirmed" message family.

## Assumptions

- The business already has an approved PayFast merchant account available for live wallet top-up processing.
- Wallet top-ups for this feature are denominated in South African rand.
- Refunds, chargebacks, and payment disputes are handled outside this feature.
- Company Owners remain the only end users who can initiate wallet top-ups; Site Administrators are the only internal users authorized to review payment troubleshooting evidence created by this feature.
- Sandbox verification is permitted only outside production and must never replace the live production settlement path.
- Proactive credential-rotation workflows, callback monitoring dashboards, and settlement alerting are operational controls outside the scope of this feature; this feature only requires owner-safe outcomes and auditable evidence when those controls fail or are absent.
- Release-governance activities such as manual test gates, rollout approvals, and constitution-compliance reviews remain outside the feature requirements themselves and are not separate functional obligations beyond the measurable outcomes stated in this specification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of wallet top-up journeys presented to end users offer credit card payment only and do not permit alternative payment methods, as evidenced by automated checkout-start and settlement-path acceptance tests for the release candidate.
- **SC-002**: At least 95% of authoritative successful wallet top-ups appear in the owner's wallet balance and history within 1 minute of the confirmed payment result reaching Payslip4All.
- **SC-003**: 0 wallet credits are created from cancelled, expired, abandoned, duplicate, unmatched, unverified, non-credit-card, or non-live payment results during acceptance testing.
- **SC-004**: At least 90% of Company Owners can complete a successful wallet top-up without support intervention during user acceptance testing, measured across a reviewable sample of 20 completed owner attempts captured in one sign-off pack that lists each attempt identifier, test date, owner or tester, outcome, whether any manual operator action was needed to start, confirm, or reconcile the payment, and final reviewer approval of the full sample.
- **SC-005**: 100% of successful wallet credits can be traced during operational review of all successful wallet credits in the release test window to the corresponding Payment Confirmation Record and the originating top-up attempt.
