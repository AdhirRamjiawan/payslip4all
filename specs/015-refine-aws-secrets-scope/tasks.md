# Tasks: AWS Secrets Scope Refinement

**Input**: Design documents from `/specs/015-refine-aws-secrets-scope/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/aws-secrets-refined-scope-contract.md, quickstart.md

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for all features in this project. Tests MUST be written and confirmed failing before implementation tasks begin.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare shared test and documentation scaffolding for refined AWS Secrets scope work.

- [X] T001 [P] Extract reusable AWS app-config artifact test helpers in `tests/Payslip4All.Web.Tests/Startup/SecretsConfigurationStartupTests.cs`
- [X] T002 [P] Create refined scope validator test scaffold in `tests/Payslip4All.Infrastructure.Tests/Configuration/AwsSecretsScopeValidationTests.cs`
- [X] T003 [P] Add refined-scope documentation regression scaffold in `tests/Payslip4All.Web.Tests/Infrastructure/AwsSecretsScopeDocumentationTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core scope catalog and validation infrastructure that blocks all user stories.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Define eligible and excluded AWS secret catalogs in `src/Payslip4All.Infrastructure/Configuration/Payslip4AllCustomConfigurationKeys.cs` and `src/Payslip4All.Infrastructure/Configuration/AwsSecretsScopeCatalog.cs`
- [X] T005 [P] Implement scope-validation result and secret-safe diagnostic helpers in `src/Payslip4All.Infrastructure/Configuration/AwsSecretsScopeValidationResult.cs` and `src/Payslip4All.Infrastructure/Configuration/AwsSecretsScopeValidator.cs`
- [X] T006 Wire AWS secret artifact parsing, validation, and eligible-key insertion into `src/Payslip4All.Web/Program.cs`
- [X] T007 Preserve DynamoDB runtime and credential loading from non-secret sources in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbConfigurationOptions.cs` and `src/Payslip4All.Web/Program.cs`

**Checkpoint**: Refined catalog and validation path exist, and story work can proceed.

---

## Phase 3: User Story 1 - Keep compliant secret-backed settings (Priority: P1) 🎯 MVP

**Goal**: Keep AWS Secrets support for eligible repo-owned app settings while preserving precedence and excluding DynamoDB and AWS runtime inputs from that path.

**Independent Test**: Start the app with only eligible settings in the rendered AWS secret artifact, verify auth, payments, and provider-selection settings still resolve correctly, and confirm `env > AWS secret > appsettings` still holds.

### Tests for User Story 1 (REQUIRED - TDD, constitution Principle I)

- [X] T008 [P] [US1] Add failing eligible-scope resolution and precedence tests in `tests/Payslip4All.Web.Tests/Startup/SecretsConfigurationStartupTests.cs`
- [X] T009 [P] [US1] Add failing hosted AWS startup regression tests for compliant secret-backed settings in `tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs`

### Implementation for User Story 1

- [X] T010 [US1] Implement eligible-key admission and precedence-safe merge behavior in `src/Payslip4All.Infrastructure/Configuration/AwsSecretsScopeCatalog.cs` and `src/Payslip4All.Web/Program.cs`
- [X] T011 [US1] Keep supported provider, auth-cookie, and PayFast settings binding through the refined scope in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbConfigurationOptions.cs` and `src/Payslip4All.Web/Program.cs`

**Checkpoint**: Eligible secret-backed settings work without reintroducing excluded scope.

---

## Phase 4: User Story 2 - Upgrade non-compliant secret payloads (Priority: P2)

**Goal**: Give operators clear migration guidance from feature 014 to the refined supported and excluded scope.

**Independent Test**: Review the deployment docs starting from a feature-014-style payload and confirm they clearly show which keys stay secret-backed, which must move out, and how to verify the migration.

### Tests for User Story 2 (REQUIRED - TDD, constitution Principle I)

- [X] T012 [P] [US2] Add failing migration-guidance documentation tests in `tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`
- [X] T013 [P] [US2] Add failing root-readme refined-scope regression tests in `tests/Payslip4All.Web.Tests/Infrastructure/AwsSecretsScopeDocumentationTests.cs`

### Implementation for User Story 2

- [X] T014 [US2] Update refined AWS secret catalog and migration guidance in `infra/aws/cloudformation/README.md` and `README.md`
- [X] T015 [US2] Update operator quickstart and refined-scope contract examples in `specs/015-refine-aws-secrets-scope/quickstart.md` and `specs/015-refine-aws-secrets-scope/contracts/aws-secrets-refined-scope-contract.md`

**Checkpoint**: Operators can migrate existing payloads without guesswork.

---

## Phase 5: User Story 3 - Fail safely on scope violations (Priority: P3)

**Goal**: Reject excluded or invalid AWS secret payloads early with actionable, secret-safe diagnostics.

**Independent Test**: Start the app with excluded keys or malformed supported values in the rendered AWS secret artifact and confirm startup blocks with key- or group-level guidance that never exposes secret contents.

### Tests for User Story 3 (REQUIRED - TDD, constitution Principle I)

- [X] T016 [P] [US3] Add failing excluded-key rejection and secret-safe diagnostic tests in `tests/Payslip4All.Web.Tests/Startup/SecretsConfigurationStartupTests.cs`
- [X] T017 [P] [US3] Add failing validator unit tests for excluded groups and malformed artifacts in `tests/Payslip4All.Infrastructure.Tests/Configuration/AwsSecretsScopeValidationTests.cs`

### Implementation for User Story 3

- [X] T018 [US3] Implement blocking excluded-key diagnostics in `src/Payslip4All.Infrastructure/Configuration/AwsSecretsScopeValidator.cs` and `src/Payslip4All.Web/Program.cs`
- [X] T019 [US3] Preserve secret-safe failures for malformed or incomplete supported values in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbConfigurationOptions.cs` and `src/Payslip4All.Web/Program.cs`

**Checkpoint**: Non-compliant payloads fail fast and safely.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency, validation, and regression confidence.

- [X] T020 [P] Reconcile supported-versus-excluded catalog examples in `README.md` and `infra/aws/cloudformation/README.md`
- [X] T021 [P] Re-run quickstart and contract scenario consistency review in `specs/015-refine-aws-secrets-scope/quickstart.md` and `specs/015-refine-aws-secrets-scope/contracts/aws-secrets-refined-scope-contract.md`
- [X] T022 Validate refined-scope regression coverage in `tests/Payslip4All.Infrastructure.Tests/Configuration/AwsSecretsScopeValidationTests.cs` and `tests/Payslip4All.Web.Tests/Startup/SecretsConfigurationStartupTests.cs`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No dependencies
- **Phase 2**: Depends on Phase 1
- **Phase 3 (US1)**: Depends on Phase 2
- **Phase 4 (US2)**: Depends on Phase 2
- **Phase 5 (US3)**: Depends on Phase 2 and is best sequenced after US1 because both touch refined validation behavior in `src/Payslip4All.Web/Program.cs`
- **Phase 6**: Depends on all completed stories

### User Story Dependencies

- **US1**: MVP and first functional increment
- **US2**: Depends on refined catalog definitions from Phase 2
- **US3**: Depends on refined catalog definitions from Phase 2 and benefits from US1 merge-path completion

### Parallel Opportunities

- Phase 1: `T001`, `T002`, `T003`
- Phase 2: `T004`, `T005`
- US1: `T008`, `T009`
- US2: `T012`, `T013`
- US3: `T016`, `T017`
- Polish: `T020`, `T021`

---

## Parallel Example: User Story 1

```bash
Task: "Add failing eligible-scope resolution and precedence tests in tests/Payslip4All.Web.Tests/Startup/SecretsConfigurationStartupTests.cs"
Task: "Add failing hosted AWS startup regression tests for compliant secret-backed settings in tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs"
```

## Parallel Example: User Story 2

```bash
Task: "Add failing migration-guidance documentation tests in tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs"
Task: "Add failing root-readme refined-scope regression tests in tests/Payslip4All.Web.Tests/Infrastructure/AwsSecretsScopeDocumentationTests.cs"
```

## Parallel Example: User Story 3

```bash
Task: "Add failing excluded-key rejection and secret-safe diagnostic tests in tests/Payslip4All.Web.Tests/Startup/SecretsConfigurationStartupTests.cs"
Task: "Add failing validator unit tests for excluded groups and malformed artifacts in tests/Payslip4All.Infrastructure.Tests/Configuration/AwsSecretsScopeValidationTests.cs"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1.
2. Complete Phase 2.
3. Complete Phase 3 (US1).
4. Run the US1 independent test.
5. Present the Manual Test Gate before any git action.

### Incremental Delivery

1. Land shared catalog and validator infrastructure.
2. Deliver US1 to preserve supported behavior.
3. Deliver US2 migration guidance.
4. Deliver US3 safe-failure behavior.
5. Finish polish and regression review.

---

## Notes

- `[P]` tasks touch different files and can be run in parallel.
- `[US#]` labels map each task to a single user story for traceability.
- Each story is intended to be independently completable and testable.
- Verify tests fail before implementation begins.
- Present the Manual Test Gate and wait for explicit approval before any `git commit`, `git merge`, or `git push`.
