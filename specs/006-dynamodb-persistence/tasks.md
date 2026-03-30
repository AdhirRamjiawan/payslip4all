---

description: "Task list for implementing AWS DynamoDB persistence"
---

# Tasks: AWS DynamoDB Persistence Option

**Input**: Design documents from `/Users/adhirramjiawan/projects/payslip4all/specs/006-dynamodb-persistence/`  
**Prerequisites**: `/Users/adhirramjiawan/projects/payslip4all/specs/006-dynamodb-persistence/plan.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/006-dynamodb-persistence/spec.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/006-dynamodb-persistence/research.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/006-dynamodb-persistence/data-model.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/006-dynamodb-persistence/quickstart.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/006-dynamodb-persistence/contracts/persistence-provider-contract.md`

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for this feature. Write the failing tests first for each phase, confirm they fail, and only then implement production changes.

**Organization**: Tasks are grouped by setup, foundational work, and user story so each story can be implemented, tested, and demonstrated independently.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Task can run in parallel because it targets a different file and has no dependency on incomplete tasks
- **[Story]**: Present only in user-story phases and maps directly to `spec.md` user stories (`[US1]`, `[US2]`, `[US3]`)
- Every task includes the exact absolute file path to create or update

---

## Phase 1: Setup

**Purpose**: Align shared project references and DynamoDB test scaffolding before blocking infrastructure work begins.

- [X] T001 Update DynamoDB package references in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Payslip4All.Infrastructure.csproj
- [X] T002 [P] Update Web startup test dependencies in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj
- [X] T003 [P] Update Infrastructure DynamoDB test dependencies in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/Payslip4All.Infrastructure.Tests.csproj

---

## Phase 2: Foundational

**Purpose**: Build the shared DynamoDB infrastructure that blocks every user story.

**⚠️ CRITICAL**: No user story work starts until this phase is complete.

### Tests for Foundational Infrastructure (REQUIRED — TDD)

- [X] T004 [P] Add failing credential precedence and validation tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbClientFactoryTests.cs
- [X] T005 [P] Add failing startup table provisioning and table-prefix tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTableProvisionerTests.cs
- [X] T006 [P] Add failing no-op unit-of-work behavior tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbUnitOfWorkTests.cs

### Implementation for Foundational Infrastructure

- [X] T007 [P] Implement credential pair validation, endpoint handling, and AWS credential resolution in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbClientFactory.cs
- [X] T008 [P] Implement DynamoDB no-op unit-of-work semantics in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbUnitOfWork.cs
- [X] T009 [P] Implement shared user-ownership extraction helpers for repository reads in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbOwnership.cs
- [X] T010 Implement DynamoDB service registration for repositories, hosted provisioning, and `IUnitOfWork` in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs
- [X] T011 Implement six-table startup provisioning, ACTIVE waits, and operator logging in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs

**Checkpoint**: Shared DynamoDB infrastructure is ready for startup wiring and repository parity work.

---

## Phase 3: User Story 1 - Select DynamoDB at Startup (Priority: P1) 🎯 MVP

**Goal**: Deployment operators can activate DynamoDB entirely through environment variables and the application bypasses relational startup work when DynamoDB is selected.

**Independent Test**: Start the application with `PERSISTENCE_PROVIDER=dynamodb` in hosted-AWS, explicit-credential, and local-emulator modes and verify provider switching, startup validation, table provisioning, and relational bypass behavior. Then start with an unknown provider, missing `DYNAMODB_REGION`, and a partial credential pair and verify fast descriptive failure.

### Tests for User Story 1 (REQUIRED — TDD)

- [X] T012 [P] [US1] Add failing provider-selection parsing and valid-option tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs
- [X] T013 [P] [US1] Add failing DynamoDB startup configuration validation tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs
- [X] T014 [P] [US1] Add failing relational bypass and dependency registration tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/DynamoDbRelationalBypassTests.cs

### Implementation for User Story 1

- [X] T015 [US1] Implement trimmed case-insensitive provider selection and DynamoDB startup validation in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs
- [X] T016 [US1] Implement DynamoDB-specific startup branching that skips EF Core initialization and migrations in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs

**Checkpoint**: DynamoDB can be selected safely at startup without changing code or checked-in configuration.

---

## Phase 4: User Story 2 - Use DynamoDB for Business Data (Priority: P2)

**Goal**: Company Owners can run the existing employee, payslip, and loan workflows on DynamoDB while preserving ownership isolation and sanitized failure handling.

**Independent Test**: Run the application with DynamoDB enabled, perform employee, loan, company, and payslip workflows for one owner, verify the data round-trips correctly, and prove a second owner cannot retrieve that data. Simulate throttling, temporary unavailability, and permission failures and confirm users get sanitized messages while logs retain diagnostics.

### Tests for User Story 2 (REQUIRED — TDD)

- [X] T017 [P] [US2] Add failing DynamoDB user repository parity tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbUserRepositoryTests.cs
- [X] T018 [P] [US2] Add failing company ownership-filtering and query tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbCompanyRepositoryTests.cs
- [X] T019 [P] [US2] Add failing employee persistence, company hydration, and ownership tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbEmployeeRepositoryTests.cs
- [X] T020 [P] [US2] Add failing loan persistence and repayment-progress tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbLoanRepositoryTests.cs
- [X] T021 [P] [US2] Add failing payslip persistence, deduction hydration, and chronological query tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbPayslipRepositoryTests.cs
- [X] T022 [P] [US2] Add failing sanitized DynamoDB exception response tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Middleware/GlobalExceptionMiddlewareTests.cs

### Implementation for User Story 2

- [X] T023 [P] [US2] Implement DynamoDB user persistence parity for `IUserRepository` in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbUserRepository.cs
- [X] T024 [P] [US2] Implement company ownership-safe queries and persistence for `ICompanyRepository` in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbCompanyRepository.cs
- [X] T025 [US2] Implement employee persistence, denormalized `userId`, and hydration behavior in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbEmployeeRepository.cs
- [X] T026 [US2] Implement loan persistence, status transitions, and employee-scoped reads in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbLoanRepository.cs
- [X] T027 [US2] Implement payslip persistence, payslip-loan-deduction storage, and payslip lookups in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Infrastructure/Persistence/DynamoDB/Repositories/DynamoDbPayslipRepository.cs
- [X] T028 [US2] Implement sanitized user-facing DynamoDB failure handling and diagnostic logging in /Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Middleware/GlobalExceptionMiddleware.cs

**Checkpoint**: DynamoDB supports the business workflows and enforces owner isolation without changing Application or Domain contracts.

---

## Phase 5: User Story 3 - Develop Locally with a DynamoDB Emulator (Priority: P3)

**Goal**: Developers can run the application and tests against DynamoDB Local using environment variables only and no live AWS account.

**Independent Test**: Start a local DynamoDB emulator, set `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, and `DYNAMODB_ENDPOINT`, leave explicit AWS credentials unset, and verify startup supplies dummy credentials, provisions prefixed tables, and completes a create-read cycle successfully.

### Tests for User Story 3 (REQUIRED — TDD)

- [X] T029 [P] [US3] Add failing local-emulator credential fallback tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbClientFactoryTests.cs
- [X] T030 [P] [US3] Add failing end-to-end DynamoDB Local startup tests in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs

### Implementation for User Story 3

- [X] T031 [US3] Implement isolated DynamoDB Local fixture setup and cleanup helpers in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbTestFixture.cs
- [X] T032 [US3] Implement Web test collection support for DynamoDB startup scenarios in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/DynamoDbStartupTestCollection.cs
- [X] T033 [US3] Document local emulator setup, prefixed-table verification, and create-read validation in /Users/adhirramjiawan/projects/payslip4all/specs/006-dynamodb-persistence/quickstart.md

**Checkpoint**: Developers can validate the DynamoDB provider locally without a live AWS account.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Preserve existing provider behavior, finish operator guidance, and lock in regression coverage.

- [X] T034 [P] Update startup regression coverage for unchanged SQLite and MySQL behavior in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs
- [X] T035 [P] Update relational persistence regression coverage alongside DynamoDB support in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/Repositories/RepositoryIntegrationTests.cs
- [X] T036 [P] Update operator-facing provider configuration guidance and unsupported migration notes in /Users/adhirramjiawan/projects/payslip4all/README.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies and can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion
- **User Story 2 (Phase 4)**: Depends on Foundational completion and should land after US1 so end-to-end startup activation already exists
- **User Story 3 (Phase 5)**: Depends on Foundational completion and on US1 startup/provider switching behavior
- **Polish (Phase 6)**: Depends on all stories intended for release

### User Story Dependency Graph

- **US1** → **US2** → **US3** for recommended delivery order
- **US2** and **US3** both require the Foundational phase
- **US3** additionally relies on the startup contract delivered by **US1**

### Within Each User Story

- Tests must be written and confirmed failing before implementation begins
- Shared infrastructure must be complete before story-specific repository or startup work begins
- Repository implementation must precede sanitized runtime failure handling validation where applicable
- **Manual Test Gate (Principle VI)**: After each implementation slice, present the manual test gate prompt and wait for engineer approval before any `git commit`, `git merge`, or `git push`

### Suggested Execution Order

1. Complete Phase 1 Setup
2. Complete Phase 2 Foundational
3. Deliver Phase 3 User Story 1 as the MVP
4. Deliver Phase 4 User Story 2 for full business-data parity
5. Deliver Phase 5 User Story 3 for local emulator support
6. Finish Phase 6 Polish and regression coverage

---

## Parallel Opportunities

- **Setup**: `T002` and `T003` can run in parallel after `T001`
- **Foundational**: `T004`, `T005`, and `T006` can run in parallel; `T007`, `T008`, and `T009` can run in parallel after the tests fail
- **US1**: `T012`, `T013`, and `T014` can run in parallel
- **US2**: `T017` through `T022` can run in parallel; `T023` and `T024` can run in parallel before deeper employee, loan, and payslip repository work
- **US3**: `T029` and `T030` can run in parallel
- **Polish**: `T034`, `T035`, and `T036` can run in parallel

---

## Parallel Example: User Story 1

```bash
# Write the startup tests together
Task: "T012 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/DynamoDbProviderSwitchingTests.cs"
Task: "T013 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs"
Task: "T014 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/DynamoDbRelationalBypassTests.cs"
```

## Parallel Example: User Story 2

```bash
# Write repository and middleware tests together
Task: "T017 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbUserRepositoryTests.cs"
Task: "T018 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbCompanyRepositoryTests.cs"
Task: "T019 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbEmployeeRepositoryTests.cs"
Task: "T020 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbLoanRepositoryTests.cs"
Task: "T021 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/Repositories/DynamoDbPayslipRepositoryTests.cs"
Task: "T022 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Middleware/GlobalExceptionMiddlewareTests.cs"
```

## Parallel Example: User Story 3

```bash
# Validate local-emulator coverage together
Task: "T029 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Infrastructure.Tests/DynamoDB/DynamoDbClientFactoryTests.cs"
Task: "T030 /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 Setup
2. Complete Phase 2 Foundational
3. Complete Phase 3 User Story 1
4. Validate DynamoDB startup modes, fail-fast validation, provisioning, and relational bypass independently
5. Present the Manual Test Gate prompt and wait for engineer approval before any commit activity
6. Stop and demo the MVP before starting repository parity work

### Incremental Delivery

1. Deliver **US1** for provider selection and safe startup behavior
2. Deliver **US2** for repository parity, ownership isolation, and sanitized runtime failures
3. Deliver **US3** for local development and CI-style emulator support
4. Finish with regression and documentation polish

### Parallel Team Strategy

1. One engineer completes Setup and Foundational work
2. After Foundational completion:
   - Engineer A delivers **US1**
   - Engineer B prepares **US2** repository tests
   - Engineer C prepares **US3** emulator tests
3. Merge story implementations in priority order after each story passes its independent test gate

---

## Notes

- `PERSISTENCE_PROVIDER` must remain the single runtime selector and must be trimmed and case-insensitive
- DynamoDB startup must require `DYNAMODB_REGION`, enforce all-or-nothing explicit credentials, and use dummy credentials only for local-emulator mode
- DynamoDB table provisioning must create `/users`, `/companies`, `/employees`, `/employee_loans`, `/payslips`, and `/payslip_loan_deductions` equivalents with the configured prefix before serving traffic
- DynamoDB repositories must preserve ownership filtering for Company Owner data on every read path
- Throttling, temporary unavailability, and permission failures must show sanitized user-facing responses while preserving operator diagnostics in logs
- Cross-provider data migration remains out of scope and must be documented rather than implemented
