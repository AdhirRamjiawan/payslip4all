# Tasks: Generic Wallet Card Top-Up

**Input**: Design documents from `/specs/008-wallet-card-topup/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Per constitution Principle I (TDD), tests are required for this feature. Write the failing tests in each phase before implementation tasks begin.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified independently.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the shared contracts and infrastructure seams the feature will use across all layers.

- [X] T001 [P] Add provider-neutral return DTO files in src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnEvidenceDto.cs, src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnResolutionDto.cs, and src/Payslip4All.Application/DTOs/Wallet/GenericHostedReturnResultDto.cs
- [X] T002 [P] Add audit repository interface files in src/Payslip4All.Application/Interfaces/Repositories/IPaymentReturnEvidenceRepository.cs, src/Payslip4All.Application/Interfaces/Repositories/IOutcomeNormalizationDecisionRepository.cs, and src/Payslip4All.Application/Interfaces/Repositories/IUnmatchedPaymentReturnRecordRepository.cs
- [X] T003 [P] Add exact-threshold time abstraction files in src/Payslip4All.Application/Interfaces/ITimeProvider.cs and src/Payslip4All.Infrastructure/Time/SystemTimeProvider.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared domain, persistence, and registration changes that block all user stories.

**⚠️ CRITICAL**: No user story work should begin until this phase is complete.

- [X] T004 [P] Add failing shared domain tests for explicit statuses, abandonment cutoff, and audit-safe entities in tests/Payslip4All.Domain.Tests/Entities/WalletTopUpAttemptTests.cs
- [X] T005 [P] Add failing shared EF Core and DynamoDB persistence tests for evidence, decisions, and unmatched records in tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpAttemptRepositoryTests.cs and tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepositoryTests.cs
- [X] T006 [P] Add failing DI and provider-switching coverage for new wallet top-up services in tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs and tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs
- [X] T007 Implement shared wallet top-up enums in src/Payslip4All.Domain/Enums/WalletTopUpAttemptStatus.cs, src/Payslip4All.Domain/Enums/PaymentReturnTrustLevel.cs, src/Payslip4All.Domain/Enums/PaymentReturnCorrelationDisposition.cs, and src/Payslip4All.Domain/Enums/PaymentReturnClaimedOutcome.cs
- [X] T008 Implement shared audit entities and exact 1-hour abandonment rules in src/Payslip4All.Domain/Entities/WalletTopUpAttempt.cs, src/Payslip4All.Domain/Entities/PaymentReturnEvidence.cs, src/Payslip4All.Domain/Entities/OutcomeNormalizationDecision.cs, and src/Payslip4All.Domain/Entities/UnmatchedPaymentReturnRecord.cs
- [X] T009 Update shared application contracts in src/Payslip4All.Application/Interfaces/IWalletTopUpService.cs, src/Payslip4All.Application/Interfaces/IHostedPaymentProvider.cs, src/Payslip4All.Application/Interfaces/Repositories/IWalletTopUpAttemptRepository.cs, and src/Payslip4All.Application/DTOs/Wallet/FinalizeWalletTopUpReturnCommand.cs
- [X] T010 Implement EF Core model mappings and relational registrations in src/Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs and src/Payslip4All.Web/Program.cs
- [X] T011 Implement DynamoDB table provisioning and service registrations in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs and src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs
- [X] T012 Generate the audit persistence migration in src/Payslip4All.Infrastructure/Migrations/ and update src/Payslip4All.Infrastructure/Migrations/PayslipDbContextModelSnapshot.cs

**Checkpoint**: Domain vocabulary, persistence shape, and DI seams are ready for story work.

---

## Phase 3: User Story 1 - Start a card-based wallet top-up (Priority: P1) 🎯 MVP

**Goal**: Let a company owner start a hosted wallet top-up, create a pending attempt, and leave Payslip4All for the hosted payment page without entering card details in-app.

**Independent Test**: Sign in as a company owner, submit a valid amount from `/portal/wallet`, confirm a pending attempt is stored with `AbandonAfterUtc`, and verify the browser is redirected to the hosted provider or simulator while invalid amounts are rejected locally.

### Tests for User Story 1 (REQUIRED — write and confirm they fail first)

- [X] T013 [P] [US1] Add failing start-flow service tests for generic return routing and `AbandonAfterUtc` creation in tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs
- [X] T014 [P] [US1] Add failing hosted-provider start tests for external redirect metadata in tests/Payslip4All.Infrastructure.Tests/HostedPayments/FakeHostedPaymentProviderTests.cs
- [X] T015 [P] [US1] Add failing wallet page tests for valid submission, invalid amount rejection, and no card-detail fields in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs

### Implementation for User Story 1

- [X] T016 [P] [US1] Update start-flow DTOs for generic hosted return data in src/Payslip4All.Application/DTOs/Wallet/StartWalletTopUpCommand.cs, src/Payslip4All.Application/DTOs/Wallet/StartWalletTopUpResultDto.cs, and src/Payslip4All.Application/DTOs/Wallet/WalletTopUpAttemptDto.cs
- [X] T017 [P] [US1] Update hosted-start provider behavior in src/Payslip4All.Infrastructure/HostedPayments/FakeHostedPaymentProvider.cs, src/Payslip4All.Infrastructure/HostedPayments/FakeHostedPaymentOptions.cs, and src/Payslip4All.Infrastructure/HostedPayments/HostedPaymentProviderFactory.cs
- [X] T018 [US1] Refactor hosted top-up creation and pending-attempt persistence in src/Payslip4All.Application/Services/WalletTopUpService.cs and src/Payslip4All.Application/Interfaces/IWalletTopUpService.cs
- [X] T019 [US1] Update the owner wallet journey to use `/portal/wallet/top-ups/return` in src/Payslip4All.Web/Pages/Wallet.razor and src/Payslip4All.Web/Pages/HostedPaymentSimulator.razor

**Checkpoint**: User Story 1 is independently testable from `/portal/wallet` through hosted hand-off.

---

## Phase 4: User Story 2 - Credit the wallet after successful payment return evidence (Priority: P1)

**Goal**: Process generic hosted returns, normalize trustworthy payment evidence, settle successful matched attempts exactly once, and keep unmatched or conflicting returns auditable without unsafe wallet effects.

**Independent Test**: Start an attempt, send trustworthy completed/cancelled/expired returns plus unmatched, replayed, low-confidence, late-final, and conflicting returns through `/portal/wallet/top-ups/return`, then verify authoritative outcomes, exactly-once wallet settlement, and generic unmatched handling.

### Tests for User Story 2 (REQUIRED — write and confirm they fail first)

- [X] T020 [P] [US2] Add failing domain normalization tests for completed, cancelled, expired, unverified, abandoned supersession, and conflicting evidence in tests/Payslip4All.Domain.Tests/Entities/WalletTopUpAttemptTests.cs
- [X] T021 [P] [US2] Add failing return-handling service tests for matched, unmatched, replayed, and late trustworthy evidence in tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs
- [X] T022 [P] [US2] Add failing EF Core settlement and audit-trail tests in tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs and tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpAttemptRepositoryTests.cs
- [X] T023 [P] [US2] Add failing DynamoDB settlement and audit parity tests in tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs and tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepositoryTests.cs
- [X] T024 [P] [US2] Add failing generic-return web tests for matched redirects and unmatched not-confirmed results in tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnTests.cs and tests/Payslip4All.Web.Tests/Pages/WalletTopUpNotConfirmedTests.cs

### Implementation for User Story 2

- [X] T025 [P] [US2] Implement provider-neutral return and resolution DTOs in src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnResult.cs, src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnEvidenceDto.cs, src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnResolutionDto.cs, src/Payslip4All.Application/DTOs/Wallet/FinalizedWalletTopUpResultDto.cs, and src/Payslip4All.Application/DTOs/Wallet/GenericHostedReturnResultDto.cs
- [X] T026 [P] [US2] Implement the normalization policy service in src/Payslip4All.Application/Services/WalletTopUpOutcomeNormalizer.cs and src/Payslip4All.Application/Interfaces/IWalletTopUpOutcomeNormalizer.cs
- [X] T027 [P] [US2] Update the provider contract and simulator evidence parsing in src/Payslip4All.Application/Interfaces/IHostedPaymentProvider.cs and src/Payslip4All.Infrastructure/HostedPayments/FakeHostedPaymentProvider.cs
- [X] T028 [P] [US2] Implement EF Core audit repositories in src/Payslip4All.Infrastructure/Persistence/Repositories/PaymentReturnEvidenceRepository.cs, src/Payslip4All.Infrastructure/Persistence/Repositories/OutcomeNormalizationDecisionRepository.cs, and src/Payslip4All.Infrastructure/Persistence/Repositories/UnmatchedPaymentReturnRecordRepository.cs
- [X] T029 [P] [US2] Implement DynamoDB audit repositories in src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPaymentReturnEvidenceRepository.cs, src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbOutcomeNormalizationDecisionRepository.cs, and src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbUnmatchedPaymentReturnRecordRepository.cs
- [X] T030 [US2] Refactor generic hosted-return processing to persist evidence first, normalize outcomes, and settle exactly once in src/Payslip4All.Application/Services/WalletTopUpService.cs
- [X] T031 [US2] Update attempt settlement persistence and replay safety in src/Payslip4All.Infrastructure/Persistence/Repositories/WalletTopUpAttemptRepository.cs and src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepository.cs
- [X] T032 [US2] Add the generic intake and unmatched result pages in src/Payslip4All.Web/Pages/WalletTopUpGenericReturn.razor, src/Payslip4All.Web/Pages/WalletTopUpReturn.razor, and src/Payslip4All.Web/Pages/WalletTopUpNotConfirmed.razor

**Checkpoint**: User Story 2 is independently testable for trustworthy settlement, unmatched privacy, replays, and late trustworthy corrections.

---

## Phase 5: User Story 3 - See incomplete and unresolved payment outcomes (Priority: P2)

**Goal**: Let the owner see explicit top-up statuses and wallet-credit linkage, keep foreign data hidden, support exact-threshold abandonment, and expose auditable reconciliation details without leaking unmatched information.

**Independent Test**: Create attempts that become pending, completed, cancelled, expired, abandoned, and unverified; review wallet history and result pages as the owning user and as a different owner; then confirm unmatched results remain generic and audit details stay linked for matched attempts only.

### Tests for User Story 3 (REQUIRED — write and confirm they fail first)

- [X] T033 [P] [US3] Add failing application tests for abandonment sweeps, owner-scoped history, and audit projection in tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs
- [X] T034 [P] [US3] Add failing web tests for wallet history statuses, foreign-owner denial, and privacy-safe unmatched pages in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs, tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnTests.cs, and tests/Payslip4All.Web.Tests/Pages/WalletTopUpNotConfirmedTests.cs
- [X] T035 [P] [US3] Add failing repository parity tests for abandonment timeout processing and owner history ordering in tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpAttemptRepositoryTests.cs and tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepositoryTests.cs

### Implementation for User Story 3

- [X] T036 [P] [US3] Extend owner-facing history and result DTOs in src/Payslip4All.Application/DTOs/Wallet/WalletTopUpAttemptDto.cs, src/Payslip4All.Application/DTOs/Wallet/FinalizedWalletTopUpResultDto.cs, and src/Payslip4All.Application/DTOs/Wallet/GenericHostedReturnResultDto.cs
- [X] T037 [P] [US3] Implement abandonment orchestration in src/Payslip4All.Application/Services/WalletTopUpAbandonmentService.cs and src/Payslip4All.Application/Interfaces/IWalletTopUpAbandonmentService.cs
- [X] T038 [P] [US3] Add abandonment sweep and owner-history queries in src/Payslip4All.Application/Interfaces/Repositories/IWalletTopUpAttemptRepository.cs, src/Payslip4All.Infrastructure/Persistence/Repositories/WalletTopUpAttemptRepository.cs, and src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepository.cs
- [X] T039 [US3] Update wallet top-up history and matched-result projections in src/Payslip4All.Application/Services/WalletTopUpService.cs
- [X] T040 [US3] Update owner wallet history UI for explicit statuses, confirmed amounts, and wallet-credit linkage in src/Payslip4All.Web/Pages/Wallet.razor
- [X] T041 [US3] Update matched and unmatched result pages for idempotent refresh and privacy-safe access handling in src/Payslip4All.Web/Pages/WalletTopUpReturn.razor and src/Payslip4All.Web/Pages/WalletTopUpNotConfirmed.razor
- [X] T042 [US3] Extend deterministic simulator scenarios for pending, abandonment, low-confidence, late-final, and conflicting evidence in src/Payslip4All.Web/Pages/HostedPaymentSimulator.razor and src/Payslip4All.Infrastructure/HostedPayments/FakeHostedPaymentProvider.cs

**Checkpoint**: User Story 3 is independently testable for owner visibility, privacy, abandonment, and reconciliation support.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish cross-story cleanup, docs, and release hardening.

- [X] T043 [P] Remove legacy `Failed` wallet top-up wording and state handling from src/Payslip4All.Domain/Enums/WalletTopUpAttemptStatus.cs, src/Payslip4All.Application/Services/WalletTopUpService.cs, src/Payslip4All.Web/Pages/WalletTopUpReturn.razor, and affected test files under tests/
- [X] T044 [P] Strengthen startup and provider-switching coverage for the new routes and repositories in tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs and tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs
- [X] T045 [P] Update developer and operator documentation for the generic hosted top-up flow in README.md and specs/008-wallet-card-topup/quickstart.md
- [X] T046 Validate SC-001 through SC-006 against specs/008-wallet-card-topup/quickstart.md before the Manual Test Gate

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** → no dependencies
- **Phase 2: Foundational** → depends on Phase 1 and blocks all story work
- **Phase 3: US1** → depends on Phase 2
- **Phase 4: US2** → depends on Phase 2 and uses the pending-attempt flow delivered in US1
- **Phase 5: US3** → depends on Phase 2 and builds on the normalized outcomes and audit records delivered in US2
- **Phase 6: Polish** → depends on all desired user stories being complete

### User Story Completion Order

1. **US1**: Establish hosted top-up initiation and pending attempts
2. **US2**: Add generic return intake, trustworthy settlement, unmatched handling, and audit persistence
3. **US3**: Add owner-facing status visibility, abandonment workflows, and reconciliation/privacy hardening

### Within Each User Story

- Write the listed tests first and confirm they fail before implementation starts.
- Complete DTOs and interfaces before service or repository logic.
- Complete application and persistence logic before Razor page wiring.
- Keep matched-attempt flows owner-scoped and unmatched flows generic throughout.
- Present the constitution Manual Test Gate prompt before any commit, merge, or push.

### Parallel Opportunities

- T001-T003 can run in parallel because they create separate setup files.
- T004-T006 can run in parallel as independent failing-test tasks.
- T013-T015 can run in parallel within US1.
- T020-T024 can run in parallel within US2.
- T025-T029 can run in parallel once US2 tests are in place because they touch separate DTO, service, and repository files.
- T033-T035 can run in parallel within US3.
- T036-T038 can run in parallel once US3 tests are in place.
- T043-T045 can run in parallel during polish.

---

## Parallel Example: User Story 1

```bash
Task T013: Add failing start-flow service tests in tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs
Task T014: Add failing hosted-provider start tests in tests/Payslip4All.Infrastructure.Tests/HostedPayments/FakeHostedPaymentProviderTests.cs
Task T015: Add failing wallet page tests in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs
```

## Parallel Example: User Story 2

```bash
Task T020: Add failing domain normalization tests in tests/Payslip4All.Domain.Tests/Entities/WalletTopUpAttemptTests.cs
Task T022: Add failing EF Core settlement tests in tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs
Task T023: Add failing DynamoDB settlement tests in tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs
Task T024: Add failing web return-flow tests in tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnTests.cs
```

## Parallel Example: User Story 3

```bash
Task T033: Add failing abandonment and history service tests in tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs
Task T034: Add failing wallet history and privacy web tests in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs
Task T035: Add failing abandonment parity tests in tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpAttemptRepositoryTests.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: US1
4. Validate the hosted hand-off flow independently from `/portal/wallet`
5. Present the Manual Test Gate prompt before any source-control action

### Incremental Delivery

1. Deliver **US1** to prove hosted hand-off, pending attempt creation, and no in-app card data capture
2. Deliver **US2** to add financially correct wallet settlement, unmatched privacy, and audit persistence
3. Deliver **US3** to add owner-visible history, abandonment processing, and reconciliation support
4. Finish with **Phase 6** cleanup, documentation, and quickstart validation

### Parallel Team Strategy

1. One engineer completes Phase 1 and coordinates shared contracts
2. Split Phase 2 across domain, persistence, and DI test work in parallel
3. After Phase 2:
   - Engineer A handles US1 web + service flow
   - Engineer B handles US2 normalization + repository work
   - Engineer C handles US3 history + privacy UI once US2 result contracts are stable

---

## Notes

- Every task line follows the required checklist format: checkbox, task ID, optional `[P]`, required story label for story phases, and exact file path(s).
- `Unmatched` remains an auditable return classification and must never become a wallet top-up attempt status.
- Use the exact 1-hour abandonment cutoff defined by `AbandonAfterUtc`.
- Keep wallet settlement exactly-once and anchored to the matched attempt on both EF Core and DynamoDB paths.
- Do not commit, merge, or push until the Manual Test Gate is explicitly approved.
