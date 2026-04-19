# Tasks: nginx HTTPS Reverse Proxy

**Input**: Design documents from `/specs/013-nginx-https-proxy/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md, contracts/nginx-gateway-contract.md

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for this feature. Write the failing xUnit/WebApplicationFactory tests first, confirm they fail, then implement the minimum change to pass.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no unmet dependencies)
- **[Story]**: User story label (`[US1]`, `[US2]`, `[US3]`)
- Every task includes the exact file path(s) to change or validate

## Path Conventions

- Web app startup: `src/Payslip4All.Web/Program.cs`
- AWS deployment assets: `infra/aws/cloudformation/payslip4all-web.yaml`, `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`, `infra/aws/cloudformation/README.md`
- New nginx assets: `infra/nginx/`
- Web deployment tests: `tests/Payslip4All.Web.Tests/{Infrastructure,Integration,Startup}`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the source-controlled nginx artifact locations and matching test files before feature implementation starts.

- [X] T001 Create the nginx asset structure in infra/nginx/README.md and infra/nginx/payslip4all.conf
- [X] T002 Create gateway test files in tests/Payslip4All.Web.Tests/Infrastructure/NginxGatewayConfigTests.cs and tests/Payslip4All.Web.Tests/Integration/ReverseProxyForwardingTests.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared runtime assumptions that every user story depends on.

**⚠️ CRITICAL**: Complete this phase before any user story implementation starts.

- [X] T003 [P] Update nginx-hosted startup expectations in tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs
- [X] T004 [P] Replace CloudFormation baseline assertions with nginx port, certificate, and bootstrap checks in tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs
- [X] T005 [P] Implement nginx runtime prerequisites, certificate directories, and ASPNETCORE_URLS=http://127.0.0.1:8080 in infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh
- [X] T006 [P] Mirror the nginx-hosted baseline network, certificate inputs, and bootstrap wiring in infra/aws/cloudformation/payslip4all-web.yaml

**Checkpoint**: nginx is the planned public edge and the app is prepared to run only on `127.0.0.1:8080`.

---

## Phase 3: User Story 1 - Serve the public site securely (Priority: P1) 🎯 MVP

**Goal**: Expose `payslip4all.co.za` over HTTPS and redirect HTTP requests to the secure URL.

**Independent Test**: Apply the repository-owned gateway configuration, request `https://payslip4all.co.za`, and confirm HTTPS succeeds while `http://payslip4all.co.za` redirects once to the equivalent HTTPS URL.

### Tests for User Story 1 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation**

- [X] T007 [P] [US1] Add nginx config tests for payslip4all.co.za HTTPS serving, TLS file references, and HTTP→HTTPS redirect in tests/Payslip4All.Web.Tests/Infrastructure/NginxGatewayConfigTests.cs

### Implementation for User Story 1

- [X] T008 [US1] Implement the payslip4all.co.za HTTP redirect server block and HTTPS listener in infra/nginx/payslip4all.conf
- [X] T009 [US1] Document TLS file references, domain prerequisites, and HTTPS activation steps in infra/nginx/README.md
- [X] T010 [P] [US1] Update HTTPS smoke-test and public-entrypoint guidance in infra/aws/cloudformation/README.md

**Checkpoint**: User Story 1 is complete when the gateway serves the production host only over HTTPS and all secure-access tests pass.

---

## Phase 4: User Story 2 - Route public traffic to the running application (Priority: P2)

**Goal**: Reverse-proxy public traffic to the internal Payslip4All app while preserving forwarded request context and Blazor Server interactivity.

**Independent Test**: Start the app on `127.0.0.1:8080`, send representative requests through the public gateway, and confirm `/health`, redirects, form flows, and upstream failure handling work without exposing the internal endpoint.

### Tests for User Story 2 (REQUIRED — TDD, constitution Principle I)

- [X] T011 [P] [US2] Add reverse-proxy integration tests for forwarded scheme/host, public /health, and generic upstream failure behavior in tests/Payslip4All.Web.Tests/Integration/ReverseProxyForwardingTests.cs
- [X] T012 [P] [US2] Extend startup tests for X-Forwarded-Host processing and local-only upstream hosting in tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs

### Implementation for User Story 2

- [X] T013 [US2] Update forwarded-header middleware for proxy-safe redirects and forwarded host support in src/Payslip4All.Web/Program.cs
- [X] T014 [US2] Add proxy_pass, forwarded headers, WebSocket upgrade handling, /health proxying, host filtering, and generic 503 fallback rules in infra/nginx/payslip4all.conf
- [X] T015 [US2] Update nginx activation, config validation, and service restart order for the 127.0.0.1:8080 upstream in infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh

**Checkpoint**: User Story 2 is complete when proxied requests behave like first-class public HTTPS requests and upstream outages return a safe failure response.

---

## Phase 5: User Story 3 - Maintain a reusable deployment gateway definition (Priority: P3)

**Goal**: Keep the nginx gateway artifact discoverable, documented, and aligned with the repository’s AWS deployment workflow.

**Independent Test**: Inspect the repo and confirm an operator can find the nginx artifact in `infra/`, identify its required certificate/upstream inputs, and follow the AWS/bootstrap guidance without any ALB/Route53/ACM assumptions.

### Tests for User Story 3 (REQUIRED — TDD, constitution Principle I)

- [X] T016 [P] [US3] Rewrite deployment documentation expectations for nginx, Elastic IP, certificate staging, and no-ALB/no-Route53/no-ACM assumptions in tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs
- [X] T017 [P] [US3] Add discoverability and operator-input assertions for infra/nginx/README.md and infra/nginx/payslip4all.conf in tests/Payslip4All.Web.Tests/Infrastructure/NginxGatewayConfigTests.cs

### Implementation for User Story 3

- [X] T018 [US3] Expand reusable gateway inputs, certificate reload steps, wrong-host validation, and operator checks in infra/nginx/README.md
- [X] T019 [P] [US3] Update the AWS operator runbook for nginx bootstrap, certificate delivery, HTTPS smoke tests, and Elastic-IP publishing in infra/aws/cloudformation/README.md
- [X] T020 [P] [US3] Replace ALB-based deployment guidance with nginx-based deployment guidance in ./README.md

**Checkpoint**: User Story 3 is complete when operators can discover the gateway artifact quickly and the documentation/tests consistently describe the nginx-based deployment path.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cross-story cleanup after the three user stories are complete.

- [X] T021 [P] Align the feature smoke-check wording with the implemented gateway workflow in specs/013-nginx-https-proxy/quickstart.md
- [X] T022 Run the gateway-focused regression suite from tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj
- [X] T023 Execute the operator validation checklist from specs/013-nginx-https-proxy/quickstart.md against infra/nginx/README.md and infra/aws/cloudformation/README.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** — no dependencies
- **Phase 2: Foundational** — depends on Phase 1 and blocks all story work
- **Phase 3: US1** — depends on Phase 2; this is the MVP
- **Phase 4: US2** — depends on Phase 2 and should follow US1 because both stories modify infra/nginx/payslip4all.conf and the hosted AWS runtime path
- **Phase 5: US3** — depends on US1 and US2 so the documentation matches the final nginx behavior
- **Phase 6: Polish** — depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational; no dependency on other user stories
- **US2 (P2)**: Starts after Foundational, but sequence it after US1 to avoid conflicts in infra/nginx/payslip4all.conf and infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh
- **US3 (P3)**: Starts after US1 and US2 so tests/docs describe the final gateway implementation rather than interim behavior

### Within Each User Story

- Write the listed tests first and confirm they fail before implementation
- Update shared infra/test files in the order listed to minimize merge conflicts
- Re-run the relevant xUnit/WebApplicationFactory coverage after each story reaches green
- **Manual Test Gate (Principle VI)**: After each implementation task, present the manual test gate prompt and wait for explicit engineer approval before any `git commit`, `git merge`, or `git push`

---

## Parallel Opportunities

- **Foundational**: T003 and T004 can run in parallel first, then T005 and T006 can run in parallel once those tests fail
- **US1**: T009 and T010 can run in parallel after T008 establishes the HTTPS listener behavior
- **US2**: T011 and T012 can run in parallel before implementation
- **US3**: T016 and T017 can run in parallel first, then T019 and T020 can run in parallel after T018 defines the reusable gateway guidance

## Parallel Example: User Story 1

```bash
Task: "Document TLS file references, domain prerequisites, and HTTPS activation steps in infra/nginx/README.md"
Task: "Update HTTPS smoke-test and public-entrypoint guidance in infra/aws/cloudformation/README.md"
```

## Parallel Example: User Story 2

```bash
Task: "Add reverse-proxy integration tests for forwarded scheme/host, public /health, and generic upstream failure behavior in tests/Payslip4All.Web.Tests/Integration/ReverseProxyForwardingTests.cs"
Task: "Extend startup tests for X-Forwarded-Host processing and local-only upstream hosting in tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs"
```

## Parallel Example: User Story 3

```bash
Task: "Rewrite deployment documentation expectations for nginx, Elastic IP, certificate staging, and no-ALB/no-Route53/no-ACM assumptions in tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs"
Task: "Add discoverability and operator-input assertions for infra/nginx/README.md and infra/nginx/payslip4all.conf in tests/Payslip4All.Web.Tests/Infrastructure/NginxGatewayConfigTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Run the US1 tests and perform the HTTPS + redirect smoke checks from specs/013-nginx-https-proxy/quickstart.md
5. Present the Manual Test Gate prompt and wait for approval before any git operation

### Incremental Delivery

1. Finish Setup + Foundational so nginx becomes the planned public edge
2. Deliver **US1** for HTTPS publishing and redirect behavior
3. Deliver **US2** for reverse-proxy forwarding, Blazor WebSocket support, and safe outage handling
4. Deliver **US3** for reusable operator documentation and updated AWS deployment guidance
5. Finish with Polish tasks to verify the docs, tests, and quickstart all match

### Suggested Team Strategy

1. One engineer completes Phases 1-2 because they change the shared deployment baseline
2. After that, one engineer can drive **US1/US2** on the nginx/runtime path while another prepares **US3** test/doc rewrites once the nginx behavior is settled
3. Rejoin for Phase 6 to run the full gateway regression suite and operator checklist

---

## Notes

- All checklist items use the required `- [ ] T### [P?] [Story?] Description with file path` format
- The outdated ALB/Route53/ACM expectations are intentionally replaced by nginx/Elastic-IP/bootstrap expectations in the AWS test and documentation tasks above
- Keep certificate material external to source control; only file paths, references, and operator steps belong in tracked files
- `infra/nginx/payslip4all.conf` is a shared hotspot across US1-US3, so complete tasks in order to avoid churn
- Do not commit or merge until the Manual Test Gate is explicitly approved
