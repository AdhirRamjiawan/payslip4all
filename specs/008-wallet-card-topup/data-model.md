# Data Model: Generic Wallet Card Top-Up

## WalletTopUpAttempt

- **Purpose**: Represents one owner-initiated hosted wallet funding journey that may ultimately settle exactly one wallet credit if trustworthy matched success is accepted.
- **Primary fields**:
  - `Id`
  - `UserId`
  - `RequestedAmount`
  - `ConfirmedChargedAmount` (nullable until authoritative `Completed`)
  - `CurrencyCode` (`ZAR`)
  - `Status` (`Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, `Unverified`)
  - `ProviderKey`
  - `ProviderSessionReference` (nullable until hosted session registration succeeds)
  - `ProviderPaymentReference` (nullable until evidence supplies it)
  - `ReturnCorrelationToken` (nullable until hosted session registration succeeds)
  - `AbandonAfterUtc` (required; exactly 1 hour after initiation)
  - `OutcomeReasonCode` (nullable normalized reason code)
  - `OutcomeMessage` (nullable owner-safe/support-safe summary)
  - `AuthoritativeOutcomeAcceptedAt` (nullable until a final authoritative outcome is accepted)
  - `AuthoritativeEvidenceId` (nullable; source Payment Return Evidence for accepted trustworthy final outcome)
  - `CreditedWalletActivityId` (nullable until wallet settlement occurs)
  - `CreatedAt`
  - `UpdatedAt`
  - `RedirectedAt` (nullable)
  - `LastEvidenceReceivedAt` (nullable)
  - `LastEvaluatedAt` (nullable)
- **Relationships**:
  - Many `WalletTopUpAttempt` records belong to one authenticated owner identified by `UserId`.
  - One `WalletTopUpAttempt` may reference many `PaymentReturnEvidence` records.
  - One `WalletTopUpAttempt` may reference many `OutcomeNormalizationDecision` records.
  - One `WalletTopUpAttempt` may link to exactly one `WalletActivity` credit entry.
- **Validation rules**:
  - `UserId` is required and must match the authenticated owner initiating or viewing the attempt.
  - `RequestedAmount` must be greater than zero and limited to two-decimal rand precision.
  - `ConfirmedChargedAmount` must be null unless the authoritative status is `Completed`; when present it must be greater than zero and limited to two-decimal rand precision.
  - `Status` may only be `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, or `Unverified`.
  - `Failed` and `Unmatched` are invalid `WalletTopUpAttempt` statuses.
  - `AbandonAfterUtc` is always `CreatedAt + 1 hour`; it is authoritative and not configurable in this feature.
  - `CreditedWalletActivityId` may be non-null only when `Status = Completed`.
  - No PAN, CVV, expiry, reusable card token, or equivalent cardholder data may be stored.
- **State transitions**:
  - `Pending` on creation and while exactly one attempt is matched but no trustworthy final outcome is accepted.
  - `Pending -> Unverified` when matched evidence claims or implies a final outcome before abandonment but lacks enough trust to accept it.
  - `Pending -> Completed | Cancelled | Expired` when matched trustworthy final Payment Return Evidence is accepted.
  - `Pending -> Abandoned` when `currentTime >= AbandonAfterUtc` and no trustworthy final outcome has been accepted.
  - `Unverified -> Completed | Cancelled | Expired` when later trustworthy matched final evidence is accepted.
  - `Unverified -> Abandoned` when the exact 1-hour threshold is reached without accepted trustworthy final evidence.
  - `Abandoned -> Completed | Cancelled | Expired` only when later trustworthy matched final evidence is accepted for exactly that attempt.
  - `Abandoned` remains `Abandoned` when late evidence is low-confidence, unresolved, unmatched, or conflicting.
  - `Completed`, `Cancelled`, and `Expired` are authoritative matched final outcomes; later conflicting evidence is audited but does not change them.

## PaymentReturnEvidence

- **Purpose**: Persistent, provider-neutral record of each incoming item of Payment Return Evidence processed by Payslip4All.
- **Primary fields**:
  - `Id`
  - `ProviderKey`
  - `SourceChannel` (`BrowserReturn`, `ProviderCallback`, `ManualReplay` if later introduced; current feature expects browser/provider return handling only)
  - `ProviderSessionReference` (nullable)
  - `ProviderPaymentReference` (nullable)
  - `ReturnCorrelationToken` (nullable)
  - `MatchedAttemptId` (nullable)
  - `CorrelationDisposition` (`ExactMatch`, `NoMatch`, `MultipleMatches`, `MissingData`, `InvalidData`, `ConflictingData`)
  - `ClaimedOutcome` (`Completed`, `Cancelled`, `Expired`, `Unknown`, nullable when not claimed)
  - `TrustLevel` (`Trustworthy`, `LowConfidence`, `Untrusted`)
  - `ConfirmedChargedAmount` (nullable)
  - `EvidenceOccurredAt` (nullable if provider does not supply it)
  - `ReceivedAt`
  - `ValidatedAt`
  - `IsAtOrAfterAbandonmentThreshold`
  - `SafePayloadSnapshot`
  - `ValidationMessage`
- **Relationships**:
  - May belong to one `WalletTopUpAttempt` when correlation yields exactly one known attempt.
  - May create one `UnmatchedPaymentReturnRecord` when correlation is not exact.
  - May be referenced by one or more `OutcomeNormalizationDecision` records when replay or reevaluation is audited.
- **Validation rules**:
  - Must store only provider-safe metadata and no sensitive cardholder data.
  - `ConfirmedChargedAmount` is required only when trustworthy success is established.
  - `MatchedAttemptId` may be populated only when `CorrelationDisposition = ExactMatch`.
  - `IsAtOrAfterAbandonmentThreshold` must be computed against the matched attempt’s `AbandonAfterUtc` when exact match exists.
  - Evidence that cannot be matched to exactly one attempt remains evidence, but not an attempt status.
- **State transitions**:
  - Created on every return-handling pass.
  - Immutable after persistence except for support metadata outside this feature.

## OutcomeNormalizationDecision

- **Purpose**: Persistent audit record describing how Payslip4All converted Payment Return Evidence or a timeout event into the authoritative business result.
- **Primary fields**:
  - `Id`
  - `AttemptId` (nullable for unmatched-only decisions)
  - `PaymentReturnEvidenceId` (nullable for timeout-only abandonment decisions)
  - `UnmatchedPaymentReturnRecordId` (nullable)
  - `DecisionType` (`EvidenceEvaluation`, `AbandonmentTimeout`)
  - `AppliedPrecedence`
  - `NormalizedOutcome`
  - `AuthoritativeOutcomeBefore` (nullable)
  - `AuthoritativeOutcomeAfter` (nullable for unmatched-only flow)
  - `DecisionReasonCode`
  - `DecisionSummary`
  - `SupersededAbandonment` (boolean)
  - `ConflictWithAcceptedFinalOutcome` (boolean)
  - `WalletEffect` (`NoCredit`, `CreditCreated`, `CreditAlreadyPresent`)
  - `WalletActivityId` (nullable)
  - `DecidedAt`
- **Relationships**:
  - Belongs to one matched `WalletTopUpAttempt` when the decision is attempt-specific.
  - May reference one `PaymentReturnEvidence`.
  - May reference one `UnmatchedPaymentReturnRecord`.
  - May reference one `WalletActivity` when settlement is linked.
- **Validation rules**:
  - `AppliedPrecedence` must align with the documented normalization order.
  - Unmatched decisions must be recorded before any attempt outcome would otherwise be considered.
  - If `ConflictWithAcceptedFinalOutcome = true`, then `AuthoritativeOutcomeBefore` must equal `AuthoritativeOutcomeAfter`.
  - If `SupersededAbandonment = true`, then `AuthoritativeOutcomeBefore = Abandoned` and `AuthoritativeOutcomeAfter` must be `Completed`, `Cancelled`, or `Expired`.
  - `WalletActivityId` may be populated only when `WalletEffect` is `CreditCreated` or `CreditAlreadyPresent`.
- **State transitions**:
  - Created for each authoritative outcome decision or timeout decision.
  - Immutable after persistence; later evidence produces additional decisions rather than mutating history.

## UnmatchedPaymentReturnRecord

- **Purpose**: Auditable record for Payment Return Evidence that could not be matched to exactly one known attempt and therefore must remain outside matched attempt history and wallet settlement.
- **Primary fields**:
  - `Id`
  - `PrimaryEvidenceId`
  - `ProviderKey`
  - `CorrelationDisposition`
  - `GenericResultCode`
  - `DisplayMessage`
  - `SafePayloadSnapshot`
  - `ReceivedAt`
  - `CreatedAt`
- **Relationships**:
  - Created from one `PaymentReturnEvidence` item.
  - May be referenced by one `OutcomeNormalizationDecision`.
- **Validation rules**:
  - Must never imply or create a wallet credit.
  - Must never appear as an owner-visible attempt status.
  - Must support the generic unmatched result without revealing guessed attempt IDs, owner identity, wallet details, or wallet-credit confirmation.
  - Must preserve enough safe data for investigation and reconciliation.
- **State transitions**:
  - Created when correlation is not an exact one-attempt match.
  - Immutable except for external investigation metadata outside this feature.

## WalletActivity (existing ledger usage)

- **Purpose**: Existing wallet ledger entry model representing the one-time credit created by an accepted trustworthy successful matched outcome.
- **Feature-specific usage**:
  - `ActivityType = Credit`
  - `ReferenceType = "WalletTopUpAttempt"`
  - `ReferenceId = WalletTopUpAttempt.Id`
  - `Amount = ConfirmedChargedAmount`
  - Linked from `WalletTopUpAttempt.CreditedWalletActivityId`
  - Linked from `OutcomeNormalizationDecision.WalletActivityId`
- **Validation rules**:
  - At most one wallet activity may be linked to a matched completed top-up attempt.
  - The amount must equal the authoritative confirmed charged amount, never merely the requested amount.
  - No wallet activity may be created for `Pending`, `Cancelled`, `Expired`, `Abandoned`, `Unverified`, or unmatched cases.

## Wallet (existing aggregate impact)

- **Purpose**: Existing owner wallet aggregate whose balance changes only after authoritative settlement.
- **Feature-specific rules**:
  - Wallet balance increases only after trustworthy matched `Completed` is accepted and settled exactly once.
  - A wallet may be created lazily on the first successful top-up if current application behavior already supports that pattern.
  - `Cancelled`, `Expired`, `Abandoned`, `Unverified`, `Pending`, and unmatched flows leave the wallet unchanged.

## Derived Rules

- Payment Return Evidence must always be checked for exact-match viability before evaluating claimed payment outcome.
- The Normalized outcome for a matched attempt is deterministic across providers because precedence is owned by Application, not by provider-specific labels.
- Abandonment takes effect exactly when `currentTime >= AbandonAfterUtc`.
- Trustworthy matched final evidence outranks `Pending`, `Unverified`, and `Abandoned`.
- Low-confidence or unresolved late evidence cannot reopen an `Abandoned` attempt.
- Once a trustworthy matched final outcome is accepted, later conflicting evidence is audited only and does not change the authoritative outcome or wallet effect.
- Every authoritative outcome and every ignored or unmatched return must remain financially auditable through linked evidence, decisions, and wallet-credit references.
