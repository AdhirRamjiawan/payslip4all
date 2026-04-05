# Research: PayFast Card Integration

## Decision 1: Keep PayFast inside the existing hosted-payment provider seam

- **Decision**: Implement PayFast as an Infrastructure `IHostedPaymentProvider` and keep request signing, hosted-checkout construction, callback parsing, and remote validation out of Web and Application.
- **Rationale**: The codebase already routes top-ups through `WalletTopUpService`, provider abstractions, and repository interfaces. Reusing that seam preserves Clean Architecture and keeps Razor presentation-only.
- **Alternatives considered**:
  - **Call PayFast directly from Razor pages**: Rejected because it puts protocol logic in Web.
  - **Create a separate payment stack outside the existing seam**: Rejected because it duplicates current wallet-top-up orchestration.

## Decision 2: Server-side PayFast callback evidence is the only settlement authority

- **Decision**: Wallet credit may be authorized only from validated server-side PayFast callback evidence delivered to a public `notify_url`; browser returns remain informational only.
- **Rationale**: The clarified rules explicitly make callback evidence authoritative. This keeps settlement auditable and prevents browser interruption or manipulation from creating wallet credit.
- **Alternatives considered**:
  - **Credit on browser success indicators**: Rejected as unsafe.
  - **Blend browser and callback signals as equal authority**: Rejected because it weakens the financial boundary.

## Decision 3: Enforce card-only both when starting checkout and when accepting settlement

- **Decision**: Every PayFast checkout request must explicitly restrict payment to cards, and inbound evidence must still prove a card payment before settlement is accepted.
- **Rationale**: Card-only is an end-to-end business invariant, not only a checkout preference. The system must refuse non-card evidence even if a gateway or merchant setting drifts.
- **Alternatives considered**:
  - **Rely on merchant-account defaults**: Rejected because the business rule becomes implicit.
  - **Allow other methods and reject only in UI**: Rejected because settlement safety must not depend on UI behavior.

## Decision 4: Successful settlement requires exact business correlation

- **Decision**: Accept authoritative success only when `m_payment_id` maps to exactly one eligible attempt (`Pending`, `Expired`, `Abandoned`, or `NotConfirmed`) for the same owner and currency and the confirmed charged amount is within R50-R1000.
- **Rationale**: The refined spec allows late authoritative correction of interim non-crediting outcomes while still preventing cross-owner or already-finalized settlement. This keeps exact-match safety strict without blocking valid late server-side confirmation.
- **Alternatives considered**:
  - **Match only by `m_payment_id`**: Rejected because it ignores owner and currency safety.
  - **Limit success to `Pending` only**: Rejected because the spec permits later authoritative completion or cancellation for interim non-crediting states.
  - **Trust the requested amount instead of the confirmed amount**: Rejected because the confirmed charged amount is settlement truth.

## Decision 5: Standardize planning vocabulary on `NotConfirmed`

- **Decision**: Planning artifacts use `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, and `NotConfirmed`, with implementation expected to map or migrate the current legacy `Unverified` terminology.
- **Rationale**: The refined feature spec consistently uses `not-confirmed`. Aligning the plan, contracts, quickstart, and data model prevents later task drift.
- **Alternatives considered**:
  - **Keep `Unverified` as the primary term**: Rejected because it diverges from the spec.
  - **Reuse `Failed` as a catch-all**: Rejected because it is too broad and conflicts with current rules.

## Decision 6: DynamoDB startup must explicitly provision or verify every payment table and log the outcome

- **Decision**: When `PERSISTENCE_PROVIDER=dynamodb`, startup must verify or create every required payment table — `wallet_topup_attempts`, `payment_return_evidences`, `outcome_normalization_decisions`, and `unmatched_payment_return_records` — plus the dependent wallet tables, and log whether each table was created or already confirmed.
- **Rationale**: Constitution Principle V requires startup auto-creation plus logging for required DynamoDB tables. Treating payment stores as explicit startup responsibilities closes the remaining constitutional gap and prevents a design that only works after manual table setup.
- **Alternatives considered**:
  - **Assume operators create payment tables manually**: Rejected because it violates the constitution.
  - **Create missing tables without logging**: Rejected because startup evidence is part of the constitutional requirement.

## Decision 7: SC-002 evidence must cover owner history visibility, not just wallet balance

- **Decision**: A successful authoritative callback must update the wallet credit, wallet balance, and owner history entry in the same unit of work, and verification must measure time-to-visibility for both balance and history within 1 minute.
- **Rationale**: The success criterion explicitly mentions both balance and history. Treating history freshness as first-class evidence prevents a design where the ledger updates promptly but the owner-facing history lags invisibly.
- **Alternatives considered**:
  - **Measure only wallet balance latency**: Rejected because it does not satisfy SC-002.
  - **Allow asynchronous history projection lag without a bound**: Rejected because it would make SC-002 unverifiable.

## Decision 8: Make abandonment explicit with a scheduled sweep plus read-through reconciliation

- **Decision**: Use a minute-interval reconciliation workflow to evaluate due attempts. The first due pass at or after the hosted deadline records `Expired`; a later due pass with no trustworthy completion or cancellation records `Abandoned`. Owner history/result reads also invoke the same reconciliation path when an attempt is overdue, so UI reads do not remain stale if the scheduler misses a cycle.
- **Rationale**: The spec defines `abandoned` as a later follow-up outcome rather than an immediate synonym for expiry. A hybrid scheduled + read-through mechanism makes that lifecycle explicit, testable, and visible in the architecture.
- **Alternatives considered**:
  - **Record `Abandoned` immediately at the deadline**: Rejected because it collapses `Expired` and `Abandoned`.
  - **Rely only on manual support checks**: Rejected because it leaves the lifecycle undefined and untestable.

## Decision 9: Only completed and cancelled are terminal authoritative outcomes

- **Decision**: `Completed` and `Cancelled` stay terminal authoritative outcomes, but `Expired`, `Abandoned`, and `NotConfirmed` remain interim non-crediting outcomes that may be superseded only by later validated server-side completion or cancellation for the same exact-matched attempt.
- **Rationale**: FR-016 and FR-027 explicitly permit later authoritative promotion from interim non-crediting states while still preserving finality for already authoritative completed or cancelled outcomes. This preserves safety without discarding trustworthy late notify evidence.
- **Alternatives considered**:
  - **Treat every non-pending state as permanently final**: Rejected because it conflicts with the refined feature spec.
  - **Allow late evidence to overwrite completed or cancelled outcomes**: Rejected because the first authoritative final outcome must remain stable.
  - **Ignore late evidence completely**: Rejected because auditability and legitimate late authoritative correction still matter.

## Decision 10: Use one generic owner-safe outcome for unsafe disclosure cases

- **Decision**: Unmatched, foreign-owner, duplicate-finalized, browser-only unsafe, callback-missing, and otherwise undisclosable cases all surface `Top-up not confirmed`.
- **Rationale**: A single generic message prevents the return flow from leaking whether another owner’s attempt exists or whether a payment already finalized.
- **Alternatives considered**:
  - **Tailor messages per unsafe case**: Rejected because it can reveal operational detail.
  - **Expose raw provider diagnostics to end users**: Rejected by FR-018 and FR-020.

## Decision 11: Persist only non-sensitive PayFast evidence

- **Decision**: Persist only the non-sensitive PayFast evidence needed for audit, correlation, and reconciliation; never store, display, or log PAN, CVV, expiry, or raw gateway diagnostics.
- **Rationale**: FR-018 is explicit and non-negotiable. Payslip4All needs durable auditability without broad payload dumping.
- **Alternatives considered**:
  - **Store full callback payloads for debugging**: Rejected because sensitive data and raw diagnostics are prohibited.
  - **Avoid persisting evidence at all**: Rejected because SC-005 requires traceability.

## Decision 12: Preserve identical behavior across EF Core and DynamoDB providers

- **Decision**: Owner filtering, evidence persistence, reconciliation transitions, history freshness, and exactly-once settlement must behave the same in relational and DynamoDB paths.
- **Rationale**: FR-019 and Constitution Principle V require behavior parity, not merely interface parity. Planning parity now prevents a relational-first design that would fail later.
- **Alternatives considered**:
  - **Implement relational first and defer DynamoDB details**: Rejected because it knowingly breaks parity.
  - **Keep reconciliation or history freshness provider-specific**: Rejected because SC-002 and FR-019 must hold everywhere.

## Decision 13: Callback delivery failure or merchant misconfiguration must remain non-crediting and owner-safe

- **Decision**: If the public `notify_url` is unreachable, callback validation fails, or merchant configuration prevents hosted checkout start, the flow stays non-crediting and owner-safe.
- **Rationale**: FR-020 does not permit best-effort settlement without trustworthy callback evidence. Operational failure must degrade to safe non-crediting behavior.
- **Alternatives considered**:
  - **Fallback to browser-return authority**: Rejected because it violates callback-authoritative settlement.
  - **Pretend checkout started successfully and reconcile later**: Rejected because it creates misleading owner expectations.

## Decision 14: Match PayFast signature construction to the provider’s form-encoding rules

- **Decision**: Generate hosted-checkout signatures and verify notify signatures from trimmed non-empty non-signature fields using PayFast-compatible form encoding, then append the configured passphrase only when present.
- **Rationale**: The recent hosted-flow debugging showed that small differences in encoding, field filtering, or passphrase handling can break both sandbox acceptance and callback trust evaluation. Making the signature recipe explicit keeps the plan aligned with the working provider implementation.
- **Alternatives considered**:
  - **Use generic URL-encoding helpers without a PayFast-specific normalization step**: Rejected because it risks mismatched signatures.
  - **Treat signature handling as an implementation detail only**: Rejected because it is part of the integration’s correctness boundary.

## Decision 15: Browser returns stay owner-safe correlation signals, not settlement signals

- **Decision**: Keep browser returns limited to correlation, evidence capture, and redirecting either to an owner-scoped result route or the generic `Top-up not confirmed` route; they never authorize wallet credit or authoritative success messaging by themselves.
- **Rationale**: Recent debugging reaffirmed that the browser path is useful for UX continuity but not trustworthy enough for settlement. Documenting this explicitly prevents the return route from drifting into a pseudo-settlement path.
- **Alternatives considered**:
  - **Show browser-return success as if payment is complete**: Rejected because callback-backed confirmation is still required.
  - **Ignore browser returns entirely**: Rejected because owner-safe correlation and UX still matter.

## Decision 16: Trustworthy PayFast notify handling requires local verification plus PayFast step-4 confirmation

- **Decision**: Treat a PayFast notify payload as trustworthy only after local signature verification, card-only evaluation, environment checks, and successful server confirmation against the PayFast validation endpoint.
- **Rationale**: The latest debugging centered on step-4 notify validation and showed that local parsing alone is not enough. Capturing the full trust chain in research keeps the implementation and contracts centered on the same authoritative rule.
- **Alternatives considered**:
  - **Trust a notify payload after local signature verification alone**: Rejected because the feature requires server-side confirmation.
  - **Skip local checks and rely only on remote validation**: Rejected because signature and payment-method checks are still part of Payslip4All’s acceptance criteria.

## Decision 17: Sandbox acceptance is configuration-bound; production settlement remains live-only

- **Decision**: Allow sandbox checkout and callback validation only when PayFast sandbox mode is explicitly configured for non-production validation, while preserving the rule that production wallet settlement must reject non-live evidence.
- **Rationale**: The current feature work included sandbox-flow debugging, so planning must preserve that practical test path without weakening FR-011. This keeps developer/UAT feedback loops working while maintaining a strict live-only production boundary.
- **Alternatives considered**:
  - **Reject sandbox end-to-end in every environment**: Rejected because it blocks safe validation of the real hosted flow.
  - **Accept sandbox evidence in production paths**: Rejected because it violates the production-trust boundary.

## Decision 18: Standardize Payment Confirmation Record terminology on validated PayFast notify evidence

- **Decision**: Use **Payment Confirmation Record** as the cross-artifact term for the authoritative settlement artifact, implemented as validated `PaymentReturnEvidence` from `SourceChannel = PayFastNotify` with successful local verification, PayFast confirmation, and server-confirmed trust.
- **Rationale**: The refined spec names this artifact explicitly, and consistent terminology across the plan, data model, contracts, and quickstart reduces drift between business language and implementation language.
- **Alternatives considered**:
  - **Use `PaymentReturnEvidence` everywhere without a business term**: Rejected because it hides the settlement boundary from reviewers.
  - **Create a second authoritative persistence concept in planning**: Rejected because it would duplicate the same audit artifact.

## Decision 19: Internal review is admin-only and privacy-minimized

- **Decision**: Introduce a `SiteAdministrator`-only internal review surface for conflicting evidence, unmatched returns, and settlement traceability, exposing only minimum non-sensitive evidence needed for reconciliation and troubleshooting.
- **Rationale**: FR-013 and the refined clarifications require an internal review path without broadening owner-visible disclosure. Keeping the review DTO/page explicitly privacy-minimized avoids accidental leakage of raw gateway data while still supporting operations.
- **Alternatives considered**:
  - **Expose evidence on owner-facing pages**: Rejected because it violates the privacy-safe messaging model.
  - **Allow all authenticated users to access review data**: Rejected because the spec reserves it for `SiteAdministrator` users only.

## Decision 20: Every affected Blazor page needs explicit component-test coverage

- **Decision**: Plan bUnit coverage for `/portal/wallet`, `/portal/wallet/top-ups/return`, `/portal/wallet/top-ups/{attemptId}/return`, `/portal/wallet/top-ups/return/not-confirmed`, and any new internal review Razor page introduced for this feature.
- **Rationale**: The constitution and the refined spec both call out component-test expectations for affected Blazor pages. Making this explicit in planning prevents a later implementation plan that covers services but skips page-level acceptance behavior.
- **Alternatives considered**:
  - **Rely on integration tests only**: Rejected because the constitution explicitly requires component tests for Blazor page components.
  - **Test only owner-facing pages**: Rejected because any internal review page introduced by the feature must also be component-testable.
