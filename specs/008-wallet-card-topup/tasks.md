# Tasks: Generic Wallet Card Top-Up

**Input**: Design documents from `/Users/adhirramjiawan/projects/payslip4all/specs/008-wallet-card-topup/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/`

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for this feature. Write failing xUnit, Moq, repository integration, DynamoDB parity, and bUnit tests before implementation in every story phase.

**Organization**: Tasks are grouped by user story so each increment can be implemented, verified, and reviewed independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Task can run in parallel with other tasks in the same phase because it touches different files and has no dependency on incomplete work
- **[Story]**: User story label for traceability (`[US1]`, `[US2]`, `[US3]`)
- Every task includes exact file paths so an implementation agent can act without extra discovery

## Path Conventions

- Domain: `src/Payslip4All.Domain/`
- Application: `src/Payslip4All.Application/`
- Infrastructure: `src/Payslip4All.Infrastructure/`
- Web: `src/Payslip4All.Web/`
- Tests: `tests/Payslip4All.*.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare shared test scaffolding, time control, simulator gating, and startup seams required for constitution-aligned TDD.

- [ ] T001 [P] Add startup red coverage for hosted-payment registrations, generic return routing, and simulator environment gating in `tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs`, `tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs`, and `tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs`
- [ ] T002 [P] Add non-production simulator red coverage for dev/test/demo-only access and deterministic fake outcomes in `tests/Payslip4All.Infrastructure.Tests/HostedPayments/FakeHostedPaymentProviderTests.cs` and `tests/Payslip4All.Web.Tests/Pages/HostedPaymentSimulatorTests.cs`
- [ ] T003 [P] Add controllable-clock and timing red coverage for SC-001 start-hand-off timing and exact 1-hour cutoff support in `tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs` and `tests/Payslip4All.Web.Tests/Integration/WalletTopUpStartFlowTimingTests.cs`
- [ ] T004 Register the shared clock abstraction, hosted-payment provider factory, and simulator environment gating in `src/Payslip4All.Application/Interfaces/ISystemClock.cs`, `src/Payslip4All.Infrastructure/Services/SystemClock.cs`, `src/Payslip4All.Infrastructure/HostedPayments/HostedPaymentProviderFactory.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs`, and `src/Payslip4All.Web/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the explicit state model, deterministic normalization policy, audit entities, and persistence parity required before any user story can ship.

**⚠️ CRITICAL**: No user story work should begin until this phase is complete.

### Tests for Foundational Phase (REQUIRED — TDD)

- [ ] T005 Add aggregate red coverage for allowed attempt statuses only, inclusive `currentTime >= AbandonAfterUtc` abandonment, trustworthy late-final supersession, low-confidence late evidence lockout, and accepted-final immutability in `tests/Payslip4All.Domain.Tests/Entities/WalletTopUpAttemptTests.cs`
- [ ] T006 [P] Add audit-entity red coverage for payment evidence, normalization decisions, unmatched return records, and wallet-credit linkage invariants in `tests/Payslip4All.Domain.Tests/Entities/PaymentReturnEvidenceTests.cs`, `tests/Payslip4All.Domain.Tests/Entities/OutcomeNormalizationDecisionTests.cs`, `tests/Payslip4All.Domain.Tests/Entities/UnmatchedPaymentReturnRecordTests.cs`, and `tests/Payslip4All.Domain.Tests/Entities/WalletActivityTests.cs`
- [ ] T007 [P] Add deterministic normalization-policy red coverage for evidence precedence, unmatched-before-outcome evaluation, inclusive abandonment threshold semantics, trustworthy-late supersession, and conflicting-after-final audit-only handling in `tests/Payslip4All.Application.Tests/Services/WalletTopUpNormalizationPolicyTests.cs`
- [ ] T008 Add application-service red coverage for generic inbound return orchestration, owner scoping, stronger audit persistence, unmatched generic result selection, and exactly-once wallet settlement in `tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs`
- [ ] T009 [P] Add EF Core repository red coverage for payment evidence, normalization decisions, superseded abandonment, unmatched return records, and wallet-credit linkage in `tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpAttemptRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Repositories/PaymentReturnEvidenceRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Repositories/OutcomeNormalizationDecisionRepositoryTests.cs`, and `tests/Payslip4All.Infrastructure.Tests/Repositories/UnmatchedPaymentReturnRepositoryTests.cs`
- [ ] T010 [P] Add DynamoDB parity red coverage for payment evidence, normalization decisions, superseded abandonment, unmatched return records, and wallet-credit linkage in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs`, `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbPaymentReturnEvidenceRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbOutcomeNormalizationDecisionRepositoryTests.cs`, and `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbUnmatchedPaymentReturnRepositoryTests.cs`

### Implementation for Foundational Phase

- [ ] T011 Update the wallet top-up aggregate to use only `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, and `Unverified`, plus authoritative evidence and abandonment metadata, in `src/Payslip4All.Domain/Enums/WalletTopUpAttemptStatus.cs` and `src/Payslip4All.Domain/Entities/WalletTopUpAttempt.cs`
- [ ] T012 [P] Create the audit-trail domain model for provider-neutral evidence, normalization decisions, unmatched records, and supporting enums in `src/Payslip4All.Domain/Entities/PaymentReturnEvidence.cs`, `src/Payslip4All.Domain/Entities/OutcomeNormalizationDecision.cs`, `src/Payslip4All.Domain/Entities/UnmatchedPaymentReturnRecord.cs`, `src/Payslip4All.Domain/Enums/PaymentEvidenceClaimedOutcome.cs`, `src/Payslip4All.Domain/Enums/PaymentEvidenceTrustLevel.cs`, `src/Payslip4All.Domain/Enums/PaymentEvidenceCorrelationDisposition.cs`, `src/Payslip4All.Domain/Enums/OutcomeNormalizationDecisionType.cs`, and `src/Payslip4All.Domain/Enums/WalletTopUpWalletEffect.cs`
- [ ] T013 [P] Update application DTOs and contracts for provider-neutral evidence, matched-vs-unmatched results, abandonment resolution, and audit repositories in `src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnEvidence.cs`, `src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnResult.cs`, `src/Payslip4All.Application/DTOs/Wallet/FinalizeWalletTopUpReturnCommand.cs`, `src/Payslip4All.Application/DTOs/Wallet/FinalizedWalletTopUpResultDto.cs`, `src/Payslip4All.Application/DTOs/Wallet/WalletTopUpAttemptDto.cs`, `src/Payslip4All.Application/Interfaces/IWalletTopUpService.cs`, `src/Payslip4All.Application/Interfaces/IHostedPaymentProvider.cs`, `src/Payslip4All.Application/Interfaces/IHostedPaymentProviderFactory.cs`, `src/Payslip4All.Application/Interfaces/IWalletTopUpNormalizationPolicy.cs`, `src/Payslip4All.Application/Interfaces/Repositories/IWalletTopUpAttemptRepository.cs`, `src/Payslip4All.Application/Interfaces/Repositories/IPaymentReturnEvidenceRepository.cs`, `src/Payslip4All.Application/Interfaces/Repositories/IOutcomeNormalizationDecisionRepository.cs`, and `src/Payslip4All.Application/Interfaces/Repositories/IUnmatchedPaymentReturnRepository.cs`
- [ ] T014 Implement the deterministic application-owned evidence-precedence policy and timeout helpers in `src/Payslip4All.Application/Services/WalletTopUpNormalizationPolicy.cs`, `src/Payslip4All.Application/Services/WalletTopUpService.cs`, and `src/Payslip4All.Application/Interfaces/ISystemClock.cs`
- [ ] T015 [P] Implement EF Core schema, mappings, repositories, and migration support for attempts, evidence, normalization decisions, unmatched return records, and wallet-credit linkage in `src/Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs`, `src/Payslip4All.Infrastructure/Persistence/Repositories/WalletTopUpAttemptRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/Repositories/PaymentReturnEvidenceRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/Repositories/OutcomeNormalizationDecisionRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/Repositories/UnmatchedPaymentReturnRepository.cs`, `src/Payslip4All.Infrastructure/Migrations/20260402140000_AddWalletTopUpEvidenceAuditTrail.cs`, `src/Payslip4All.Infrastructure/Migrations/20260402140000_AddWalletTopUpEvidenceAuditTrail.Designer.cs`, and `src/Payslip4All.Infrastructure/Migrations/PayslipDbContextModelSnapshot.cs`
- [ ] T016 [P] Implement DynamoDB table provisioning and repositories for attempts, evidence, normalization decisions, unmatched return records, and wallet-credit linkage in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPaymentReturnEvidenceRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbOutcomeNormalizationDecisionRepository.cs`, and `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbUnmatchedPaymentReturnRepository.cs`

**Checkpoint**: The explicit status model, deterministic normalization policy, audit entities, and EF Core/DynamoDB persistence parity are ready for story work.

---

## Phase 3: User Story 1 - Start a card-based wallet top-up (Priority: P1) 🎯

**Goal**: Let a signed-in `CompanyOwner` enter a positive amount, create a `Pending` top-up attempt with an exact 1-hour abandonment deadline, and leave Payslip4All for an external hosted payment page without Payslip4All collecting card details.

**Independent Test**: Sign in as a company owner, open `/portal/wallet`, start a valid top-up, confirm the app stores a pending attempt with `AbandonAfterUtc = CreatedAt + 1 hour`, and verify the browser is handed off to an external hosted page or non-production simulator without any card-entry fields inside Payslip4All.

### Tests for User Story 1 (REQUIRED — TDD, constitution Principle I)

- [ ] T017 [P] [US1] Add wallet page red coverage for positive amount validation, no in-app card fields, and generic return route usage in `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`
- [ ] T018 [P] [US1] Add start-hosted-top-up service red coverage for persist-before-redirect, exact 1-hour `AbandonAfterUtc`, and opaque provider correlation values in `tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs`
- [ ] T019 [P] [US1] Add fake-provider start red coverage for external redirect URLs, simulator-only behavior, and no PAN/CVV/expiry handling in `tests/Payslip4All.Infrastructure.Tests/HostedPayments/FakeHostedPaymentProviderTests.cs`
- [ ] T020 [P] [US1] Add SC-001 hand-off timing red coverage from `/portal/wallet` to hosted-page redirect in `tests/Payslip4All.Web.Tests/Integration/WalletTopUpStartFlowTimingTests.cs`

### Implementation for User Story 1

- [ ] T021 [US1] Implement provider-agnostic top-up initiation with exact 1-hour abandonment timestamps and persist-before-redirect behavior in `src/Payslip4All.Application/Services/WalletTopUpService.cs`, `src/Payslip4All.Application/DTOs/Wallet/StartWalletTopUpCommand.cs`, and `src/Payslip4All.Application/DTOs/Wallet/StartWalletTopUpResultDto.cs`
- [ ] T022 [P] [US1] Update the hosted-payment provider seam to start only through the generic return flow and store opaque references only in `src/Payslip4All.Application/Interfaces/IHostedPaymentProvider.cs`, `src/Payslip4All.Application/Interfaces/IHostedPaymentProviderFactory.cs`, `src/Payslip4All.Infrastructure/HostedPayments/FakeHostedPaymentProvider.cs`, and `src/Payslip4All.Infrastructure/HostedPayments/FakeHostedPaymentOptions.cs`
- [ ] T023 [P] [US1] Update the owner wallet funding form to post positive rand amounts, use `/portal/wallet/top-ups/return`, and keep card data out of Razor in `src/Payslip4All.Web/Pages/Wallet.razor`
- [ ] T024 [P] [US1] Keep the hosted-payment simulator dev/test/demo-only while exposing deterministic scenario inputs for later stories in `src/Payslip4All.Web/Pages/HostedPaymentSimulator.razor` and `src/Payslip4All.Web/Program.cs`

**Checkpoint**: Owners can start a hosted wallet top-up, create a `Pending` attempt first, and leave Payslip4All without any in-app card-entry flow.

---

## Phase 4: User Story 2 - Credit the wallet after successful payment return evidence (Priority: P1)

**Goal**: Process generic inbound payment returns with deterministic evidence precedence, credit the wallet exactly once only for trustworthy matched `Completed` evidence, keep `Cancelled`/`Expired`/`Unverified`/`Abandoned`/unmatched flows non-crediting, allow trustworthy matched late final evidence to supersede `Abandoned`, and keep later conflicts audit-only after an accepted trustworthy final outcome.

**Independent Test**: Create a pending attempt, simulate trustworthy completed/cancelled/expired returns, low-confidence claimed-final evidence, unmatched returns, exact-threshold abandonment, trustworthy late final evidence after abandonment, and conflicting evidence after an accepted final outcome; then confirm the wallet changes exactly once only for accepted trustworthy completion and every other flow persists the correct audit trail without leaking private information.

### Tests for User Story 2 (REQUIRED — TDD, constitution Principle I)

- [ ] T025 [P] [US2] Add normalization-policy red coverage for deterministic cross-provider precedence, unmatched-before-outcome handling, inclusive threshold abandonment, trustworthy late-final supersession, and low-confidence late-evidence lockout in `tests/Payslip4All.Application.Tests/Services/WalletTopUpNormalizationPolicyTests.cs`
- [ ] T026 [P] [US2] Add finalize-return service red coverage for trustworthy completion, charged-amount mismatch handling, cancelled/expired/unverified outcomes, superseded abandonment, conflicting-after-final audit-only behavior, and replay idempotency in `tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs`
- [ ] T027 [P] [US2] Add EF Core red coverage for evidence persistence, normalization decisions, unmatched return records, wallet-credit linkage, and exactly-once settlement in `tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Repositories/PaymentReturnEvidenceRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Repositories/OutcomeNormalizationDecisionRepositoryTests.cs`, and `tests/Payslip4All.Infrastructure.Tests/Repositories/UnmatchedPaymentReturnRepositoryTests.cs`
- [ ] T028 [P] [US2] Add DynamoDB parity red coverage for evidence persistence, normalization decisions, unmatched return records, wallet-credit linkage, and exactly-once settlement in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs`, `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbPaymentReturnEvidenceRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbOutcomeNormalizationDecisionRepositoryTests.cs`, and `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbUnmatchedPaymentReturnRepositoryTests.cs`
- [ ] T029 [P] [US2] Add web red coverage for generic inbound return intake, matched result routing, privacy-safe unmatched not-confirmed flow, and SC-005 no-leak behavior in `tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnEntryTests.cs`, `tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnTests.cs`, and `tests/Payslip4All.Web.Tests/Pages/WalletTopUpNotConfirmedTests.cs`

### Implementation for User Story 2

- [ ] T030 [US2] Implement generic inbound return orchestration that persists evidence first, applies deterministic precedence, records normalization decisions, and returns matched-vs-unmatched results in `src/Payslip4All.Application/Services/WalletTopUpService.cs` and `src/Payslip4All.Application/Services/WalletTopUpNormalizationPolicy.cs`
- [ ] T031 [P] [US2] Update the provider-neutral evidence contract to emit claimed outcome, trust level, correlation data, safe payload snapshots, and no `Failed` business outcome in `src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnEvidence.cs`, `src/Payslip4All.Application/Interfaces/IHostedPaymentProvider.cs`, and `src/Payslip4All.Infrastructure/HostedPayments/FakeHostedPaymentProvider.cs`
- [ ] T032 [P] [US2] Implement EF Core persistence and settlement changes for matched trustworthy evidence, unmatched return records, normalization decisions, superseded abandonment, and audit-only conflicts in `src/Payslip4All.Infrastructure/Persistence/Repositories/WalletTopUpAttemptRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/Repositories/PaymentReturnEvidenceRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/Repositories/OutcomeNormalizationDecisionRepository.cs`, and `src/Payslip4All.Infrastructure/Persistence/Repositories/UnmatchedPaymentReturnRepository.cs`
- [ ] T033 [P] [US2] Implement DynamoDB parity for matched trustworthy evidence, unmatched return records, normalization decisions, superseded abandonment, and audit-only conflicts in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPaymentReturnEvidenceRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbOutcomeNormalizationDecisionRepository.cs`, and `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbUnmatchedPaymentReturnRepository.cs`
- [ ] T034 [US2] Create the generic inbound return page and unmatched generic result page while repurposing the existing matched result page for matched attempts only in `src/Payslip4All.Web/Pages/WalletTopUpReturnEntry.razor`, `src/Payslip4All.Web/Pages/WalletTopUpNotConfirmed.razor`, and `src/Payslip4All.Web/Pages/WalletTopUpReturn.razor`
- [ ] T035 [US2] Update finalization DTOs and service interfaces so unmatched returns use a generic non-attempt-specific result flow and matched results stay owner-safe and refresh-stable in `src/Payslip4All.Application/DTOs/Wallet/FinalizeWalletTopUpReturnCommand.cs`, `src/Payslip4All.Application/DTOs/Wallet/FinalizedWalletTopUpResultDto.cs`, `src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnResult.cs`, and `src/Payslip4All.Application/Interfaces/IWalletTopUpService.cs`

**Checkpoint**: Trustworthy matched final evidence settles exactly once by the confirmed charged amount, low-confidence late evidence cannot reopen `Abandoned`, later conflicts are audit-only after an accepted final outcome, and unmatched returns stay generic and privacy-safe.

---

## Phase 5: User Story 3 - See incomplete and unresolved payment outcomes (Priority: P2)

**Goal**: Let owners see only their own matched attempts with explicit statuses (`Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, `Unverified`), confirmed charged amounts and wallet-credit linkage where applicable, while keeping unmatched returns generic and preserving a financially credible audit trail for reconciliation and SC-004/SC-006 visibility.

**Independent Test**: Create owner-scoped attempts with `Completed`, `Cancelled`, `Expired`, `Pending`, `Unverified`, and `Abandoned` outcomes; revisit matched result pages; send unmatched returns; and confirm each owner sees only their own matched attempt history, wallet-credit linkage, stable result messaging, and no leakage from the generic unmatched flow.

### Tests for User Story 3 (REQUIRED — TDD, constitution Principle I)

- [ ] T036 [P] [US3] Add wallet history red coverage for explicit statuses, confirmed charged amounts, wallet-credit indicators, and no synthetic unmatched statuses in `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`
- [ ] T037 [P] [US3] Add matched-result and access-control red coverage for stable refresh behavior, owner-only matched results, and no foreign payment data leakage in `tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnTests.cs` and `tests/Payslip4All.Web.Tests/Security/CrossEmployerHttpTests.cs`
- [ ] T038 [P] [US3] Add service and repository red coverage for abandonment sweeps, matched-history visibility, reconciliation queries, and superseded-abandonment audit traceability in `tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpAttemptRepositoryTests.cs`, and `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepositoryTests.cs`
- [ ] T039 [P] [US3] Add SC-004 and SC-006 timing red coverage for owner-visible history updates and late-final reclassification visibility in `tests/Payslip4All.Web.Tests/Integration/WalletTopUpHistoryVisibilityTimingTests.cs` and `tests/Payslip4All.Web.Tests/Integration/WalletTopUpLateEvidenceTimingTests.cs`

### Implementation for User Story 3

- [ ] T040 [US3] Implement owner-scoped history, matched-result retrieval, and abandonment-resolution entry points in `src/Payslip4All.Application/Services/WalletTopUpService.cs` and `src/Payslip4All.Application/Interfaces/IWalletTopUpService.cs`
- [ ] T041 [P] [US3] Implement EF Core and DynamoDB query support for exact-threshold abandonment sweeps, newest-first owner history, and reconciliation-grade audit joins in `src/Payslip4All.Infrastructure/Persistence/Repositories/WalletTopUpAttemptRepository.cs` and `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepository.cs`
- [ ] T042 [P] [US3] Extend wallet top-up projections with authoritative timestamps, superseded-abandonment indicators, and wallet-credit linkage for matched attempts only in `src/Payslip4All.Application/DTOs/Wallet/WalletTopUpAttemptDto.cs`, `src/Payslip4All.Application/DTOs/Wallet/FinalizedWalletTopUpResultDto.cs`, and `src/Payslip4All.Application/DTOs/Wallet/WalletActivityDto.cs`
- [ ] T043 [US3] Update the owner wallet page to show explicit matched statuses, confirmed charged amounts, wallet-credit linkage, and no unmatched synthetic history items in `src/Payslip4All.Web/Pages/Wallet.razor`
- [ ] T044 [US3] Update the matched result page to show stable authoritative outcomes, trustworthy late-final supersession messaging, and unchanged wallet effects after audit-only conflicts in `src/Payslip4All.Web/Pages/WalletTopUpReturn.razor`

**Checkpoint**: Owners can see only their own matched attempt outcomes and linked credits, unresolved matched attempts become `Abandoned` exactly at the inclusive 1-hour threshold, and reconciliation data remains traceable through evidence, decisions, superseded abandonment, and wallet-credit linkage.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish verification coverage, documentation, simulator boundaries, and the constitution-mandated manual test gate.

- [ ] T045 [P] Update rollout and verification guidance for deterministic precedence, inclusive abandonment, unmatched privacy rules, simulator restrictions, and audit linkage in `README.md` and `specs/008-wallet-card-topup/quickstart.md`
- [ ] T046 [P] Add or refresh explicit verification coverage for SC-002, SC-003, and SC-005 in `tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs`, `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs`, and `tests/Payslip4All.Web.Tests/Pages/WalletTopUpNotConfirmedTests.cs`
- [ ] T047 [P] Add or refresh explicit verification coverage for SC-001, SC-004, and SC-006 in `tests/Payslip4All.Web.Tests/Integration/WalletTopUpStartFlowTimingTests.cs`, `tests/Payslip4All.Web.Tests/Integration/WalletTopUpHistoryVisibilityTimingTests.cs`, and `tests/Payslip4All.Web.Tests/Integration/WalletTopUpLateEvidenceTimingTests.cs`
- [ ] T048 [P] Run the domain, application, infrastructure, and web regression suites in `tests/Payslip4All.Domain.Tests/Payslip4All.Domain.Tests.csproj`, `tests/Payslip4All.Application.Tests/Payslip4All.Application.Tests.csproj`, `tests/Payslip4All.Infrastructure.Tests/Payslip4All.Infrastructure.Tests.csproj`, and `tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj`
- [ ] T049 [P] Execute the quickstart manual scenarios for SC-001, SC-004, SC-005, and SC-006 using `specs/008-wallet-card-topup/quickstart.md`
- [ ] T050 Present the constitution-aligned Manual Test Gate prompt using `specs/008-wallet-card-topup/quickstart.md` by summarising implemented changes, listing recommended manual tests, listing pending git operations, and waiting for explicit `approve` or `decline` before any `git commit`, `git merge`, or `git push`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** — can start immediately
- **Phase 2: Foundational** — depends on Phase 1 and blocks all user story work
- **Phase 3: US1** — depends on Phase 2
- **Phase 4: US2** — depends on Phase 2 and on US1 because return processing requires persisted pending attempts and the generic hosted start flow
- **Phase 5: US3** — depends on Phase 2 and should follow US2 because owner-visible history and result pages rely on authoritative outcomes, audit decisions, and matched/unmatched routing already existing
- **Phase 6: Polish** — depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: No dependency on other user stories; it delivers the secure hosted-payment hand-off
- **US2 (P1)**: Depends on US1 because authoritative return handling requires started attempts and provider correlation metadata
- **US3 (P2)**: Depends on US2 because explicit statuses, wallet-credit linkage, superseded abandonment messaging, and audit traceability rely on finalized matched outcomes

### Within Each User Story

- Tests MUST be written first and confirmed failing before implementation begins
- Domain and Application rules come before Infrastructure persistence and Web wiring
- Deterministic evidence precedence stays in Application; provider parsing stays in Infrastructure; Razor components stay orchestration-only
- Owner scoping must be enforced in repositories, services, and pages for every matched-attempt read
- Unmatched returns must remain non-attempt-specific and privacy-safe end to end
- **Manual Test Gate (Principle VI — NON-NEGOTIABLE)**: After every implementation task, emit the Manual Test Gate prompt that summarises what was implemented, lists recommended manual tests, lists pending git operations, and waits for explicit engineer `approve` or `decline` before any `git commit`, `git merge`, or `git push`
- A story is complete only when automated tests pass, the relevant quickstart scenarios pass, the Manual Test Gate is approved, and CI is green

### Parallel Opportunities

- T001, T002, and T003 can run in parallel during setup
- T006, T007, T009, and T010 can run in parallel once T005 defines the core failing expectations
- T012, T013, T015, and T016 can run in parallel after T011 establishes the explicit status model
- In US1, T017, T018, T019, and T020 can run in parallel; T022, T023, and T024 can run in parallel after T021
- In US2, T025, T026, T027, T028, and T029 can run in parallel; T031, T032, and T033 can run in parallel after T030
- In US3, T036, T037, T038, and T039 can run in parallel; T041 and T042 can run in parallel after T040
- In Polish, T045, T046, T047, T048, and T049 can run in parallel before T050

---

## Parallel Example: User Story 1

```bash
# Write the failing US1 tests together
Task: "T017 Add wallet page red coverage in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs"
Task: "T018 Add start-hosted-top-up service red coverage in tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs"
Task: "T019 Add fake-provider start red coverage in tests/Payslip4All.Infrastructure.Tests/HostedPayments/FakeHostedPaymentProviderTests.cs"
Task: "T020 Add SC-001 timing red coverage in tests/Payslip4All.Web.Tests/Integration/WalletTopUpStartFlowTimingTests.cs"

# After start orchestration lands, finish the provider and UI pieces together
Task: "T022 Update the hosted-payment provider seam in src/Payslip4All.Application/Interfaces/IHostedPaymentProvider.cs and src/Payslip4All.Infrastructure/HostedPayments/FakeHostedPaymentProvider.cs"
Task: "T023 Update the owner wallet funding form in src/Payslip4All.Web/Pages/Wallet.razor"
Task: "T024 Gate the simulator in src/Payslip4All.Web/Pages/HostedPaymentSimulator.razor and src/Payslip4All.Web/Program.cs"
```

## Parallel Example: User Story 2

```bash
# Write the failing US2 tests together
Task: "T025 Add normalization-policy red coverage in tests/Payslip4All.Application.Tests/Services/WalletTopUpNormalizationPolicyTests.cs"
Task: "T026 Add finalize-return service red coverage in tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs"
Task: "T027 Add EF Core red coverage in tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs and related repository tests"
Task: "T028 Add DynamoDB parity red coverage in tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs and related repository tests"
Task: "T029 Add web red coverage in tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnEntryTests.cs, WalletTopUpReturnTests.cs, and WalletTopUpNotConfirmedTests.cs"

# After orchestration lands, finish persistence and routing together
Task: "T031 Update provider-neutral evidence files in src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnEvidence.cs and src/Payslip4All.Infrastructure/HostedPayments/FakeHostedPaymentProvider.cs"
Task: "T032 Implement EF Core persistence changes in src/Payslip4All.Infrastructure/Persistence/Repositories/"
Task: "T033 Implement DynamoDB parity changes in src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/"
```

## Parallel Example: User Story 3

```bash
# Write the failing US3 tests together
Task: "T036 Add wallet history red coverage in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs"
Task: "T037 Add matched-result and access-control red coverage in tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnTests.cs and tests/Payslip4All.Web.Tests/Security/CrossEmployerHttpTests.cs"
Task: "T038 Add abandonment and reconciliation red coverage in tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs and repository tests"
Task: "T039 Add SC-004 and SC-006 timing red coverage in tests/Payslip4All.Web.Tests/Integration/WalletTopUpHistoryVisibilityTimingTests.cs and WalletTopUpLateEvidenceTimingTests.cs"

# After service/query support lands, finish projections and UI together
Task: "T041 Implement repository query support in src/Payslip4All.Infrastructure/Persistence/Repositories/WalletTopUpAttemptRepository.cs and src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepository.cs"
Task: "T042 Extend projections in src/Payslip4All.Application/DTOs/Wallet/WalletTopUpAttemptDto.cs and related DTO files"
Task: "T043 Update wallet history UI in src/Payslip4All.Web/Pages/Wallet.razor"
```

---

## Implementation Strategy

### Functional MVP First

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: US1 to establish secure hosted-payment start flow
4. Complete Phase 4: US2 to make the feature financially useful by turning trustworthy matched completion into exactly-once wallet credit
5. Present the Manual Test Gate prompt and wait for engineer `approve`
6. Validate SC-001, SC-002, SC-003, and SC-005 before broader rollout

### Incremental Delivery

1. Setup + Foundational → deterministic normalization, audit storage, persistence parity, and exact-threshold rules are ready
2. Add US1 → owners can initiate hosted top-ups safely
3. Add US2 → trustworthy matched final evidence can settle wallets exactly once while unmatched and low-confidence flows stay safe
4. Add US3 → owners gain complete explicit status visibility and reconciliation-grade traceability
5. Finish Polish → verify SC-001 through SC-006 and apply the Manual Test Gate

### Team Strategy

With multiple developers:

1. One developer handles Setup + shared clock/provider wiring
2. One developer handles Foundational domain/application policy work
3. One developer handles EF Core persistence while another handles DynamoDB parity after the foundational contracts land
4. After Foundational is complete:
   - Developer A: US1 start flow
   - Developer B: US2 return normalization and settlement
   - Developer C: US3 history/result visibility and timing verification

---

## Notes

- `[P]` tasks touch different files and can be run in parallel safely
- Story labels map every user-story task to `US1`, `US2`, or `US3` for traceability
- `Failed` is not a valid attempt status or business outcome anywhere in this feature
- Unmatched returns are separate auditable records and must never appear as synthetic attempt statuses
- The generic unmatched result must reveal no guessed attempt ID, owner identity, wallet details, or wallet-credit confirmation
- Trustworthy matched late final evidence may supersede `Abandoned`; low-confidence late evidence may not
- Once a trustworthy matched final outcome is accepted, later conflicting evidence is audit-only and cannot change wallet effects
- The simulator remains strictly dev/test/demo only and must never blur the production boundary that real card entry happens on an external hosted payment page
