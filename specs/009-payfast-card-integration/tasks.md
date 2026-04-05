# Tasks: PayFast Card Integration

**Input**: Design documents from `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/`  
**Required inputs**: `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/plan.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/spec.md`  
**Additional inputs used**: `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/data-model.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/research.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/quickstart.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/contracts/payfast-hosted-payment-contract.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/contracts/wallet-payfast-topup-application-contract.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/contracts/wallet-payfast-topup-ui-contract.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/contracts/site-admin-payment-review-contract.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/contracts/dynamodb-payment-storage-contract.md`

**Tests**: TDD is required in this repository. Every user-story phase starts with failing xUnit and bUnit coverage that must be written and verified failing before implementation begins.

**Organization**: Tasks are grouped by user story priority so each story can be implemented, tested, and demonstrated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Task can run in parallel with other tasks in the same phase
- **[Story]**: Present only in user-story phases as `[US1]`, `[US2]`, `[US3]`
- Every task includes exact file path(s)

---

## Phase 1: Setup

**Purpose**: Prepare shared fixtures, release-evidence scaffolding, and validation assets before feature coding starts.

- [X] T001 [P] Refresh shared PayFast checkout, notify, and browser-return fixtures for card-only, sandbox, live, and conflicting-evidence scenarios in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastTestData.cs`
- [X] T002 [P] Refresh reusable reconciliation-clock and audit assertion helpers for Payment Confirmation Record and non-sensitive evidence checks in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpTestClock.cs` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpAuditAssertions.cs`
- [X] T003 [P] Align release-evidence checklist scaffolding with quickstart scenarios for SC-004, SC-005, admin review, and startup-log capture in `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/checklists/sc-004-uat-sign-off-pack.md` and `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/quickstart.md`

---

## Phase 2: Foundational

**Purpose**: Shared domain, terminology, and persistence prerequisites that block all user stories.

**⚠️ CRITICAL**: Complete and confirm the failing tests in this phase before changing shared production code.

- [X] T004 [P] Add failing domain lifecycle and amount-rule coverage for `Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, `NotConfirmed`, legacy `Unverified` mapping, exact-match eligibility, and exactly-once credit invariants in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Domain.Tests/Entities/WalletTopUpAttemptTests.cs`
- [X] T005 [P] Add failing application orchestration coverage for notify-only settlement authority, browser-return informational behavior, standardized Payment Confirmation Record terminology, and unified owner-safe messaging in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpOutcomeNormalizerTests.cs`
- [X] T006 [P] Add failing relational and DynamoDB parity coverage for Payment Confirmation Record persistence, normalization decisions, unmatched returns, and exactly-once settlement in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs`
- [X] T007 [P] Add failing EF Core migration coverage for all new PayFast audit fields, admin-review query fields, and terminology-backed schema updates in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/Persistence/PayslipDbContextMigrationTests.cs`
- [X] T008 Update shared wallet top-up lifecycle entities and enums for authoritative notify validation, PayFast confirmation, audit linkage, and non-sensitive persistence in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Domain/Enums/WalletTopUpAttemptStatus.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Domain/Entities/WalletTopUpAttempt.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Domain/Entities/PaymentReturnEvidence.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Domain/Entities/OutcomeNormalizationDecision.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Domain/Entities/UnmatchedPaymentReturnRecord.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Domain/Entities/WalletActivity.cs`
- [X] T009 Update shared wallet DTOs, commands, and application interfaces for Payment Confirmation Record terminology, owner-safe return flows, and SiteAdministrator review contracts in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnEvidenceDto.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnResolutionDto.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/HostedPaymentReturnResult.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/FinalizedWalletTopUpResultDto.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/GenericHostedReturnResultDto.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/StartWalletTopUpCommand.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/StartWalletTopUpResultDto.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/FinalizeWalletTopUpReturnCommand.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/WalletTopUpAttemptDto.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/SiteAdministratorPaymentReviewDto.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/IHostedPaymentProvider.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/IWalletTopUpService.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/IWalletTopUpOutcomeNormalizer.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/IWalletTopUpAbandonmentService.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/Repositories/IPaymentReturnEvidenceRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/Repositories/IOutcomeNormalizationDecisionRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/Repositories/IUnmatchedPaymentReturnRecordRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/Repositories/IWalletTopUpAttemptRepository.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/Repositories/IWalletActivityRepository.cs`
- [X] T010 Update relational payment persistence mappings and unit-of-work coverage for Payment Confirmation Record, review-safe evidence projection, and traceable wallet settlement in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/WalletTopUpAttemptRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/PaymentReturnEvidenceRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/OutcomeNormalizationDecisionRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/UnmatchedPaymentReturnRecordRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/WalletActivityRepository.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/WalletRepository.cs`
- [X] T011 Update DynamoDB payment persistence mappings and ownership parity for Payment Confirmation Record, review-safe evidence projection, and exactly-once settlement in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbUnitOfWork.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbOwnership.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPaymentReturnEvidenceRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbOutcomeNormalizationDecisionRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbUnmatchedPaymentReturnRecordRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletActivityRepository.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletRepository.cs`
- [X] T012 Generate or refresh the EF Core migration and model snapshot for the PayFast audit and review schema in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Migrations` and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Migrations/PayslipDbContextModelSnapshot.cs`

**Checkpoint**: Shared contracts, entities, repositories, and migration-backed schema are ready for story work.

---

## Phase 3: User Story 1 - Start a wallet top-up with a credit card (Priority: P1) 🎯 MVP

**Goal**: Let an authenticated `CompanyOwner` start a PayFast-hosted wallet top-up with valid ZAR input and explicit card-only checkout.

**Independent Test**: Sign in as a `CompanyOwner`, start a top-up from `/portal/wallet`, verify invalid amounts are rejected before persistence, verify a pending attempt is stored before redirect, and verify the hosted request is card-only.

### Tests for User Story 1 (REQUIRED — TDD)

- [X] T013 [P] [US1] Add failing bUnit wallet-page tests for owner-only access, R50-R1000 validation, two-decimal validation, card-only UX copy, and owner-safe start-failure messaging in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`
- [X] T014 [P] [US1] Add failing hosted-checkout infrastructure tests for PayFast-compatible signature construction over trimmed non-empty non-signature fields, passphrase inclusion only when configured, `payment_method=cc`, and public `notify_url` inclusion in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs`
- [X] T015 [P] [US1] Add failing start-flow application tests for owner-only initiation, pending-attempt persistence before redirect, hosted deadline initialization, and non-crediting initiation failure handling in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs`

### Implementation for User Story 1

- [X] T016 [US1] Implement PayFast signature normalization and hosted request construction for trimmed non-empty non-signature fields, optional passphrase appending, card-only checkout, and config-bound sandbox or live host selection in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedPayments/PayFastSignatureVerifier.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedPayments/PayFastHostedPaymentProvider.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedPayments/PayFastHostedPaymentOptions.cs`
- [X] T017 [US1] Implement owner-scoped start-top-up orchestration, pending-attempt creation, hosted deadline setup, and safe initiation-result DTO mapping in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Services/WalletTopUpService.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/StartWalletTopUpCommand.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/StartWalletTopUpResultDto.cs`
- [X] T018 [P] [US1] Implement pending-attempt persistence for provider references, return correlation, hosted deadlines, and reconciliation timestamps in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/WalletTopUpAttemptRepository.cs` and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepository.cs`
- [X] T019 [P] [US1] Wire PayFast provider configuration, public callback URLs, and option binding in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedPayments/HostedPaymentProviderFactory.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/appsettings.json`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/appsettings.Development.json`
- [X] T020 [US1] Implement the wallet top-up UI with validation, hosted-page disclosure, explicit card-only messaging, and owner-safe “Payment could not be started” handling in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/Wallet.razor`

**Checkpoint**: User Story 1 is complete when owners can start a valid PayFast card-only hosted top-up and invalid starts never create attempts.

---

## Phase 4: User Story 2 - Receive a trustworthy payment result (Priority: P2)

**Goal**: Credit the wallet exactly once from trustworthy server-side PayFast notify evidence while keeping browser returns informational, owner-safe, and audit-linked.

**Independent Test**: Complete, cancel, expire, abandon, and replay top-up flows; verify only trustworthy notify evidence can settle the wallet; verify browser returns remain informational only; verify unsafe cases route to `Top-up not confirmed`; and verify successful callbacks update wallet balance and owner history within 1 minute.

### Tests for User Story 2 (REQUIRED — TDD)

- [X] T021 [P] [US2] Add failing application settlement tests for notify-only authority, exact-match correlation across `Pending`, `Expired`, `Abandoned`, and `NotConfirmed`, confirmed-amount crediting, late authoritative upgrade, replay idempotency, conflict retention, and Payment Confirmation Record linkage in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpOutcomeNormalizerTests.cs`
- [X] T022 [P] [US2] Add failing reconciliation tests for `Pending -> Expired`, `Expired -> Abandoned`, read-through reconciliation, and auditable trigger-source recording in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpAbandonmentServiceTests.cs`
- [X] T023 [P] [US2] Add failing notify-validation infrastructure tests for local signature verification, PayFast step-4 server confirmation, card-only proof, production live-path enforcement, and non-sensitive payload persistence in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs`
- [X] T024 [P] [US2] Add failing bUnit tests for informational-only browser-return landing and owner-scoped result routing in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/WalletTopUpGenericReturnTests.cs` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnTests.cs`
- [X] T025 [P] [US2] Add failing bUnit tests for unified `Top-up not confirmed` messaging, non-crediting browser success, and wallet history freshness on owner pages in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/WalletTopUpNotConfirmedTests.cs` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`
- [X] T026 [P] [US2] Add failing relational settlement persistence tests for Payment Confirmation Record linkage, unmatched-return auditing, SC-005 traceability, and exactly-once wallet credits in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs`
- [X] T027 [P] [US2] Add failing DynamoDB settlement parity tests for Payment Confirmation Record linkage, unmatched-return auditing, SC-005 traceability, and exactly-once wallet credits in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs`

### Implementation for User Story 2

- [X] T028 [US2] Implement PayFast notify parsing and trust evaluation with local signature verification, PayFast-compatible verification parameter construction, step-4 server confirmation, card-only checks, environment checks, and safe payload snapshots in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedPayments/PayFastHostedPaymentProvider.cs` and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedPayments/PayFastSignatureVerifier.cs`
- [X] T029 [US2] Implement callback-authoritative settlement, exact-match evaluation, confirmed-amount crediting, standardized Payment Confirmation Record linkage, generic `Top-up not confirmed` normalization, and orphaned-credit prevention in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Services/WalletTopUpService.cs` and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Services/WalletTopUpOutcomeNormalizer.cs`
- [X] T030 [US2] Implement the public notify endpoint with request metadata forwarding, safe acknowledgement behavior, and no browser-based settlement bypass in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Endpoints/PayFastNotifyEndpoint.cs` and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`
- [X] T031 [US2] Implement scheduled and read-through reconciliation for `Expired` and `Abandoned` outcomes in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/IWalletTopUpAbandonmentService.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Services/WalletTopUpAbandonmentService.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedServices/WalletTopUpReconciliationHostedService.cs`
- [X] T032 [P] [US2] Implement relational settlement persistence for Payment Confirmation Record, normalization decisions, unmatched returns, wallet credits, and one-unit-of-work balance and history freshness in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/PaymentReturnEvidenceRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/OutcomeNormalizationDecisionRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/UnmatchedPaymentReturnRecordRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/WalletActivityRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/WalletRepository.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs`
- [X] T033 [P] [US2] Implement DynamoDB settlement persistence parity for Payment Confirmation Record, normalization decisions, unmatched returns, wallet credits, and exactly-once settlement in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbUnitOfWork.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPaymentReturnEvidenceRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbOutcomeNormalizationDecisionRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbUnmatchedPaymentReturnRecordRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletActivityRepository.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletRepository.cs`
- [X] T034 [US2] Implement owner-safe browser-return processing, owner-scoped result rendering, generic not-confirmed routing, and fresh wallet and history reads in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Services/WalletTopUpService.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/WalletTopUpGenericReturn.razor`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/WalletTopUpReturn.razor`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/WalletTopUpNotConfirmed.razor`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/Wallet.razor`

**Checkpoint**: User Story 2 is complete when trustworthy notify processing is the only settlement authority, browser returns stay informational, unsafe cases remain owner-safe, and settlement is exactly-once with full audit linkage.

---

## Phase 5: User Story 3 - Run live gateway payments safely (Priority: P3)

**Goal**: Run PayFast safely across sandbox and live modes, preserve live-only production settlement, and keep internal review restricted to `SiteAdministrator` users with minimum non-sensitive evidence exposure.

**Independent Test**: Run sandbox validation in a non-production configuration and live mode in production configuration, verify non-live or non-card evidence never settles production wallets, verify SiteAdministrator-only internal review access and privacy-safe fields, and verify DynamoDB startup logs created-versus-confirmed table state before payment traffic begins.

### Tests for User Story 3 (REQUIRED — TDD)

- [X] T035 [P] [US3] Add failing infrastructure tests for config-bound sandbox or live host selection, sandbox acceptance only outside production, live-only production settlement, public `notify_url` validation, card-only settlement rejection of non-card evidence, and owner-safe initiation-failure classification in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs`
- [X] T036 [P] [US3] Add failing DynamoDB bootstrap tests for verify-or-create behavior, active-status waits, created-versus-confirmed logging, and fail-fast startup in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTableProvisionerTests.cs` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTableProvisionerUnitTests.cs`
- [X] T037 [P] [US3] Add failing application review-query tests for SiteAdministrator-only access, Payment Confirmation Record lookup, safe unmatched-return review, and privacy-minimized evidence projection in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpAdminReviewServiceTests.cs`
- [X] T038 [P] [US3] Add failing bUnit tests for SiteAdministrator-only internal review rendering, minimum non-sensitive evidence display, and safe unmatched/conflict rows in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/Admin/WalletTopUpReviewTests.cs`
- [X] T039 [P] [US3] Add failing web startup tests for DynamoDB payment bootstrap registration, public notify route exposure, and startup validation wiring in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs`, `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs`, and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs`

### Implementation for User Story 3

- [X] T040 [US3] Implement config-bound sandbox or live checkout behavior, live-only production settlement enforcement, and owner-safe initiation failure classification in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedPayments/PayFastHostedPaymentOptions.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedPayments/PayFastHostedPaymentProvider.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedPayments/HostedPaymentProviderFactory.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/appsettings.json`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/appsettings.Development.json`
- [X] T041 [US3] Implement SiteAdministrator review DTOs, application query orchestration, and Payment Confirmation Record terminology in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/DTOs/Wallet/SiteAdministratorPaymentReviewDto.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Interfaces/IWalletTopUpService.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application/Services/WalletTopUpService.cs`
- [X] T042 [P] [US3] Implement relational and DynamoDB admin-review query support plus privacy-equivalent evidence filtering in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/WalletTopUpAttemptRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/PaymentReturnEvidenceRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/OutcomeNormalizationDecisionRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/Repositories/UnmatchedPaymentReturnRecordRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletTopUpAttemptRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPaymentReturnEvidenceRepository.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbOutcomeNormalizationDecisionRepository.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbUnmatchedPaymentReturnRecordRepository.cs`
- [X] T043 [US3] Implement DynamoDB payment table verification, automatic creation, activation waits, bootstrap hosting, and created-versus-confirmed logging before traffic is accepted in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/HostedServices/DynamoDbPaymentBootstrapHostedService.cs`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`
- [X] T044 [US3] Implement the SiteAdministrator-only review page, privacy-minimized rendering, and admin navigation entry in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/Admin/WalletTopUpReview.razor`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Shared/NavMenu.razor`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`

**Checkpoint**: User Story 3 is complete when sandbox behavior is configuration-bound, production settlement remains live-only, startup verifies DynamoDB payment tables, and admin review is restricted, privacy-minimized, and read-only.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish release evidence, solution-wide validation, and constitution-aligned test coverage checks across all stories.

- [X] T045 [P] Add SC-002 automated freshness coverage for both wallet balance and owner history visibility within 1 minute in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/WalletTests.cs` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/WalletTopUpReturnTests.cs`
- [X] T046 [P] Add SC-005 automated traceability coverage from wallet credit to Payment Confirmation Record to originating attempt across both persistence providers in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs`
- [X] T047 [P] Finalize UAT and operational evidence documents for SC-004, SC-005, FR-024 review, startup-log inspection, and admin-review privacy checks in `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/checklists/sc-004-uat-sign-off-pack.md` and `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/quickstart.md`
- [ ] T048 Run final feature validation against `/Users/adhirramjiawan/projects/payslip4all/Payslip4All.sln`, `/Users/adhirramjiawan/projects/payslip4all/Directory.Build.props`, and `/Users/adhirramjiawan/projects/payslip4all/specs/009-payfast-card-integration/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies; T001-T003 can start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1; blocks all user stories.
- **Phase 3 (US1)**: Depends on Phase 2.
- **Phase 4 (US2)**: Depends on Phase 2 and the working pending-attempt start flow from US1.
- **Phase 5 (US3)**: Depends on Phase 2 and the working start plus settlement behavior from US1 and US2.
- **Phase 6 (Polish)**: Depends on the user stories selected for release.

### User Story Dependencies

- **US1**: No dependency on other user stories after Foundational is complete.
- **US2**: Depends on US1 because authoritative settlement targets the started pending attempts.
- **US3**: Depends on US1 and US2 because environment safety and admin review must prove the end-to-end payment path.

### Execution Graph

```text
Setup -> Foundational -> US1 -> US2 -> US3 -> Polish
T001-T003 -> T004-T012 -> T013-T020 -> T021-T034 -> T035-T044 -> T045-T048
```

### Within Each User Story

1. Write and verify the failing tests before any implementation task for that story.
2. Update domain, application, and persistence behavior before wiring the final Razor surfaces that depend on it.
3. Keep browser returns informational only; settlement authority belongs only to trustworthy notify handling plus PayFast confirmation.
4. Treat `Top-up not confirmed` as the unified owner-facing message family everywhere unsafe-to-disclose behavior appears.
5. Present the Manual Test Gate prompt after implementation work and before any `git commit`, `git merge`, or `git push`.

---

## Parallel Opportunities

- T001-T003 can run in parallel.
- T004-T007 can run in parallel.
- US1 tests T013-T015 can run in parallel.
- US2 tests T021-T027 can run in parallel.
- US2 persistence tasks T032 and T033 can run in parallel after T029 defines application behavior.
- US3 tests T035-T039 can run in parallel.
- US3 review-query implementation T041 and T042 can run in parallel after T037 clarifies the failing contract.
- Polish tasks T045-T047 can run in parallel; T048 stays last.

---

## Parallel Example: User Story 1

```bash
Task: T013 [US1] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/WalletTests.cs
Task: T014 [US1] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs
Task: T015 [US1] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs
```

## Parallel Example: User Story 2

```bash
Task: T021 [US2] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpServiceTests.cs
Task: T023 [US2] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs
Task: T024 [US2] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/WalletTopUpGenericReturnTests.cs
Task: T026 [US2] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/Repositories/WalletTopUpSettlementTests.cs
Task: T027 [US2] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletTopUpSettlementTests.cs
```

## Parallel Example: User Story 3

```bash
Task: T035 [US3] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs
Task: T036 [US3] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTableProvisionerTests.cs
Task: T037 [US3] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Services/WalletTopUpAdminReviewServiceTests.cs
Task: T038 [US3] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Pages/Admin/WalletTopUpReviewTests.cs
Task: T039 [US3] /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs
```

---

## Implementation Strategy

### MVP First

1. Complete Setup and Foundational.
2. Deliver US1 only.
3. Validate the `/portal/wallet` top-up start flow independently.
4. Demo the hosted card-only start journey before settlement work begins.

### Incremental Delivery

1. Add US1 for hosted checkout start.
2. Add US2 for authoritative settlement, browser-return safety, and reconciliation.
3. Add US3 for environment safety, admin review, and startup provisioning.
4. Finish with cross-cutting release evidence and final validation.

### Suggested MVP Scope

- **MVP**: User Story 1 only.
- **Release-ready core**: User Stories 1 and 2.
- **Full feature**: User Stories 1, 2, and 3 plus Polish.

### Release Evidence Mapping

| Criterion | Planned evidence |
|-----------|------------------|
| SC-001 | T014, T023, T035 |
| SC-002 | T024, T025, T032, T033, T045 |
| SC-003 | T021, T023, T026, T027, T035 |
| SC-004 | T003, T047 |
| SC-005 | T026, T027, T046 |

---

## Notes

- All tasks remain unchecked by design.
- Setup, Foundational, and Polish phases intentionally have no story labels.
- User-story phases use `[US1]`, `[US2]`, and `[US3]` on every task.
- Explicit bUnit coverage is planned for every affected Blazor page: `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/Wallet.razor`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/WalletTopUpGenericReturn.razor`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/WalletTopUpReturn.razor`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/WalletTopUpNotConfirmed.razor`, and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Pages/Admin/WalletTopUpReview.razor`.
- Browser-return tasks explicitly preserve informational-only behavior and routing either to an owner-scoped result route or the generic not-confirmed route.
- Notify-validation tasks explicitly require local verification plus PayFast step-4 server confirmation before any settlement decision.
- Environment tasks explicitly preserve configuration-bound sandbox handling only outside production and live-only settlement in production.
- Internal review tasks explicitly keep evidence exposure SiteAdministrator-only, read-only, and limited to minimum non-sensitive fields.
