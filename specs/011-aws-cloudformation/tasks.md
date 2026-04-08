# Tasks: AWS CloudFormation Deployment

**Input**: Design documents from `specs/011-aws-cloudformation/`  
**Required inputs**: `specs/011-aws-cloudformation/plan.md`, `specs/011-aws-cloudformation/spec.md`  
**Additional inputs used**: `specs/011-aws-cloudformation/research.md`, `specs/011-aws-cloudformation/data-model.md`, `specs/011-aws-cloudformation/contracts/cloudformation-deployment-contract.md`, `specs/011-aws-cloudformation/quickstart.md`

**Tests**: TDD is required in this repository (constitution Principle I). Each user-story phase starts with failing xUnit tests that lock CloudFormation template structure, startup behaviour, and DynamoDB backup behaviour before any implementation begins. Test tasks are non-optional.

**Organization**: Tasks are grouped by user story priority so each deployment slice can be implemented, tested, and validated independently.

**Gap resolutions encoded in this task list**:
- **FR-002 sequencing fix**: ALB + target group + health-based routing are provisioned in US1 (working environment) so the app is behind a load balancer from day one; Route 53 + ACM + public `payslip4all.co.za` access are added in US2. MVP no longer promises public reachability.
- **FR-006 allowlist coverage**: Explicit test (T012) verifies `AllowedSshCidr` is a template parameter wired to the security group ingress rule with no hardcoded operator-access range.
- **FR-009 signal inventory coverage**: Explicit test (T005) verifies the complete required output set — `ApplicationUrl`, `InstanceId`, `LoadBalancerArn`, security-group identifiers, and backup mode — is present in the template before implementation.
- **FR-011 EC2 replacement coverage**: Explicit test (T013) verifies the CloudFormation template's resource structure keeps DynamoDB, networking, and ALB resources independent of the EC2 instance lifecycle so replacement does not destroy application data or shared infrastructure.
- **SC-005 workflow validation**: Explicit documentation test (T015) asserts that `infra/aws/cloudformation/README.md` enumerates exactly five manual pre-launch actions matching the contract-defined steps.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Task can run in parallel with other tasks in the same phase (different files, no blocking dependency)
- **[Story]**: Present only in user-story phases as `[US1]`, `[US2]`, `[US3]`
- Every task includes exact file path(s)

---

## Phase 1: Setup

**Purpose**: Prepare the CloudFormation workspace, bootstrap assets, and dedicated deployment test files.

- [X] T001 Create the deployment asset scaffold in `infra/aws/cloudformation/payslip4all-web.yaml`, `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`, and `infra/aws/cloudformation/README.md`
- [X] T002 [P] Create CloudFormation template and deployment-document test files in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs` and `tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`
- [X] T003 [P] Create AWS startup and DynamoDB backup test files in `tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs` and `tests/Payslip4All.Infrastructure.Tests/Persistence/DynamoDB/DynamoDbBackupProtectionTests.cs`

---

## Phase 2: Foundational

**Purpose**: Lock the shared deployment assumptions every user story depends on before story-specific implementation begins.

**⚠️ CRITICAL**: All failing tests in this phase must be confirmed failing before user-story work starts.

- [X] T004 [P] Add failing template-contract tests for all required CloudFormation parameters (including externalized inputs from the contract) and the base compute and DynamoDB deployment defaults in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T005 [P] Add failing template output tests asserting the complete FR-009 operator-visible signal inventory: `ApplicationUrl`, `InstanceId`, `LoadBalancerArn`, at least two security-group identifier outputs, and a backup-mode output in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T006 [P] Add failing startup tests for the load-balancer health endpoint registration and AWS DynamoDB environment binding (`PERSISTENCE_PROVIDER`, `DYNAMODB_REGION`, `DYNAMODB_TABLE_PREFIX`) in `tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs`
- [X] T007 [P] Add failing backup-protection tests for point-in-time recovery enablement and restore-to-new-table safety in `tests/Payslip4All.Infrastructure.Tests/Persistence/DynamoDB/DynamoDbBackupProtectionTests.cs`
- [X] T008 Implement the reusable deployment health endpoint and register it in startup in `src/Payslip4All.Web/Endpoints/HealthEndpoint.cs` and `src/Payslip4All.Web/Program.cs`
- [X] T009 Create the shared CloudFormation parameter block, output skeleton (covering the full FR-009 signal set), and bootstrap environment variable rendering in `infra/aws/cloudformation/payslip4all-web.yaml` and `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`

**Checkpoint**: Shared deployment tests, health endpoint, and base infrastructure skeleton are ready for user-story implementation.

---

## Phase 3: User Story 1 - Deploy a working environment (Priority: P1) 🎯 MVP

**Goal**: Give operators one CloudFormation-driven path to launch a working Payslip4All environment on EC2 behind an ALB with DynamoDB-backed persistence. Public `payslip4all.co.za` reachability is NOT required at this stage (see US2).

**Independent Test**: Launch the stack with required inputs and confirm the EC2-hosted app starts successfully, the ALB target is healthy, DynamoDB tables are provisioned, and no core AWS resources were created by hand. Public DNS reachability is out of scope for this story.

### Tests for User Story 1 (REQUIRED — TDD, confirm FAILING before T016)

- [X] T010 [P] [US1] Add failing template tests for VPC, public subnets, EC2 security group, IAM instance profile, and single EC2 instance resource structure in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T011 [P] [US1] Add failing template tests for ALB resource, target group, HTTP listener, and health-check configuration confirming the app is behind a load-balanced entry point (FR-002 base) without Route 53 or ACM in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T012 [P] [US1] Add failing template test for FR-006 operator access allowlist: verify `AllowedSshCidr` is a declared parameter wired to the EC2 security group SSH ingress rule and that no operator-access CIDR range is hardcoded in the template in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T013 [P] [US1] Add failing template test for FR-011 EC2 replacement safety: verify DynamoDB-related resources, ALB, and networking resources are declared independently of the EC2 instance such that replacing or recreating the instance does not trigger deletion of shared infrastructure or application data in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T014 [P] [US1] Add failing documentation tests for operator launch prerequisites and required runtime environment variables (`PERSISTENCE_PROVIDER`, `DYNAMODB_REGION`, `DYNAMODB_TABLE_PREFIX`) in `tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`
- [X] T015 [US1] Add failing documentation test for SC-005: assert that `infra/aws/cloudformation/README.md` enumerates exactly five manual pre-launch actions matching the contract-defined steps (publish artifact, confirm ACM issuance, confirm Route 53 authority, gather secret references, launch stack) in `tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`

### Implementation for User Story 1

- [X] T016 [US1] Implement VPC, public subnets, security groups, IAM instance profile, EC2 instance, ALB, target group, HTTP listener, and health-based routing resources in `infra/aws/cloudformation/payslip4all-web.yaml`
- [X] T017 [US1] Implement the instance bootstrap flow that installs and starts Payslip4All with `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, and `DYNAMODB_TABLE_PREFIX` and no hardcoded reusable secrets in `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`
- [X] T018 [US1] Document stack launch inputs, secret reference patterns, the five-action pre-launch workflow enumeration, and first-deploy steps in `infra/aws/cloudformation/README.md`

**Checkpoint**: US1 is complete when operators can launch one working EC2 + ALB + DynamoDB environment, the ALB target is healthy, and the five-step launch workflow is documented.

---

## Phase 4: User Story 2 - Publish the application on payslip4all.co.za (Priority: P2)

**Goal**: Expose the deployed application through `payslip4all.co.za` with secure public routing and payslip4all.co.za-derived operational naming. This phase extends the US1 ALB with HTTPS, Route 53, and ACM integration.

**Independent Test**: Complete the deployment with domain prerequisites and confirm `https://payslip4all.co.za` resolves to the ALB-backed application, HTTP traffic redirects to HTTPS, and the EC2 instance is identifiable through payslip4all.co.za-based metadata.

### Tests for User Story 2 (REQUIRED — TDD, confirm FAILING before T021)

- [X] T019 [P] [US2] Add failing template tests for HTTPS ALB listener with ACM certificate reference, HTTP-to-HTTPS redirect rule, Route 53 alias record pointing `payslip4all.co.za` at the ALB, and payslip4all.co.za-derived `Name` tag on the EC2 instance and ALB in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`
- [X] T020 [P] [US2] Add failing documentation tests for DNS ownership confirmation prerequisite, ACM certificate issuance prerequisite, and non-free-tier cost disclosure for ALB and Route 53 in `tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`

### Implementation for User Story 2

- [X] T021 [US2] Implement HTTPS ALB listener with ACM certificate attachment, HTTP-to-HTTPS redirect rule, Route 53 alias record for `payslip4all.co.za`, and payslip4all.co.za-derived instance and load-balancer tags in `infra/aws/cloudformation/payslip4all-web.yaml`
- [X] T022 [US2] Document public-domain setup, HTTPS behaviour, operator-visible resource naming, and non-free-tier cost exceptions in `infra/aws/cloudformation/README.md` and `README.md`

**Checkpoint**: US2 is complete when `https://payslip4all.co.za` resolves to the application and operators can identify the EC2 instance from payslip4all.co.za-derived metadata.

---

## Phase 5: User Story 3 - Protect DynamoDB data with automated recovery (Priority: P3)

**Goal**: Ensure the DynamoDB-backed deployment has automated point-in-time recovery protection on all application tables and a documented restore path that does not require rebuilding the public entry point.

**Independent Test**: Confirm PITR is enabled for every Payslip4All DynamoDB table after stack launch, verify the restore drill creates a new target table without touching the live environment, and validate the runbook steps produce the expected outcome.

### Tests for User Story 3 (REQUIRED — TDD, confirm FAILING before T025)

- [X] T023 [P] [US3] Add failing infrastructure tests for PITR enablement on every Payslip4All DynamoDB table and for restore-to-new-table behaviour that avoids overwriting live data in `tests/Payslip4All.Infrastructure.Tests/Persistence/DynamoDB/DynamoDbBackupProtectionTests.cs`
- [X] T024 [P] [US3] Add failing template and documentation tests for backup-related IAM permissions on the instance role, the FR-009 backup-mode output, and restore runbook content (including the safe non-live-target restore path) in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs` and `tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`

### Implementation for User Story 3

- [X] T025 [US3] Implement the `DynamoDbBackupProtectionHostedService` that enables PITR on all application tables at startup and register it through `DynamoDbServiceExtensions` alongside the existing `DynamoDbTableProvisioner` in `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbBackupProtectionHostedService.cs`, `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbServiceExtensions.cs`, and `src/Payslip4All.Infrastructure/Persistence/DynamoDB/DynamoDbTableProvisioner.cs`
- [X] T026 [US3] Add DynamoDB backup IAM permissions (`dynamodb:UpdateContinuousBackups`, `dynamodb:DescribeContinuousBackups`, `dynamodb:RestoreTableToPointInTime`) and the backup-mode CloudFormation output to the instance role and Outputs section in `infra/aws/cloudformation/payslip4all-web.yaml`
- [X] T027 [US3] Document automated backup behaviour, operator-led restore steps (restore to non-live table, validate, promote), recovery validation criteria, and the 60-minute restore drill target in `infra/aws/cloudformation/README.md`

**Checkpoint**: US3 is complete when PITR is enabled on all application tables, operators can execute a restore drill into a new table, and the runbook is verified by the documentation tests.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Align root documentation, operational guidance, and full-suite validation across all stories.

- [X] T028 [P] Add final deployment entry-point references and CloudFormation usage notes to `README.md` and `infra/aws/cloudformation/README.md`
- [X] T029 [P] Align health-check path, forwarded-header handling, and operational logging configuration for a behind-ALB deployment in `src/Payslip4All.Web/Program.cs` and `infra/aws/cloudformation/README.md`
- [X] T030 Run the full targeted deployment validation suites and the quickstart walkthrough in `tests/Payslip4All.Web.Tests/Infrastructure/AwsCloudFormationTemplateTests.cs`, `tests/Payslip4All.Web.Tests/Infrastructure/AwsDeploymentDocumentationTests.cs`, `tests/Payslip4All.Web.Tests/Startup/AwsDeploymentStartupTests.cs`, `tests/Payslip4All.Infrastructure.Tests/Persistence/DynamoDB/DynamoDbBackupProtectionTests.cs`, and `specs/011-aws-cloudformation/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion; blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion; MVP deployment slice — no public domain required
- **User Story 2 (Phase 4)**: Depends on Foundational completion; extends the US1 ALB with public domain and HTTPS; shares `payslip4all-web.yaml` and README with US1 so sequence after US1 to avoid merge conflicts
- **User Story 3 (Phase 5)**: Depends on Foundational completion; shares DynamoDB infrastructure and deployment docs; sequence after US1 to avoid conflicting edits to the same files
- **Polish (Phase 6)**: Depends on all required user stories being complete

### User Story Dependencies

- **US1 (P1)**: No dependency on other stories after Foundational; delivers EC2 + ALB + DynamoDB working environment
- **US2 (P2)**: Extends US1's ALB with Route 53 and ACM; must not merge `payslip4all-web.yaml` changes until US1 implementation tasks (T016–T018) are complete
- **US3 (P3)**: Extends the instance IAM role and adds a hosted service; must not merge IAM or startup changes until US1 implementation tasks (T016–T017) are complete

### Within Each User Story

- Write the failing tests first (TDD gate: confirm FAIL before writing implementation files)
- Lock CloudFormation resource, signal, and security behaviour in tests before editing deployment assets
- Implement infrastructure and application changes before documentation updates within the story
- Run the story's targeted tests to confirm they pass before moving to the next story
- **Manual Test Gate (constitution Principle VI)**: After each implementation task, present the gate prompt to the engineer and await explicit `approve` before any `git commit`, `git merge`, or `git push`

### Parallel Opportunities

- `T002` and `T003` can run in parallel (different test files)
- Within **Phase 2**: `T004`, `T005`, `T006`, and `T007` can run in parallel (different test files and concerns)
- Within **US1 tests**: `T010`, `T011`, `T012`, `T013`, and `T014` can run in parallel (same test file but different test methods; `T015` depends on README content so run after `T014`)
- Within **US2 tests**: `T019` and `T020` can run in parallel
- Within **US3 tests**: `T023` and `T024` can run in parallel
- Within **Phase 6**: `T028` and `T029` can run in parallel before `T030`

---

## Parallel Example: User Story 1

```bash
# Launch the US1 template failing tests together (different test methods, same file):
Task T010: "VPC, subnets, EC2 SG, IAM profile, EC2 instance structure"
Task T011: "ALB resource, target group, HTTP listener, health-check configuration"
Task T012: "AllowedSshCidr parameter wired to security group ingress, no hardcoded CIDR"
Task T013: "EC2 replacement safety — DynamoDB and ALB independent of EC2 lifecycle"

# Launch documentation tests in parallel:
Task T014: "Launch prerequisites and runtime environment variables"
```

## Parallel Example: User Story 2

```bash
# Launch the US2 failing tests together:
Task T019: "HTTPS listener, HTTP redirect, Route 53 alias, payslip4all.co.za tags — AwsCloudFormationTemplateTests.cs"
Task T020: "ACM prerequisites, DNS ownership, cost disclosures — AwsDeploymentDocumentationTests.cs"
```

## Parallel Example: User Story 3

```bash
# Launch the US3 failing tests together:
Task T023: "PITR enablement and restore-to-new-table safety — DynamoDbBackupProtectionTests.cs"
Task T024: "Backup IAM permissions, backup-mode output, restore runbook content — AwsCloudFormationTemplateTests.cs + AwsDeploymentDocumentationTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1
   - Delivers EC2 + ALB (no public domain) + DynamoDB working environment
   - Five-action launch workflow documented and test-verified
   - ALB, allowlist, replacement safety, and signal inventory all test-covered
4. Present Manual Test Gate prompt — await engineer `approve` before any git operation
5. **STOP and VALIDATE**: confirm operators can launch the working environment independently of public DNS

### Incremental Delivery

1. Finish Setup + Foundational → deployment contract, runtime expectations, and signal inventory locked
2. Deliver **US1** → core launchable AWS environment with ALB and DynamoDB, five-step workflow verified
3. Deliver **US2** → public `payslip4all.co.za` entry point, HTTPS, Route 53, operational naming
4. Deliver **US3** → automated DynamoDB PITR protection and documented restore drill
5. Finish with **Polish** to align docs and run full validation suite

### Parallel Team Strategy

1. One developer handles Phase 1 Setup and Phase 2 Foundational together
2. After Foundational checkpoint:
   - Developer A: US1 — compute, ALB, bootstrap, allowlist, replacement safety, five-step docs
   - Developer B: US2 — public domain routing and DNS/TLS docs (branches off after US1 YAML is stable)
   - Developer C: US3 — DynamoDB backup protection hosted service and restore docs
3. Merge changes to `infra/aws/cloudformation/payslip4all-web.yaml` and `infra/aws/cloudformation/README.md` carefully — these files are shared across US1, US2, and US3

---

## Notes

- `[P]` tasks can run in parallel because they target separate files or non-overlapping test methods
- All user-story tasks carry story labels and exact file paths for traceability
- Each story is independently testable from the operator's perspective before subsequent stories begin
- FR-002 is fully satisfied at US2 completion (public load-balanced entry on `payslip4all.co.za`); US1 satisfies the load-balancer infrastructure prerequisite without requiring public DNS
- FR-006, FR-009, FR-011, and SC-005 each have dedicated test tasks (T012, T005, T013, T015) to ensure they are verified before implementation is marked complete
- The CloudFormation template and deployment README are shared assets across stories; parallel work must focus on isolated test methods and separate code files first to avoid merge conflicts
- Do not auto-commit; the Manual Test Gate must be presented and approved before any `git commit`, `git merge`, or `git push`
