# Feature Specification: AWS Secrets Scope Refinement

**Feature Branch**: `015-refine-aws-secrets-scope`  
**Created**: 2026-04-24  
**Status**: Ready for Planning  
**Input**: User description: "Refine AWS Secrets-backed custom configuration so it excludes DynamoDB runtime and credential keys, keeping the feature constitution-compliant while preserving AWS Secrets support for the remaining repo-owned custom app settings."

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

<!--
  All features MUST comply with the Payslip4All Constitution (.specify/memory/constitution.md).
  Key obligations per principle:

  I.  TDD — Every functional requirement listed below must have acceptance scenarios
      usable as test specifications. Tests MUST be written before implementation.

  II. Clean Architecture — Functional requirements MUST be assignable to exactly one
      layer: Domain (rules), Application (use-case interfaces/DTOs), Infrastructure
      (data/services), or Web (UI/Blazor). Cross-layer leakage is a spec defect.

  III. Blazor Web App — UI acceptance scenarios MUST describe component-level
       behaviour, not API calls or database state.

  IV. Basic Authentication — Any story involving user data MUST specify ownership
      filtering (Company Owner sees only their own data).

  V.  Database — Any story that introduces new entities MUST list them under
      Key Entities below; schema changes become EF Core migrations.
-->
- **Domain**: No payroll, wallet, or ownership rules change; this feature only narrows which configuration groups may use the AWS Secrets-backed custom configuration path.
- **Application**: Application services must continue using resolved configuration values without knowing whether an eligible value came from checked-in defaults, AWS Secrets, or environment overrides.
- **Infrastructure**: The supported-setting catalog, excluded-setting rules, compliance validation, and operator guidance are infrastructure concerns.
- **Web**: Startup and runtime behavior must preserve eligible AWS Secrets-backed settings while preventing excluded DynamoDB runtime and credential groups from being consumed through that path.
- **TDD Alignment**: Each requirement below is expressed as observable behavior so tests can be written first for supported-scope resolution, excluded-scope rejection, upgrade behavior, and secret-safe diagnostics.

### User Story 1 - Keep compliant secret-backed settings (Priority: P1)

As a deployment operator, I want AWS Secrets support to remain available for eligible repo-owned custom app settings while excluding DynamoDB runtime and credential inputs so that I keep centralized secret management for app-owned configuration without violating the project constitution.

**Why this priority**: This is the core outcome of the feature. If eligible settings cannot stay secret-backed, or if excluded DynamoDB and credential groups are still accepted, the refinement fails both operationally and constitutionally.

**Independent Test**: Configure a deployment so eligible custom settings come from AWS Secrets while excluded DynamoDB runtime and credential inputs are absent from that payload, then verify affected app features still behave correctly and the deployment remains compliant.

**Acceptance Scenarios**:

1. **Given** a deployment stores eligible repo-owned custom settings such as authentication cookie configuration and hosted-payment settings in AWS Secrets, **When** the application starts and those features are exercised, **Then** the eligible settings resolve successfully from AWS Secrets and the affected features behave the same as before.
2. **Given** an eligible setting exists in both checked-in defaults and AWS Secrets, **When** configuration is resolved, **Then** the AWS Secrets-backed value is the one applied for that setting unless a higher-priority override is present.
3. **Given** an eligible setting exists in environment overrides and AWS Secrets, **When** configuration is resolved, **Then** the environment override remains highest priority and the AWS Secrets-backed value still outranks checked-in defaults.

---

### User Story 2 - Upgrade non-compliant secret payloads (Priority: P2)

As a deployment operator, I want clear migration guidance from feature 014 so that I can remove DynamoDB runtime and credential groups from the AWS Secrets-backed payload without losing secret-backed support for the remaining eligible app settings.

**Why this priority**: Existing deployments may already rely on the broader feature 014 catalog. They need a safe path to become compliant without guesswork or unnecessary rollback of supported secret-backed settings.

**Independent Test**: Start with a feature 014-style payload that includes both eligible settings and excluded DynamoDB runtime or credential entries, then verify the system identifies what must move out of scope and what can remain secret-backed.

**Acceptance Scenarios**:

1. **Given** an existing AWS Secrets-backed payload includes excluded DynamoDB runtime or credential settings, **When** the deployment is validated, **Then** operators receive clear guidance identifying the excluded groups and the need to remove them from the secret-backed custom configuration scope.
2. **Given** a deployment uses DynamoDB and also needs eligible secret-backed app settings, **When** the operator migrates to the refined scope, **Then** the excluded DynamoDB runtime and credential inputs are supplied outside the secret-backed payload while the eligible app settings continue to work from AWS Secrets.
3. **Given** a deployment does not use any excluded DynamoDB runtime or credential settings in its AWS Secrets-backed payload, **When** it adopts this refinement, **Then** supported secret-backed settings continue working without additional reconfiguration.

---

### User Story 3 - Fail safely on scope violations (Priority: P3)

As a deployment operator, I want non-compliant or invalid AWS Secrets-backed configuration to fail with actionable but secret-safe diagnostics so that I can correct the issue quickly without exposing sensitive values.

**Why this priority**: Safe diagnostics reduce upgrade risk and operational confusion, but they depend on the supported and excluded scope rules already being defined.

**Independent Test**: Validate a deployment with excluded keys or malformed supported values in its AWS Secrets-backed payload and confirm the deployment is blocked or the affected feature is withheld with clear, non-sensitive guidance.

**Acceptance Scenarios**:

1. **Given** an AWS Secrets-backed payload contains excluded DynamoDB runtime or credential settings, **When** validation runs, **Then** the deployment is flagged as non-compliant with operator-facing guidance that names the unsupported group without showing the secret value.
2. **Given** an eligible supported setting is malformed or incomplete in AWS Secrets, **When** configuration is validated, **Then** the system rejects the invalid value using the same business-facing rules applied to other configuration sources and does not reveal secret contents.
3. **Given** an operator reviews diagnostics for a scope or validation failure, **When** the failure is reported, **Then** the message identifies the affected setting group and corrective action without exposing AWS credential material or secret payload contents.

### Edge Cases

- An AWS Secrets-backed payload contains both supported settings and excluded DynamoDB runtime or credential groups in the same deployment.
- A deployment switches to DynamoDB after previously using only relational settings and forgets to move the newly required DynamoDB runtime inputs out of the AWS Secrets-backed custom configuration scope.
- A supported setting exists simultaneously in checked-in defaults, AWS Secrets, and environment overrides with different values.
- An existing feature 014 deployment never used excluded keys; the refinement must not disrupt its supported secret-backed settings.
- A supported grouped setting is only partially present in AWS Secrets; the group must still satisfy the same completeness rules as it would from any other source.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST refine the AWS Secrets-backed custom configuration scope introduced by feature 014 so that only eligible repo-owned custom app settings remain supported through that path.
- **FR-002**: The eligible AWS Secrets-backed scope MUST continue to cover remaining repo-owned custom app settings that the application owns, including authentication cookie configuration and HostedPayments:PayFast settings.
- **FR-003**: DynamoDB runtime settings and AWS credential settings MUST be explicitly excluded from the AWS Secrets-backed custom configuration scope.
- **FR-004**: The system MUST NOT consume excluded DynamoDB runtime settings or AWS credential settings from the AWS Secrets-backed custom configuration payload.
- **FR-005**: If excluded DynamoDB runtime or AWS credential settings are present in the AWS Secrets-backed payload, the system MUST produce actionable operator-facing validation that identifies the unsupported setting group and required corrective action without exposing sensitive values.
- **FR-006**: Eligible AWS Secrets-backed settings MUST preserve the established configuration precedence in which environment overrides win over AWS Secrets-backed values and AWS Secrets-backed values win over checked-in defaults.
- **FR-007**: Deployments that rely only on eligible AWS Secrets-backed settings MUST continue to start and operate without requiring duplicate copies of those settings in other sources.
- **FR-008**: Deployments that use DynamoDB MUST be able to combine eligible AWS Secrets-backed app settings with separate non-secret-backed runtime and credential sources for the excluded DynamoDB inputs.
- **FR-009**: The feature MUST provide updated scope documentation that distinguishes supported AWS Secrets-backed settings from excluded DynamoDB runtime and AWS credential settings and explains how feature 014 deployments migrate to the refined scope.
- **FR-010**: Validation and completeness rules for eligible settings MUST remain the same regardless of whether those settings come from AWS Secrets, environment overrides, or checked-in defaults.
- **FR-011**: Operator diagnostics and user-facing failures related to this feature MUST never reveal secret values, AWS credential contents, or equivalent sensitive configuration data.
- **FR-012**: The feature MUST make the supported-setting catalog and the excluded-setting catalog separately identifiable so operators can verify configuration compliance before deployment or upgrade.

### Key Entities *(include if feature involves data)*

- **Eligible Secret-Backed Setting Group**: A repo-owned custom application setting or setting group that remains supported through the AWS Secrets-backed configuration path after this refinement.
- **Excluded DynamoDB Runtime Group**: The DynamoDB runtime and AWS credential settings that must remain outside the AWS Secrets-backed custom configuration payload.
- **Scope Validation Result**: The operator-visible outcome that determines whether a deployment's AWS Secrets-backed payload is compliant, non-compliant, or incomplete.
- **Migration Guidance**: The documented instructions that help operators move from the broader feature 014 scope to the refined compliant scope without losing supported secret-backed settings.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In scope-validation testing, 100% of eligible setting groups listed in the refined catalog can be supplied from AWS Secrets and resolved successfully without requiring duplicate values in another source.
- **SC-002**: In compliance testing, 100% of attempts to place excluded DynamoDB runtime or AWS credential settings in the AWS Secrets-backed payload are flagged before the deployment is treated as valid.
- **SC-003**: In mixed-source testing, 100% of eligible settings continue to follow the documented precedence order across checked-in defaults, AWS Secrets, and environment overrides.
- **SC-004**: In upgrade testing of feature 014-style payloads, operators receive a clear supported-versus-excluded scope result on the first validation attempt in 100% of tested cases.
- **SC-005**: In regression smoke testing of affected features that use eligible secret-backed settings, 100% of tested user flows behave the same as before the scope refinement.
- **SC-006**: In security validation, 0 tested diagnostics or user-facing failures expose secret values or AWS credential contents.

## Assumptions

- Feature 014 established the baseline AWS Secrets-backed configuration mechanism, and this feature only narrows and clarifies which settings are allowed to use it.
- DynamoDB runtime inputs and AWS credential inputs continue to be available through separate runtime-specific sources outside this feature's AWS Secrets-backed custom configuration payload.
- No new business capability is introduced; the value of this feature is compliance, scope clarity, and preservation of supported secret-backed app settings.
- Authentication cookie configuration and HostedPayments:PayFast are representative supported setting groups that must remain compatible with the refined scope.
