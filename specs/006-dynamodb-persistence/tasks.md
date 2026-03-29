---

description: "Regenerated task list for 006-dynamodb-persistence implementation"
---

# Tasks: AWS DynamoDB Persistence Option

**Input**: Design documents from `/specs/006-dynamodb-persistence/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/persistence-provider-contract.md`

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for this feature. Write the failing tests for each relevant phase before changing production code.

**Organization**: Tasks are grouped by phase and by user story so each story remains independently implementable and testable.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Task can run in parallel with other marked tasks because it targets different files and has no dependency on incomplete work
- **[Story]**: User story label for story phases only (`[US1]`, `[US2]`, `[US3]`)
- Every task includes the exact repo file path to change or create

---

## Phase 1: Setup

**Purpose**: Align package references and shared DynamoDB test scaffolding before feature work starts.

- [X] T001 Update DynamoDB package references in `src/Payslip4All.Infrastructure/Payslip4All.Infrastructure.csproj`
- [X] T002 [P] Update DynamoDB test dependencies in `tests/Payslip4All.Infrastructure.Tests/Payslip4All.Infrastructure.Tests.csproj`
- [X] T003 [P] Reconcile DynamoDB Local fixture setup, unique table-prefix isolation, and cleanup helpers in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTestFixture.cs`

---

## Phase 2: Foundational

**Purpose**: Build the shared DynamoDB infrastructure that blocks all user stories.

**⚠️ CRITICAL**: Complete this phase before starting user-story implementation.

### Tests for Foundational Infrastructure (REQUIRED — TDD)

- [X] T004 [P] Add failing credential-precedence coverage for explicit credentials, local-emulator dummy credentials, and hosted-AWS credential-chain fallback in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbClientFactoryTests.cs`
- [X] T005 [P] Add failing no-op `IUnitOfWork` coverage for `SaveChangesAsync`, transaction methods, and disposal behavior in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbUnitOfWorkTests.cs`
- [X] T006 [P] Add failing table-provisioner coverage for default and explicit table prefixes, six-table creation, created-table logs, ACTIVE waits, and startup permission failures in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTableProvisionerTests.cs`

### Implementation for Foundational Infrastructure

- [X] T007 [P] Implement DynamoDB client construction with explicit credentials, local-emulator dummy credentials, hosted-AWS credential-chain fallback, and paired credential validation in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbClientFactory.cs`
- [X] T008 [P] Implement no-op `IUnitOfWork` behavior in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbUnitOfWork.cs`
- [X] T009 Implement DynamoDB DI registration for the client, repositories, unit of work, and hosted table provisioner in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs`
- [X] T010 Implement six-table auto-provisioning, missing-table creation logs, ACTIVE polling, and fail-fast permission handling in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs`

**Checkpoint**: DynamoDB infrastructure is ready for provider switching and repository work.

---

## Phase 3: User Story 1 - Configure DynamoDB Provider via Environment Variables (Priority: P1) 🎯 MVP

**Goal**: Deployment operators can select DynamoDB entirely through environment variables, and the app bypasses the relational startup path when `PERSISTENCE_PROVIDER=dynamodb`.

**Independent Test**: Start the app with `PERSISTENCE_PROVIDER=dynamodb`, valid `DYNAMODB_REGION`, optional `DYNAMODB_ENDPOINT`, optional `DYNAMODB_TABLE_PREFIX`, and either explicit AWS credentials or a hosted AWS identity, and verify DynamoDB services register, missing tables are provisioned, relational startup is bypassed, and startup succeeds. Then start with an invalid provider, missing region, or partial credential pair and verify fail-fast errors.

### Tests for User Story 1 (REQUIRED — TDD)

- [X] T011 [P] [US1] Add failing provider-switching coverage for default SQLite, explicit MySQL, explicit DynamoDB, invalid values, and trim/case-insensitive parsing in `tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs`
- [X] T012 [P] [US1] Add failing startup-validation coverage for `DYNAMODB_REGION`, `DYNAMODB_ENDPOINT`, `DYNAMODB_TABLE_PREFIX`, paired explicit credentials, and hosted-AWS credential-chain fallback in `tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs`
- [X] T013 [P] [US1] Add failing startup-bypass coverage proving `PERSISTENCE_PROVIDER=dynamodb` skips `PayslipDbContext` registration and relational migration execution in `tests/Payslip4All.Web.Tests/Startup/DynamoDbRelationalBypassTests.cs`

### Implementation for User Story 1

- [X] T014 [US1] Update provider selection, valid-value guards, trimming, and DynamoDB startup validation rules in `src/Payslip4All.Web/Program.cs`
- [X] T015 [US1] Update startup branching so `src/Payslip4All.Web/Program.cs` bypasses `PayslipDbContext` initialization, EF repository registration, and relational migration execution when `PERSISTENCE_PROVIDER=dynamodb`

**Checkpoint**: Provider selection works end to end, and the DynamoDB path no longer touches relational startup code.

---

## Phase 4: User Story 2 - Read and Write All Domain Data via DynamoDB (Priority: P2)

**Goal**: Company owners can manage users, companies, employees, payslips, and loans through DynamoDB with correct ownership filtering, repository parity, and sanitized user-facing failure handling.

**Independent Test**: Run the application against DynamoDB, then create and query employees, generate payslips, and record loans for one owner while confirming a second owner never sees that data. Trigger throttling, temporary unavailability, and permission failures and verify user-facing responses remain sanitized while operator logs keep diagnostic detail.

### Tests for User Story 2 (REQUIRED — TDD)

- [X] T016 [P] [US2] Add failing CRUD and lookup coverage for DynamoDB user persistence in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbUserRepositoryTests.cs`
- [X] T017 [P] [US2] Add failing ownership-filtering and company-hydration coverage in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbCompanyRepositoryTests.cs`
- [X] T018 [P] [US2] Add failing employee ownership, uniqueness, company hydration, and loan-hydration coverage in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbEmployeeRepositoryTests.cs`
- [X] T019 [P] [US2] Add failing loan CRUD, ownership, and repayment-progress coverage in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbLoanRepositoryTests.cs`
- [X] T020 [P] [US2] Add failing payslip CRUD, reverse-chronological reads, hydrated loan deductions, and payslip-loan-deduction persistence coverage in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbPayslipRepositoryTests.cs`
- [X] T021 [P] [US2] Add failing sanitized-response coverage for DynamoDB throttling, temporary unavailability, and permission exceptions in `tests/Payslip4All.Web.Tests/Middleware/GlobalExceptionMiddlewareTests.cs`

### Implementation for User Story 2

- [X] T022 [P] [US2] Implement or reconcile DynamoDB user persistence against `IUserRepository` in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbUserRepository.cs`
- [X] T023 [P] [US2] Implement or reconcile DynamoDB company persistence and ownership filtering against `ICompanyRepository` in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbCompanyRepository.cs`
- [X] T024 [US2] Implement or reconcile employee persistence, denormalized `userId`, uniqueness checks, and hydrated `Company` and `Loans` navigation data in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbEmployeeRepository.cs`
- [X] T025 [US2] Implement or reconcile loan persistence, repayment progress updates, and ownership-safe reads in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbLoanRepository.cs`
- [X] T026 [US2] Implement or reconcile payslip persistence, hydrated loan deductions, and payslip-loan-deduction storage inside `src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPayslipRepository.cs`
- [X] T027 [US2] Update DynamoDB runtime exception sanitization and operator logging in `src/Payslip4All.Web/Middleware/GlobalExceptionMiddleware.cs`

**Checkpoint**: DynamoDB repository parity is achieved while keeping payslip-loan-deduction persistence inside `DynamoDbPayslipRepository.cs`.

---

## Phase 5: User Story 3 - Local Development with DynamoDB Local (Priority: P3)

**Goal**: Developers can run the app and tests against DynamoDB Local using only environment variables and without an AWS account.

**Independent Test**: Start DynamoDB Local on `http://localhost:8000`, set `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION=us-east-1`, and `DYNAMODB_ENDPOINT=http://localhost:8000` while leaving explicit AWS credentials unset, then boot the app and verify startup supplies dummy credentials, creates the required prefixed tables, and completes a create-read cycle successfully.

### Tests for User Story 3 (REQUIRED — TDD)

- [X] T028 [P] [US3] Add failing DynamoDB Local client coverage for `DYNAMODB_ENDPOINT=http://localhost:8000`, automatic dummy credentials, and no AWS account assumptions in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbClientFactoryTests.cs`
- [ ] T029 [P] [US3] Add failing end-to-end DynamoDB Local startup coverage for prefixed table creation, automatic dummy credentials, and startup logs before request handling in `tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs`

### Implementation for User Story 3

- [X] T030 [US3] Reconcile local-emulator fixture defaults and startup verification helpers in `tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTestFixture.cs`
- [X] T031 [US3] Update local-development and AWS deployment guidance to cover local dummy credentials plus hosted-AWS standard authentication in `specs/006-dynamodb-persistence/quickstart.md`

**Checkpoint**: Developers can run and validate the feature locally with DynamoDB Local only.

---

## Phase 6: Polish

**Purpose**: Preserve non-DynamoDB behavior, finalize operator documentation, and lock in regression coverage.

- [X] T032 [P] Add or update startup regression coverage for unchanged SQLite and MySQL behavior and removal of `DatabaseProvider` fallback assumptions in `tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs`
- [X] T033 [P] Add or update relational repository regression coverage so SQLite and MySQL behavior stays green after DynamoDB changes in `tests/Payslip4All.Infrastructure.Tests/Repositories/RepositoryIntegrationTests.cs`
- [X] T034 Update provider-configuration documentation to reflect standard AWS authentication, missing-table auto-creation, unsupported cross-provider migration, and required `CreateTable` IAM permission in `README.md`

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

- **Setup**: `T002` and `T003` can run in parallel after `T001`
- **Foundational tests**: `T004`, `T005`, and `T006` can run in parallel
- **Foundational implementation**: `T007` and `T008` can run in parallel; `T009` depends on their shared contracts, and `T010` depends on `T007`
- **US1 tests**: `T011`, `T012`, and `T013` can run in parallel
- **US2 tests**: `T016` through `T021` can run in parallel
- **US2 implementation**: `T022` and `T023` can run in parallel; `T024` through `T026` follow as entity relationships deepen
- **US3 tests**: `T028` and `T029` can run in parallel
- **Polish**: `T032` and `T033` can run in parallel

---

## Parallel Example: User Story 1

```bash
# Write the startup tests together
Task: "T011 Update tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs"
Task: "T012 Add tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs"
Task: "T013 Add tests/Payslip4All.Web.Tests/Startup/DynamoDbRelationalBypassTests.cs"
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
3. Use standard AWS authentication behavior: explicit credentials when provided, dummy credentials for local emulators, and the default credential chain for hosted AWS deployments

---

## Notes

- Explicitly cover `DYNAMODB_REGION`, `DYNAMODB_ENDPOINT`, `DYNAMODB_TABLE_PREFIX`, `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY`, while allowing hosted AWS deployments to rely on the standard credential chain
- When `PERSISTENCE_PROVIDER=dynamodb`, the relational path must not initialize `PayslipDbContext` or execute EF Core migrations
- Missing DynamoDB tables must be auto-created at startup and logged clearly for operators
- Throttling, temporary unavailability, and permission failures must show sanitized user-facing errors while preserving operator diagnostics in logs
- Data migration between SQLite/MySQL and DynamoDB remains out of scope and should be documented, not implemented
