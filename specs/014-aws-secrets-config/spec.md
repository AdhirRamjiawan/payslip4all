# Feature Specification: AWS Secrets-Sourced Custom Configuration

**Feature Branch**: `014-aws-secrets-config`  
**Created**: 2026-04-23  
**Status**: Ready for Planning  
**Input**: User description: "Ensure all custom appsettings can also be sourced from AWS Secrets."

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

- **Domain**: No payroll, wallet, or ownership business rules change; the feature only changes how existing custom configuration values are supplied.
- **Application**: Application services and contracts must continue consuming resolved configuration values without knowing whether they came from files, environment variables, or AWS Secrets Manager.
- **Infrastructure**: Configuration sourcing, secret retrieval, source precedence, validation, and operator diagnostics belong to Infrastructure concerns.
- **Web**: Startup must compose configuration sources, apply deterministic precedence, and fail safely when required custom settings cannot be resolved.
- **TDD Alignment**: Each functional requirement below is expressed as observable startup or runtime behaviour so implementation can be driven by failing acceptance tests for source resolution, fallback behaviour, and failure handling before code changes are made.

### User Story 1 - Run deployments from AWS Secrets (Priority: P1)

As a deployment operator, I want repo-owned custom settings to be loadable from AWS Secrets Manager so that production environments can run without depending on checked-in appsettings values or plain-text deployment variables for those settings.

**Why this priority**: This is the core value of the feature. If deployments cannot resolve custom settings from AWS Secrets Manager, the feature does not achieve its configuration-management goal.

**Independent Test**: Configure a deployment environment so all in-scope custom settings are supplied through AWS Secrets Manager, start the application, and verify startup succeeds and the affected features behave correctly without duplicating those settings in checked-in appsettings files.

**Acceptance Scenarios**:

1. **Given** an operator stores all required repo-owned custom settings in AWS Secrets Manager for a deployment environment, **When** the application starts, **Then** it resolves those settings successfully and reaches a usable state without requiring duplicate checked-in appsettings values for those same settings.
2. **Given** a custom setting is available from both checked-in appsettings and AWS Secrets Manager, **When** the deployment is configured to use AWS Secrets Manager, **Then** the secret-backed value is the one applied at runtime.

---

### User Story 2 - Preserve flexible source selection (Priority: P2)

As a deployment operator, I want each repo-owned custom setting to continue working from existing sources while also supporting AWS Secrets Manager so that I can migrate settings gradually and keep emergency override options.

**Why this priority**: Teams need a safe migration path. Supporting AWS Secrets Manager must not break existing environments or force every setting to move at once.

**Independent Test**: Run one environment using only existing configuration sources and another with a mixed configuration where some covered settings come from AWS Secrets Manager and others remain in existing sources, then verify both environments behave correctly.

**Acceptance Scenarios**:

1. **Given** an existing deployment that does not use AWS Secrets Manager, **When** the application starts after this feature is released, **Then** all current custom settings still resolve as they do today and no new mandatory deployment input is introduced.
2. **Given** a deployment supplies one covered custom setting through an environment variable and the same setting through AWS Secrets Manager, **When** the application starts, **Then** the documented precedence order is applied consistently so operators can predict which value wins.
3. **Given** only some covered custom settings have been migrated to AWS Secrets Manager, **When** the application starts, **Then** secret-backed and non-secret-backed settings are combined into one complete runtime configuration without changing user-visible feature behaviour.

---

### User Story 3 - Fail safely on secret problems (Priority: P3)

As a deployment operator, I want configuration failures related to AWS Secrets Manager to stop unsafe startup and provide actionable diagnostics so that I can fix configuration issues without exposing secret contents to end users or support staff.

**Why this priority**: Safe failure handling protects production reliability and security, but it depends on the higher-priority source-resolution rules already being in place.

**Independent Test**: Start the application with missing, unreadable, or malformed secret-backed custom settings and verify the application either refuses to start or blocks the affected flow with clear operator diagnostics while exposing no secret values.

**Acceptance Scenarios**:

1. **Given** a required custom setting is expected from AWS Secrets Manager but the secret cannot be read, **When** the application starts, **Then** startup fails before the affected feature can run and operators receive a clear diagnostic message that identifies the missing or inaccessible configuration without revealing the secret value.
2. **Given** a secret-backed custom setting contains an invalid value format, **When** the application validates configuration, **Then** the invalid setting is rejected with an actionable operator-facing error and no secret content is shown in user-facing responses or logs.

---

### Edge Cases

- What happens when a secret provides only part of a grouped custom configuration section? The system must reject the incomplete configuration using the same validation rules applied to non-secret sources.
- What happens when a covered setting exists in environment variables, AWS Secrets Manager, and checked-in appsettings with different values? The system must apply one documented precedence order consistently.
- What happens when AWS Secrets Manager access is unavailable during startup? The system must not continue into an unsafe partially configured state for features that require the missing values.
- What happens when a deployment uses no AWS Secrets Manager values at all? Existing custom configuration behaviour must remain unchanged.
- What happens when secret values are rotated after deployment? The next normal configuration reload or application restart is assumed to pick up the updated values; immediate live refresh is out of scope for this feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow every repo-owned custom configuration value that is currently sourced from appsettings files or environment variables to also be sourced from AWS Secrets Manager in deployment environments.
- **FR-002**: The in-scope configuration set MUST include only settings explicitly consumed by Payslip4All code or startup decisions and MUST exclude untouched framework-default configuration sections unless the application has an explicit dependency on them.
- **FR-003**: The system MUST allow deployments to use a mix of configuration sources, with some covered settings coming from AWS Secrets Manager and others continuing to come from existing sources.
- **FR-004**: The system MUST preserve existing behaviour for deployments that do not use AWS Secrets Manager.
- **FR-005**: The system MUST apply a deterministic precedence order when the same covered setting is supplied from multiple sources, with environment variables overriding AWS Secrets Manager values and AWS Secrets Manager values overriding checked-in appsettings values.
- **FR-006**: Required custom settings MUST keep the same validation and completeness rules regardless of whether their values come from files, environment variables, or AWS Secrets Manager.
- **FR-007**: The system MUST support secret-backed resolution for structured custom configuration sections as well as individual custom settings when the application depends on grouped values.
- **FR-008**: The system MUST allow the application to start and operate normally when all required covered settings are supplied through AWS Secrets Manager without duplicating those settings in another deployment source.
- **FR-009**: If a required covered setting cannot be retrieved from AWS Secrets Manager, the system MUST fail safely before the affected feature becomes available.
- **FR-010**: If a secret-backed covered setting is malformed or incomplete, the system MUST reject it with an actionable operator-facing error that identifies the configuration problem without exposing secret contents.
- **FR-011**: User-facing error handling for configuration failures MUST avoid exposing secret values, AWS credential details, or other sensitive secret metadata.
- **FR-012**: Operator diagnostics MUST identify which covered setting or configuration group failed to resolve, which source was being used, and whether the failure was caused by missing data, unreadable data, or invalid data.
- **FR-013**: The feature MUST define and document the full list of repo-owned custom settings covered by AWS Secrets Manager support so operators can verify deployment completeness.
- **FR-014**: The feature MUST define and document how operators associate AWS Secrets Manager entries to the covered settings in a deployment environment.
- **FR-015**: The feature MUST ensure that adding another repo-owned custom setting in the future follows the same source-selection rules established by this feature instead of requiring a separate configuration pattern.

### Key Entities *(include if feature involves data)*

- **Covered Custom Setting**: Any repo-owned configuration value or grouped configuration section that Payslip4All explicitly consumes for startup or feature behaviour and is therefore in scope for AWS Secrets Manager sourcing.
- **Configuration Source Set**: The ordered set of allowed inputs for a covered custom setting, consisting of checked-in appsettings, deployment environment variables, and AWS Secrets Manager.
- **Secret Mapping Definition**: The documented rule that tells operators how a covered custom setting is associated with an AWS Secrets Manager entry for a given deployment environment.
- **Configuration Validation Failure**: An operator-visible failure state triggered when a covered custom setting is missing, inaccessible, incomplete, or invalid after source resolution.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In deployment validation, 100% of covered custom settings listed for this feature can be supplied from AWS Secrets Manager and resolved successfully without requiring duplicate checked-in appsettings values for those settings.
- **SC-002**: An existing environment that does not use AWS Secrets Manager starts successfully after the feature is introduced with no additional mandatory configuration steps.
- **SC-003**: In mixed-source validation, 100% of tested deployments resolve the documented precedence order correctly when the same covered setting is present in more than one source.
- **SC-004**: When a required secret-backed setting is missing, unreadable, or malformed, the application produces an actionable operator-facing failure within 30 seconds of startup validation in 100% of tested cases.
- **SC-005**: In smoke testing of affected application features, 100% of scenarios using secret-backed custom settings behave the same for end users as equivalent scenarios using existing configuration sources.
- **SC-006**: In security validation, 0 tested user-facing errors or operator diagnostics expose secret values or AWS credential contents.

## Assumptions

- AWS deployment environments can grant the application permission to read the required AWS Secrets Manager entries before startup begins.
- Generic framework-owned sections such as baseline logging and hosting defaults are out of scope unless Payslip4All code explicitly depends on a value from those sections.
- Secret-backed values may be stored as individual settings or grouped settings, as long as operators can provide the complete covered configuration required by the application.
- Secret rotation is expected to take effect on the next normal application restart or configuration reload rather than through immediate hot reloading.
