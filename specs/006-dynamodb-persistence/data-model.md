# Data Model: AWS DynamoDB Persistence Option

**Feature**: 006-dynamodb-persistence  
**Phase**: 1 — Design  
**Date**: 2026-03-29

> Domain entities remain unchanged. This document describes the DynamoDB storage model and
> the runtime configuration model required to activate it.

---

## 1. Runtime Configuration Model

### 1.1 `PersistenceProviderSelection`

**Purpose**: Determines which persistence path the application activates at startup.

| Field | Type | Required | Validation / Rules |
|---|---|---|---|
| `PERSISTENCE_PROVIDER` | `string` | No | Trimmed, case-insensitive. Valid values: `sqlite`, `mysql`, `dynamodb`. Defaults to `sqlite` when unset. Invalid values fail startup with a message listing valid options. |

**State transitions**:
- `unset` → `sqlite`
- `sqlite` / `mysql` → relational EF Core path
- `dynamodb` → DynamoDB DI path + table provisioning path
- any other value → startup failure

### 1.2 `DynamoDbRuntimeConfig`

**Purpose**: Operator-facing runtime settings used only when
`PERSISTENCE_PROVIDER=dynamodb`.

| Field | Type | Required | Validation / Rules |
|---|---|---|---|
| `DYNAMODB_REGION` | `string` | Yes | Required for DynamoDB; trimmed; must map to an AWS region understood by `RegionEndpoint.GetBySystemName`. Missing value fails fast at startup. |
| `DYNAMODB_ENDPOINT` | `string` | No | Optional endpoint override, primarily for local emulators such as `http://localhost:8000`. When present and explicit credentials are absent, dummy credentials are supplied. |
| `DYNAMODB_TABLE_PREFIX` | `string` | No | Optional; defaults to `payslip4all`; prefixes all six table names. |
| `AWS_ACCESS_KEY_ID` | `string` | Conditionally | Optional overall, but if provided then `AWS_SECRET_ACCESS_KEY` must also be provided. Explicit credentials take precedence over every other auth mechanism. |
| `AWS_SECRET_ACCESS_KEY` | `string` | Conditionally | Optional overall, but if provided then `AWS_ACCESS_KEY_ID` must also be provided. |

**Credential resolution order**:
1. explicit `AWS_ACCESS_KEY_ID` + `AWS_SECRET_ACCESS_KEY`,
2. dummy credentials for local-emulator mode when `DYNAMODB_ENDPOINT` is set and explicit credentials are absent,
3. AWS SDK default credential chain / hosted identity for real AWS when explicit credentials are absent and no endpoint override is configured.

**Validation rules**:
- `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` are all-or-nothing.
- Missing `DYNAMODB_REGION` is a startup error.
- Real AWS deployments must rely on either explicit credentials or a resolvable AWS SDK
  credential source; local-emulator mode may use dummy placeholder credentials.

### 1.3 `ProvisionedTable`

**Purpose**: Represents the operational status of each required DynamoDB table during
startup provisioning.

| Field | Type | Notes |
|---|---|---|
| `TableName` | `string` | `{prefix}_{entity}` naming convention |
| `Exists` | `bool` | Derived from `DescribeTable` |
| `CreatedByStartup` | `bool` | True when `CreateTable` ran in the current startup cycle |
| `Status` | `string` | Expected lifecycle: `Missing` → `Creating` → `ACTIVE`, or `Existing` → `ACTIVE` |
| `Logged` | `bool` | True once the operator-facing startup log entry is written |

**State transitions**:
- `Missing` → `Creating` → `ACTIVE`
- `Existing` → `ACTIVE`
- permission/config failure → startup abort

---

## 2. Storage Model Overview

The DynamoDB persistence design uses six tables mirroring the existing relational data
shape. Every table name is prefixed with `DYNAMODB_TABLE_PREFIX` (default `payslip4all`).
Each table uses a string `id` partition key holding the entity GUID.

Ownership filtering is enforced by storing `userId` on all multi-tenant records that need
company-owner isolation without relational joins.

---

## 3. Tables

### 3.1 `{prefix}_users`

**Purpose**: Stores registered application users.

| Attribute | Type | Validation / Notes |
|---|---|---|
| `id` | `S` | Primary key; GUID |
| `email` | `S` | Unique login lookup value; normalized email |
| `passwordHash` | `S` | BCrypt/PBKDF2 output; never plain text |
| `createdAt` | `S` | ISO-8601 UTC timestamp |

**GSI**: `email-index` on `email`

**Relationships**:
- one `User` owns many `Company` records

### 3.2 `{prefix}_companies`

**Purpose**: Stores employer companies owned by a user.

| Attribute | Type | Validation / Notes |
|---|---|---|
| `id` | `S` | Primary key; GUID |
| `name` | `S` | Required; max 200 chars |
| `address` | `S` | Optional; max 500 chars |
| `uifNumber` | `S` | Optional; max 50 chars |
| `sarsPayeNumber` | `S` | Optional; max 30 chars |
| `userId` | `S` | Required owner reference; primary ownership filter |
| `createdAt` | `S` | ISO-8601 UTC timestamp |

**GSI**: `userId-index` on `userId`

**Relationships**:
- belongs to one `User`
- has many `Employee` records

### 3.3 `{prefix}_employees`

**Purpose**: Stores employees belonging to a company.

| Attribute | Type | Validation / Notes |
|---|---|---|
| `id` | `S` | Primary key; GUID |
| `firstName` | `S` | Required; max 100 chars |
| `lastName` | `S` | Required; max 100 chars |
| `idNumber` | `S` | Required; max 20 chars |
| `employeeNumber` | `S` | Required; unique within company |
| `startDate` | `S` | Required; ISO date |
| `occupation` | `S` | Required; max 150 chars |
| `uifReference` | `S` | Optional; max 50 chars |
| `monthlyGrossSalary` | `S` | Decimal stored as string to preserve precision |
| `companyId` | `S` | Required FK reference |
| `userId` | `S` | Required denormalized owner id for query filtering |
| `createdAt` | `S` | ISO-8601 UTC timestamp |

**GSI**: `companyId-index` on `companyId`

**Relationships**:
- belongs to one `Company`
- has many `EmployeeLoan` records
- has many `Payslip` records

### 3.4 `{prefix}_employee_loans`

**Purpose**: Stores employee loan schedules and repayment progress.

| Attribute | Type | Validation / Notes |
|---|---|---|
| `id` | `S` | Primary key; GUID |
| `description` | `S` | Required; max 300 chars |
| `totalLoanAmount` | `S` | Decimal stored as string |
| `numberOfTerms` | `N` | Positive integer |
| `monthlyDeductionAmount` | `S` | Decimal stored as string |
| `paymentStartDate` | `S` | ISO date |
| `termsCompleted` | `N` | Integer; concurrency-sensitive |
| `status` | `N` | Enum value for active/completed |
| `employeeId` | `S` | Required FK reference |
| `userId` | `S` | Required denormalized owner id |
| `createdAt` | `S` | ISO-8601 UTC timestamp |

**GSI**: `employeeId-index` on `employeeId`

**State transitions**:
- `Active` with `termsCompleted = 0..(numberOfTerms-1)`
- `Completed` once `termsCompleted >= numberOfTerms`

### 3.5 `{prefix}_payslips`

**Purpose**: Stores generated payslips for an employee and pay period.

| Attribute | Type | Validation / Notes |
|---|---|---|
| `id` | `S` | Primary key; GUID |
| `payPeriodMonth` | `N` | 1–12 |
| `payPeriodYear` | `N` | Four-digit year |
| `grossEarnings` | `S` | Decimal stored as string |
| `uifDeduction` | `S` | Decimal stored as string |
| `totalLoanDeductions` | `S` | Decimal stored as string |
| `totalDeductions` | `S` | Decimal stored as string |
| `netPay` | `S` | Decimal stored as string |
| `employeeId` | `S` | Required FK reference |
| `userId` | `S` | Required denormalized owner id |
| `generatedAt` | `S` | ISO-8601 UTC timestamp |

**GSI**: `employeeId-index` on `employeeId` (with chronological ordering support in the
repository query strategy)

**Validation / uniqueness**:
- one payslip per employee + pay period
- must remain readable only by the owning `userId`

### 3.6 `{prefix}_payslip_loan_deductions`

**Purpose**: Stores per-loan deduction lines associated with a payslip.

| Attribute | Type | Validation / Notes |
|---|---|---|
| `id` | `S` | Primary key; GUID |
| `payslipId` | `S` | Required FK reference |
| `employeeLoanId` | `S` | Required FK reference |
| `description` | `S` | Required; max 300 chars |
| `amount` | `S` | Decimal stored as string |

**GSI**: `payslipId-index` on `payslipId`

**Relationships**:
- belongs to one `Payslip`
- references one `EmployeeLoan`

---

## 4. Cross-Table Relationship Rules

| Parent | Child | Rule |
|---|---|---|
| `User` | `Company` | `Company.userId` must equal owning user |
| `Company` | `Employee` | `Employee.companyId` references the parent company; `Employee.userId` must equal the owning company's `userId` |
| `Employee` | `EmployeeLoan` | `EmployeeLoan.employeeId` references the employee; `EmployeeLoan.userId` must equal the owning employee's `userId` |
| `Employee` | `Payslip` | `Payslip.employeeId` references the employee; `Payslip.userId` must equal the owning employee's `userId` |
| `Payslip` | `PayslipLoanDeduction` | `PayslipLoanDeduction.payslipId` references the payslip |

---

## 5. Storage Conventions

### Monetary values

All monetary values are stored as strings (`S`) rather than DynamoDB numbers when precise
decimal round-tripping is required:

- `monthlyGrossSalary`
- `totalLoanAmount`
- `monthlyDeductionAmount`
- `grossEarnings`
- `uifDeduction`
- `totalLoanDeductions`
- `totalDeductions`
- `netPay`
- `amount`

### Ownership filtering

Every repository method returning company-scoped data must verify `userId` before returning
results. Ownership filtering is a storage-level invariant, not just an application-service
concern.

### Startup invariants

When `PERSISTENCE_PROVIDER=dynamodb`:
- `PayslipDbContext` is not constructed,
- EF Core migration execution is skipped,
- all required DynamoDB tables must be confirmed active before serving traffic.
