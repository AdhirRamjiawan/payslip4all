# Feature Specification: AWS DynamoDB Persistence Option

**Feature Branch**: `006-dynamodb-persistence`  
**Created**: 2026-03-28  
**Status**: Refined  
**Input**: User description: "I want to include another data persistence option for AWS DynamoDB in addition to sqlite and mysql. It's important to make it configurable by environment variables"

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

This feature adds DynamoDB as an alternative persistence provider selected entirely through runtime environment variables. Production changes are limited to the **Infrastructure** layer plus **Web** startup and middleware behavior needed to select the provider, bypass the relational startup path, provision required tables, and surface sanitized runtime failures. The **Application** layer contracts and **Domain** entities remain unchanged.

**Constitution alignment notice (Principles I, II, IV, and V)**: The approved DynamoDB provider exception allows a parallel Infrastructure implementation when `PERSISTENCE_PROVIDER=dynamodb`. The feature must keep the existing repository contracts unchanged, preserve ownership filtering, bypass relational migrations only for the DynamoDB path, create required tables before serving traffic, and obtain runtime configuration from environment variables.

**Layer assignment**:

| Concern | Layer |
|---------|-------|
| Repository contracts and service contracts (unchanged) | Application |
| DynamoDB persistence behavior, repository parity, and table provisioning | Infrastructure |
| Provider selection, relational-startup bypass, and sanitized request error handling | Web |
| Business entities and payroll rules (unchanged) | Domain |

### User Story 1 - Select DynamoDB at Startup (Priority: P1)

As a deployment operator, I can select DynamoDB entirely through environment variables so that I can start the application in AWS or against a local emulator without changing code or checked-in configuration files.

**Why this priority**: This is the minimum viable capability. Without correct provider selection, startup validation, and relational-path bypass behavior, DynamoDB cannot be used safely in any environment.

**Independent Test**: Start the application with `PERSISTENCE_PROVIDER=dynamodb` under each supported runtime mode and verify startup selects the DynamoDB path, validates the environment contract, provisions missing tables, bypasses relational startup, and either starts successfully or fails fast with a descriptive operator-facing error. This story does not require CRUD parity testing.

**Acceptance Scenarios**:

1. **Given** `PERSISTENCE_PROVIDER` is not set, **When** the application starts, **Then** it uses the existing SQLite startup path and preserves current behavior.
2. **Given** `PERSISTENCE_PROVIDER=mysql`, **When** the application starts, **Then** it uses the existing MySQL startup path and preserves current behavior.
3. **Given** `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION` is set, no local endpoint is configured, and no explicit AWS credential pair is provided, **When** the application starts in a hosted AWS environment, **Then** it uses DynamoDB, relies on the hosting environment's standard AWS identity resolution, bypasses relational startup, and starts successfully.
4. **Given** `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION` is set, and both `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` are provided, **When** the application starts, **Then** it uses that explicit credential pair for DynamoDB access and bypasses relational startup.
5. **Given** `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION` is set, `DYNAMODB_ENDPOINT` points to a local emulator, and no explicit AWS credential pair is provided, **When** the application starts, **Then** it connects to the local emulator using automatically supplied placeholder credentials, bypasses relational startup, and starts successfully.
6. **Given** `PERSISTENCE_PROVIDER=dynamodb` and only one of `AWS_ACCESS_KEY_ID` or `AWS_SECRET_ACCESS_KEY` is provided, **When** the application starts, **Then** startup fails fast with a clear message that both values must be supplied together.
7. **Given** `PERSISTENCE_PROVIDER=dynamodb` and `DYNAMODB_REGION` is missing, **When** the application starts, **Then** startup fails fast with a clear message identifying the missing required variable.
8. **Given** `PERSISTENCE_PROVIDER` is set to an unrecognized value, **When** the application starts, **Then** startup fails fast with a clear message listing the supported provider values.
9. **Given** `PERSISTENCE_PROVIDER=dynamodb` and one or more required tables do not exist, **When** the application starts, **Then** the missing tables are created before requests are served and each created table is recorded in operator logs.

---

### User Story 2 - Use DynamoDB for Business Data (Priority: P2)

As a Company Owner, I can perform the existing employee, payslip, and loan workflows while DynamoDB is the active persistence provider so that DynamoDB is a viable production option without changing how I use the application.

**Why this priority**: Provider selection alone is not enough. DynamoDB must support the application's existing business workflows and ownership isolation before it can be considered production-ready.

**Independent Test**: Run the application with DynamoDB enabled, perform the existing employee, payslip, and loan workflows for one Company Owner, verify the resulting data can be read back correctly, and confirm a second owner cannot access that data. Trigger transient and permission-related persistence failures and verify users receive sanitized messages while operators receive diagnostic logs.

**Acceptance Scenarios**:

1. **Given** a Company Owner is authenticated while DynamoDB is active, **When** they create and later retrieve an employee, **Then** the employee data is persisted and returned correctly for that owner.
2. **Given** a Company Owner is authenticated while DynamoDB is active, **When** they generate a payslip, **Then** the payslip is persisted and can be retrieved in the same workflow context.
3. **Given** a Company Owner is authenticated while DynamoDB is active, **When** they record a loan for an employee, **Then** the loan is persisted and appears in that employee's loan history.
4. **Given** two Company Owners have data stored while DynamoDB is active, **When** either owner retrieves companies, employees, payslips, or loans, **Then** only records belonging to that owner are returned.
5. **Given** DynamoDB throttles a request, becomes temporarily unavailable, or denies access, **When** a user performs a data operation, **Then** the user receives a sanitized error response and operator logs retain enough detail to diagnose the failure.

---

### User Story 3 - Develop Locally with a DynamoDB Emulator (Priority: P3)

As a developer, I can run the application against a local DynamoDB emulator using environment variables only so that I can validate DynamoDB behavior without requiring a live AWS account.

**Why this priority**: Local emulator support improves development and testability, but it depends on the higher-priority startup contract and repository parity already being in place.

**Independent Test**: Start a local DynamoDB emulator, set `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, and `DYNAMODB_ENDPOINT`, leave explicit AWS credentials unset, and verify startup succeeds, required tables are created with the configured prefix behavior, and a create-read cycle completes successfully.

**Acceptance Scenarios**:

1. **Given** `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION` is set, and `DYNAMODB_ENDPOINT` points to a local emulator, **When** explicit AWS credentials are absent, **Then** the application uses placeholder credentials acceptable to the emulator and starts successfully.
2. **Given** a local emulator is configured and required tables do not yet exist, **When** the application starts, **Then** it creates the missing prefixed tables before serving requests.
3. **Given** a developer performs a basic create-read cycle against the local emulator, **When** the operation completes, **Then** the data is persisted and retrieved without requiring a live AWS account.

---

### Edge Cases

- What happens when only one credential from the explicit AWS key pair is supplied? → Startup must fail fast and explain that both credential variables must be supplied together.
- What happens when `DYNAMODB_ENDPOINT` targets a local emulator that is unavailable? → Startup must fail clearly before serving requests and operator logs must identify the connection failure context.
- What happens when the configured credentials can read data but cannot create missing tables? → Startup must fail before the application serves traffic, and operator logs must make the permission problem diagnosable.
- What happens when providers are switched after data already exists in a different backend? → Cross-provider data migration is out of scope and must be documented as unsupported by this feature.
- What happens when provider values contain surrounding whitespace or different casing? → Provider selection must trim and compare values case-insensitively.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST select the active persistence provider from the `PERSISTENCE_PROVIDER` runtime environment variable during application startup.
- **FR-002**: The system MUST support `sqlite`, `mysql`, and `dynamodb` as valid provider values, treat those values case-insensitively, trim surrounding whitespace, and default to `sqlite` when the variable is not supplied.
- **FR-003**: The DynamoDB provider path MUST leave existing Application-layer contracts and Domain entities unchanged while activating only Infrastructure behavior plus Web startup and middleware behavior needed for the DynamoDB runtime path.
- **FR-004**: The system MUST fail fast at startup with a descriptive message listing valid options when `PERSISTENCE_PROVIDER` is set to an unrecognized value.
- **FR-005**: When `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION` MUST be treated as required and startup MUST fail fast when it is missing or blank.
- **FR-006**: When `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_ENDPOINT` MUST be optional and, when supplied, MUST direct the application to the specified DynamoDB-compatible endpoint instead of the hosted AWS service endpoint.
- **FR-007**: When `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_TABLE_PREFIX` MUST be optional and the system MUST apply a documented default prefix when the variable is absent.
- **FR-008**: When both `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` are supplied, the system MUST use them together as the explicit AWS credential pair for DynamoDB access.
- **FR-009**: When only one of `AWS_ACCESS_KEY_ID` or `AWS_SECRET_ACCESS_KEY` is supplied, startup MUST fail fast with a descriptive message explaining that the credential variables are valid only as a complete pair.
- **FR-010**: When `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_ENDPOINT` targets a local emulator, and no explicit AWS credential pair is supplied, the system MUST automatically use non-empty placeholder credentials suitable for local emulators.
- **FR-011**: When `PERSISTENCE_PROVIDER=dynamodb`, no local emulator endpoint is configured, and no explicit AWS credential pair is supplied, the system MUST rely on the hosting environment's standard AWS credential resolution path.
- **FR-012**: When `PERSISTENCE_PROVIDER=dynamodb`, the system MUST automatically create any required DynamoDB tables that do not already exist before serving requests.
- **FR-013**: When table creation occurs at startup, the system MUST record an operator log entry for each created table, and when table creation cannot complete because of permissions or connectivity, startup MUST fail before serving traffic.
- **FR-014**: When `PERSISTENCE_PROVIDER=dynamodb`, the system MUST bypass relational persistence startup behavior, including relational database initialization and relational migration execution.
- **FR-015**: The DynamoDB persistence path MUST support the application's existing business operations for Users, Companies, Employees, Payslips, and Loans without changing the visible user workflow.
- **FR-016**: The DynamoDB persistence path MUST enforce Company Owner ownership filtering on all business-data reads so that one owner cannot retrieve another owner's records.
- **FR-017**: Switching between supported persistence providers MUST require only runtime configuration changes and MUST NOT require source-code changes.
- **FR-018**: The existing SQLite and MySQL persistence paths MUST remain available and behaviorally unchanged when their respective providers are selected.
- **FR-019**: When DynamoDB operations fail because of throttling, temporary unavailability, startup misconfiguration, or permission problems, the system MUST return sanitized user-facing error messages that do not expose raw infrastructure details.
- **FR-020**: The system MUST capture operator-facing diagnostic logs for DynamoDB startup validation failures, credential-contract violations, table-provisioning outcomes, and runtime persistence failures so operators can identify the failing mode, request context, or missing permission without relying on user reports alone.

### Runtime Configuration Contract

| Runtime mode | DYNAMODB_REGION | DYNAMODB_ENDPOINT | DYNAMODB_TABLE_PREFIX | AWS_ACCESS_KEY_ID | AWS_SECRET_ACCESS_KEY | Required behavior |
|--------------|-----------------|-------------------|-----------------------|-------------------|-----------------------|-------------------|
| Hosted AWS with explicit credentials | Required | Optional | Optional | Required | Required | Use the supplied credential pair for DynamoDB access. |
| Hosted AWS with default credential fallback | Required | Optional | Optional | Optional (leave unset unless both credential variables are supplied) | Optional (leave unset unless both credential variables are supplied) | If both credential variables are absent, rely on the hosting environment's standard AWS credential resolution path. |
| Local emulator with explicit credentials | Required | Required | Optional | Required | Required | Connect to the local endpoint and use the supplied credential pair. |
| Local emulator without explicit credentials | Required | Required | Optional | Optional (leave unset unless both credential variables are supplied) | Optional (leave unset unless both credential variables are supplied) | Connect to the local endpoint and auto-supply placeholder credentials acceptable to the emulator. |
| Invalid partial credential pair | Required | Optional | Optional | Invalid if supplied without `AWS_SECRET_ACCESS_KEY` | Invalid if supplied without `AWS_ACCESS_KEY_ID` | Fail fast at startup and explain that explicit credentials must be supplied as a complete pair. |

### Assumptions

- Local DynamoDB emulators accept syntactically valid placeholder credentials even when they do not verify those values against AWS.
- Hosted AWS deployments can supply credentials either through an explicit environment-variable pair or through the environment's standard AWS identity resolution path.
- The default table prefix is a stable documented value used consistently when `DYNAMODB_TABLE_PREFIX` is absent.
- Cross-provider data migration is out of scope for this feature; switching providers changes where future data is stored, not how existing data is migrated.
- Existing user workflows, ownership rules, and business calculations remain unchanged by the introduction of DynamoDB as an additional persistence option.

### Key Entities *(include if feature involves data)*

- **Persistence Provider Configuration**: The runtime settings that determine which persistence backend is active and, for DynamoDB, which region, endpoint, table prefix, and credential mode are used.
- **Company-Owned Business Records**: The set of Users, Companies, Employees, Payslips, and Loans that must remain functionally available and ownership-scoped regardless of which persistence provider is active.
- **Operator Diagnostics**: Startup and runtime records that capture configuration errors, provisioning results, and persistence failure details needed for support and incident response.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Operators can switch between SQLite, MySQL, and DynamoDB using runtime configuration changes only, with zero source-code changes required.
- **SC-002**: In validation testing, each supported DynamoDB startup mode succeeds or fails exactly as specified: hosted AWS with explicit credentials, hosted AWS with default credential fallback, local emulator with explicit credentials, local emulator without explicit credentials, and invalid partial credential pair.
- **SC-003**: Misconfigured provider or DynamoDB startup settings produce a clear, actionable startup failure within 5 seconds of application launch.
- **SC-004**: All existing employee, payslip, and loan workflows complete successfully against DynamoDB in end-to-end validation without changing the user-facing workflow.
- **SC-005**: Ownership-isolation validation demonstrates that 100% of tested cross-owner queries return only the requesting owner's data while DynamoDB is active.
- **SC-006**: When required DynamoDB tables are missing, startup creates all missing tables before the first request is served and records each creation in operator logs.
- **SC-007**: In tested throttling, temporary unavailability, and permission-failure scenarios, 100% of user-facing error responses remain sanitized and 100% of corresponding failures produce operator log entries with diagnostic context.
- **SC-008**: A developer can complete the documented local-emulator startup and a basic create-read validation flow without a live AWS account.
