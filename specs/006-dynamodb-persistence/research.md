# Research: AWS DynamoDB Persistence Option

**Feature**: 006-dynamodb-persistence  
**Phase**: 0 — Research & Decision Log  
**Date**: 2026-03-29

---

## Decision 1: Persistence Provider Selection Contract

**Decision**: Standardize on `PERSISTENCE_PROVIDER` as the single runtime selector for
`sqlite`, `mysql`, and `dynamodb`, with case-insensitive, trimmed matching and a default
of `sqlite` when unset.

**Rationale**: The feature spec requires environment-variable-driven provider selection
with zero code changes across environments. Keeping one explicit selector in `Program.cs`
preserves existing SQLite/MySQL behavior while making the DynamoDB path operationally
obvious to deployers and test fixtures.

**Alternatives considered**:
- Keep a legacy `DatabaseProvider` setting in parallel — rejected because it creates
  conflicting startup precedence and operator confusion.
- Use provider-specific boolean flags — rejected because it does not scale and makes
  invalid mixed states likely.

---

## Decision 2: AWS Authentication Precedence

**Decision**: Use the following credential resolution behavior for DynamoDB:

1. if `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` are both supplied, construct the
   DynamoDB client with those explicit credentials,
2. else, if `DYNAMODB_ENDPOINT` is set, assume a local emulator and supply syntactically
   valid dummy credentials,
3. else, for real AWS, let `AmazonDynamoDBClient(config)` use the AWS SDK for .NET
   default credential chain / hosted identity.

**Rationale**: This matches the feature update and aligns with standard AWS SDK behavior:
explicit credentials must win when intentionally supplied, local emulators usually require
non-empty placeholder credentials even though they do not authenticate them, and hosted
AWS deployments should rely on the SDK's normal provider chain (for example IAM roles,
container credentials, instance metadata, and configured profiles) instead of hardcoded
production secrets.

**Alternatives considered**:
- Require explicit access keys in every environment — rejected because it breaks hosted
  AWS best practice and conflicts with the constitution's prohibition on hardcoded
  production credentials.
- Always inject dummy credentials when explicit credentials are absent — rejected because
  that would disable standard hosted-AWS identity resolution for real DynamoDB.
- Require `AWS_SESSION_TOKEN` as part of the base contract — rejected because the feature
  only mandates access-key pairs when explicitly provided; the SDK chain can already
  resolve temporary credentials where needed.

---

## Decision 3: DynamoDB Table Topology

**Decision**: Use a multi-table DynamoDB design with one table per persisted aggregate or
line-item collection:

| Table (with prefix) | PK | GSI(s) |
|---|---|---|
| `{prefix}_users` | `id` | `email-index` |
| `{prefix}_companies` | `id` | `userId-index` |
| `{prefix}_employees` | `id` | `companyId-index` |
| `{prefix}_employee_loans` | `id` | `employeeId-index` |
| `{prefix}_payslips` | `id` | `employeeId-index` |
| `{prefix}_payslip_loan_deductions` | `id` | `payslipId-index` |

**Rationale**: The existing repository contracts and domain model are already organized by
entity. A multi-table design maps more directly to those interfaces, keeps repository code
readable for a team accustomed to EF Core, and still supports the needed query patterns
through targeted GSIs.

**Alternatives considered**:
- Single-table DynamoDB design — rejected for this feature because it would require a much
  broader access-pattern redesign and substantially higher implementation complexity.

---

## Decision 4: Ownership Filtering Strategy

**Decision**: Denormalize `userId` onto DynamoDB child items (`Company`, `Employee`,
`EmployeeLoan`, `Payslip`, and stored payslip loan deductions when needed for traversal)
and enforce ownership in every repository query or post-fetch validation.

**Rationale**: DynamoDB does not support relational joins, but the constitution requires
the same company-owner isolation guarantees as the relational providers. Storing `userId`
with the item avoids repeated parent lookups and keeps repository implementations aligned
with the existing `Application` contracts.

**Alternatives considered**:
- Resolve ownership by loading parent records on every read — rejected due to extra
  latency, read amplification, and unnecessary complexity on high-frequency paths.
- Move ownership checks into the Application layer only — rejected because the
  constitution requires data-access filtering, not just service-layer filtering.

---

## Decision 5: Startup Provisioning Strategy

**Decision**: Provision missing DynamoDB tables at application startup through a hosted
service, log each created table, and bypass EF Core migration execution entirely when
`PERSISTENCE_PROVIDER=dynamodb`.

**Rationale**: The spec requires first-run operability without manual relational-style
migration steps and explicitly states that DynamoDB tables must be created before user
traffic is served. A startup provisioner gives local development, CI, and new AWS
deployments the same deterministic boot path.

**Alternatives considered**:
- Require operators to pre-create all tables manually — rejected because it violates the
  spec and slows down developer onboarding.
- Reuse `PayslipDbContext` migration startup for DynamoDB — rejected because the
  constitution's DynamoDB exception explicitly bypasses EF Core in this provider path.

---

## Decision 6: Unit of Work Semantics

**Decision**: Keep `IUnitOfWork` unchanged and implement a DynamoDB no-op
`DynamoDbUnitOfWork`.

**Rationale**: The feature must preserve existing `Application` contracts. Existing
repository methods already save immediately in the relational implementation, so a no-op
unit of work maintains compatibility without pushing DynamoDB transaction concerns into
the Application layer.

**Alternatives considered**:
- Introduce DynamoDB-specific transactional repository contracts — rejected because it
  would violate the requirement to keep existing Application interfaces unchanged.
- Force every service to branch on provider type — rejected because it breaks Clean
  Architecture and scatters infrastructure concerns into application services.

---

## Decision 7: Test Strategy and Failure Surface

**Decision**: Cover the DynamoDB provider with:

- startup/provider-switching tests in `Payslip4All.Web.Tests`,
- client-factory, provisioner, and repository tests in `Payslip4All.Infrastructure.Tests`,
- DynamoDB Local for integration coverage,
- explicit scenarios for invalid provider values, missing region, credential-pair
  validation, ownership isolation, and startup table creation.

User-facing failures from throttling, temporary unavailability, or permission issues
should be surfaced as sanitized messages while detailed exception information is retained
in logs.

**Rationale**: The constitution requires TDD and DynamoDB-local CI coverage, while the spec
requires fail-fast startup validation and sanitized user messaging. Separating startup,
repository, and operator-facing behavior into dedicated test layers keeps the provider
exception auditable and maintainable.

**Alternatives considered**:
- Live AWS integration tests in CI — rejected by the constitution.
- Repository-only tests without startup wiring coverage — rejected because provider
  registration and EF Core bypass are core feature requirements.

---

## Resolved Clarifications

| Clarification | Resolution |
|---|---|
| Which runtime key selects the provider? | `PERSISTENCE_PROVIDER`, trimmed and case-insensitive, default `sqlite` |
| How should AWS auth behave? | Explicit keys first; dummy creds for local endpoint; SDK default chain for real AWS |
| How are tables created? | Startup hosted service provisions missing tables and logs each creation |
| How is tenant isolation preserved without joins? | `userId` is denormalized and checked on every repository path |
| Must Application interfaces change? | No — all DynamoDB work remains an Infrastructure implementation of existing contracts |
| How is CI/local testing handled? | DynamoDB Local plus xUnit/WebApplicationFactory/repository fixture coverage |
