# Data Model: PayFast Card Integration

## WalletTopUpAttempt

- **Purpose**: Owner-scoped record for one PayFast wallet top-up from initiation through authoritative outcome and, at most, one wallet credit.
- **Existing implementation anchor**: `WalletTopUpAttempt`
- **Primary fields**:
  - `Id`
  - `UserId`
  - `RequestedAmount`
  - `ConfirmedChargedAmount` (nullable until authoritative completion)
  - `CurrencyCode` (`ZAR`)
  - `Status` (`Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, `NotConfirmed`; legacy `Unverified` must be mapped or migrated)
  - `ProviderKey` (`payfast`)
  - `ProviderSessionReference`
  - `ProviderPaymentReference` (`pf_payment_id`, nullable until evidence arrives)
  - `MerchantPaymentReference` (`m_payment_id`)
  - `ReturnCorrelationToken`
  - `HostedPageDeadline`
  - `NextReconciliationDueAt`
  - `ExpiredAt`
  - `AbandonedAt`
  - `LastReconciledAt`
  - `OutcomeReasonCode`
  - `OutcomeMessage`
  - `CreditedWalletActivityId`
  - `AuthoritativeEvidenceId`
  - `CreatedAt`, `UpdatedAt`, `RedirectedAt`, `LastValidatedAt`, `LastEvaluatedAt`, `LastEvidenceReceivedAt`, `CompletedAt`, `AuthoritativeOutcomeAcceptedAt`
- **Relationships**:
  - One `CompanyOwner` has many top-up attempts.
  - One attempt may reference many payment evidence records.
  - One attempt may reference many normalization decisions.
  - One attempt links to at most one wallet credit activity.
- **Validation rules**:
  - `UserId` must always match the authenticated owner for owner-visible reads and owner-initiated writes.
  - `RequestedAmount` must be R50-R1000 inclusive with two-decimal ZAR precision.
  - `CurrencyCode` is `ZAR` for this feature and must match the authoritative confirmation currency.
  - `ConfirmedChargedAmount`, when present, must also be within R50-R1000 inclusive.
  - A valid settlement candidate exists only when `m_payment_id` maps to exactly one eligible attempt in `Pending`, `Expired`, `Abandoned`, or `NotConfirmed` for the same owner and currency and the confirmed amount is in range.
  - `NextReconciliationDueAt` is initialized to `HostedPageDeadline`; after `Expired` it is advanced for the later abandonment follow-up; it is cleared after a final settled/disqualified state.
  - No PAN, CVV, expiry, or raw gateway diagnostics may be stored on the attempt.
- **State transitions**:
  - `Pending -> Completed` only after validated server-side PayFast callback evidence proves exact match, same owner, same currency, confirmed amount in range, live path, PayFast confirmation, and card-only settlement.
  - `Expired -> Completed`, `Abandoned -> Completed`, and `NotConfirmed -> Completed` are allowed only when later validated server-side PayFast callback evidence proves the same exact-matched attempt completed successfully.
  - `Pending -> Cancelled` when trustworthy callback evidence confirms customer or gateway cancellation.
  - `Expired -> Cancelled`, `Abandoned -> Cancelled`, and `NotConfirmed -> Cancelled` are allowed only when later validated server-side callback evidence proves cancellation for the same exact-matched attempt.
  - `Pending -> Expired` when scheduled or read-through reconciliation runs at or after `HostedPageDeadline` without trustworthy completion or cancellation.
  - `Expired -> Abandoned` when a later scheduled or read-through reconciliation still finds no trustworthy completion or cancellation.
  - `Pending -> NotConfirmed` when browser-only or otherwise unsafe evidence is processed for owner-visible outcome purposes but settlement authority is still absent.
  - `NotConfirmed -> Expired` is allowed when reconciliation reaches the hosted deadline without authoritative completion or cancellation.
  - `Completed` and `Cancelled` are terminal authoritative outcomes; duplicate or conflicting later callbacks after those states are audit-only and may trigger internal review but never additional credit.

## PaymentConfirmationRecord

- **Purpose**: The authoritative server-side PayFast callback evidence that may authorize settlement.
- **Implementation note**: Standardized planning term for a validated `PaymentReturnEvidence` row whose `SourceChannel = PayFastNotify`, `SignatureVerified = true`, `SourceVerified = true`, and `ServerConfirmed = true`.
- **Primary fields**:
  - `PaymentReturnEvidenceId`
  - `MatchedAttemptId`
  - `MerchantPaymentReference`
  - `ProviderPaymentReference`
  - `PaymentStatus`
  - `PaymentMethodCode`
  - `ConfirmedChargedAmount`
  - `ConfirmedCurrencyCode`
  - `SignatureVerified`
  - `SourceVerified`
  - `ServerConfirmed`
  - `ValidatedAt`
  - `SafePayloadSnapshot`
- **Validation rules**:
  - Callback evidence must arrive through a publicly reachable `notify_url`.
  - Callback evidence is authoritative only after server-side validation succeeds.
  - Settlement may proceed only if card-only, live-path, exact-match, amount-range, and PayFast confirmation rules all pass.
  - Safe payload data may include correlation and audit fields only; no sensitive card data or raw gateway diagnostics are retained.

## PaymentReturnEvidence

- **Purpose**: Durable provider-safe record of every inbound PayFast/browser payload considered during correlation, settlement, and reconciliation.
- **Existing implementation anchor**: `PaymentReturnEvidence`
- **Primary fields**:
  - `Id`
  - `ProviderKey`
  - `SourceChannel` (`BrowserReturn`, `PayFastNotify`, `StatusCheck`, `ManualReplay`)
  - `ProviderSessionReference`
  - `ProviderPaymentReference` (`pf_payment_id`)
  - `MerchantPaymentReference` (`m_payment_id`)
  - `ReturnCorrelationToken`
  - `MatchedAttemptId` (nullable until a safe exact match exists)
  - `CorrelationDisposition` (`ExactMatch`, `NoMatch`, `MultipleMatches`, `MissingData`, `InvalidData`, `ConflictingData`, `ForeignOwner`, `DuplicateFinalized`)
  - `ClaimedOutcome` (`Completed`, `Cancelled`, `Expired`, `Unknown`)
  - `TrustLevel` (`Trustworthy`, `LowConfidence`, `Untrusted`)
  - `PaymentMethodCode`
  - `EnvironmentMode` (`Live`, `Sandbox`, `Fake`)
  - `SignatureVerified`
  - `SourceVerified`
  - `ServerConfirmed`
  - `ConfirmedChargedAmount`
  - `ConfirmedCurrencyCode`
  - `EvidenceOccurredAt`
  - `ReceivedAt`
  - `ValidatedAt`
  - `SafePayloadSnapshot`
  - `ValidationMessage`
- **Relationships**:
  - Many evidence records may relate to one top-up attempt.
  - One evidence record may become the authoritative evidence for a completed or cancelled attempt.
  - One evidence record may create one `UnmatchedPaymentReturnRecord`.
- **Validation rules**:
  - Browser-return evidence may be stored for audit and UX, but it may never authorize wallet credit by itself.
  - `TrustLevel = Trustworthy` requires successful PayFast validation checks on the server path, including local signature verification and PayFast step-4 server confirmation.
  - Successful settlement requires `PaymentMethodCode` to prove card-only confirmation.
  - Successful settlement requires `EnvironmentMode = Live` in production; sandbox evidence is accepted only when the environment is explicitly configured for sandbox validation.
  - `ConfirmedChargedAmount` and `ConfirmedCurrencyCode` must be checked against the matched eligible attempt before success is accepted.
  - `SafePayloadSnapshot` must exclude PAN, CVV, expiry, and raw gateway diagnostics.

## OutcomeNormalizationDecision

- **Purpose**: Application-owned audit record describing how Payslip4All interpreted evidence or a timeout/reconciliation event and whether the wallet may be credited.
- **Existing implementation anchor**: `OutcomeNormalizationDecision`
- **Primary fields**:
  - `Id`
  - `AttemptId`
  - `PaymentReturnEvidenceId`
  - `UnmatchedPaymentReturnRecordId`
  - `DecisionType`
  - `TriggerSource` (`AuthoritativeCallback`, `ScheduledSweep`, `ReadThroughHistory`, `ReadThroughResult`, `BrowserReturn`)
  - `AppliedPrecedence`
  - `NormalizedOutcome`
  - `AuthoritativeOutcomeBefore`
  - `AuthoritativeOutcomeAfter`
  - `DecisionReasonCode`
  - `DecisionSummary`
  - `ConflictWithAcceptedFinalOutcome`
  - `SupersededNotConfirmed`
  - `WalletEffect` (`NoCredit`, `CreditCreated`, `CreditAlreadyPresent`)
  - `WalletActivityId`
  - `DecidedAt`
- **Validation rules**:
  - No authoritative success is possible before exact match, same owner, same currency, confirmed amount in range, and callback trust are proven.
  - Browser-only evidence must always normalize to `NoCredit`.
  - Unmatched, foreign-owner, duplicate-finalized, non-card, non-live, callback-missing, late-finalized, or otherwise unsafe evidence must normalize to `NoCredit`.
  - Expiry and abandonment are distinct decisions: deadline expiry first, later follow-up abandonment second.
  - `TriggerSource` must make the abandonment mechanism auditable.
  - Unsafe-to-disclose cases must map to the same owner-facing `Top-up not confirmed` result family.

## WalletCreditRecord

- **Purpose**: Exactly-once wallet ledger effect created only from a trustworthy successful PayFast result.
- **Existing implementation anchor**: `WalletActivity` plus `Wallet`
- **Primary fields**:
  - `Id`
  - `WalletId`
  - `ActivityType` (`Credit`)
  - `Amount`
  - `ReferenceType` (`WalletTopUpAttempt`)
  - `ReferenceId` (`WalletTopUpAttempt.Id`)
  - `BalanceAfterActivity`
  - `OccurredAt`
- **Validation rules**:
  - Created only after validated server-side callback success.
  - `Amount` must equal the confirmed charged amount, not merely the requested amount.
  - At most one wallet credit may exist per top-up attempt across relational and DynamoDB providers.
  - Every successful wallet credit must be traceable to one authoritative confirmation record and one originating attempt.

## UnmatchedPaymentReturnRecord

- **Purpose**: Privacy-safe audit record for evidence that cannot be disclosed safely as a specific owner’s top-up result.
- **Existing implementation anchor**: `UnmatchedPaymentReturnRecord`
- **Primary fields**:
  - `Id`
  - `PrimaryEvidenceId`
  - `ProviderKey`
  - `CorrelationDisposition`
  - `GenericResultCode` (`not_confirmed`)
  - `DisplayMessage` (`Top-up not confirmed`)
  - `SafePayloadSnapshot`
  - `ReceivedAt`
  - `CreatedAt`
- **Validation rules**:
  - Must never create or imply wallet credit.
  - Must use the same owner-safe outcome for unmatched, foreign-owner, duplicate-finalized, callback-missing, and other unsafe-to-disclose cases.
  - Must not reveal whether any specific attempt exists or already completed.

## OwnerTopUpHistoryView (derived)

- **Purpose**: Owner-facing history is a derived read model built from `WalletTopUpAttempt` records plus wallet balance/credit links; no separate projection is required for this feature.
- **Derived fields**:
  - newest-first attempt list
  - requested amount
  - confirmed amount when present
  - current status
  - wallet-credit indicator
  - owner-safe display message
- **Freshness rules**:
  - Authoritative callback processing must commit attempt update, normalization decision, wallet activity, and wallet balance in one unit of work.
  - History queries must reflect successful callback outcomes within 1 minute of callback receipt.
  - When a listed attempt is overdue (`NextReconciliationDueAt <= now`) and still unresolved, history reads invoke read-through reconciliation before returning results.

## SiteAdministratorPaymentReviewView (derived)

- **Purpose**: Internal read model for `SiteAdministrator`-only troubleshooting and reconciliation review with minimum non-sensitive evidence exposure.
- **Derived fields**:
  - attempt identifier
  - owner/user identifier
  - requested amount
  - confirmed amount when available
  - authoritative status
  - `PaymentConfirmationRecordId`
  - normalization decision summary and reason code
  - `ConflictWithAcceptedFinalOutcome`
  - correlation disposition
  - wallet activity identifier when credit exists
  - safe correlation references (`m_payment_id`, `pf_payment_id`)
  - timestamps needed for reconciliation
- **Validation rules**:
  - Available only to `SiteAdministrator` users.
  - Must exclude PAN, CVV, expiry, raw gateway diagnostics, and secrets.
  - Must support review of unmatched returns and conflicting late evidence without mutating settlement state.
  - Must render only fields already classified as safe in persisted evidence.

## Cross-Provider Persistence Rules

- Relational and DynamoDB implementations must enforce identical owner filtering on start, history, result, and reconciliation flows.
- Relational and DynamoDB implementations must persist the same safe evidence fields and the same normalization decisions.
- Relational and DynamoDB implementations must enforce exactly-once wallet credit creation for the same attempt.
- DynamoDB startup must auto-create or verify the required payment tables and log whether each table was created or already available.
- Any new relational schema field needed for this model must be added through an EF Core migration and mirrored in DynamoDB repository serialization.
- Relational and DynamoDB implementations must support the same admin-only review queries over safe evidence fields.

## Derived Rules

- Card-only is a cross-artifact invariant: request construction, evidence validation, normalization, and settlement all enforce credit-card-only behavior.
- Amount bounds are a cross-artifact invariant: UI, Domain, Application, Infrastructure validation, and tests all enforce R50-R1000 inclusive.
- Wallet credit authority is a cross-artifact invariant: only validated server-side PayFast callback evidence may create credit.
- Browser returns are always informational and owner-safe.
- Callback delivery failure or merchant misconfiguration is always non-crediting.
- Exact-match safety is a cross-artifact invariant: one eligible attempt (`Pending`, `Expired`, `Abandoned`, or `NotConfirmed`), same owner, same currency, confirmed amount in range.
- `Expired` and `Abandoned` are distinct lifecycle checkpoints driven by explicit reconciliation triggers.
- `Completed` and `Cancelled` are the only terminal authoritative outcomes; `Expired`, `Abandoned`, and `NotConfirmed` remain non-crediting interim states until valid later notify evidence proves otherwise.
- Wallet settlement remains exactly-once across both EF Core and DynamoDB paths.
- SC-002 evidence must prove both balance visibility and history visibility within 1 minute.
- Internal review is admin-only and privacy-minimized across all artifacts.
