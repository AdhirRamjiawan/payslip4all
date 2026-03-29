# Research: AWS DynamoDB Persistence Option

**Feature**: 006-dynamodb-persistence
**Phase**: 0 — Research & Decision Log
**Date**: 2026-03-28

---

## Decision 1: Configuration Key Naming

**Decision**: Standardise on `PERSISTENCE_PROVIDER` as the single environment variable
for selecting the persistence backend, replacing the existing `DatabaseProvider`
configuration key.

**Rationale**: The feature spec explicitly mandates `PERSISTENCE_PROVIDER`. The
existing `DatabaseProvider` key lives in `appsettings.json` and is read via
`builder.Configuration["DatabaseProvider"]`; it is not yet a formal standard.
Unifying under one key reduces operator confusion.

**Migration impact**: `Program.cs` must be updated to read `PERSISTENCE_PROVIDER`
instead of `DatabaseProvider`. The `appsettings.json` key `DatabaseProvider` must be
removed; its value was `sqlite` (the default). In .NET, environment variables map
directly to configuration keys, so `PERSISTENCE_PROVIDER=dynamodb` will be read by
`builder.Configuration["PERSISTENCE_PROVIDER"]`.

**Alternatives considered**:
- Keep `DatabaseProvider` and add `dynamodb` as a third value — rejected because it
  conflicts with the approved spec and leaves a poorly named key in the codebase.
- Support both keys with a fallback — rejected as it permanently complicates startup
  logic for no lasting benefit.

---

## Decision 2: DynamoDB Table Design Strategy

**Decision**: Multi-table design — one DynamoDB table per entity type (6 tables).

**Rationale**: The existing access patterns are inherently relational (filter by
`userId`, `companyId`, `employeeId`). Multi-table design maps 1:1 to the existing
entity model, making the implementation readable for a team experienced with EF Core.
DynamoDB Global Secondary Indexes (GSIs) handle the secondary access patterns.

**Alternatives considered**:
- Single-table design (DynamoDB best practice for high-scale SaaS) — rejected because
  it would require a complete redesign of the access patterns and the skill investment
  is not justified at the current scale. Single-table can be migrated to later.

**Table list**:

| Table (with prefix) | PK | SK | GSIs |
|---|---|---|---|
| `{prefix}_users` | `id` (S) | — | `email-index` (PK: email) |
| `{prefix}_companies` | `id` (S) | — | `userId-index` (PK: userId) |
| `{prefix}_employees` | `id` (S) | — | `companyId-index` (PK: companyId) |
| `{prefix}_employee_loans` | `id` (S) | — | `employeeId-index` (PK: employeeId) |
| `{prefix}_payslips` | `id` (S) | — | `employeeId-index` (PK: employeeId) |
| `{prefix}_payslip_loan_deductions` | `id` (S) | — | `payslipId-index` (PK: payslipId) |

Default prefix: `payslip4all` (overridable via `DYNAMODB_TABLE_PREFIX` env var).

---

## Decision 3: Ownership Filtering Strategy

**Decision**: Denormalise `userId` onto all child entities (Employee, EmployeeLoan,
Payslip, PayslipLoanDeduction) as a stored attribute. Ownership filtering in DynamoDB
repositories uses this denormalised field rather than cross-table lookups.

**Rationale**: DynamoDB has no JOIN operation. The EF Core repositories enforce
ownership via navigation properties (e.g., `e.Company.UserId == userId`). In DynamoDB,
the equivalent requires either a cross-table read (fetch parent company to get userId)
or storing `userId` directly on the child. Denormalisation is the DynamoDB standard
pattern and avoids the read amplification of cross-table ownership lookups.

**Impact on data model**: Each DynamoDB item for Employee, EmployeeLoan, Payslip, and
PayslipLoanDeduction will include a `userId` attribute. This attribute is written at
insert time and never updated (companies cannot change ownership). The Application-layer
interfaces and Domain entities are unchanged — `userId` is only a DynamoDB storage
concern.

**Alternatives considered**:
- Cross-table ownership verification (fetch company by companyId, check userId) —
  rejected due to read amplification and latency cost on every query.

---

## Decision 4: IUnitOfWork for DynamoDB

**Decision**: Implement `DynamoDbUnitOfWork` as a no-op for all methods.

**Rationale**: Examining the existing EF Core repositories, every `AddAsync`,
`UpdateAsync`, and `DeleteAsync` method calls `_db.SaveChangesAsync()` immediately —
they do not rely on deferred commit. The `PayslipGenerationService` calls
`_unitOfWork.SaveChangesAsync()` at the end of `GeneratePayslipAsync()`, but this is a
second call on an already-committed EF Core context (effectively a no-op in EF Core too
when no pending changes exist). DynamoDB repositories commit on each SDK call, so there
is nothing to flush at the unit-of-work level. The `BeginTransactionAsync`,
`CommitTransactionAsync`, and `RollbackTransactionAsync` methods are also no-ops.

**Atomicity note**: The payslip generation flow (add payslip + update N loans) is not
atomic across DynamoDB calls in this implementation. The `ExistsAsync` duplicate check
and the idempotent overwrite path in `PayslipGenerationService` provide sufficient
safety for the current scale. Full atomicity can be introduced via DynamoDB
`TransactWriteItems` in a future iteration if required.

**Alternatives considered**:
- DynamoDB TransactWriteItems for payslip + loan updates — rejected for this feature
  because it would require threading the loan update items through the PayslipRepository
  interface (a cross-cutting concern), changing the repository contract and adding
  scope not required by the spec.

---

## Decision 5: AWS SDK and Local Emulation

**Decision**: Use `AWSSDK.DynamoDBv2` (the AWS SDK v3 .NET DynamoDB client).
For local development and CI integration tests, use the `amazon/dynamodb-local`
Docker image. Test infrastructure is wrapped in an `IAsyncLifetime` xUnit fixture.

**Rationale**: `AWSSDK.DynamoDBv2` is the officially mandated package per constitution
v1.3.0. DynamoDB Local is the official AWS emulator, available as a Docker image with
no licensing restrictions. It is fully compatible with `AWSSDK.DynamoDBv2` and supports
all required APIs (CreateTable, PutItem, GetItem, Query, DeleteItem, UpdateItem).

**Env var configuration**:

| Variable | Required | Default | Description |
|---|---|---|---|
| `PERSISTENCE_PROVIDER` | No | `sqlite` | One of: `sqlite`, `mysql`, `dynamodb` |
| `DYNAMODB_REGION` | When dynamodb | `us-east-1` | AWS region |
| `DYNAMODB_ENDPOINT` | No | AWS endpoint | Override for local emulator |
| `AWS_ACCESS_KEY_ID` | When dynamodb (non-IAM) | — | AWS access key |
| `AWS_SECRET_ACCESS_KEY` | When dynamodb (non-IAM) | — | AWS secret key |
| `DYNAMODB_TABLE_PREFIX` | No | `payslip4all` | Prefix for all table names |

When `DYNAMODB_ENDPOINT` is set, the SDK is configured to use it instead of the
regional AWS endpoint. This supports `http://localhost:8000` for DynamoDB Local.
When running on AWS with an IAM role attached, `AWS_ACCESS_KEY_ID` and
`AWS_SECRET_ACCESS_KEY` can be omitted; the SDK's credential chain resolves the role.

---

## Decision 6: DynamoDB Client Registration

**Decision**: Register `IAmazonDynamoDB` (the SDK client interface) as a `Singleton`
in the DI container. All DynamoDB repository implementations receive it via constructor
injection.

**Rationale**: The SDK client is thread-safe and designed to be a long-lived singleton.
Creating a new client per request (Scoped) would waste connection pool resources.

**Factory**: A `DynamoDbClientFactory` static helper builds the `AmazonDynamoDBClient`
from environment variables and is called once during `builder.Services` registration.

---

## Decision 7: Table Auto-Creation

**Decision**: Implement `DynamoDbTableProvisioner` as an `IHostedService` that runs
at startup. It calls `DescribeTable` for each required table; if the table does not
exist (`ResourceNotFoundException`), it calls `CreateTable` and waits for the table
to become `ACTIVE`. Each creation is logged at Information level.

**Rationale**: Auto-creation (FR-011, Option A) simplifies developer onboarding and
reduces the operational burden for small deployments. The provisioner runs before the
application begins serving requests, ensuring tables exist before any repository call.

**CI impact**: The DynamoDB integration tests also call the provisioner (or create
tables directly via the SDK fixture) before running test assertions.

---

## Resolved Unknowns

| Unknown | Resolution |
|---|---|
| How to enforce ownership without EF Core JOINs | Denormalise `userId` onto child entities |
| How to handle `IUnitOfWork` without a DbContext | No-op `DynamoDbUnitOfWork` (matches actual EF Core runtime behaviour) |
| Should tables be auto-created? | Yes (Option A — chosen by engineer) |
| Which DynamoDB SDK to use? | `AWSSDK.DynamoDBv2` (constitution-mandated) |
| How to test without a live AWS account? | `amazon/dynamodb-local` Docker image in CI |
| How to name the config key? | `PERSISTENCE_PROVIDER` (replaces `DatabaseProvider`) |
