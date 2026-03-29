# Feature Specification: AWS DynamoDB Persistence Option

**Feature Branch**: `006-dynamodb-persistence`  
**Created**: 2026-03-28  
**Status**: Draft  
**Input**: User description: "I want to include another data persistence option for AWS DynamoDB in addition to sqlite and mysql. It's important to make it configurable by environment variables"

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

This feature lives entirely in the **Infrastructure** layer. It introduces a new set of repository implementations that satisfy the same `Application`-layer interfaces (`IUserRepository`, `ICompanyRepository`, `IEmployeeRepository`, `IPayslipRepository`, `ILoanRepository`) already defined by the existing EF Core implementations. The **Domain** and **Application** layers require zero changes.

**Constitution alignment notice (Principle V)**: The constitution now includes an approved DynamoDB provider exception for this feature. When `PERSISTENCE_PROVIDER=dynamodb`, the feature must keep the existing `Application`-layer repository interfaces unchanged, bypass the relational migration path entirely, create missing DynamoDB tables automatically at startup, and source all DynamoDB configuration from environment variables only.

**Layer assignment**:

| Concern | Layer |
|---------|-------|
| Repository interfaces (unchanged) | Application |
| DynamoDB repository implementations | Infrastructure |
| Provider selection via env var at startup | Infrastructure / Web (Program.cs) |
| Domain entities (unchanged) | Domain |
| UI (no change) | Web |

### User Story 1 - Configure DynamoDB Provider via Environment Variables (Priority: P1)

As a deployment operator, I can set an environment variable to select DynamoDB as the persistence backend so that I can deploy the application to AWS without modifying any code or config files checked into source control.

**Why this priority**: This is the foundational capability everything else depends on. Without provider selection via environment variables, there is no DynamoDB feature. It also directly addresses the core user requirement.

**Independent Test**: Can be fully tested by starting the application with `PERSISTENCE_PROVIDER=dynamodb` set and confirming the app starts successfully, connects to DynamoDB, and a basic read/write operation completes — without touching any other environment.

**Acceptance Scenarios**:

1. **Given** the environment variable `PERSISTENCE_PROVIDER` is not set, **When** the application starts, **Then** it uses SQLite (the existing default) and all existing behaviour is preserved.
2. **Given** `PERSISTENCE_PROVIDER=sqlite`, **When** the application starts, **Then** it uses SQLite as the persistence backend.
3. **Given** `PERSISTENCE_PROVIDER=mysql`, **When** the application starts, **Then** it uses MySQL as the persistence backend (existing behaviour unchanged).
4. **Given** `PERSISTENCE_PROVIDER=dynamodb` and the required DynamoDB environment variables are set, **When** the application starts, **Then** it registers and uses the DynamoDB repository implementations and bypasses the relational database migration path.
5. **Given** `PERSISTENCE_PROVIDER=dynamodb` but required DynamoDB environment variables are missing or invalid, **When** the application starts, **Then** it fails fast with a clear, descriptive error message indicating which variable is missing.
6. **Given** `PERSISTENCE_PROVIDER` is set to an unrecognised value, **When** the application starts, **Then** it fails fast with a clear error listing the valid options (`sqlite`, `mysql`, `dynamodb`).
7. **Given** `PERSISTENCE_PROVIDER=dynamodb` and one or more required DynamoDB tables do not yet exist, **When** the application starts, **Then** the missing tables are created automatically before user traffic is served and each created table is logged for operators.

---

### User Story 2 - Read and Write All Domain Data via DynamoDB (Priority: P2)

As a Company Owner using an AWS-hosted deployment, I can perform all standard application tasks (managing employees, generating payslips, recording loans) with data persisted in DynamoDB, and all data remains correctly scoped to my company.

**Why this priority**: Functional parity with the existing relational providers is essential for DynamoDB to be a viable production choice. Without it, the provider is unusable.

**Independent Test**: Can be fully tested by running the full application against a DynamoDB backend (local or AWS) and exercising employee creation, payslip generation, and loan recording end-to-end — each operation reads back correctly and is scoped to the authenticated company owner.

**Acceptance Scenarios**:

1. **Given** a Company Owner is authenticated with `PERSISTENCE_PROVIDER=dynamodb`, **When** they create an employee, **Then** the employee is persisted in DynamoDB and appears in subsequent list requests scoped to their company only.
2. **Given** a Company Owner is authenticated with `PERSISTENCE_PROVIDER=dynamodb`, **When** they generate a payslip for an employee, **Then** the payslip record is persisted in DynamoDB and is retrievable by that owner.
3. **Given** a Company Owner is authenticated with `PERSISTENCE_PROVIDER=dynamodb`, **When** they record a loan for an employee, **Then** the loan is persisted in DynamoDB and is visible in the employee's loan history.
4. **Given** two different Company Owners exist in DynamoDB, **When** Owner A queries their employees, **Then** only Owner A's employees are returned — Owner B's data is never exposed (ownership filtering is enforced).
5. **Given** a DynamoDB connection is temporarily unavailable, **When** a user attempts to read or write data, **Then** a user-friendly error message is displayed without leaking infrastructure details.

---

### User Story 3 - Local Development with DynamoDB Local (Priority: P3)

As a developer, I can run a local DynamoDB emulator and point the application at it using environment variables so that I can develop and test DynamoDB behaviour without incurring AWS costs or requiring internet access.

**Why this priority**: Developer experience is important but not blocking — production DynamoDB usage (P1/P2) is the primary goal. Local emulation is a quality-of-life improvement.

**Independent Test**: Can be fully tested by running the application with `PERSISTENCE_PROVIDER=dynamodb` and `DYNAMODB_ENDPOINT=http://localhost:8000` against a locally running DynamoDB emulator and exercising a create-read cycle.

**Acceptance Scenarios**:

1. **Given** `PERSISTENCE_PROVIDER=dynamodb` and `DYNAMODB_ENDPOINT` points to a local emulator, **When** the application starts, **Then** it connects to the local emulator rather than AWS.
2. **Given** `DYNAMODB_ENDPOINT` is not set and `PERSISTENCE_PROVIDER=dynamodb`, **When** the application starts, **Then** it connects to the AWS DynamoDB service using the configured region and environment-variable credentials.

---

### Edge Cases

- What happens when a DynamoDB table that the application depends on does not exist at startup? → The application must create the missing table automatically before serving requests and record which table was created in startup logs.
- What happens when DynamoDB throttles a request (provisioned capacity exceeded)? → The application must surface a generic transient error message to the user, avoid exposing provider-specific error codes, and preserve enough detail in operator logs for troubleshooting.
- What happens when data written by one provider (e.g., SQLite) is expected in another (e.g., DynamoDB) after a switch? → Data migration between providers is explicitly out of scope for this feature. Switching providers on a live dataset with existing data is unsupported and must be documented.
- What happens when environment variables contain whitespace or incorrect casing (e.g., `DynamoDB` instead of `dynamodb`)? → The provider value comparison must be case-insensitive and trimmed.
- What happens if the configured DynamoDB credentials lack the required permissions? → The application must fail fast at startup with a permission error, not at runtime during a user action.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST select the active persistence provider based on the value of the `PERSISTENCE_PROVIDER` environment variable at application startup.
- **FR-002**: The system MUST support `sqlite`, `mysql`, and `dynamodb` as valid values for `PERSISTENCE_PROVIDER` (case-insensitive). When not set, it MUST default to `sqlite`.
- **FR-003**: The system MUST fail fast at startup with a descriptive error message when `PERSISTENCE_PROVIDER=dynamodb` and any required DynamoDB environment variable is absent.
- **FR-004**: The system MUST fail fast at startup with a descriptive error message listing valid options when `PERSISTENCE_PROVIDER` is set to an unrecognised value.
- **FR-005**: The DynamoDB persistence implementation MUST satisfy all existing `Application`-layer repository interfaces without modification to those interfaces.
- **FR-006**: The DynamoDB persistence implementation MUST enforce company-ownership filtering on all multi-tenant queries, matching the behaviour of the existing SQLite and MySQL implementations.
- **FR-007**: The system MUST support the following DynamoDB configuration environment variables: `DYNAMODB_REGION` (AWS region), `DYNAMODB_ENDPOINT` (optional; overrides the AWS endpoint for local emulation), `DYNAMODB_TABLE_PREFIX` (optional; controls table naming), `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY`.
- **FR-008**: The system MUST support all existing domain entity operations (Users, Companies, Employees, Payslips, Loans) when running with the DynamoDB provider.
- **FR-009**: Switching the `PERSISTENCE_PROVIDER` value MUST NOT require any code changes — only environment variable updates.
- **FR-010**: The existing SQLite and MySQL persistence paths MUST remain fully functional and unchanged when their respective provider values are configured.
- **FR-011**: The application MUST automatically create any required DynamoDB tables at startup if they do not already exist. A clear log entry MUST be written for each table created. The `CreateTable` IAM permission is therefore a prerequisite for the configured AWS credentials.
- **FR-012**: When `PERSISTENCE_PROVIDER=dynamodb`, the system MUST bypass the relational persistence startup path entirely, including `PayslipDbContext` initialization and relational migration execution.
- **FR-013**: When DynamoDB requests fail because of throttling, temporary unavailability, or permission issues, the system MUST show a sanitized error message to the affected user and MUST NOT expose raw infrastructure details in the user-facing response.

### Assumptions

- DynamoDB access credentials are supplied through environment variables so deployment behavior remains explicit and consistent with the constitution-approved provider rules.
- DynamoDB table names will follow a predictable naming convention using the configured `DYNAMODB_TABLE_PREFIX`, defaulting to `payslip4all` when the variable is unset.
- Data migration between providers (e.g., from SQLite to DynamoDB) is out of scope for this feature.
- The DynamoDB data model will be designed to satisfy the existing query patterns (e.g., list employees by company, list payslips by employee) without requiring changes to the Application layer.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Switching between SQLite, MySQL, and DynamoDB requires only environment variable changes — zero code changes and zero redeployment of binaries.
- **SC-002**: All existing acceptance scenarios for employee management, payslip generation, and loan recording pass without modification when run against the DynamoDB provider.
- **SC-003**: Application startup with an invalid or misconfigured `PERSISTENCE_PROVIDER` produces a clear, actionable error message within 5 seconds — no silent failures or cryptic stack traces exposed to the operator.
- **SC-004**: Ownership isolation is maintained across providers: a Company Owner's queries against DynamoDB never return another owner's data, verifiable by integration tests covering multi-tenant scenarios.
- **SC-005**: The existing SQLite and MySQL provider test suites continue to pass at 100% after this feature is merged — no regression introduced.
- **SC-006**: A developer can run the full application against a local DynamoDB emulator with no AWS account required, following documented setup steps.
- **SC-007**: When DynamoDB is selected and required tables are missing, startup prepares all required tables before the first user action can fail because of missing table infrastructure.
- **SC-008**: In all tested DynamoDB outage, throttling, and permission-error scenarios, end users see a generic recoverable error message with no provider-specific exception text.
