# Feature Specification: Generic Wallet Card Top-Up

**Feature Branch**: `008-wallet-card-topup`  
**Created**: 2026-04-02  
**Status**: Ready for Planning  
**Input**: User description: "create a new feature for generic credit card payment to top up wallet. The credit card details must not be stored within the app. Once the payment gateway redirects back to payslip4all app should we credit the user's wallet with the amount their credit card was charged. I want to specify the payment gateway in a following feature. Please keep implementation in this feature generic"

## Clarifications

### Session 2026-04-02

- Q: What should the allowed confirmation window be before a never-returned or unresolved wallet top-up attempt is marked as abandoned? → A: 1 hour after initiation.
- Q: Should `failed` remain a distinct wallet top-up status, or should the feature use only the explicit negative outcomes already listed? → A: Remove `failed` and use only explicit statuses.

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

- Every requirement below is expressed as user-visible behaviour with acceptance scenarios so it can be covered by tests before implementation begins.
- The specification defines measurable pass/fail outcomes for successful, cancelled, expired, abandoned, unverified, and unmatched-return cases so later planning can map each one to explicit verification and release-quality gates.
- The feature is only considered ready for delivery when every acceptance scenario can be demonstrated with the wallet balance, payment-attempt status, and user-visible outcome all matching the expected result.
- Outcome normalization must be deterministic across providers: the same payment return evidence must always map to the same normalized outcome, wallet effect, and user-visible message.
- The feature is scoped so business rules can remain in the domain and application layers, payment-provider integrations can stay in infrastructure, and wallet/payment pages can stay in the web layer.
- All authenticated wallet and payment journeys are owner-scoped: a company owner may only initiate, view, and receive credits for their own wallet top-up attempts.
- The feature introduces payment-related records that must be listed under Key Entities and remain compatible with the existing wallet-credit model.
- Credit card entry and processing occur outside Payslip4All so sensitive card details are never captured or retained by the application.
- Any hosted payment simulator used for development, demonstration, or test support is outside the production customer card-entry journey and must not change the rule that real card entry occurs only on an external hosted payment page.

### User Story 1 - Start a card-based wallet top-up (Priority: P1)

A company owner can choose a wallet top-up amount and be redirected to an external card payment page to complete the payment without entering card details inside Payslip4All.

**Why this priority**: Without a secure way to start a hosted payment journey, there is no usable card top-up flow and no business value for wallet funding by card.

**Independent Test**: Can be fully tested by signing in as a company owner, choosing a valid top-up amount, starting a card payment, and confirming the app creates a pending top-up attempt and redirects the user away from Payslip4All to continue payment.

**Acceptance Scenarios**:

1. **Given** a signed-in company owner with access to their wallet, **When** they request a valid top-up amount using card payment, **Then** the system creates a pending payment attempt for that owner and redirects them to an external hosted payment page.
2. **Given** a signed-in company owner, **When** they try to start a card top-up with a zero or negative amount, **Then** the system rejects the request and does not create a payment attempt.
3. **Given** a signed-in company owner, **When** they start a card top-up, **Then** Payslip4All does not ask for, display, or persist full credit card details.

---

### User Story 2 - Credit the wallet after successful payment return evidence (Priority: P1)

A company owner returning from the hosted payment page has their wallet credited only after Payslip4All confirms the payment completed successfully, using the amount that was actually charged.

**Why this priority**: The wallet must only be credited for completed payments, and it must reflect the real charged amount rather than an unverified requested amount.

**Independent Test**: Can be fully tested by creating a payment attempt, simulating successful and conflicting payment return evidence, and confirming the wallet balance increases exactly once by the confirmed charged amount only when trustworthy successful evidence is accepted.

**Acceptance Scenarios**:

1. **Given** a pending top-up attempt for a signed-in company owner, **When** the external payment flow returns with a confirmed successful payment, **Then** the system credits that owner wallet by the confirmed charged amount and marks the attempt as completed.
2. **Given** a pending top-up attempt, **When** the external payment flow returns with a cancelled outcome, **Then** the system does not credit the wallet, marks the attempt as cancelled, and tells the owner that the top-up was cancelled and no funds were added.
3. **Given** a pending top-up attempt, **When** the external payment flow returns with an expired outcome, **Then** the system does not credit the wallet, marks the attempt as expired, and tells the owner that the top-up expired before completion.
4. **Given** a pending top-up attempt, **When** the external payment flow returns with data that claims payment success but cannot be verified with enough confidence to trust the outcome before the 1-hour abandonment point, **Then** the system does not credit the wallet, marks the attempt as unverified, and tells the owner that the wallet was not credited because the payment could not be confirmed.
5. **Given** a return to Payslip4All includes missing, invalid, or conflicting reference data so the response cannot be matched to exactly one known top-up attempt, **When** the system receives that return, **Then** it does not credit any wallet, preserves an audit record of the unmatched return, and shows a generic not-confirmed result that is not routed through or labeled as a specific payment attempt.
6. **Given** a successful payment return is received more than once for the same payment attempt, **When** the system processes the repeated return, **Then** the wallet is credited only once and the repeated processing does not create a duplicate credit.
7. **Given** a top-up attempt has already been marked abandoned because 1 hour passed without trustworthy final evidence, **When** later trustworthy final evidence for exactly that attempt arrives, **Then** the system records that evidence, replaces the abandoned outcome with the corresponding final outcome, and credits the wallet only if the late trustworthy evidence confirms a completed payment with a confirmed charged amount.
8. **Given** a matched top-up attempt already has a trustworthy final outcome, **When** later conflicting evidence arrives for that same attempt, **Then** the system preserves the existing final outcome and wallet effect, records the conflicting evidence for audit, and does not create an additional or reversed wallet credit within this feature.

---

### User Story 3 - See incomplete and unresolved payment outcomes (Priority: P2)

A company owner can understand whether a card top-up is pending, completed, cancelled, expired, abandoned, or unverified, can see clearly when no wallet credit was created, and is protected from privacy leaks when unmatched payment return evidence is shown generically.

**Why this priority**: Owners need confidence that their payment was handled correctly even when they never finish the hosted flow or return with unresolved results, and support teams need a clear record when investigating disputes or repeated redirects.

**Independent Test**: Can be fully tested by creating payment attempts with completed, cancelled, expired, abandoned, and unverified outcomes; sending unmatched payment return evidence; and confirming the owner can view only their own attempts, statuses, charged amounts, linked wallet credits where applicable, and a generic unmatched result with no ownership or wallet leakage.

**Acceptance Scenarios**:

1. **Given** a company owner with one or more card top-up attempts, **When** they view their wallet funding history, **Then** they see only their own payment attempts with statuses, requested amounts, confirmed charged amounts where available, and a clear indication of whether the wallet was credited.
2. **Given** a pending top-up attempt where the owner leaves the hosted payment page and never returns, **When** 1 hour passes from initiation without a verified final outcome, **Then** the system marks the attempt as abandoned, keeps the wallet balance unchanged, and shows the owner that the top-up was not completed.
3. **Given** a completed card top-up, **When** the company owner reviews their wallet activity, **Then** they can identify that the wallet credit came from a completed card payment.
4. **Given** one company owner attempts to view another owner payment attempts or results, **When** access is requested, **Then** the system denies access and shows no other owner payment data.
5. **Given** a user is returned to Payslip4All with unmatched payment return evidence, **When** the generic result is shown, **Then** the app displays a generic not-confirmed outcome without exposing a guessed attempt identifier, whether a guessed attempt exists, another owner identity, wallet details, or any wallet credit as completed.
6. **Given** a matched attempt reaches a final business outcome or is later reclassified from abandoned after trustworthy final evidence arrives, **When** the record is reviewed for reconciliation, **Then** the business can trace the attempt, each auditable payment return evidence item used in the decision, any superseded abandonment, and any linked wallet credit.

---

### Edge Cases

- The confirmed charged amount may be lower or higher than the originally requested amount; the wallet credit must use only the confirmed charged amount once the payment is verified.
- A user may refresh or reopen the return URL after the wallet has already been credited; repeat processing must not create an additional wallet credit.
- The hosted payment flow may return without enough information to match the response to exactly one known attempt; in that case no wallet may be credited, the unmatched return must remain auditable, and the generic result must not reveal whether a guessed attempt, owner, or wallet exists.
- The payment return evidence may still be unresolved when the user returns to Payslip4All; the attempt must remain pending until a final verifiable outcome is known or the attempt is treated as abandoned.
- A user may start a payment, close the external page, and never return to the app; if no verified final outcome is received within 1 hour of initiation, the attempt must be marked abandoned and no wallet credit created.
- A return may imply that a payment finished but still lack trustworthy evidence of the final outcome; when that happens for exactly one known attempt before the 1-hour abandonment point, the attempt must be marked unverified rather than pending or completed.
- Different providers may send different combinations of references and outcome labels for the same real-world event; Payslip4All must normalize those signals consistently according to the same decision rules.
- Trustworthy final evidence may arrive after an attempt has already been marked abandoned; in that case the trustworthy final evidence must replace the abandoned outcome, remain auditable alongside the earlier abandonment, and create a wallet credit only if it confirms a completed payment.
- Low-confidence or conflicting evidence that arrives after an attempt is already abandoned must not reopen the attempt unless it provides trustworthy final evidence for exactly one known attempt.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow an authenticated company owner to initiate a wallet top-up request for a positive rand amount using a generic hosted card payment flow.
- **FR-002**: The system MUST create a payment attempt record before redirecting the user to the external payment page.
- **FR-003**: The system MUST redirect the user from Payslip4All to an external hosted payment page to complete card entry and payment authorization.
- **FR-004**: The system MUST NOT store full credit card details within Payslip4All at any point in the top-up flow.
- **FR-005**: The system MUST record the requested top-up amount, ownership, attempt status, and enough external reference information to match payment return evidence back to the correct attempt when an exact match is possible.
- **FR-006**: The system MUST credit the company owner wallet only after the app receives and validates trustworthy successful payment return evidence for that payment attempt.
- **FR-007**: The system MUST credit the wallet using the amount confirmed as charged by the accepted trustworthy successful evidence, even if that amount differs from the originally requested amount.
- **FR-008**: The system MUST NOT credit the wallet for cancelled payment attempts and MUST mark those attempts as cancelled.
- **FR-009**: The system MUST NOT credit the wallet for expired payment attempts and MUST mark those attempts as expired.
- **FR-010**: The system MUST leave the wallet balance unchanged for any payment attempt that never reaches a verified final outcome and MUST mark that attempt as abandoned once 1 hour has passed since initiation.
- **FR-011**: The system MUST leave the wallet balance unchanged for any matched payment return evidence that claims or implies a final outcome but lacks enough trustworthy evidence to accept that final outcome before an authoritative trustworthy final outcome exists, and MUST classify the attempt as unverified unless the 1-hour abandonment rule has already taken effect.
- **FR-012**: The system MUST leave all wallet balances unchanged when payment return evidence cannot be matched to exactly one known payment attempt, MUST preserve an auditable record of the unmatched return, and MUST NOT treat unmatched as a payment-attempt status.
- **FR-013**: The system MUST process payment return evidence idempotently so the same successful payment attempt cannot credit the wallet more than once.
- **FR-014**: The system MUST retain a user-visible status for each payment attempt, including at minimum pending, completed, cancelled, expired, abandoned, and unverified outcomes.
- **FR-015**: The system MUST present clear user-facing messages after return to Payslip4All so the company owner understands whether the wallet was credited, is still pending, was cancelled, expired, abandoned, unverified, or could not be matched to a confirmed top-up result.
- **FR-016**: The system MUST let a company owner view only their own card top-up attempts and related wallet credits.
- **FR-017**: The system MUST preserve a financially auditable decision trail for each payment attempt and each unmatched return record, including every payment return evidence item relied on for a status or wallet decision, the normalized outcome reached from that evidence, any superseded abandoned state, and the wallet credit linkage when completion occurs.
- **FR-018**: The system MUST keep payment-gateway behaviour generic so this feature does not depend on a named gateway, gateway-specific user experience, or gateway-specific business rules.
- **FR-019**: The system MUST surface unmatched returns through a generic not-confirmed result that does not depend on or expose a matched payment-attempt route, identifier, owner identity, wallet details, or whether a guessed attempt exists.
- **FR-020**: The system MUST normalize payment outcomes using a single deterministic decision policy with explicit evidence-precedence rules so equivalent payment return evidence from different providers is classified into the same normalized outcome.
- **FR-021**: If a hosted payment simulator is provided for development, testing, or demonstrations, the system MUST keep it clearly separate from the production customer journey, MUST NOT accept real card details through Payslip4All, and MUST preserve the rule that production card entry happens only on an external hosted payment page.
- **FR-022**: The system MUST treat payment return evidence that cannot be matched to exactly one known attempt as unmatched before evaluating any claimed payment outcome.
- **FR-023**: The system MUST treat trustworthy matched final evidence for completed, cancelled, or expired as higher precedence than pending, unverified, or abandoned states, including when the trustworthy final evidence arrives after the 1-hour abandonment point.
- **FR-024**: The system MUST keep the first accepted trustworthy final outcome for a matched attempt authoritative if later evidence conflicts with it, while preserving the later conflicting evidence in the audit trail and leaving wallet effects unchanged within this feature.

### Payment Return Evidence Normalization Rules

The system must classify each incoming item of payment return evidence using the precedence rules below so that provider-specific wording does not change the resulting business outcome.

| Precedence | Evidence observed by Payslip4All | Normalized outcome | Wallet effect | User-visible result |
| --- | --- | --- | --- | --- |
| 1 | The evidence cannot be matched to exactly one known payment attempt because it matches none, matches multiple attempts, or contains missing, invalid, or conflicting correlation data. | Unmatched return record | No wallet credit and no attempt status change based solely on that evidence. | Generic not-confirmed result that is not tied to a specific payment attempt. |
| 2 | The evidence matches exactly one known payment attempt and provides trustworthy evidence that payment completed successfully, including the confirmed charged amount. | Completed | Credit the matched wallet exactly once by the confirmed charged amount. | Attempt-specific success result and credited wallet activity. |
| 3 | The evidence matches exactly one known payment attempt and provides trustworthy evidence that the payer cancelled the payment. | Cancelled | No wallet credit. | Attempt-specific cancelled result. |
| 4 | The evidence matches exactly one known payment attempt and provides trustworthy evidence that the payment session expired before completion. | Expired | No wallet credit. | Attempt-specific expired result. |
| 5 | The evidence matches exactly one known payment attempt, claims or implies a final outcome, but Payslip4All cannot trust that evidence enough to classify it as completed, cancelled, or expired, and the attempt has not yet reached the 1-hour abandonment point. | Unverified | No wallet credit. | Attempt-specific unverified result with a message that the wallet was not credited because the payment could not be confirmed. |
| 6 | The evidence matches exactly one known payment attempt, does not establish any trustworthy final outcome, and the attempt is still within 1 hour of initiation. | Pending | No wallet credit. | Attempt-specific pending result until a final outcome is known or the attempt becomes abandoned. |
| 7 | Exactly one known payment attempt exists, but 1 hour has passed since initiation without any trustworthy final outcome having been accepted. | Abandoned | No wallet credit. | Attempt-specific abandoned result showing the top-up was not completed. |

Additional normalization notes:

- `Unmatched` is a separate auditable return classification and must never appear as a payment-attempt status in owner history.
- `Pending` is only for matched attempts whose final outcome is still genuinely unknown and that have not yet reached the 1-hour abandonment point.
- `Unverified` is only for matched attempts where a final outcome is being claimed or implied, but the available evidence is not trustworthy enough to accept it before abandonment takes effect.
- Trustworthy matched final evidence for completed, cancelled, or expired always outranks pending, unverified, and abandoned states for that same attempt.
- If trustworthy matched final evidence arrives after an attempt was marked abandoned, the attempt must be reclassified to that trustworthy final outcome, the earlier abandonment must remain visible in the audit trail, and the wallet effect must be applied only if the trustworthy final outcome is completed.
- Once a trustworthy final outcome has been accepted for a matched attempt, later conflicting evidence must be retained for audit and investigation but must not change the authoritative final outcome or create, reverse, or duplicate a wallet credit within this feature.
- If evidence arrives after abandonment and still does not provide trustworthy final evidence for exactly one known attempt, the attempt remains abandoned and the late evidence is only recorded for audit.
- When an unmatched return is later resolved outside this feature, that resolution must not retroactively imply that a wallet credit should have happened at the time of the unmatched result without new trustworthy evidence tied to exactly one attempt.

### Key Entities *(include if feature involves data)*

- **Wallet Top-Up Payment Attempt**: A record of an owner-initiated wallet funding journey, including owner identity, requested amount, confirmed charged amount, status, timestamps, and external payment reference values needed for reconciliation.
- **Payment Return Evidence**: Information returned to Payslip4All after the external card-payment journey and evaluated for trustworthiness, matchability, and normalized outcome.
- **Unmatched Payment Return Record**: An auditable record of payment return evidence that could not be matched to exactly one known payment attempt, is surfaced through a generic not-confirmed result, and therefore cannot create a wallet credit or an attempt status.
- **Wallet Credit Activity**: An auditable wallet ledger entry showing that a successful hosted card payment increased the wallet balance.

### Assumptions

- The external payment page is fully hosted outside Payslip4All, and card entry never happens inside the app.
- Any fake or simulator hosted payment page is only a development, test, or demonstration support surface and is not a production in-app card-entry experience.
- A later feature will choose the specific gateway and provider-specific integration details.
- Refunds, chargebacks, partial captures after completion, and recurring payments are outside the scope of this feature.
- A payment attempt may remain pending or unverified until Payslip4All can determine a trustworthy final outcome from the payment return evidence or the attempt is treated as abandoned after 1 hour has passed since initiation.
- When payment return evidence cannot be matched to exactly one known attempt, the safe default is to credit no wallet and treat the data as unmatched until a person investigates it outside this feature.
- Abandonment is a timeout outcome rather than trustworthy final payment evidence, so later trustworthy matched final evidence may replace abandonment, but later low-confidence or conflicting evidence may not.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In acceptance testing of the standard wallet funding journey, 95% of company owners can start a valid card-based wallet top-up and reach the external hosted payment hand-off in under 1 minute from opening the wallet funding flow.
- **SC-002**: 100% of confirmed successful payment returns credit the wallet exactly once by the confirmed charged amount.
- **SC-003**: 100% of cancelled, expired, abandoned, unverified, or unmatched-return outcomes leave all wallet balances unchanged.
- **SC-004**: For 100% of matched payment attempts, company owners can see whether each of their own card top-up attempts is pending, completed, cancelled, expired, abandoned, or unverified within 5 minutes of Payslip4All determining that attempt outcome.
- **SC-005**: 100% of unmatched or uncorrelatable returns show a generic not-confirmed result that reveals no attempt identifier, owner identity, wallet details, or wallet credit confirmation and creates no wallet credit.
- **SC-006**: 100% of matched attempts that receive trustworthy final evidence after first being marked abandoned are reclassified to the corresponding final outcome within 5 minutes, preserve the earlier abandonment in the audit trail, and create at most one wallet credit only when the late trustworthy final evidence confirms completion.

### Success Criteria Verification Intent

- **SC-001 verification intent**: Measure the elapsed time from when a company owner opens the wallet funding flow to when Payslip4All hands the owner off to the external hosted payment page for a valid amount; this verifies the top-up start journey rather than payment completion.
- **SC-004 verification intent**: Measure the elapsed time from when Payslip4All determines the normalized outcome for a matched payment attempt to when that same attempt status is visible to the owning company owner in wallet funding history; unmatched returns are verified separately under SC-005 because they are not attempt statuses.
- **SC-006 verification intent**: Measure the elapsed time from when trustworthy matched final evidence is received after an abandonment decision to when the matched attempt shows the new final outcome, retains the prior abandonment in its audit history, and reflects the correct one-time wallet effect if the final outcome is completed.
