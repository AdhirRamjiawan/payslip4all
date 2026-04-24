# Tasks: AWS Secrets-Sourced Custom Configuration

**Input**: Design documents from `/specs/014-aws-secrets-config/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md, contracts/aws-secrets-configuration-contract.md

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for this feature. Write the failing xUnit/WebApplicationFactory tests first, confirm they fail, then implement the minimum change to pass.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no unmet dependencies)
- **[Story]**: User story label (`[US1]`, `[US2]`, `[US3]`)
- Every task includes the exact file path(s) to change or validate

## Path Conventions

- Web startup: `src/Payslip4All.Web/Program.cs`
- Infrastructure config/runtime: `src/Payslip4All.Infrastructure/`
- AWS deployment assets: `infra/aws/cloudformation/payslip4all-web.yaml`, `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`, `infra/aws/cloudformation/README.md`
- Feature artifacts: `specs/014-aws-secrets-config/`
- Web tests: `tests/Payslip4All.Web.Tests/{Startup,Infrastructure,Integration}`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the missing task artifact and feature-specific test locations before implementation starts.

- [X] T001 Create or confirm the feature task list in `specs/014-aws-secrets-config/tasks.md`
- [X] T002 Create secrets-configuration startup and documentation test coverage in `tests/Payslip4All.Web.Tests/Startup/` and `tests/Payslip4All.Web.Tests/Infrastructure/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Put the source-ordering and shared configuration contracts in place before any user story implementation starts.

**⚠️ CRITICAL**: Complete this phase before any user story implementation starts.

- [X] T003 [P] Add failing tests for AWS-secret config file loading, source precedence, and DynamoDB validation in `tests/Payslip4All.Web.Tests/Startup/`
- [X] T004 [P] Add failing tests for CloudFormation/bootstrap/docs secret contract updates in `tests/Payslip4All.Web.Tests/Infrastructure/`
- [X] T005 Implement shared configuration constants/options for covered custom settings in `src/Payslip4All.Infrastructure/`
- [X] T006 Implement explicit configuration-source ordering (`appsettings -> AWS secret artifact -> environment variables`) in `src/Payslip4All.Web/Program.cs`

**Checkpoint**: The app can recognize an optional AWS-secret-backed config artifact without changing existing non-secret deployments.

---

## Phase 3: User Story 1 - Resolve startup-critical custom settings from AWS Secrets (Priority: P1) 🎯 MVP

**Goal**: Operators can source persistence and startup-critical custom settings from AWS Secrets Manager instead of only checked-in appsettings or raw environment variables.

**Independent Test**: Start the app with startup-critical covered keys present only in the rendered AWS-secret config file and confirm the selected persistence path boots successfully; confirm environment overrides still win.

### Tests for User Story 1 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation**

- [X] T007 [P] [US1] Add startup tests for secrets-only DynamoDB configuration, missing required secret-backed values, and env-over-secret precedence in `tests/Payslip4All.Web.Tests/Startup/SecretsConfigurationStartupTests.cs`
- [X] T008 [P] [US1] Extend `tests/Payslip4All.Web.Tests/Startup/DynamoDbConfigurationValidationTests.cs` to cover secret-backed configuration sources instead of environment-only assumptions

### Implementation for User Story 1

- [X] T009 [US1] Replace direct environment-variable reads in `src/Payslip4All.Web/Program.cs` with resolved configuration for `PERSISTENCE_PROVIDER`, connection strings, `DYNAMODB_*`, and AWS credential keys
- [X] T010 [US1] Replace direct environment-variable reads in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/` with injected configuration/options for client creation, table prefixing, and PITR behavior
- [X] T011 [US1] Preserve backward-compatible defaults for deployments that do not provide an AWS-secret config artifact in `src/Payslip4All.Web/Program.cs` and `src/Payslip4All.Infrastructure/Persistence/DynamoDB/`

**Checkpoint**: Startup-critical custom settings resolve correctly from appsettings, AWS-secret config, or environment variables in the documented order.

---

## Phase 4: User Story 2 - Source feature-specific custom settings from AWS Secrets (Priority: P2)

**Goal**: Operators can source non-startup custom settings such as auth cookie lifetime and PayFast options from AWS Secrets Manager without changing the app’s feature behavior.

**Independent Test**: Resolve `Auth:Cookie:ExpireDays` and `HostedPayments:PayFast:*` values from the AWS-secret config file, then confirm the cookie/auth and PayFast option binding paths behave as if the values came from tracked appsettings.

### Tests for User Story 2 (REQUIRED — TDD, constitution Principle I)

- [X] T012 [P] [US2] Add startup tests for secret-backed auth cookie and PayFast option binding in `tests/Payslip4All.Web.Tests/Startup/SecretsConfigurationStartupTests.cs`
- [X] T013 [P] [US2] Add or extend docs/tests that assert the covered custom-setting catalog and secret key examples in `tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`

### Implementation for User Story 2

- [X] T014 [US2] Ensure `Auth:Cookie:ExpireDays` and `HostedPayments:PayFast:*` bind from the new AWS-secret config source in `src/Payslip4All.Web/Program.cs`
- [X] T015 [US2] Document the covered key catalog, secret payload format, and precedence examples in `README.md`, `infra/aws/cloudformation/README.md`, and `specs/014-aws-secrets-config/quickstart.md`

**Checkpoint**: Feature-specific custom settings behave the same regardless of whether they come from checked-in appsettings, the AWS-secret artifact, or environment overrides.

---

## Phase 5: User Story 3 - Support the AWS deployment workflow end to end (Priority: P3)

**Goal**: The hosted AWS deployment path can fetch the custom app-config secret, render it safely, and expose enough operator documentation to use it correctly.

**Independent Test**: Inspect the AWS template, bootstrap script, and docs and confirm operators can supply a custom app-config secret ARN, bootstrap renders the artifact safely, and the docs explain how to validate mixed-source and failure scenarios.

### Tests for User Story 3 (REQUIRED — TDD, constitution Principle I)

- [X] T016 [P] [US3] Extend CloudFormation template assertions for the custom app-config secret parameter, IAM permission, outputs, and rendered config path in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T017 [P] [US3] Extend AWS deployment startup/documentation assertions for rendered secret config behavior in `tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs` and `tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`

### Implementation for User Story 3

- [X] T018 [US3] Add the custom app-config secret parameter, IAM access, bootstrap rendering path, and outputs in `infra/aws/cloudformation/payslip4all-web.yaml`
- [X] T019 [US3] Render the AWS secret into a protected JSON config artifact in `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`
- [X] T020 [US3] Update operator guidance for the new app-config secret contract in `infra/aws/cloudformation/README.md` and `README.md`

**Checkpoint**: The AWS deployment path supports secret-backed custom configuration without exposing secret values or breaking legacy deployments.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and artifact alignment after the user stories are complete.

- [X] T021 [P] Align the feature quickstart and contract docs with the final implementation in `specs/014-aws-secrets-config/quickstart.md` and `specs/014-aws-secrets-config/contracts/aws-secrets-configuration-contract.md`
- [X] T022 Run the secrets-configuration-focused regression suite from `tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj`
- [X] T023 Execute the operator validation flow described in `specs/014-aws-secrets-config/quickstart.md` against the AWS docs and bootstrap/template artifacts

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** — no dependencies
- **Phase 2: Foundational** — depends on Phase 1 and blocks all story work
- **Phase 3: US1** — depends on Phase 2; this is the MVP
- **Phase 4: US2** — depends on US1 because the same configuration pipeline powers both stories
- **Phase 5: US3** — depends on US1 and US2 so the deployment assets match the final runtime behavior
- **Phase 6: Polish** — depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational; no dependency on other user stories
- **US2 (P2)**: Starts after US1 because it relies on the shared configuration ordering and options binding introduced there
- **US3 (P3)**: Starts after US1 and US2 so the AWS deployment docs and assets describe the final contract

### Within Each User Story

- Write the listed tests first and confirm they fail before implementation
- Update shared runtime/configuration code before docs
- Re-run the relevant xUnit/WebApplicationFactory coverage after each story reaches green
- **Manual Test Gate (Principle VI)**: After implementation, present the manual test gate prompt and wait for explicit engineer approval before any `git commit`, `git merge`, or `git push`

---

## Parallel Opportunities

- **Foundational**: T003 and T004 can run in parallel before T005 and T006
- **US1**: T007 and T008 can run in parallel before T009 and T010
- **US2**: T012 and T013 can run in parallel before T014 and T015
- **US3**: T016 and T017 can run in parallel before T018, T019, and T020

---

## Notes

- All checklist items use the required `- [X] T### [P?] [Story?] Description with file path` format
- The covered catalog is limited to repo-owned custom settings explicitly consumed by Payslip4All code
- AWS secret values must never be logged or surfaced in user-visible diagnostics
- Non-secret deployments must remain valid when no AWS secret reference is supplied
- Do not commit or merge until the Manual Test Gate is explicitly approved
