# Tasks: Document Payment Gateway Setup

**Input**: Design documents from `specs/010-payment-gateway-readme/`  
**Required inputs**: `specs/010-payment-gateway-readme/plan.md`, `specs/010-payment-gateway-readme/spec.md`  
**Additional inputs used**: `specs/010-payment-gateway-readme/research.md`, `specs/010-payment-gateway-readme/data-model.md`, `specs/010-payment-gateway-readme/contracts/readme-payment-gateway-setup-contract.md`, `specs/010-payment-gateway-readme/quickstart.md`

**Tests**: TDD is required in this repository. Each user-story phase starts with failing xUnit and/or bUnit coverage that locks the runtime behavior the README will describe before documentation is updated.

**Organization**: Tasks are grouped by user story priority so each story can be implemented, tested, and reviewed independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Task can run in parallel with other tasks in the same phase
- **[Story]**: Present only in user-story phases as `[US1]`, `[US2]`, `[US3]`
- Every task includes exact file path(s)

---

## Phase 1: Setup

**Purpose**: Prepare the README workspace and dedicated test files for the payment-gateway documentation feature.

- [X] T001 Create the payment-gateway setup section scaffold and table-of-contents entry in `README.md`
- [X] T002 [P] Create dedicated PayFast setup regression test files in `tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentOptionsTests.cs` and `tests/Payslip4All.Web.Tests/Startup/PayFastSetupDocumentationTests.cs`

---

## Phase 2: Foundational

**Purpose**: Lock the shared runtime assumptions that the README will document before any story-specific documentation changes begin.

**⚠️ CRITICAL**: Complete and confirm the failing tests in this phase before updating story-specific README content.

- [X] T003 [P] Add failing xUnit coverage for required PayFast configuration keys, safe `HostedPayments:PayFast` binding, and invalid `PublicNotifyUrl` preconditions in `tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentOptionsTests.cs`
- [X] T004 [P] Add failing web/integration coverage for hosted wallet redirect readiness and informational-only browser returns in `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`, `tests/Payslip4All.Web.Tests/Pages/WalletTopUpGenericReturnTests.cs`, and `tests/Payslip4All.Web.Tests/Startup/PayFastSetupDocumentationTests.cs`
- [X] T005 Align the README payment-gateway headings and section order with `specs/010-payment-gateway-readme/contracts/readme-payment-gateway-setup-contract.md` in `README.md`

**Checkpoint**: Shared test coverage and README structure are ready for user-story implementation.

---

## Phase 3: User Story 1 - Configure the gateway from the setup guide (Priority: P1) 🎯 MVP

**Goal**: Let a developer or operator identify every required PayFast setup value, understand its purpose, and place secrets safely without reading source code.

**Independent Test**: Hand the README to a teammate unfamiliar with Payslip4All payments and confirm they can identify the supported gateway, every required `HostedPayments:PayFast` key, and the correct non-committed places to supply secrets.

### Tests for User Story 1 (REQUIRED — TDD)

- [X] T006 [P] [US1] Add failing xUnit coverage for the documented PayFast option surface and startup validation expectations in `tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentOptionsTests.cs`
- [X] T007 [P] [US1] Add failing bUnit coverage that the wallet top-up flow remains hosted and does not collect card details directly in `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`

### Implementation for User Story 1

- [X] T008 [US1] Add the payment gateway overview and required `HostedPayments:PayFast` configuration table to `README.md`
- [X] T009 [US1] Add secret-handling guidance for private local settings, environment variables, and deployment secrets to `README.md`

**Checkpoint**: User Story 1 is complete when the README alone lets a maintainer prepare a valid payment-gateway configuration safely.

---

## Phase 4: User Story 2 - Understand public callback and environment requirements (Priority: P2)

**Goal**: Explain sandbox versus live mode clearly and make the public HTTPS notify callback requirement unambiguous.

**Independent Test**: Review the README and confirm a maintainer can explain when `UseSandbox` should be true or false, why `PublicNotifyUrl` must be public HTTPS, and why `localhost` or private callback addresses break trustworthy confirmation.

### Tests for User Story 2 (REQUIRED — TDD)

- [X] T010 [P] [US2] Add failing xUnit coverage for sandbox/live PayFast endpoint selection and callback validation rules in `tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentOptionsTests.cs` and `tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs`
- [X] T011 [P] [US2] Add failing startup coverage for the mapped PayFast notify route and public callback expectations in `tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs` and `tests/Payslip4All.Web.Tests/Startup/PayFastSetupDocumentationTests.cs`

### Implementation for User Story 2

- [X] T012 [US2] Add sandbox-versus-live environment guidance and `UseSandbox` setup examples to `README.md`
- [X] T013 [US2] Add callback guidance for `/api/payments/payfast/notify`, public HTTPS reachability, and informational-only browser returns to `README.md`

**Checkpoint**: User Story 2 is complete when the README makes environment choice and callback reachability requirements clear enough to avoid the most common setup mistakes.

---

## Phase 5: User Story 3 - Verify the setup end to end (Priority: P3)

**Goal**: Give maintainers a short verification path and first-pass troubleshooting guidance so they can confirm the gateway is working after configuration.

**Independent Test**: Follow the README after configuration and confirm a maintainer can launch a hosted wallet top-up, recognize that notify handling is the trustworthy confirmation path, and diagnose missing credentials, wrong mode, or invalid callback setup in one pass.

### Tests for User Story 3 (REQUIRED — TDD)

- [X] T014 [P] [US3] Add failing bUnit coverage for hosted top-up start, owner-safe verification messaging, and informational-only browser return behavior in `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs` and `tests/Payslip4All.Web.Tests/Pages/WalletTopUpGenericReturnTests.cs`
- [X] T015 [P] [US3] Add failing xUnit coverage for gateway-start failure categories that the troubleshooting guide will reference in `tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs`

### Implementation for User Story 3

- [X] T016 [US3] Add the end-to-end payment-gateway verification walkthrough to `README.md`
- [X] T017 [US3] Add first-pass troubleshooting guidance for missing credentials, invalid `PublicNotifyUrl`, and wrong sandbox/live mode to `README.md`
- [X] T018 [US3] Replace stale fake-provider wallet verification wording with PayFast-aligned verification steps in `README.md`

**Checkpoint**: User Story 3 is complete when the README supports first-time setup verification and troubleshooting without relying on tribal knowledge or source inspection.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Remove conflicting wording and validate the final documentation against the planned scenarios.

- [X] T019 [P] Remove or rewrite conflicting payment-gateway wording elsewhere in `README.md` so the final document has one consistent PayFast setup narrative
- [X] T020 Run the targeted regression suites and quickstart validation referenced by `tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentOptionsTests.cs`, `tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs`, `tests/Payslip4All.Web.Tests/Pages/WalletTests.cs`, `tests/Payslip4All.Web.Tests/Pages/WalletTopUpGenericReturnTests.cs`, and `specs/010-payment-gateway-readme/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion; blocks all user-story work
- **User Story 1 (Phase 3)**: Depends on Foundational completion; MVP slice
- **User Story 2 (Phase 4)**: Depends on Foundational completion; may be delivered after US1 and remains independently testable
- **User Story 3 (Phase 5)**: Depends on Foundational completion; may be delivered after US1 and US2 but remains independently testable
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: No dependency on other user stories after Foundational
- **US2 (P2)**: No functional dependency on US1, but shares `README.md`, so sequence README edits to avoid merge conflicts
- **US3 (P3)**: No functional dependency on US1 or US2, but also shares `README.md`, so sequence README edits to avoid merge conflicts

### Within Each User Story

- Write the failing tests first and confirm they fail before editing implementation or documentation
- Lock runtime behavior in xUnit/bUnit before describing it in `README.md`
- Update README content only after the supporting failing tests exist
- Run the story’s targeted validation before moving to the next story
- **Manual Test Gate (constitution approval rule)**: After implementation is complete, present the gate prompt to the engineer and await `approve` before any `git commit`, `git merge`, or `git push`

### Parallel Opportunities

- `T002`, `T003`, and `T004` can run in parallel because they touch separate test files and setup assets
- Within **US1**, `T006` and `T007` can run in parallel
- Within **US2**, `T010` and `T011` can run in parallel
- Within **US3**, `T014` and `T015` can run in parallel
- Final validation task `T020` can be prepared while `T019` is being completed, but should run after README edits land

---

## Parallel Example: User Story 1

```bash
# Launch the User Story 1 failing tests together:
Task: "Add failing xUnit coverage for the documented PayFast option surface in tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentOptionsTests.cs"
Task: "Add failing bUnit coverage that the wallet top-up flow remains hosted in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs"
```

## Parallel Example: User Story 2

```bash
# Launch the User Story 2 failing tests together:
Task: "Add failing xUnit coverage for sandbox/live endpoint selection in tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentOptionsTests.cs and tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs"
Task: "Add failing startup coverage for the mapped PayFast notify route in tests/Payslip4All.Web.Tests/Startup/StartupDependencyTests.cs and tests/Payslip4All.Web.Tests/Startup/PayFastSetupDocumentationTests.cs"
```

## Parallel Example: User Story 3

```bash
# Launch the User Story 3 failing tests together:
Task: "Add failing bUnit coverage for hosted top-up start and verification messaging in tests/Payslip4All.Web.Tests/Pages/WalletTests.cs and tests/Payslip4All.Web.Tests/Pages/WalletTopUpGenericReturnTests.cs"
Task: "Add failing xUnit coverage for gateway-start failure categories in tests/Payslip4All.Infrastructure.Tests/HostedPayments/PayFastHostedPaymentProviderTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate that the README alone now covers the supported gateway, required keys, and secret handling
5. Present the Manual Test Gate prompt before any git operation

### Incremental Delivery

1. Finish Setup + Foundational to lock the documented runtime assumptions
2. Deliver **US1** for configuration clarity and safe secret handling
3. Deliver **US2** for environment and callback clarity
4. Deliver **US3** for verification and troubleshooting
5. Finish with Polish to remove stale wording and run targeted validation

### Parallel Team Strategy

1. One developer handles shared setup/foundational tests
2. After Foundational completion:
   - Developer A: US1 tests and config-table content
   - Developer B: US2 tests and environment/callback content
   - Developer C: US3 tests and verification/troubleshooting content
3. Merge README edits sequentially to avoid same-file conflicts

---

## Notes

- [P] tasks indicate separate files or otherwise non-blocking work
- All user-story tasks include exact file paths and story labels
- Every story remains independently testable from the README consumer’s perspective
- Because the main deliverable is `README.md`, sequence README-writing tasks even when the surrounding tests can run in parallel
- Do not auto-commit; respect the Manual Test Gate before any git operation
