# Data Model: AWS DynamoDB Persistence Option

**Feature**: 006-dynamodb-persistence
**Phase**: 1 — Design
**Date**: 2026-03-28

> This document describes the DynamoDB storage model only. Domain entities
> (`Payslip4All.Domain`) are unchanged. The DynamoDB representation is an
> Infrastructure concern; no Application or Domain code is modified.

---

## Overview

Six DynamoDB tables mirror the existing relational schema. All table names are
prefixed with `DYNAMODB_TABLE_PREFIX` (default: `payslip4all`). Each table uses a
single string partition key (`id`) holding the entity's GUID.

Ownership filtering is enforced by denormalising `userId` onto every child entity
item. This avoids cross-table lookups (which DynamoDB does not natively support).

---

## Tables

### 1. `{prefix}_users`

**Purpose**: Stores registered user accounts.

| Attribute | DynamoDB Type | Notes |
|-----------|---------------|-------|
| `id` | S (PK) | GUID |
| `email` | S | Unique; used for login lookup |
| `passwordHash` | S | BCrypt hash |
| `createdAt` | S | ISO 8601 UTC |

**GSI — `email-index`**:
- PK: `email` (S)
- Projection: ALL
- Used by: `GetByEmailAsync`, `ExistsAsync`

**Access patterns**:
- `GetByEmailAsync(email)` → Query `email-index` PK=email, limit 1
- `AddAsync(user)` → PutItem
- `ExistsAsync(email)` → Query `email-index` PK=email, Select COUNT

---

### 2. `{prefix}_companies`

**Purpose**: Stores companies owned by users.

| Attribute | DynamoDB Type | Notes |
|-----------|---------------|-------|
| `id` | S (PK) | GUID |
| `name` | S | Max 200 chars |
| `address` | S | Optional; max 500 chars |
| `uifNumber` | S | Optional; max 50 chars |
| `sarsPayeNumber` | S | Optional; max 30 chars |
| `userId` | S | Owner; used for ownership filtering |
| `createdAt` | S | ISO 8601 UTC |

**GSI — `userId-index`**:
- PK: `userId` (S)
- Projection: ALL
- Used by: `GetAllByUserIdAsync`

**Access patterns**:
- `GetAllByUserIdAsync(userId)` → Query `userId-index` PK=userId
- `GetByIdAsync(id, userId)` → GetItem PK=id; verify `userId` attribute matches
- `GetByIdWithEmployeesAsync(id, userId)` → GetItem + Query employees table (companyId-index)
- `AddAsync` → PutItem
- `UpdateAsync` → PutItem (full replace)
- `DeleteAsync` → DeleteItem
- `HasEmployeesAsync(id)` → Query employees `companyId-index` PK=companyId, limit 1, Select COUNT

---

### 3. `{prefix}_employees`

**Purpose**: Stores employees belonging to companies.

| Attribute | DynamoDB Type | Notes |
|-----------|---------------|-------|
| `id` | S (PK) | GUID |
| `firstName` | S | Max 100 chars |
| `lastName` | S | Max 100 chars |
| `idNumber` | S | Max 20 chars |
| `employeeNumber` | S | Unique per company |
| `startDate` | S | ISO 8601 date (YYYY-MM-DD) |
| `occupation` | S | Max 150 chars |
| `uifReference` | S | Optional; max 50 chars |
| `monthlyGrossSalary` | S | Stored as string to preserve decimal precision |
| `companyId` | S | FK reference |
| `userId` | S | Denormalised from Company for ownership filtering |
| `createdAt` | S | ISO 8601 UTC |

**GSI — `companyId-index`**:
- PK: `companyId` (S)
- Projection: ALL
- Used by: `GetAllByCompanyIdAsync`

**Uniqueness constraint** (`employeeNumber` per `companyId`): Enforced via a
conditional PutItem expression — the DynamoDB repository checks existence before
insert using a Query on `companyId-index` filtered by `employeeNumber`.

**Access patterns**:
- `GetAllByCompanyIdAsync(companyId, userId)` → Query `companyId-index` PK=companyId;
  filter items where `userId` attribute matches (client-side filter post-query)
- `GetByIdAsync(id, userId)` → GetItem PK=id; verify `userId` matches
- `GetByIdWithLoansAsync(id, userId)` → GetItem + Query loans `employeeId-index`
- `AddAsync` → PutItem
- `UpdateAsync` → PutItem (full replace)
- `DeleteAsync` → DeleteItem
- `HasPayslipsAsync(id)` → Query payslips `employeeId-index` PK=id, limit 1, Select COUNT

---

### 4. `{prefix}_employee_loans`

**Purpose**: Stores loan records for employees.

| Attribute | DynamoDB Type | Notes |
|-----------|---------------|-------|
| `id` | S (PK) | GUID |
| `description` | S | Max 300 chars |
| `totalLoanAmount` | S | Decimal as string |
| `numberOfTerms` | N | Integer |
| `monthlyDeductionAmount` | S | Decimal as string |
| `paymentStartDate` | S | ISO 8601 date |
| `termsCompleted` | N | Integer; updated on payslip generation |
| `status` | N | `0` = Active, `1` = Completed (enum int) |
| `employeeId` | S | FK reference |
| `userId` | S | Denormalised for ownership filtering |
| `createdAt` | S | ISO 8601 UTC |

**GSI — `employeeId-index`**:
- PK: `employeeId` (S)
- Projection: ALL
- Used by: `GetAllByEmployeeIdAsync`

**Concurrency on `termsCompleted`**: The EF Core model marks `TermsCompleted` as a
concurrency token. For DynamoDB, `UpdateAsync` uses a conditional UpdateItem
expression: `SET termsCompleted = :newVal IF termsCompleted = :expectedVal`. If the
condition fails, a `ConditionalCheckFailedException` is thrown and propagated as an
`InvalidOperationException` (matching existing domain behaviour).

**Access patterns**:
- `GetAllByEmployeeIdAsync(employeeId, userId)` → Query `employeeId-index` PK=employeeId;
  verify `userId` matches on each item
- `GetByIdAsync(id, userId)` → GetItem PK=id; verify `userId` matches
- `AddAsync` → PutItem
- `UpdateAsync` → UpdateItem with `termsCompleted` condition
- `DeleteAsync` → DeleteItem

---

### 5. `{prefix}_payslips`

**Purpose**: Stores generated payslips for employees.

| Attribute | DynamoDB Type | Notes |
|-----------|---------------|-------|
| `id` | S (PK) | GUID |
| `payPeriodMonth` | N | 1–12 |
| `payPeriodYear` | N | e.g., 2026 |
| `grossEarnings` | S | Decimal as string |
| `uifDeduction` | S | Decimal as string |
| `totalLoanDeductions` | S | Decimal as string |
| `totalDeductions` | S | Decimal as string |
| `netPay` | S | Decimal as string |
| `employeeId` | S | FK reference |
| `userId` | S | Denormalised for ownership filtering |
| `generatedAt` | S | ISO 8601 UTC |

**GSI — `employeeId-index`**:
- PK: `employeeId` (S)
- Sort key: `generatedAt` (S) — enables ordering by recency
- Projection: ALL
- Used by: `GetAllByEmployeeIdAsync`

**Uniqueness constraint** (`employeeId` + `payPeriodMonth` + `payPeriodYear`): Enforced
client-side via `ExistsAsync` check before insert (mirrors the EF Core unique index).

**Access patterns**:
- `GetAllByEmployeeIdAsync(employeeId, userId)` → Query `employeeId-index`
  PK=employeeId ScanIndexForward=false; verify `userId` matches; load loan deductions
  per payslip via batch Query on `payslipId-index`
- `GetByIdAsync(id, userId)` → GetItem PK=id; verify `userId`; load deductions
- `ExistsAsync(employeeId, month, year)` → Query `employeeId-index`, filter by
  `payPeriodMonth` and `payPeriodYear`, Select COUNT
- `AddAsync` → PutItem payslip + PutItem each `PayslipLoanDeduction`
- `DeleteAsync` → DeleteItem payslip + Query + DeleteItem each deduction

---

### 6. `{prefix}_payslip_loan_deductions`

**Purpose**: Stores per-loan deduction line items for a payslip. Stored separately to
avoid DynamoDB item size limits and to enable querying by payslip.

| Attribute | DynamoDB Type | Notes |
|-----------|---------------|-------|
| `id` | S (PK) | GUID |
| `payslipId` | S | FK reference |
| `employeeLoanId` | S | FK reference |
| `description` | S | Max 300 chars |
| `amount` | S | Decimal as string |

**GSI — `payslipId-index`**:
- PK: `payslipId` (S)
- Projection: ALL
- Used by: load deductions for a payslip

**Access patterns**:
- Load all deductions for a payslip → Query `payslipId-index` PK=payslipId

---

## Entity Hydration

When the DynamoDB repositories return domain entities (e.g., `Employee`), they must
hydrate navigation properties that the Application services depend on:

- `Employee.Company` — required by `PayslipGenerationService.GetPdfAsync` to access
  `Company.Name`, `Company.Address`, etc. DynamoDB employee repositories MUST fetch
  the company record and populate this navigation property when
  `GetByIdWithLoansAsync` is called.
- `Payslip.Employee` + `Employee.Company` — required by `GetPdfAsync`.
  `PayslipRepository.GetByIdAsync` MUST fetch and hydrate both.
- `Payslip.LoanDeductions` — populated by querying `payslipId-index`.

---

## Decimal Storage Convention

DynamoDB's `N` type loses decimal precision for values like `1234.56` stored as
numbers due to floating-point representation. All monetary values (`MonthlyGrossSalary`,
`TotalLoanAmount`, `MonthlyDeductionAmount`, `GrossEarnings`, etc.) MUST be stored as
`S` (string) using `decimal.ToString("G")` and parsed back with
`decimal.Parse(value, CultureInfo.InvariantCulture)`.

---

## Table Provisioning

`DynamoDbTableProvisioner` creates all six tables at startup if they do not exist.
Each `CreateTable` call specifies:
- `BillingMode`: PAY_PER_REQUEST (on-demand; no capacity planning required for dev/small prod)
- All GSIs defined above with `ALL` projection
- Waits for table status `ACTIVE` before proceeding to the next table

The provisioner logs at `Information` level: `"DynamoDB table '{tableName}' created."` or
`"DynamoDB table '{tableName}' already exists — skipping."`.
