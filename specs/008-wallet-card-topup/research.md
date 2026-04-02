# Research: Generic Wallet Card Top-Up

## Decision 1: Standardize the core planning vocabulary

- **Decision**: Use the exact terms **Payment Return Evidence**, **Normalized outcome**, and **Unmatched Payment Return Record** throughout the design artifacts. Use `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, and `Unverified` only for matched attempt statuses, and never use `Failed` as an attempt status or outcome in this feature.
- **Rationale**: The refined spec now depends on precise distinctions between matched attempt outcomes, unmatched records, and the evidence items that lead to them. Shared terminology prevents the provider abstraction, audit model, UI, and implementation tasks from drifting into inconsistent naming that would weaken financial traceability.
- **Alternatives considered**:
  - **Keep legacy "failed" wording for generic negative outcomes**: Rejected because the refined spec explicitly removes `failed`.
  - **Use "unmatched outcome" as an attempt status**: Rejected because unmatched returns remain separate auditable records, not attempt states.

## Decision 2: Normalize with a single application-owned evidence-precedence policy

- **Decision**: Evaluate every incoming Payment Return Evidence item with one deterministic application-owned policy in this order: (1) exact-match viability, (2) trustworthy matched final evidence for `Completed`, `Cancelled`, or `Expired`, (3) accepted final-outcome lock, (4) low-confidence claimed final evidence, (5) still-unresolved matched evidence, and (6) abandonment baseline when the exact 1-hour threshold has been reached.
- **Rationale**: The spec now requires explicit evidence-precedence rules across providers. Centralizing the policy in Application keeps provider-specific parsing in Infrastructure while guaranteeing that equivalent evidence produces the same Normalized outcome, wallet effect, and user-visible result regardless of provider wording.
- **Alternatives considered**:
  - **Let each provider map directly to business outcomes**: Rejected because it would break provider-agnostic behavior.
  - **Choose outcome before checking matchability**: Rejected because FR-022 requires unmatched handling before claimed outcome evaluation.

## Decision 3: Treat the 1-hour abandonment threshold as an inclusive cutoff

- **Decision**: Store `AbandonAfterUtc = InitiatedAt + 1 hour`, and treat `currentTime >= AbandonAfterUtc` as the point at which `Abandoned` becomes the baseline authoritative state unless higher-precedence trustworthy matched final evidence is being accepted for that same evaluation.
- **Rationale**: The refined spec asks for explicit handling around the exact threshold, not just vague "later" behavior. An inclusive cutoff removes ambiguity for evidence arriving exactly at the 1-hour mark and allows tests to deterministically distinguish pre-threshold `Pending` / `Unverified` behavior from post-threshold `Abandoned` behavior.
- **Alternatives considered**:
  - **Use only `currentTime > AbandonAfterUtc`**: Rejected because it leaves the exact threshold ambiguous.
  - **Use provider hosted-page expiry as the abandonment trigger**: Rejected because abandonment is an application rule fixed by spec.

## Decision 4: Allow trustworthy matched late final evidence to supersede `Abandoned`

- **Decision**: If a matched attempt is `Abandoned` because the threshold was reached, later trustworthy final Payment Return Evidence for exactly that attempt may replace `Abandoned` with `Completed`, `Cancelled`, or `Expired`. If the accepted late final evidence is trustworthy success, the wallet is credited exactly once by the confirmed charged amount and linked back to the authoritative evidence trail.
- **Rationale**: The spec explicitly says abandonment is not trustworthy final payment evidence and may be superseded by matched trustworthy late evidence. This keeps the business outcome financially correct without hiding the fact that the system previously timed the attempt out.
- **Alternatives considered**:
  - **Freeze `Abandoned` permanently**: Rejected because it would violate FR-023 and SC-006.
  - **Allow any late evidence to reopen the attempt**: Rejected because only trustworthy matched final evidence may supersede abandonment.

## Decision 5: Low-confidence or conflicting late evidence cannot reopen `Abandoned`

- **Decision**: If evidence arrives at or after the abandonment threshold and does not provide trustworthy final evidence for exactly one known attempt, the attempt remains `Abandoned`. The late evidence is still recorded, but it does not reopen the attempt, credit a wallet, or displace the authoritative outcome.
- **Rationale**: The refined spec now distinguishes late trustworthy correction from late low-confidence noise. That separation keeps the system safe against ambiguous, tampered, or weakly supported late returns while preserving a defensible audit trail for later investigation.
- **Alternatives considered**:
  - **Downgrade `Abandoned` back to `Pending` or `Unverified` on any new signal**: Rejected because it would create unstable owner-visible outcomes.
  - **Discard late low-confidence evidence entirely**: Rejected because the spec requires auditability even when evidence is ignored operationally.

## Decision 6: Once a trustworthy final outcome is accepted, it stays authoritative

- **Decision**: The first accepted trustworthy final outcome for a matched attempt becomes authoritative. Later conflicting Payment Return Evidence is recorded and audited, but it cannot change the authoritative outcome or create, reverse, or duplicate a wallet credit within this feature.
- **Rationale**: The refined spec now explicitly locks the accepted trustworthy final outcome. This provides deterministic user-visible history and prevents financially dangerous oscillation when late or replayed provider signals conflict with an already accepted final settlement.
- **Alternatives considered**:
  - **Always accept the most recent evidence**: Rejected because later evidence may be contradictory, replayed, or lower quality.
  - **Reverse wallet credits automatically on later conflicts**: Rejected because refunds and reversals are out of scope for this feature.

## Decision 7: Persist both evidence items and decision records for financially credible audit

- **Decision**: Persist every handled Payment Return Evidence item plus a separate Normalized outcome decision record that captures precedence applied, authoritative outcome before/after, whether abandonment was superseded, and any wallet-credit linkage. Persist each unmatched case as its own Unmatched Payment Return Record tied back to the source evidence.
- **Rationale**: FR-017 now requires more than a final status snapshot. A financially credible audit trail must explain not only what outcome exists now, but why it was accepted, what evidence was ignored or superseded, and which wallet credit entry corresponds to the accepted trustworthy successful evidence.
- **Alternatives considered**:
  - **Store only the latest attempt status and wallet activity link**: Rejected because it cannot explain superseded abandonment, ignored conflicts, or unmatched evidence.
  - **Store one flattened audit blob on the attempt**: Rejected because it weakens queryability and makes unmatched records awkward.

## Decision 8: Keep unmatched processing generic and privacy-preserving

- **Decision**: Route returns through a generic inbound handler first. If the evidence cannot be matched to exactly one known attempt, create an Unmatched Payment Return Record and send the user to a generic not-confirmed result that does not reveal a guessed attempt identifier, whether a guessed attempt exists, owner identity, wallet details, or wallet-credit confirmation.
- **Rationale**: The refined unmatched flow is both a security and privacy requirement. A generic result avoids turning correlation failures into an oracle that leaks payment-attempt existence or other-owner wallet information.
- **Alternatives considered**:
  - **Return directly to an attempt-specific URL and show "not found" on mismatch**: Rejected because the route itself leaks guessed identifiers.
  - **Show wallet balance or owner name on unmatched results for reassurance**: Rejected because the spec explicitly forbids this leakage.

## Decision 9: Keep exactly-once wallet settlement anchored to the matched attempt

- **Decision**: Use the matched `WalletTopUpAttempt` as the idempotency anchor for settlement on both EF Core and DynamoDB paths. The accepted trustworthy successful evidence may create at most one wallet credit and one wallet-credit linkage, even when returns are replayed or reprocessed.
- **Rationale**: Successful replays and conflicting evidence are explicitly in scope, and the constitution requires parity across approved persistence providers. Attempt-anchored idempotency keeps the authoritative business record and the ledger entry aligned.
- **Alternatives considered**:
  - **Deduplicate only by provider payment reference**: Rejected because provider identifiers alone do not express the application’s authoritative settlement boundary.
  - **Rely on UI refresh protection only**: Rejected because replay protection must live in persistence and application logic.

## Decision 10: Keep the provider seam generic and simulator-only for non-production card entry

- **Decision**: Continue using a provider abstraction that emits provider-neutral Payment Return Evidence and hosted-session details, while reserving any fake or simulator hosted page for development, testing, and demonstrations only. Real production card entry remains entirely outside Payslip4All.
- **Rationale**: The feature must remain provider-agnostic until a later gateway-selection feature, and the security boundary around card entry must not be blurred by simulator convenience. This keeps the design aligned with both the refined spec and the constitution’s separation-of-concerns rules.
- **Alternatives considered**:
  - **Bake gateway-specific semantics into the application contract now**: Rejected because gateway selection is explicitly deferred.
  - **Let the simulator act like a production in-app payment page**: Rejected because real customer card entry must remain external.
