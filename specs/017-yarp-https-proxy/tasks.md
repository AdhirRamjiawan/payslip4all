# Tasks: YARP HTTPS Reverse Proxy Migration

**Input**: Design documents from `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/`  
**Prerequisites**: `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/plan.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/spec.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/research.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/data-model.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md`

**Tests**: Per Payslip4All Constitution v1.4.0 Principle I, tests are required for this feature. Every user story below starts with failing xUnit/integration/documentation tests before implementation tasks begin.

**Governed Dependencies**: `Yarp.ReverseProxy` 2.2.x remains approved only for the repository-owned public HTTPS edge inside `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web` under Payslip4All Constitution v1.4.0.

**Organization**: Tasks are grouped by user story so each story can be implemented, tested, and reviewed independently while keeping `/health` as the single readiness smoke-check and `quickstart.md` as the single operator-facing entrypoint.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel when the task touches different files and has no dependency on incomplete work
- **[Story]**: Present only for user-story phase tasks (`[US1]`, `[US2]`, `[US3]`)
- Every task below includes an exact file path and uses the required checklist format

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Reconfirm the approved YARP scope and align the current feature seams before new failing tests are added.

- [X] T001 Reconfirm YARP governance and branch scope against `/Users/adhirramjiawan/projects/payslip4all/.specify/memory/constitution.md`, `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/plan.md`, and `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/spec.md`
- [X] T002 Inventory the current gateway runtime, hosted deployment, and test seams in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`, `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/ReverseProxyModeOptions.cs`, `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/ReverseProxyForwardingTests.cs`, `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs`, and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T003 [P] Rename `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/NginxGatewayConfigTests.cs` to `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/YarpGatewayConfigTests.cs` and align the class/file naming with YARP-first terminology

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared TDD and validation scaffolding that blocks all user-story delivery.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Add failing startup tests for fail-closed certificate prerequisites, the exact activation error `HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.`, and YARP-only service registration in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs`
- [X] T005 [P] Add failing hosted-artifact tests for public ports `80/443`, hosted default upstream `http://127.0.0.1:8080`, and no public exposure of the backend app in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T006 [P] Add failing repository-contract tests for constitution v1.4.0 YARP approval, canonical contract ownership in `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md`, quickstart-first operator guidance in `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md`, and historical-only nginx references in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/YarpGatewayConfigTests.cs`
- [X] T007 Implement shared exact-response and timing assertions for `421`, exact `503 Service temporarily unavailable.`, and the within-10-seconds bound in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/ReverseProxyContractAssertions.cs`
- [X] T008 Implement stricter reverse-proxy startup validation for internal-only upstream rules, certificate prerequisites, and the exact fail-closed activation error in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/ReverseProxyModeOptions.cs`
- [X] T009 Implement the common gateway-mode startup scaffold, forwarded-header baseline, and reusable proxy route registration in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`

**Checkpoint**: Shared validation, hosted defaults, and gateway startup seams are ready for story-by-story TDD.

---

## Phase 3: User Story 1 - Serve the public site securely through YARP (Priority: P1) 🎯 MVP

**Goal**: Serve `https://payslip4all.co.za` through the YARP/Kestrel edge, redirect HTTP to HTTPS in one step, keep `/health` as the single readiness smoke-check, and fail closed if certificate activation cannot succeed.

**Independent Test**: Start gateway mode with the required host, upstream, and certificate inputs, verify `http://payslip4all.co.za` redirects once to the equivalent HTTPS URL, and verify 3 consecutive requests to `https://payslip4all.co.za/health` each succeed within 5 seconds.

### Tests for User Story 1 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation**

- [X] T010 [P] [US1] Add failing integration tests for single-step HTTP→HTTPS redirect and 3 consecutive `/health` successes within 5 seconds each in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/ReverseProxyForwardingTests.cs`
- [X] T011 [P] [US1] Add failing operator-guide tests for `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md` covering `/health` as the single smoke-check, the 3-consecutive-request procedure, and the exact certificate activation error in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`

### Implementation for User Story 1

- [X] T012 [US1] Implement correct-host HTTPS serving, single-step HTTP→HTTPS redirect, and public `/health` routing in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`
- [X] T013 [US1] Implement certificate startup gating and the exact activation error `HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.` in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/ReverseProxyModeOptions.cs`
- [X] T014 [P] [US1] Align hosted gateway listener and certificate environment defaults in `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/payslip4all-web.yaml`
- [X] T015 [P] [US1] Align certificate retrieval, `.pfx` materialization, and fail-closed startup behavior in `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`

**Checkpoint**: User Story 1 delivers the secure public HTTPS entry point and its fail-closed startup behavior.

---

## Phase 4: User Story 2 - Route public traffic to the backend application through the YARP edge (Priority: P2)

**Goal**: Forward public-host traffic to the internal backend endpoint while preserving redirects, Blazor/SignalR interactions, and form/navigation context, rejecting wrong hosts with `421`, and returning the exact `503 Service temporarily unavailable.` response within 10 seconds when the upstream is unavailable.

**Independent Test**: Run the backend on `http://127.0.0.1:8080`, send representative requests through `https://payslip4all.co.za`, verify redirects stay on the public host, Blazor/SignalR interactions stay bound to the public host, form submissions and follow-up navigation preserve the public host, wrong-host requests return `421`, and upstream failures return the exact `503 Service temporarily unavailable.` body within 10 seconds.

### Tests for User Story 2 (REQUIRED — TDD, constitution Principle I)

- [X] T016 [P] [US2] Add failing integration tests for redirect preservation, Blazor/SignalR host binding, and form/navigation preservation in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/ReverseProxyForwardingTests.cs`
- [X] T017 [P] [US2] Add failing integration tests for wrong-host `421` rejection and the exact `503 Service temporarily unavailable.` response within 10 seconds in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/ReverseProxyFailureTests.cs`

### Implementation for User Story 2

- [X] T018 [US2] Implement forwarded request-context preservation for redirects, Blazor/SignalR, and form/navigation behavior in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`
- [X] T019 [US2] Implement wrong-host `421` handling and exact `503 Service temporarily unavailable.` response collapsing within 10 seconds in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`
- [X] T020 [US2] Enforce hosted default upstream `http://127.0.0.1:8080` and internal-only override validation in `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/ReverseProxyModeOptions.cs`
- [X] T021 [P] [US2] Align hosted upstream and timeout-related gateway environment values in `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/payslip4all-web.yaml`
- [X] T022 [P] [US2] Mirror the internal-only upstream default and timeout-safe gateway startup values in `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`

**Checkpoint**: User Story 2 preserves public-host behavior through YARP and covers the wrong-host and upstream-failure paths independently.

---

## Phase 5: User Story 3 - Maintain a reusable repository-owned YARP deployment definition (Priority: P3)

**Goal**: Keep the canonical contract, quickstart entrypoint, and supporting hosted/reference documents aligned without reintroducing nginx-defined behavior or letting non-quickstart documents become the primary operator guide.

**Independent Test**: Starting from `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md`, an operator can locate the canonical contract, identify every activation input, and complete the documented `/health` smoke-check procedure in 10 minutes or less without using any other operator guide as the primary source.

### Tests for User Story 3 (REQUIRED — TDD, constitution Principle I)

- [X] T023 [P] [US3] Add failing documentation tests that enforce `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md` as the single operator-facing entrypoint and require an explicit link to `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md` in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`
- [X] T024 [P] [US3] Add failing repository-contract tests for secondary/reference-only infra docs, hosted default upstream `http://127.0.0.1:8080`, exact 10-second wording, and no new nginx-defined public-edge requirements in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/YarpGatewayConfigTests.cs`

### Implementation for User Story 3

- [X] T025 [US3] Refresh canonical public-edge behavior only in `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md` for public host, `/health`, redirects, Blazor/SignalR and form/navigation preservation, wrong-host `421`, exact `503 Service temporarily unavailable.`, the within-10-seconds bound, and fail-closed certificate activation
- [X] T026 [US3] Refresh `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md` as the single operator-facing entrypoint with prerequisites, 3 consecutive `/health` checks, redirect/Blazor/SignalR/form validation, wrong-host `421`, the exact `503 Service temporarily unavailable.` response within 10 seconds, and the exact certificate activation error
- [X] T027 [P] [US3] Align `/Users/adhirramjiawan/projects/payslip4all/infra/yarp/README.md` and `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/README.md` as secondary/reference-only documents that point back to `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md`
- [X] T028 [P] [US3] Update `/Users/adhirramjiawan/projects/payslip4all/README.md` to direct operators to `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md` first and treat infra documentation as reference-only

**Checkpoint**: User Story 3 leaves the repository with one canonical contract, one operator-facing entrypoint, and reference-only supporting docs.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Complete the constitution-required quality gates and quickstart-driven end-to-end validation.

- [X] T029 [P] Run the gateway regression suite in `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj`
- [X] T030 [P] Run the validation build for `/Users/adhirramjiawan/projects/payslip4all/Payslip4All.sln`
- [X] T031 [P] Collect constitution-required `>=80%` coverage evidence from `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Domain.Tests/Payslip4All.Domain.Tests.csproj` and `/Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Application.Tests/Payslip4All.Application.Tests.csproj` for `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Domain` and `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Application`
- [ ] T032 Run the quickstart-driven manual smoke validation and Manual Test Gate using `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md` and `/Users/adhirramjiawan/projects/payslip4all/.specify/memory/constitution.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 → Phase 2**: Setup must complete before foundational TDD scaffolding begins.
- **Phase 2 → Phase 3/4/5**: Foundational validation blocks all user stories.
- **Phase 3 (US1)**: Should complete first because secure public HTTPS reachability is the MVP.
- **Phase 4 (US2)**: Follows US1 so forwarding and failure behavior are validated against the working public edge.
- **Phase 5 (US3)**: Finalize after US1 and US2 so contract and documentation reflect implemented behavior exactly.
- **Phase 6**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2 and has no dependency on other user stories.
- **US2 (P2)**: Starts after Phase 2 but is easiest to finish after US1 enables the public edge.
- **US3 (P3)**: Starts after Phase 2 but should be finalized after US1 and US2 so quickstart and supporting docs match the tested behavior.

### Within Each User Story

- Write the story’s failing tests first and confirm they fail for the intended reason before implementation begins.
- Keep `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md` as the canonical contract source.
- Keep `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md` as the single operator-facing entrypoint; all other docs are secondary/reference-only.
- Preserve the hosted default upstream `http://127.0.0.1:8080`, wrong-host `421`, exact `503 Service temporarily unavailable.`, and the within-10-seconds bound wherever the story touches runtime or docs.
- **Manual Test Gate (Principle VI)**: Present the gate prompt after implementation work and await `approve` before any `git commit`, `git merge`, or `git push`.

### Dependency Graph

`Setup → Foundational → US1 (MVP) → US2 → US3 → Polish`

---

## Parallel Opportunities

- **Setup**: `T003` can run in parallel after `T001`/`T002` begin.
- **Foundational**: `T004`, `T005`, and `T006` can run in parallel.
- **US1**: `T010` and `T011` can run in parallel; after runtime work begins, `T014` and `T015` can run in parallel.
- **US2**: `T016` and `T017` can run in parallel; `T021` and `T022` can run in parallel after the runtime behavior is implemented.
- **US3**: `T023` and `T024` can run in parallel; `T027` and `T028` can run in parallel after `T025` and `T026`.
- **Polish**: `T029`, `T030`, and `T031` can run in parallel before `T032`.

---

## Parallel Example: User Story 1

```bash
Task: "T010 Add failing integration tests for single-step HTTP→HTTPS redirect and 3 consecutive /health successes within 5 seconds each in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/ReverseProxyForwardingTests.cs"
Task: "T011 Add failing operator-guide tests for quickstart.md covering /health as the single smoke-check, the 3-consecutive-request procedure, and the exact certificate activation error in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs"

Task: "T014 Align hosted gateway listener and certificate environment defaults in /Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/payslip4all-web.yaml"
Task: "T015 Align certificate retrieval, .pfx materialization, and fail-closed startup behavior in /Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh"
```

## Parallel Example: User Story 2

```bash
Task: "T016 Add failing integration tests for redirect preservation, Blazor/SignalR host binding, and form/navigation preservation in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/ReverseProxyForwardingTests.cs"
Task: "T017 Add failing integration tests for wrong-host 421 rejection and the exact 503 Service temporarily unavailable. response within 10 seconds in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Integration/ReverseProxyFailureTests.cs"

Task: "T021 Align hosted upstream and timeout-related gateway environment values in /Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/payslip4all-web.yaml"
Task: "T022 Mirror the internal-only upstream default and timeout-safe gateway startup values in /Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh"
```

## Parallel Example: User Story 3

```bash
Task: "T023 Add failing documentation tests that enforce quickstart.md as the single operator-facing entrypoint and require an explicit link to contracts/yarp-gateway-contract.md in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs"
Task: "T024 Add failing repository-contract tests for secondary/reference-only infra docs, hosted default upstream http://127.0.0.1:8080, exact 10-second wording, and no new nginx-defined public-edge requirements in /Users/adhirramjiawan/projects/payslip4all/tests/Payslip4All.Web.Tests/Infrastructure/YarpGatewayConfigTests.cs"

Task: "T027 Align /Users/adhirramjiawan/projects/payslip4all/infra/yarp/README.md and /Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/README.md as secondary/reference-only documents that point back to quickstart.md"
Task: "T028 Update /Users/adhirramjiawan/projects/payslip4all/README.md to direct operators to quickstart.md first and treat infra documentation as reference-only"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Run the US1 tests plus the quickstart `/health` and redirect smoke checks.
5. Present the Manual Test Gate and stop for approval before any git operation.

### Incremental Delivery

1. Deliver US1 for secure public HTTPS reachability and fail-closed certificate activation.
2. Add US2 for forwarded-context preservation, wrong-host `421`, and exact `503 Service temporarily unavailable.` behavior within 10 seconds.
3. Add US3 for canonical contract, quickstart-first operator guidance, and reference-only supporting docs.
4. Finish with Phase 6 build, test, coverage, and manual smoke validation.

### Parallel Team Strategy

1. One engineer completes Setup + Foundational.
2. After Phase 2:
   - Engineer A drives US1 runtime and certificate activation behavior.
   - Engineer B drives US2 forwarding and failure-mode behavior.
   - Engineer C drives US3 contract and documentation alignment.
3. Re-converge for Phase 6 validation and Manual Test Gate.

---

## Notes

- `[P]` appears only where the work can proceed on different files without waiting on incomplete changes.
- `[US#]` labels appear only in user-story phases.
- `/health` remains the single readiness smoke-check contract throughout this task plan.
- The 3-consecutive-request `/health` verification is explicitly encoded for SC-003.
- Non-quickstart documents are secondary/reference-only and must point back to `quickstart.md`.
- All runtime and documentation tasks preserve the hosted default upstream `http://127.0.0.1:8080`, wrong-host `421`, exact `503 Service temporarily unavailable.`, fail-closed certificate activation, and the exact 10-second bound.
