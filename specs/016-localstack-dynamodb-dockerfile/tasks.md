# Tasks: Local DynamoDB Development Environment

**Input**: Design documents from `/specs/016-localstack-dynamodb-dockerfile/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/local-dynamodb-runtime.md, quickstart.md

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for this feature. Write the failing test coverage first in `tests/Payslip4All.Web.Tests` before changing implementation or documentation files.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `- [ ] T### [P?] [US#?] Description with file path`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the shared failing-test surface and reusable local DynamoDB test scaffolding.

- [X] T001 [P] Extend LocalStack image contract assertions in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackIntegrationConfigTests.cs
- [X] T002 [P] Extend local DynamoDB startup validation coverage in tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs
- [X] T003 [P] Refactor reusable local endpoint and table-prefix test helpers in tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Align the shared LocalStack runtime contract and DynamoDB startup plumbing before story work begins.

**⚠️ CRITICAL**: Complete this phase before starting any user story.

- [X] T004 [P] Align the pinned LocalStack runtime settings with the design contract in infra/localstack/Dockerfile
- [X] T005 [P] Enforce the local runtime configuration and paired-credential contract in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbConfigurationOptions.cs
- [X] T006 [P] Preserve emulator-safe credential fallback for configured local endpoints in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbClientFactory.cs
- [X] T007 [P] Add actionable LocalStack readiness failures to src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs
- [X] T008 Keep DynamoDB local startup registration aligned with the validated runtime contract in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs

**Checkpoint**: LocalStack contract, configuration validation, and startup plumbing are ready for story delivery.

---

## Phase 3: User Story 1 - Start a local persistence service (Priority: P1) 🎯 MVP

**Goal**: Provide a reusable LocalStack artifact that starts a DynamoDB-compatible service on the documented local endpoint without live AWS dependencies.

**Independent Test**: Build and run the container from `infra/localstack/Dockerfile`, then verify a DynamoDB-compatible service is reachable on `http://localhost:8000` using the documented workflow without a live AWS account.

### Tests for User Story 1 (REQUIRED — TDD)

- [X] T009 [P] [US1] Add Dockerfile runtime contract coverage in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackIntegrationConfigTests.cs
- [X] T010 [P] [US1] Add LocalStack build/run documentation contract coverage in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackReadmeContractTests.cs

### Implementation for User Story 1

- [X] T011 [US1] Refine the LocalStack DynamoDB image contract in infra/localstack/Dockerfile
- [X] T012 [US1] Document the LocalStack build, run, and verification flow in infra/localstack/README.md
- [X] T013 [US1] Verify the build/run flow in specs/016-localstack-dynamodb-dockerfile/quickstart.md against infra/localstack/Dockerfile and infra/localstack/README.md

**Checkpoint**: Contributors can build and start the LocalStack DynamoDB container consistently from the repository.

---

## Phase 4: User Story 2 - Use local DynamoDB-backed workflows (Priority: P2)

**Goal**: Let the application and local smoke-test workflow use the emulated DynamoDB service through the existing DynamoDB provider contract.

**Independent Test**: Start LocalStack, set the documented DynamoDB environment values, and verify representative DynamoDB-backed startup and repository workflows succeed against `http://localhost:8000`.

### Tests for User Story 2 (REQUIRED — TDD)

- [X] T014 [P] [US2] Extend local DynamoDB provisioning coverage in tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs
- [X] T015 [P] [US2] Add representative local repository workflow coverage in tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalWorkflowTests.cs
- [X] T016 [P] [US2] Add failing local configuration and unreachable-endpoint coverage in tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs

### Implementation for User Story 2

- [X] T017 [US2] Clarify local region, endpoint, and credential validation errors in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbConfigurationOptions.cs
- [X] T018 [US2] Keep the documented local endpoint path source-code neutral in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbClientFactory.cs and src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs
- [X] T019 [US2] Improve LocalStack-unavailable guidance during startup provisioning in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs
- [X] T020 [US2] Verify the local integration workflow in tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj against specs/016-localstack-dynamodb-dockerfile/quickstart.md

**Checkpoint**: The app and representative DynamoDB-backed workflows run successfully against the local emulator.

---

## Phase 5: User Story 3 - Reuse a standard team setup (Priority: P3)

**Goal**: Make the LocalStack DynamoDB workflow discoverable, repeatable, and easy to troubleshoot for any contributor.

**Independent Test**: A contributor unfamiliar with the setup can find the LocalStack instructions in the repository, start the service, configure the app, and follow the troubleshooting guidance without extra tribal knowledge.

### Tests for User Story 3 (REQUIRED — TDD)

- [X] T021 [P] [US3] Extend repository onboarding coverage for LocalStack discovery in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackIntegrationConfigTests.cs
- [X] T022 [P] [US3] Add LocalStack troubleshooting and configurability documentation coverage in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackDocumentationContractTests.cs

### Implementation for User Story 3

- [X] T023 [P] [US3] Update repository entry-point guidance for local DynamoDB setup in README.md
- [X] T024 [P] [US3] Expand start, stop, verify, configurable values, and troubleshooting guidance in infra/localstack/README.md
- [X] T025 [US3] Validate the contributor walkthrough in README.md and infra/localstack/README.md against specs/016-localstack-dynamodb-dockerfile/quickstart.md

**Checkpoint**: The LocalStack DynamoDB workflow is documented consistently for team reuse and onboarding.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish cross-story validation and consistency checks.

- [ ] T026 [P] Run the LocalStack-focused test surface in tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj
- [X] T027 [P] Tighten cross-file LocalStack troubleshooting and runtime wording in README.md, infra/localstack/README.md, and src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbConfigurationOptions.cs
- [X] T028 Validate final build, run, runtime, and troubleshooting guidance against specs/016-localstack-dynamodb-dockerfile/contracts/local-dynamodb-runtime.md and specs/016-localstack-dynamodb-dockerfile/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** → no dependencies
- **Phase 2: Foundational** → depends on Phase 1
- **Phase 3: US1** → depends on Phase 2
- **Phase 4: US2** → depends on Phase 3 because the local service must exist before app workflows can use it
- **Phase 5: US3** → depends on Phase 4 so documentation reflects the working setup and failure guidance
- **Phase 6: Polish** → depends on all completed story phases

### User Story Dependencies

- **US1**: first deliverable and MVP
- **US2**: depends on US1
- **US3**: depends on US1 and US2

### Within Each User Story

- Write tests first and confirm they fail before implementation.
- Complete runtime/configuration changes before verification tasks.
- Finish documentation changes only after the runtime behavior is working.
- Present the Manual Test Gate before any commit, merge, or push.

---

## Parallel Opportunities

- **Setup**: T001, T002, and T003 can run in parallel because they touch different test files.
- **Foundational**: T004, T005, T006, and T007 can run in parallel before T008 syncs the shared registration path.
- **US1**: T009 and T010 can run in parallel; after they fail, T011 and T012 can run in parallel.
- **US2**: T014, T015, and T016 can run in parallel because they touch separate test files.
- **US3**: T021 and T022 can run in parallel; after they fail, T023 and T024 can run in parallel.
- **Polish**: T026 and T027 can run in parallel before T028 performs the final consistency check.

---

## Parallel Example: User Story 1

```bash
Task: "Add Dockerfile runtime contract coverage in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackIntegrationConfigTests.cs"
Task: "Add LocalStack build/run documentation contract coverage in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackReadmeContractTests.cs"
```

## Parallel Example: User Story 2

```bash
Task: "Extend local DynamoDB provisioning coverage in tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs"
Task: "Add representative local repository workflow coverage in tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalWorkflowTests.cs"
Task: "Add failing local configuration and unreachable-endpoint coverage in tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs"
```

## Parallel Example: User Story 3

```bash
Task: "Update repository entry-point guidance for local DynamoDB setup in README.md"
Task: "Expand start, stop, verify, configurable values, and troubleshooting guidance in infra/localstack/README.md"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Deliver User Story 1 as the MVP.
3. Verify the container build/run workflow from `infra/localstack/Dockerfile`.

### Incremental Delivery

1. Ship **US1** to establish the reusable local service.
2. Ship **US2** to prove app and smoke-test workflows run against LocalStack.
3. Ship **US3** to standardize team onboarding and troubleshooting.

---

## Notes

- Task IDs run in dependency order from T001 to T028.
- `[P]` is used only for tasks that can be executed in parallel without file conflicts.
- User story labels appear only in user-story phases.
- Every task includes at least one exact repository file path.
- Manual Test Gate remains mandatory before any commit, merge, or push.
