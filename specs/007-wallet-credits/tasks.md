# Tasks: Customer Wallet Credits

**Input**: Design documents from `/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/`  
**Prerequisites**: `/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/plan.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/spec.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/research.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/data-model.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/quickstart.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/contracts/wallet-application-contract.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/007-wallet-credits/contracts/wallet-ui-contract.md`

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for all stories in this feature. Write failing xUnit, Moq, repository integration, DynamoDB parity, Razor Page integration, and bUnit tests before implementation.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently after the foundational phase is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel when they touch different files and do not depend on incomplete tasks
- **[Story]**: User story label for traceability (`[US1]`, `[US2]`, `[US3]`, `[US4]`)
- Every task includes the exact file path or directory to change

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Refresh shared scaffolding so the wallet, pricing, and public landing-page work can start with failing tests first.

- [X] T001 Refresh shared wallet and pricing seed helpers in `tests/Payslip4All.Infrastructure.Tests/Repositories/RepositoryIntegrationTests.cs` and `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTestFixture.cs`
- [X] T002 [P] Create wallet, admin-pricing, and public-homepage web test scaffolding in `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`, `tests/Payslip4All.Web.Tests/Pages/Admin/WalletPricingTests.cs`, `tests/Payslip4All.Web.Tests/Pages/IndexModelTests.cs`, and `tests/Payslip4All.Web.Tests/Integration/PublicLandingPageTests.cs`
- [X] T003 [P] Create shared pricing and wallet test builders in `tests/Payslip4All.Application.Tests/Services/PayslipGenerationServiceTests.cs`, `tests/Payslip4All.Application.Tests/Services/PayslipPricingServiceTests.cs`, and `tests/Payslip4All.Application.Tests/Services/WalletServiceTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared domain, persistence, provider-parity, and dependency-injection seams required before any user story can ship.

**⚠️ CRITICAL**: Complete this phase before starting any user story work.

### Tests for Foundational Phase (REQUIRED — TDD)

- [X] T004 [P] Add wallet domain rule tests in `tests/Payslip4All.Domain.Tests/Entities/WalletTests.cs`, `tests/Payslip4All.Domain.Tests/Entities/WalletActivityTests.cs`, `tests/Payslip4All.Domain.Tests/Entities/PayslipPricingSettingTests.cs`, and `tests/Payslip4All.Domain.Tests/Services/WalletCalculatorTests.cs`
- [X] T005 [P] Add EF Core and DynamoDB repository parity tests in `tests/Payslip4All.Infrastructure.Tests/Repositories/WalletRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Repositories/WalletActivityRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Repositories/PayslipPricingRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletRepositoryTests.cs`, `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletActivityRepositoryTests.cs`, and `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbPayslipPricingRepositoryTests.cs`
- [X] T006 [P] Add startup and provider-switch tests for wallet and public pricing registration in `tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs`, `tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs`, and `tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs`

### Implementation for Foundational Phase

- [X] T007 Create shared wallet and pricing domain types in `src/Payslip4All.Domain/Entities/Wallet.cs`, `src/Payslip4All.Domain/Entities/WalletActivity.cs`, `src/Payslip4All.Domain/Entities/PayslipPricingSetting.cs`, `src/Payslip4All.Domain/Enums/WalletActivityType.cs`, and `src/Payslip4All.Domain/Services/WalletCalculator.cs`
- [X] T008 [P] Create wallet and pricing DTOs plus contracts in `src/Payslip4All.Application/DTOs/Wallet/WalletDto.cs`, `src/Payslip4All.Application/DTOs/Wallet/WalletActivityDto.cs`, `src/Payslip4All.Application/DTOs/Wallet/AddWalletCreditCommand.cs`, `src/Payslip4All.Application/DTOs/Pricing/PayslipPricingSettingDto.cs`, `src/Payslip4All.Application/DTOs/Pricing/UpdatePayslipPriceCommand.cs`, `src/Payslip4All.Application/Interfaces/IWalletService.cs`, `src/Payslip4All.Application/Interfaces/IPayslipPricingService.cs`, `src/Payslip4All.Application/Interfaces/Repositories/IWalletRepository.cs`, `src/Payslip4All.Application/Interfaces/Repositories/IWalletActivityRepository.cs`, and `src/Payslip4All.Application/Interfaces/Repositories/IPayslipPricingRepository.cs`
- [X] T009 Implement shared wallet and pricing application services in `src/Payslip4All.Application/Services/WalletService.cs` and `src/Payslip4All.Application/Services/PayslipPricingService.cs`
- [X] T010 Implement EF Core wallet and pricing schema plus repositories in `src/Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs`, `src/Payslip4All.Infrastructure/Persistence/Repositories/WalletRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/Repositories/WalletActivityRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/Repositories/PayslipPricingRepository.cs`, and `src/Payslip4All.Infrastructure/Migrations/20260401084422_AddWalletAndPricing.cs`
- [X] T011 [P] Implement DynamoDB wallet and pricing repositories plus table provisioning in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbWalletActivityRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPayslipPricingRepository.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs`, and `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs`
- [X] T012 Register wallet and pricing services for relational and DynamoDB providers in `src/Payslip4All.Web/Program.cs`

**Checkpoint**: Foundation ready — wallet entities, pricing services, EF Core persistence, DynamoDB parity, and DI registration exist for all story phases.

---

## Phase 3: User Story 1 - Generate payslips using wallet credits (Priority: P1) 🎯 MVP

**Goal**: Charge the authenticated company owner wallet only when payslip generation succeeds and block generation when funds are insufficient.

**Independent Test**: Fund a wallet, generate a payslip successfully, then attempt another generation with insufficient funds and confirm only the successful generation creates a debit and records the charged amount.

### Tests for User Story 1 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation**

- [X] T013 [P] [US1] Extend wallet-charge orchestration tests for success, failure, and latest-price handling in `tests/Payslip4All.Application.Tests/Services/PayslipGenerationServiceTests.cs`
- [X] T014 [P] [US1] Add generate-payslip wallet balance, charged amount, and insufficient-funds UI tests in `tests/Payslip4All.Web.Tests/Pages/GeneratePayslipTests.cs`
- [X] T015 [P] [US1] Add immediate price-update propagation and validation tests in `tests/Payslip4All.Application.Tests/Services/PayslipPricingServiceTests.cs` and `tests/Payslip4All.Web.Tests/Pages/Admin/WalletPricingTests.cs`

### Implementation for User Story 1

- [X] T016 [US1] Extend payslip charge metadata in `src/Payslip4All.Domain/Entities/Payslip.cs`, `src/Payslip4All.Application/DTOs/Payslip/PayslipDto.cs`, and `src/Payslip4All.Application/DTOs/Payslip/PayslipResult.cs`
- [X] T017 [US1] Implement wallet debit orchestration and insufficient-funds blocking in `src/Payslip4All.Application/Services/PayslipGenerationService.cs` and `src/Payslip4All.Application/Interfaces/IPayslipService.cs`
- [X] T018 [P] [US1] Implement the SiteAdministrator pricing page at `src/Payslip4All.Web/Pages/Admin/WalletPricing.razor`
- [X] T019 [US1] Update the payslip generation workflow with wallet balance, current price, charged-amount confirmation, and insufficient-funds messaging in `src/Payslip4All.Web/Pages/Payslips/GeneratePayslip.razor`
- [X] T020 [P] [US1] Add SiteAdministrator wallet-pricing navigation in `src/Payslip4All.Web/Shared/NavMenu.razor`

**Checkpoint**: User Story 1 is complete when pricing can change, successful payslip generations debit the latest price, and insufficient-funds attempts leave balance unchanged.

---

## Phase 4: User Story 2 - Add funds to the wallet (Priority: P2)

**Goal**: Let a company owner add positive rand credits to their wallet and record each successful credit as a ledger entry.

**Independent Test**: Open `/portal/wallet`, submit valid and invalid top-up amounts, and verify the balance and credit activity change only after valid submissions.

### Tests for User Story 2 (REQUIRED — TDD, constitution Principle I)

- [X] T021 [P] [US2] Add wallet top-up service tests for valid amounts, invalid amounts, and cumulative balances in `tests/Payslip4All.Application.Tests/Services/WalletServiceTests.cs`
- [X] T022 [P] [US2] Add wallet top-up bUnit tests for form submission and validation feedback in `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`
- [X] T023 [P] [US2] Add EF Core and DynamoDB top-up ledger tests in `tests/Payslip4All.Infrastructure.Tests/Repositories/WalletActivityRepositoryTests.cs` and `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletActivityRepositoryTests.cs`

### Implementation for User Story 2

- [X] T024 [US2] Implement wallet top-up command handling and ledger credits in `src/Payslip4All.Application/Services/WalletService.cs`
- [X] T025 [US2] Implement the CompanyOwner wallet top-up workflow at `src/Payslip4All.Web/Pages/Wallet.razor`
- [X] T026 [P] [US2] Add the CompanyOwner wallet navigation link in `src/Payslip4All.Web/Shared/NavMenu.razor`

**Checkpoint**: User Story 2 is complete when owners can add funds through `/portal/wallet`, invalid amounts are rejected, and each valid top-up creates exactly one credit activity.

---

## Phase 5: User Story 3 - See wallet balance, pricing, and activity (Priority: P3)

**Goal**: Show each company owner their current balance, current payslip price, and recent wallet activity with strict ownership filtering.

**Independent Test**: Open `/portal/wallet` for owners with and without wallet history and confirm the page shows only that owner’s balance, current price, and newest-first activity.

### Tests for User Story 3 (REQUIRED — TDD, constitution Principle I)

- [X] T027 [P] [US3] Add wallet read-model tests for zero-balance views, newest-first activity ordering, and ownership filtering in `tests/Payslip4All.Application.Tests/Services/WalletServiceTests.cs`
- [X] T028 [P] [US3] Add wallet dashboard bUnit tests for balance, current price, activity history, and empty-state messaging in `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`
- [X] T029 [P] [US3] Add cross-owner wallet access tests in `tests/Payslip4All.Web.Tests/Security/CrossEmployerHttpTests.cs` and `tests/Payslip4All.Infrastructure.Tests/Repositories/WalletRepositoryTests.cs`

### Implementation for User Story 3

- [X] T030 [US3] Implement wallet dashboard queries and recent-activity projection in `src/Payslip4All.Application/Services/WalletService.cs` and `src/Payslip4All.Application/DTOs/Wallet/WalletDto.cs`
- [X] T031 [US3] Update `/portal/wallet` to show current price, balance summary, ordered activity history, and empty-state messaging in `src/Payslip4All.Web/Pages/Wallet.razor`
- [X] T032 [P] [US3] Surface wallet summary information on the owner dashboard in `src/Payslip4All.Web/Pages/Dashboard.razor`

**Checkpoint**: User Story 3 is complete when wallet reads show zero-state and activity-state views correctly, display the latest price, and never reveal another owner’s data.

---

## Phase 6: User Story 4 - Understand wallet pricing from the public landing page (Priority: P4)

**Goal**: Show anonymous visitors how wallet credits work on `/` and display the current public payslip price without exposing private account data.

**Independent Test**: Visit `/` while signed out and verify the page explains wallet credits, shows the configured public rand price, keeps registration and sign-in CTAs visible, and never shows customer-specific balances or wallet activity.

### Tests for User Story 4 (REQUIRED — TDD, constitution Principle I)

- [X] T033 [P] [US4] Add anonymous landing-page integration tests for wallet messaging, current public price, and CTA visibility in `tests/Payslip4All.Web.Tests/Integration/PublicLandingPageTests.cs`
- [X] T034 [P] [US4] Add public pricing page-model tests for configured-price loading and fallback messaging in `tests/Payslip4All.Web.Tests/Pages/IndexModelTests.cs`

### Implementation for User Story 4

- [X] T035 [US4] Load public pricing and fallback display state in `src/Payslip4All.Web/Pages/Index.cshtml.cs`
- [X] T036 [US4] Add wallet-credit marketing copy and current public payslip price content to `src/Payslip4All.Web/Pages/Index.cshtml`
- [X] T037 [US4] Refine the public landing-page wallet section with anonymous-safe copy, unavailable-price fallback text, and registration/login calls to action in `src/Payslip4All.Web/Pages/Index.cshtml`

**Checkpoint**: User Story 4 is complete when the public home page explains wallet credits, shows the current configured price when available, and remains free of private wallet data.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Tighten regressions, documentation, provider parity, and public/homepage resilience across all wallet flows.

- [X] T038 [P] Add quickstart-aligned UI regression coverage in `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`, `tests/Payslip4All.Web.Tests/Pages/GeneratePayslipTests.cs`, `tests/Payslip4All.Web.Tests/Pages/Admin/WalletPricingTests.cs`, and `tests/Payslip4All.Web.Tests/Integration/PublicLandingPageTests.cs`
- [X] T039 [P] Add public-price provider-switch and startup regression coverage in `tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs` and `tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs`
- [ ] T040 [P] Update rollout and manual verification guidance for wallet pricing, top-ups, payslip charging, and the public landing page in `README.md` and `specs/007-wallet-credits/quickstart.md`
- [ ] T041 Harden wallet, pricing, and public-homepage error handling plus user-facing copy in `src/Payslip4All.Application/Services/WalletService.cs`, `src/Payslip4All.Application/Services/PayslipPricingService.cs`, `src/Payslip4All.Web/Pages/Wallet.razor`, `src/Payslip4All.Web/Pages/Payslips/GeneratePayslip.razor`, `src/Payslip4All.Web/Pages/Index.cshtml.cs`, and `src/Payslip4All.Web/Pages/Index.cshtml`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — blocks all user stories until complete
- **User Story 1 (Phase 3)**: Depends on Foundational completion
- **User Story 2 (Phase 4)**: Depends on Foundational completion and can proceed independently of US1
- **User Story 3 (Phase 5)**: Depends on Foundational completion and can proceed independently of US1 and US2, though `src/Payslip4All.Web/Pages/Wallet.razor` coordination is required if US2 and US3 run concurrently
- **User Story 4 (Phase 6)**: Depends on Foundational completion and can proceed independently of US1-US3 because it uses only public pricing reads and landing-page rendering
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: No dependency on other user stories; this is the MVP charging flow
- **US2 (P2)**: No dependency on US1; reuses foundational wallet services and repositories
- **US3 (P3)**: No business dependency on US1 or US2; coordinate changes to `src/Payslip4All.Web/Pages/Wallet.razor` if parallel work is active
- **US4 (P4)**: No dependency on authenticated wallet stories; it depends only on the shared pricing service and public Razor Page model

### Within Each User Story

- Write tests first and confirm they fail before implementation begins
- Complete Domain/Application work before Web UI wiring
- Keep ownership filtering in repositories and services by authenticated `UserId`
- Maintain EF Core and DynamoDB parity for every repository-backed wallet and pricing path
- Keep public landing-page work anonymous-safe and limited to site-wide pricing plus marketing copy
- **Manual Test Gate (Principle VI)**: After implementation is complete, present the manual test gate prompt and await explicit engineer approval before any `git commit`, `git merge`, or `git push`
- Story completion requires passing automated tests, manual verification approval, and green CI

### Parallel Opportunities

- T002 and T003 can run in parallel during setup
- T004, T005, and T006 can run in parallel before foundational implementation
- T008 and T011 can run in parallel after T007 establishes the shared domain model
- In US1, T013, T014, and T015 can run in parallel; T018 and T020 can run in parallel after T017
- In US2, T021, T022, and T023 can run in parallel; T025 and T026 can run in parallel after T024
- In US3, T027, T028, and T029 can run in parallel; T031 and T032 can run in parallel after T030
- In US4, T033 and T034 can run in parallel; T035 must land before T036 and T037
- In Polish, T038, T039, and T040 can run in parallel before T041 finalizes cross-cutting copy and error handling

---

## Parallel Example: User Story 1

```bash
# Write the failing US1 tests together
Task: "T013 Extend wallet-charge orchestration tests in tests/Payslip4All.Application.Tests/Services/PayslipGenerationServiceTests.cs"
Task: "T014 Add generate-payslip wallet balance, charged amount, and insufficient-funds UI tests in tests/Payslip4All.Web.Tests/Pages/GeneratePayslipTests.cs"
Task: "T015 Add immediate price-update propagation tests in tests/Payslip4All.Application.Tests/Services/PayslipPricingServiceTests.cs and tests/Payslip4All.Web.Tests/Pages/Admin/WalletPricingTests.cs"

# After application charging logic lands, finish the admin UI pieces together
Task: "T018 Implement the SiteAdministrator pricing page at src/Payslip4All.Web/Pages/Admin/WalletPricing.razor"
Task: "T020 Add SiteAdministrator wallet-pricing navigation in src/Payslip4All.Web/Shared/NavMenu.razor"
```

## Parallel Example: User Story 2

```bash
# Write the failing US2 tests together
Task: "T021 Add wallet top-up service tests in tests/Payslip4All.Application.Tests/Services/WalletServiceTests.cs"
Task: "T022 Add wallet top-up bUnit tests in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs"
Task: "T023 Add EF Core and DynamoDB top-up ledger tests in tests/Payslip4All.Infrastructure.Tests/Repositories/WalletActivityRepositoryTests.cs and tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbWalletActivityRepositoryTests.cs"

# After the wallet top-up logic is ready, finish the owner-facing UI together
Task: "T025 Implement the CompanyOwner wallet top-up workflow at src/Payslip4All.Web/Pages/Wallet.razor"
Task: "T026 Add the CompanyOwner wallet navigation link in src/Payslip4All.Web/Shared/NavMenu.razor"
```

## Parallel Example: User Story 3

```bash
# Write the failing US3 tests together
Task: "T027 Add wallet read-model tests in tests/Payslip4All.Application.Tests/Services/WalletServiceTests.cs"
Task: "T028 Add wallet dashboard bUnit tests in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs"
Task: "T029 Add cross-owner wallet access tests in tests/Payslip4All.Web.Tests/Security/CrossEmployerHttpTests.cs and tests/Payslip4All.Infrastructure.Tests/Repositories/WalletRepositoryTests.cs"

# After the read model is ready, update separate UI surfaces together
Task: "T031 Update /portal/wallet in src/Payslip4All.Web/Pages/Wallet.razor"
Task: "T032 Surface wallet summary information on the owner dashboard in src/Payslip4All.Web/Pages/Dashboard.razor"
```

## Parallel Example: User Story 4

```bash
# Write the failing US4 tests together
Task: "T033 Add anonymous landing-page integration tests in tests/Payslip4All.Web.Tests/Integration/PublicLandingPageTests.cs"
Task: "T034 Add public pricing page-model tests in tests/Payslip4All.Web.Tests/Pages/IndexModelTests.cs"

# After the Index page model exposes the public price, finish the landing-page content
Task: "T036 Add wallet-credit marketing copy and current public payslip price content to src/Payslip4All.Web/Pages/Index.cshtml"
Task: "T037 Refine the public landing-page wallet section with anonymous-safe copy and fallback text in src/Payslip4All.Web/Pages/Index.cshtml"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Present the Manual Test Gate prompt and await `approve`
5. Validate the quickstart scenarios for pricing changes and funded vs. insufficient wallet generation attempts
6. Stop after US1 if only the MVP is required

### Incremental Delivery

1. Deliver Setup + Foundational to establish shared wallet and pricing infrastructure
2. Deliver US1 to enforce wallet charging during payslip generation
3. Deliver US2 to restore wallet balances through owner-initiated top-ups
4. Deliver US3 to improve transparency with balance, price, and activity views
5. Deliver US4 to expose public wallet messaging and the current public price on `/`
6. Finish with Polish for regression safety, docs, provider parity, and message hardening

### Parallel Team Strategy

1. One developer owns foundational domain and persistence work while another prepares failing Application, Web, and integration tests
2. After foundational completion:
   - Developer A: US1 charging flow and admin pricing page
   - Developer B: US2 top-up flow
   - Developer C: US3 wallet visibility and security coverage
   - Developer D: US4 public landing-page pricing and messaging
3. Coordinate changes to `src/Payslip4All.Web/Pages/Wallet.razor` between US2 and US3 and to `src/Payslip4All.Web/Pages/Index.cshtml` between US4 and Polish work

---

## Notes

- All task lines follow the required checklist pattern `- [ ] T### ...`
- `[P]` marks tasks that can proceed without waiting on another incomplete task in the same phase
- `[US1]`, `[US2]`, `[US3]`, and `[US4]` appear only on user story tasks
- Tests are mandatory for this feature because the constitution requires TDD
- Maintain auditable wallet activity and provider parity for both EF Core and DynamoDB paths
- Keep the public landing page limited to public pricing and wallet messaging only
- Do not perform any `git commit`, `git merge`, or `git push` until the Manual Test Gate is approved
