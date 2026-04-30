# Tasks: Local DynamoDB Development Environment

**Input**: Design documents from `/specs/016-localstack-dynamodb-dockerfile/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/local-dynamodb-runtime.md`, `quickstart.md`

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for this feature. Write the failing test coverage first in `tests/Payslip4All.Web.Tests` before changing implementation or documentation files.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `- [ ] T### [P?] [US#?] Description with file path`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the shared failing-test surface and reusable local DynamoDB scaffolding used by all stories.

- [ ] T001 [P] Extend LocalStack runtime contract assertions in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackIntegrationConfigTests.cs
- [ ] T002 [P] Extend LocalStack README contract coverage in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackReadmeContractTests.cs
- [ ] T003 [P] Refine reusable local emulator helpers in tests/Payslip4All.Web.Tests/Integration/LocalDynamoDbTestHarness.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Align the shared LocalStack runtime contract and DynamoDB startup plumbing before user-story work begins.

**⚠️ CRITICAL**: Complete this phase before starting any user story.

- [ ] T004 Migrate the source-controlled LocalStack image from infra/localstack/LocalStackDockerfile to infra/localstack/Dockerfile
- [ ] T005 [P] Enforce the local endpoint, region, and paired-credential startup contract in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbConfigurationOptions.cs
- [ ] T006 [P] Preserve emulator-safe dummy credentials and source-code-neutral endpoint handling in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbClientFactory.cs
- [ ] T007 [P] Add actionable LocalStack readiness and table-activation failures in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs
- [ ] T008 Keep DynamoDB local registration aligned with validated runtime settings in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs and src/Payslip4All.Web/Program.cs

**Checkpoint**: The repository has a single agreed LocalStack image path plus validated DynamoDB startup plumbing that all stories can build on.

---

## Phase 3: User Story 1 - Start a local persistence service (Priority: P1) 🎯 MVP

**Goal**: Provide a reusable LocalStack artifact that starts a DynamoDB-compatible local service without requiring a live AWS account.

**Independent Test**: From a repository checkout, build and run the source-controlled LocalStack image, then verify a DynamoDB-compatible service is reachable using the documented local connection details.

### Tests for User Story 1 (REQUIRED — TDD)

- [ ] T009 [P] [US1] Add Docker artifact contract coverage for infra/localstack/Dockerfile in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackIntegrationConfigTests.cs
- [ ] T010 [P] [US1] Add build, run, verify, and stop workflow coverage for infra/localstack/README.md in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackReadmeContractTests.cs

### Implementation for User Story 1

- [ ] T011 [US1] Create the pinned DynamoDB-only LocalStack image contract in infra/localstack/Dockerfile and retire infra/localstack/LocalStackDockerfile
- [ ] T012 [US1] Document the LocalStack build, run, readiness, and stop flow in infra/localstack/README.md
- [ ] T013 [US1] Align the feature quickstart steps in specs/016-localstack-dynamodb-dockerfile/quickstart.md with infra/localstack/Dockerfile and infra/localstack/README.md

**Checkpoint**: Contributors can discover, build, and start the LocalStack DynamoDB container consistently from the repository.

---

## Phase 4: User Story 2 - Use local DynamoDB-backed workflows (Priority: P2)

**Goal**: Let the application and smoke-test workflow run against the emulator through the existing DynamoDB provider contract.

**Independent Test**: Start LocalStack, set the documented DynamoDB environment values, then verify representative startup and repository workflows succeed against the emulator without code changes.

### Tests for User Story 2 (REQUIRED — TDD)

- [ ] T014 [P] [US2] Extend local provisioning coverage in tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs
- [ ] T015 [P] [US2] Extend representative local workflow coverage in tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalWorkflowTests.cs
- [ ] T016 [P] [US2] Add failure-path validation for missing region, invalid endpoint, and partial credentials in tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs

### Implementation for User Story 2

- [ ] T017 [US2] Clarify local runtime validation and guidance in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbConfigurationOptions.cs
- [ ] T018 [US2] Keep emulator endpoint and dummy-credential behavior aligned in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbClientFactory.cs and src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs
- [ ] T019 [US2] Surface LocalStack-unavailable and activation-timeout guidance in src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs
- [ ] T020 [US2] Keep the DynamoDB provider startup path and documented integration entrypoint aligned in src/Payslip4All.Web/Program.cs and tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj

**Checkpoint**: The app and LocalStack-backed validation workflow run successfully through the documented DynamoDB provider path.

---

## Phase 5: User Story 3 - Reuse a standard team setup (Priority: P3)

**Goal**: Make the LocalStack DynamoDB workflow discoverable, repeatable, and easy for contributors to troubleshoot.

**Independent Test**: A contributor unfamiliar with the setup can find the repository guidance, start the service, configure the app, verify the workflow, and follow troubleshooting steps without extra tribal knowledge.

### Tests for User Story 3 (REQUIRED — TDD)

- [ ] T021 [P] [US3] Add repository-entry onboarding coverage in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackIntegrationConfigTests.cs
- [ ] T022 [P] [US3] Add cross-README discovery, configurability, and troubleshooting coverage in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackDocumentationContractTests.cs

### Implementation for User Story 3

- [ ] T023 [P] [US3] Update contributor entry-point guidance for local DynamoDB setup in README.md
- [ ] T024 [P] [US3] Expand start, stop, verify, configurable values, and troubleshooting guidance in infra/localstack/README.md
- [ ] T025 [US3] Align contributor walkthrough wording across README.md, infra/localstack/README.md, and specs/016-localstack-dynamodb-dockerfile/quickstart.md

**Checkpoint**: The LocalStack DynamoDB workflow is documented consistently for team reuse and onboarding.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish repository-wide validation and final consistency checks.

- [ ] T026 [P] Run the LocalStack-focused integration surface in tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj
- [ ] T027 [P] Reconcile remaining host and endpoint wording across README.md, infra/localstack/README.md, and specs/016-localstack-dynamodb-dockerfile/contracts/local-dynamodb-runtime.md
- [ ] T028 Validate the final build, run, runtime, and troubleshooting flow against specs/016-localstack-dynamodb-dockerfile/contracts/local-dynamodb-runtime.md and specs/016-localstack-dynamodb-dockerfile/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** → no dependencies
- **Phase 2: Foundational** → depends on Phase 1 and blocks all user stories
- **Phase 3: US1** → depends on Phase 2
- **Phase 4: US2** → depends on US1 because the local service must exist before workflows can consume it
- **Phase 5: US3** → depends on US1 and US2 so the team guide reflects the working setup and failure guidance
- **Phase 6: Polish** → depends on all completed story phases

### User Story Dependencies

- **US1 (P1)**: first deliverable and MVP
- **US2 (P2)**: depends on US1
- **US3 (P3)**: depends on US1 and US2

### Within Each User Story

- Write the tests first and confirm they fail before implementation.
- Complete runtime and startup behavior before updating final verification or documentation tasks.
- Keep documentation changes aligned with the implemented LocalStack contract, not placeholders.
- Present the Manual Test Gate before any commit, merge, or push.

---

## Parallel Opportunities

- **Setup**: T001, T002, and T003 can run in parallel because they touch separate files.
- **Foundational**: T005, T006, and T007 can run in parallel after T004 establishes the single LocalStack Dockerfile path.
- **US1**: T009 and T010 can run in parallel; after they fail, T011 and T012 can proceed in sequence before T013 validates the quickstart.
- **US2**: T014, T015, and T016 can run in parallel because they cover separate test files.
- **US3**: T021 and T022 can run in parallel; after they fail, T023 and T024 can run in parallel before T025 reconciles the walkthrough.
- **Polish**: T026 and T027 can run in parallel before T028 performs the final end-to-end consistency check.

---

## Parallel Example: User Story 1

```bash
Task: "Add Docker artifact contract coverage for infra/localstack/Dockerfile in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackIntegrationConfigTests.cs"
Task: "Add build, run, verify, and stop workflow coverage for infra/localstack/README.md in tests/Payslip4All.Web.Tests/Infrastructure/LocalStackReadmeContractTests.cs"
```

## Parallel Example: User Story 2

```bash
Task: "Extend local provisioning coverage in tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalStartupTests.cs"
Task: "Extend representative local workflow coverage in tests/Payslip4All.Web.Tests/Integration/DynamoDbLocalWorkflowTests.cs"
Task: "Add failure-path validation for missing region, invalid endpoint, and partial credentials in tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs"
```

## Parallel Example: User Story 3

```bash
Task: "Update contributor entry-point guidance for local DynamoDB setup in README.md"
Task: "Expand start, stop, verify, configurable values, and troubleshooting guidance in infra/localstack/README.md"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Verify the LocalStack image can be built and started from the repository-owned Dockerfile.
5. Stop and validate US1 independently before moving on.

### Incremental Delivery

1. Ship **US1** to establish the reusable local persistence service.
2. Ship **US2** to prove the app and integration workflow run against the emulator.
3. Ship **US3** to standardize contributor onboarding and troubleshooting.
4. Finish with **Polish** to run the focused test suite and reconcile final contract wording.

### Parallel Team Strategy

1. One engineer completes Setup plus the Dockerfile migration in Phase 2.
2. A second engineer can tackle T005-T007 in parallel once the Dockerfile path is settled.
3. After US1 is green, multiple engineers can split US2 test coverage, startup/runtime behavior, and US3 documentation work.

---

## Notes

- Task IDs run in dependency order from T001 to T028.
- `[P]` is used only where the work is parallelizable without file conflicts.
- `[US#]` labels appear only in user-story phases.
- Every task includes at least one exact repository file path.
- The current repository still contains `infra/localstack/LocalStackDockerfile`; this plan treats migrating to `infra/localstack/Dockerfile` as foundational feature work.
- Manual Test Gate approval remains mandatory before any commit, merge, or push.
