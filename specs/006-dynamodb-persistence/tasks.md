---

description: "Regenerated task list for 006-dynamodb-persistence implementation"
---

# Tasks: AWS DynamoDB Persistence Option

**Input**: Design documents from `/specs/006-dynamodb-persistence/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for this feature. Write the failing tests for each implementation phase before changing production files.

**Organization**: Tasks are grouped by phase and by user story so each story remains independently implementable and testable.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Task can run in parallel with other marked tasks because it targets different files and has no dependency on incomplete work
- **[Story]**: User story label for story phases only (`[US1]`, `[US2]`, `[US3]`)
- Every task includes the exact repo file path to change or create

---

## Phase 1: Setup

**Purpose**: Align packages and shared DynamoDB test scaffolding before feature work starts.

- [ ] T001 [P] Confirm or update `AWSSDK.DynamoDBv2` package references in `src/Payslip4All.Infrastructure/Payslip4All.Infrastructure.csproj` and `tests/Payslip4All.Infrastructure.Tests/Payslip4All.Infrastructure.Tests.csproj`
- [ ] T002 [P] Reconcile shared DynamoDB test bootstrap, unique table-prefix isolation, and deterministic cleanup in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTestFixture.cs`

---

## Phase 2: Foundational

**Purpose**: Build the shared DynamoDB infrastructure that blocks all user stories.

**⚠️ CRITICAL**: Complete this phase before starting user-story implementation.

### Tests for Foundational Infrastructure (REQUIRED — TDD)

- [ ] T003 [P] Add failing environment-variable-only client factory coverage for `DYNAMODB_REGION`, `DYNAMODB_ENDPOINT`, `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY` in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbClientFactoryTests.cs`
- [ ] T004 [P] Add failing no-op unit-of-work coverage for `SaveChangesAsync`, transaction methods, and disposal behavior in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbUnitOfWorkTests.cs`
- [ ] T005 [P] Add failing table-provisioner coverage for default and explicit `DYNAMODB_TABLE_PREFIX`, six-table creation, missing-table logs, ACTIVE waits, and startup permission failures in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTableProvisionerTests.cs`

### Implementation for Foundational Infrastructure

- [ ] T006 [P] Implement environment-variable-only DynamoDB client construction and required variable validation in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbClientFactory.cs`
- [ ] T007 [P] Implement no-op `IUnitOfWork` behavior in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbUnitOfWork.cs`
- [ ] T008 Implement DynamoDB DI registration for the client, repositories, unit of work, and hosted table provisioner in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs`
- [ ] T009 Implement six-table auto-provisioning, missing-table creation logs, ACTIVE polling, and fail-fast permission handling in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs`

**Checkpoint**: DynamoDB infrastructure is ready for provider switching and repository work.

---

## Phase 3: User Story 1 - Configure DynamoDB Provider via Environment Variables (Priority: P1) 🎯 MVP

**Goal**: Deployment operators can select DynamoDB entirely through environment variables, and the app bypasses the relational startup path when `PERSISTENCE_PROVIDER=dynamodb`.

**Independent Test**: Start the app with `PERSISTENCE_PROVIDER=dynamodb`, valid `DYNAMODB_REGION`, `DYNAMODB_ENDPOINT`, `DYNAMODB_TABLE_PREFIX`, `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY` values, and verify DynamoDB services register, missing tables are provisioned, relational startup is bypassed, and startup succeeds. Then start with invalid provider or missing required variables and verify fail-fast errors.

### Tests for User Story 1 (REQUIRED — TDD)

- [ ] T010 [P] [US1] Add failing provider-switching coverage for default SQLite, explicit MySQL, explicit DynamoDB, invalid values, and trim/case-insensitive parsing in `tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs`
- [ ] T011 [P] [US1] Add failing startup-validation coverage for `DYNAMODB_REGION`, `DYNAMODB_ENDPOINT`, `DYNAMODB_TABLE_PREFIX`, `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY` in `tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs`
- [ ] T012 [P] [US1] Add failing startup-bypass coverage proving `PERSISTENCE_PROVIDER=dynamodb` skips `PayslipDbContext` registration and relational migration execution in `tests/Payslip4All.Web.Tests/Startup/DynamoDbRelationalBypassTests.cs`

### Implementation for User Story 1

- [ ] T013 [US1] Update provider selection, valid-value guards, trimming, and environment-variable-only DynamoDB validation in `src/Payslip4All.Web/Program.cs`
- [ ] T014 [US1] Update startup branching so `src/Payslip4All.Web/Program.cs` bypasses `PayslipDbContext` initialization, EF repository registration, and relational migration execution when `PERSISTENCE_PROVIDER=dynamodb`
- [ ] T015 [US1] Update DynamoDB operator guidance comments to reflect environment-variable-only credentials and explicit `DYNAMODB_*` variables in `src/Payslip4All.Web/Program.cs` and `src/Payslip4All.Web/appsettings.json`

**Checkpoint**: Provider selection works end to end, and the DynamoDB path no longer touches relational startup code.

---

## Phase 4: User Story 2 - Read and Write All Domain Data via DynamoDB (Priority: P2)

**Goal**: Company owners can manage users, companies, employees, payslips, and loans through DynamoDB with correct ownership filtering, navigation hydration, table-backed persistence, and sanitized user-facing failure handling.

**Independent Test**: Run the application against DynamoDB, then create and query employees, generate payslips, and record loans for one owner while confirming a second owner never sees that data. Trigger throttling, temporary unavailability, and permission failures and verify user-facing responses remain sanitized while operator logs keep diagnostic detail.

### Tests for User Story 2 (REQUIRED — TDD)

- [ ] T016 [P] [US2] Add failing CRUD and lookup coverage for DynamoDB user persistence in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbUserRepositoryTests.cs`
- [ ] T017 [P] [US2] Add failing ownership-filtering and company-hydration coverage in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbCompanyRepositoryTests.cs`
- [ ] T018 [P] [US2] Add failing employee ownership, uniqueness, company hydration, and loan-hydration coverage in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbEmployeeRepositoryTests.cs`
- [ ] T019 [P] [US2] Add failing loan CRUD, ownership, and optimistic-concurrency coverage in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbLoanRepositoryTests.cs`
- [ ] T020 [P] [US2] Add failing payslip CRUD, reverse-chronological reads, hydrated loan deductions, and payslip-loan-deduction persistence coverage in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbPayslipRepositoryTests.cs`
- [ ] T021 [P] [US2] Add failing sanitized-response coverage for DynamoDB throttling, temporary unavailability, and permission exceptions in `tests/Payslip4All.Web.Tests/Middleware/GlobalExceptionMiddlewareTests.cs`

### Implementation for User Story 2

- [ ] T022 [P] [US2] Implement or reconcile DynamoDB user persistence against `IUserRepository` in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbUserRepository.cs`
- [ ] T023 [P] [US2] Implement or reconcile DynamoDB company persistence and ownership filtering against `ICompanyRepository` in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbCompanyRepository.cs`
- [ ] T024 [US2] Implement or reconcile employee persistence, denormalized `userId`, uniqueness checks, and hydrated `Company` and `Loans` navigation data in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbEmployeeRepository.cs`
- [ ] T025 [US2] Implement or reconcile loan persistence, conditional `termsCompleted` updates, and ownership-safe reads in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbLoanRepository.cs`
- [ ] T026 [US2] Implement or reconcile payslip persistence, hydrated loan deductions, and payslip-loan-deduction storage inside `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPayslipRepository.cs`
- [ ] T027 [US2] Update DynamoDB runtime exception sanitization and operator logging in `src/Payslip4All.Web/Middleware/GlobalExceptionMiddleware.cs`

**Checkpoint**: DynamoDB repository parity is achieved while keeping payslip-loan-deduction persistence inside `DynamoDbPayslipRepository.cs`.

---

## Phase 5: User Story 3 - Local Development with DynamoDB Local (Priority: P3)

**Goal**: Developers can run the app and tests against DynamoDB Local using only environment variables and without an AWS account.

**Independent Test**: Start DynamoDB Local on `http://localhost:8000`, set `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_ENDPOINT=http://localhost:8000`, dummy `AWS_ACCESS_KEY_ID`, dummy `AWS_SECRET_ACCESS_KEY`, and a unique `DYNAMODB_TABLE_PREFIX`, then boot the app and verify startup creates the required tables before serving requests.

### Tests for User Story 3 (REQUIRED — TDD)

- [ ] T028 [P] [US3] Add failing DynamoDB Local client coverage for `DYNAMODB_ENDPOINT=http://localhost:8000`, dummy environment-variable credentials, and no AWS account assumptions in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbClientFactoryTests.cs`
- [ ] T029 [P] [US3] Add failing end-to-end DynamoDB Local startup coverage for prefixed table creation and startup logs before request handling in `tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs`

### Implementation for User Story 3

- [ ] T030 [US3] Reconcile local-emulator fixture and provisioner behavior for deterministic table creation and cleanup with unique prefixes in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTestFixture.cs` and `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs`
- [ ] T031 [US3] Update local-development and AWS deployment guidance to environment-variable-only DynamoDB configuration in `specs/006-dynamodb-persistence/quickstart.md`

**Checkpoint**: Developers can run and validate the feature locally with DynamoDB Local only.

---

## Phase 6: Polish

**Purpose**: Preserve non-DynamoDB behavior, finalize operator documentation, and lock in regression coverage.

- [ ] T032 [P] Add or update startup regression coverage for unchanged SQLite and MySQL behavior and removal of `DatabaseProvider` fallback assumptions in `tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs`
- [ ] T033 [P] Add or update relational repository regression coverage so SQLite and MySQL behavior stays green after DynamoDB changes in `tests/Payslip4All.Infrastructure.Tests/Repositories/RepositoryIntegrationTests.cs`
- [ ] T034 Update provider-configuration documentation to reflect environment-variable-only DynamoDB credentials, missing-table auto-creation, and unsupported cross-provider migration in `README.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; begin immediately
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational
- **User Story 2 (Phase 4)**: Depends on Foundational and is validated after US1 so provider wiring already exists
- **User Story 3 (Phase 5)**: Depends on Foundational and benefits from US1 startup/provider switching
- **Polish (Phase 6)**: Depends on the stories you intend to ship

### User Story Dependencies

- **US1**: No dependency on other user stories after Foundational
- **US2**: Depends on Foundational infrastructure and should be delivered after US1
- **US3**: Depends on Foundational infrastructure and US1 startup/provider switching

### Within Each User Story

- Tests must be written and confirmed failing before implementation starts
- Shared infrastructure before story-specific implementation
- Repository work before runtime exception-handling validation
- **Manual Test Gate (Principle VI)**: After each implementation slice, present the manual test gate prompt and wait for engineer approval before any `git commit`, `git merge`, or `git push`

### Suggested Execution Order

1. Complete Setup
2. Complete Foundational
3. Deliver **US1** as the MVP
4. Deliver **US2** for full DynamoDB parity
5. Deliver **US3** for local developer workflow
6. Finish Polish and regression coverage

---

## Parallel Opportunities

- **Setup**: `T001` and `T002` can run in parallel
- **Foundational tests**: `T003`, `T004`, and `T005` can run in parallel
- **Foundational implementation**: `T006` and `T007` can run in parallel; `T008` depends on the client and unit-of-work shapes from those tasks
- **US1 tests**: `T010`, `T011`, and `T012` can run in parallel
- **US2 tests**: `T016` through `T021` can run in parallel
- **US2 implementation**: `T022` and `T023` can run in parallel; `T024` through `T026` follow as entity relationships deepen
- **US3 tests**: `T028` and `T029` can run in parallel
- **Polish**: `T032` and `T033` can run in parallel

---

## Parallel Example: User Story 1

```bash
# Write the startup tests together
Task: "T010 Update tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs"
Task: "T011 Add tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs"
Task: "T012 Add tests/Payslip4All.Web.Tests/Startup/DynamoDbRelationalBypassTests.cs"
```

## Parallel Example: User Story 2

```bash
# Write repository and middleware tests together
Task: "T016 Update tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbUserRepositoryTests.cs"
Task: "T017 Update tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbCompanyRepositoryTests.cs"
Task: "T018 Update tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbEmployeeRepositoryTests.cs"
Task: "T019 Update tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbLoanRepositoryTests.cs"
Task: "T020 Update tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbPayslipRepositoryTests.cs"
Task: "T021 Update tests/Payslip4All.Web.Tests/Middleware/GlobalExceptionMiddlewareTests.cs"
```

## Parallel Example: User Story 3

```bash
# Validate local-emulator coverage together
Task: "T028 Update tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbClientFactoryTests.cs"
Task: "T029 Add tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Setup and Foundational phases
2. Complete **US1** so `PERSISTENCE_PROVIDER=dynamodb` is selectable entirely by environment variables
3. Verify DynamoDB startup validates required environment variables, auto-creates missing tables, and bypasses relational initialization and migrations
4. Stop and validate the MVP before expanding to repository parity

### Incremental Delivery

1. **US1**: Provider switching, env-var-only configuration, and startup safety
2. **US2**: Full DynamoDB CRUD parity, ownership isolation, and sanitized runtime failures
3. **US3**: Local developer workflow with DynamoDB Local
4. **Polish**: Relational regression safety and operator documentation

### Architecture Consistency Rule

1. Keep all DynamoDB persistence under `src/Payslip4All.Infrastructure/Persistence/DynamoDB/`
2. Keep payslip-loan-deduction persistence inside `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPayslipRepository.cs`
3. Do **not** add IAM role support, shared credential-file support, or generic SDK credential-chain fallback work for DynamoDB configuration

---

## Notes

- Explicitly cover `DYNAMODB_REGION`, `DYNAMODB_ENDPOINT`, `DYNAMODB_TABLE_PREFIX`, `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY`
- When `PERSISTENCE_PROVIDER=dynamodb`, the relational path must not initialize `PayslipDbContext` or execute EF Core migrations
- Missing DynamoDB tables must be auto-created at startup and logged clearly for operators
- Throttling, temporary unavailability, and permission failures must show sanitized user-facing errors while preserving operator diagnostics in logs
- Data migration between SQLite/MySQL and DynamoDB remains out of scope and should be documented, not implemented
