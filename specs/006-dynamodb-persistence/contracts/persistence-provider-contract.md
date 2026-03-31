# Persistence Provider Contract: AWS DynamoDB

**Feature**: 006-dynamodb-persistence  
**Phase**: 1 — Contracts  
**Date**: 2026-03-29

This feature does not add new HTTP routes or new Blazor pages. The external contract for
this feature is the operator-facing startup configuration and provider-selection behavior.

---

## 1. Provider Selection Contract

| Input | Required | Accepted values | Outcome |
|---|---|---|---|
| `PERSISTENCE_PROVIDER` | No | `sqlite`, `mysql`, `dynamodb` | Selects the active persistence path |

**Rules**:
- Value comparison is trimmed and case-insensitive.
- Unset value defaults to `sqlite`.
- Any unrecognized value fails startup and must list the valid options.

---

## 2. DynamoDB Configuration Contract

These inputs apply only when `PERSISTENCE_PROVIDER=dynamodb`.

| Variable | Required | Contract |
|---|---|---|
| `DYNAMODB_REGION` | Yes | Required AWS region string for DynamoDB client configuration |
| `DYNAMODB_ENDPOINT` | No | Optional endpoint override for local emulators such as DynamoDB Local |
| `DYNAMODB_TABLE_PREFIX` | No | Optional prefix for all six DynamoDB tables; defaults to `payslip4all` |
| `AWS_ACCESS_KEY_ID` | Conditional | Optional explicit credential; if set, `AWS_SECRET_ACCESS_KEY` must also be set |
| `AWS_SECRET_ACCESS_KEY` | Conditional | Optional explicit credential; if set, `AWS_ACCESS_KEY_ID` must also be set |

**Required-table contract**:

Startup must ensure the following tables exist before user traffic is served:

- `{prefix}_users`
- `{prefix}_companies`
- `{prefix}_employees`
- `{prefix}_employee_loans`
- `{prefix}_payslips`
- `{prefix}_payslip_loan_deductions`

Each created table must generate a clear operator log entry.

---

## 3. Credential Resolution Contract

When `PERSISTENCE_PROVIDER=dynamodb`, credential resolution must follow this precedence:

| Priority | Condition | Behavior |
|---|---|---|
| 1 | `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` both supplied | Use explicit credentials |
| 2 | `DYNAMODB_ENDPOINT` supplied and explicit credentials absent | Use dummy credentials for local emulators |
| 3 | No explicit credentials and no endpoint override | Use AWS SDK default credential chain / hosted identity |

**Notes**:
- Explicit credentials always override other sources.
- Dummy credentials are valid only for local-emulator scenarios.
- Hosted AWS deployments should rely on SDK-resolved credentials whenever explicit
  credentials are not intentionally supplied.

---

## 4. Startup Behavior Contract

### Successful DynamoDB startup

When configuration is valid and DynamoDB is selected, the application must:

1. register DynamoDB repositories implementing all existing `Application` interfaces,
2. bypass `PayslipDbContext` registration and relational migration execution,
3. create missing DynamoDB tables at startup,
4. log each table creation,
5. begin serving requests only after provisioning completes.

### Failure cases

| Failure | Expected behavior |
|---|---|
| Unknown `PERSISTENCE_PROVIDER` | Fail startup with valid options listed |
| Missing `DYNAMODB_REGION` | Fail startup with a descriptive missing-variable error |
| Only one explicit AWS credential variable supplied | Fail startup with a descriptive credential-pair error |
| DynamoDB permission/config issue during provisioning | Fail startup; do not continue with partially initialized persistence |

---

## 5. Backward-Compatibility Contract

| Provider | Compatibility requirement |
|---|---|
| `sqlite` | Existing behavior remains unchanged |
| `mysql` | Existing behavior remains unchanged |
| `dynamodb` | New provider path implements the same repository contracts without changing Application or Domain APIs |

No new end-user routes are introduced by this feature; existing authenticated pages and
service flows must continue to function regardless of the selected persistence provider.
