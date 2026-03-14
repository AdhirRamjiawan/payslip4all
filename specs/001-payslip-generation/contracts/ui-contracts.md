# UI Contracts: Payslip Generation System (001)

**Phase**: 1 — Design  
**Date**: 2025-07-15  
**Project Type**: Blazor Server Web Application

This document defines the page/component contracts for all Blazor UI surfaces. Each contract specifies the route, required injected services, input parameters, outputs/events, and key UI states.

---

## Contract Format

Each contract follows this structure:
- **Route** — `@page` directive value
- **Authorization** — access rules
- **Injected Services** — interfaces from `Payslip4All.Application`
- **Parameters / Inputs** — route params or component parameters
- **UI States** — loading, empty, error, data
- **Events / Actions** — user interactions and their outcomes
- **Validation** — client-side validation rules surfaced by the component
- **Navigation** — where the user goes after each action

---

## 1. Authentication Pages

### 1.1 Register Page

**File**: `Pages/Auth/Register.razor`  
**Route**: `/register`  
**Authorization**: Anonymous only (redirect to `/` if already authenticated)

**Injected Services**:
```csharp
@inject IAuthenticationService AuthService
@inject NavigationManager Nav
```

**Form Inputs**:
| Field | Type | Validation |
|-------|------|------------|
| Email | `string` | Required; valid email format; max 256 chars |
| Password | `string` | Required; min 8 chars |
| Confirm Password | `string` | Required; must match Password |

**UI States**:
| State | Description |
|-------|-------------|
| `Idle` | Form visible, all fields empty |
| `Submitting` | Form disabled, spinner shown |
| `Error` | Generic error banner shown (never reveals if email exists — FR-004) |
| `Success` | Redirect to `/` (dashboard) |

**Events / Actions**:
- `OnSubmit` → calls `AuthService.RegisterAsync(RegisterCommand)` → on success: sign in + redirect to `/`; on failure: show generic error
- `[Already have an account? Login]` → navigate to `/login`

**Navigation**:
- Success → `/` (dashboard)
- Cancel / link → `/login`

---

### 1.2 Login Page

**File**: `Pages/Auth/Login.razor`  
**Route**: `/login`  
**Authorization**: Anonymous only (redirect to `/` if already authenticated)

**Injected Services**:
```csharp
@inject IAuthenticationService AuthService
@inject NavigationManager Nav
```

**Form Inputs**:
| Field | Type | Validation |
|-------|------|------------|
| Email | `string` | Required; valid email format |
| Password | `string` | Required |

**UI States**:
| State | Description |
|-------|-------------|
| `Idle` | Form visible |
| `Submitting` | Form disabled, spinner shown |
| `Error` | Generic "Invalid credentials" banner (never specifies which field — FR-004) |
| `Success` | Redirect to `/` (dashboard) |

**Events / Actions**:
- `OnSubmit` → calls `AuthService.LoginAsync(LoginCommand)` → on success: sign in + redirect to `/`; on failure: show generic error
- `[Don't have an account? Register]` → navigate to `/register`

**Navigation**:
- Success → `/` or the original requested URL (`returnUrl` query parameter)
- Link → `/register`

---

### 1.3 Logout Endpoint

**File**: `Pages/Auth/Logout.razor`  
**Route**: `/logout`  
**Authorization**: Authenticated users only  
**Behaviour**: Not a visible page — immediately signs the user out and redirects to `/login`.

---

## 2. Dashboard / Home

### 2.1 Dashboard Page

**File**: `Pages/Dashboard.razor`  
**Route**: `/` (index)  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Injected Services**:
```csharp
@inject ICompanyService CompanyService
@inject AuthenticationStateProvider AuthState
```

**UI States**:
| State | Description |
|-------|-------------|
| `Loading` | Spinner while companies are fetched |
| `Empty` | "No companies yet" call-to-action with Add Company button (edge case from spec) |
| `Data` | Company cards/list with Name, Address, Employee Count, action buttons |
| `Error` | Error banner |

**Actions**:
- `[Add Company]` → navigate to `/companies/create`
- `[View / Manage]` on company card → navigate to `/companies/{companyId}`

---

## 3. Company Management Pages

### 3.1 Company List (embedded in Dashboard)

Company listing is part of the Dashboard (2.1). No separate list page.

---

### 3.2 Create Company Page

**File**: `Pages/Companies/CreateCompany.razor`  
**Route**: `/companies/create`  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Injected Services**:
```csharp
@inject ICompanyService CompanyService
@inject NavigationManager Nav
```

**Form Inputs**:
| Field | Type | Validation |
|-------|------|------------|
| Company Name | `string` | Required; non-empty; max 200 chars (FR-011) |
| Address | `string?` | Optional; if provided must be non-empty; max 500 chars (FR-011) |

**UI States**:
| State | Description |
|-------|-------------|
| `Idle` | Empty form |
| `Submitting` | Disabled form + spinner |
| `ValidationError` | Inline field errors |
| `ServerError` | Error banner |
| `Success` | Redirect to dashboard |

**Events / Actions**:
- `OnSubmit` → `CompanyService.CreateCompanyAsync(CreateCompanyCommand)` → redirect to `/`
- `[Cancel]` → navigate to `/`

---

### 3.3 Edit Company Page

**File**: `Pages/Companies/EditCompany.razor`  
**Route**: `/companies/{companyId:guid}/edit`  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Route Parameters**: `companyId` (Guid)

**Injected Services**:
```csharp
@inject ICompanyService CompanyService
@inject NavigationManager Nav
```

**On Load**: Fetch company by `companyId`; verify ownership (returns 404 if not owned — FR-008)

**Form Inputs** (pre-populated):
| Field | Type | Validation |
|-------|------|------------|
| Company Name | `string` | Required; non-empty; max 200 chars |
| Address | `string?` | Optional; non-empty if provided |

**UI States**: Loading → Form (same as Create) → Success / Error

**Events / Actions**:
- `OnSubmit` → `CompanyService.UpdateCompanyAsync(UpdateCompanyCommand)` → redirect to `/companies/{companyId}`
- `[Cancel]` → navigate to `/companies/{companyId}`

---

### 3.4 Company Detail Page

**File**: `Pages/Companies/CompanyDetail.razor`  
**Route**: `/companies/{companyId:guid}`  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Injected Services**:
```csharp
@inject ICompanyService CompanyService
@inject IEmployeeService EmployeeService
@inject NavigationManager Nav
```

**UI States**:
| State | Description |
|-------|-------------|
| `Loading` | Spinner |
| `NotFound` | 404-style message (ownership filter — FR-008) |
| `EmptyEmployees` | "No employees yet" + Add Employee CTA |
| `Data` | Company info header + employee table |

**Actions**:
- `[Edit Company]` → navigate to `/companies/{companyId}/edit`
- `[Delete Company]` → confirm dialog → `CompanyService.DeleteCompanyAsync(companyId)` → on success: redirect to `/`; on failure (has employees): show error banner (FR-010)
- `[Add Employee]` → navigate to `/companies/{companyId}/employees/create`
- `[View Employee]` row → navigate to `/companies/{companyId}/employees/{employeeId}`

---

## 4. Employee Management Pages

### 4.1 Create Employee Page

**File**: `Pages/Employees/CreateEmployee.razor`  
**Route**: `/companies/{companyId:guid}/employees/create`  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Route Parameters**: `companyId` (Guid)

**Injected Services**:
```csharp
@inject IEmployeeService EmployeeService
@inject NavigationManager Nav
```

**Form Inputs**:
| Field | Type | Validation |
|-------|------|------------|
| First Name | `string` | Required; max 100 chars (FR-016) |
| Last Name | `string` | Required; max 100 chars (FR-016) |
| ID Number | `string` | Required; max 20 chars (FR-016) |
| Employee Number | `string` | Required; max 50 chars (FR-016) |
| Start Date | `DateOnly` | Required (FR-016) |
| Occupation | `string` | Required; max 150 chars (FR-016) |
| UIF Reference | `string?` | Optional; max 50 chars |
| Monthly Gross Salary | `decimal` | Required; must be > 0 (FR-017) |

**Events / Actions**:
- `OnSubmit` → `EmployeeService.CreateEmployeeAsync(CreateEmployeeCommand)` → redirect to `/companies/{companyId}/employees/{newId}`
- `[Cancel]` → navigate to `/companies/{companyId}`

---

### 4.2 Edit Employee Page

**File**: `Pages/Employees/EditEmployee.razor`  
**Route**: `/companies/{companyId:guid}/employees/{employeeId:guid}/edit`  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Route Parameters**: `companyId`, `employeeId`

**On Load**: Fetch employee; verify ownership via company chain

**Form Inputs**: Same as Create Employee (pre-populated)

**Events / Actions**:
- `OnSubmit` → `EmployeeService.UpdateEmployeeAsync(UpdateEmployeeCommand)` → redirect to employee detail
- `[Cancel]` → navigate to `/companies/{companyId}/employees/{employeeId}`

---

### 4.3 Employee Detail Page

**File**: `Pages/Employees/EmployeeDetail.razor`  
**Route**: `/companies/{companyId:guid}/employees/{employeeId:guid}`  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Injected Services**:
```csharp
@inject IEmployeeService EmployeeService
@inject IPayslipService PayslipService
@inject ILoanService LoanService
@inject NavigationManager Nav
```

**Sections**:
1. **Employee Header** — name, employee number, occupation, salary
2. **Payslip History** — table (Month/Year, Gross, Net Pay, Download PDF) in reverse-chronological order (FR-024)
3. **Active Loans** — table (Description, Total, Monthly Amount, Start Date, Terms Completed/Total, Status, Actions)
4. **Completed Loans** — read-only loan history (FR-029)

**Actions**:
- `[Edit Employee]` → `/companies/{companyId}/employees/{employeeId}/edit`
- `[Delete Employee]` → confirm → `EmployeeService.DeleteEmployeeAsync(employeeId)` → on success: `/companies/{companyId}`; on failure (has payslips): error banner (FR-015)
- `[Generate Payslip]` → navigate to `/companies/{companyId}/employees/{employeeId}/payslips/generate`
- `[Download PDF]` on payslip row → `PayslipService.GetPdfAsync(payslipId)` → browser file download
- `[Add Loan]` → navigate to `/companies/{companyId}/employees/{employeeId}/loans/create`
- `[Edit Loan]` on active loan (only when `TermsCompleted == 0`) → loan edit page
- `[Delete Loan]` on active loan (only when `TermsCompleted == 0`) → confirm → `LoanService.DeleteLoanAsync(loanId)`

---

### 4.4 Add Loan Page

**File**: `Pages/Employees/Loans/CreateLoan.razor`  
**Route**: `/companies/{companyId:guid}/employees/{employeeId:guid}/loans/create`  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Form Inputs**:
| Field | Type | Validation |
|-------|------|------------|
| Description | `string` | Required; max 300 chars (FR-026) |
| Total Loan Amount | `decimal` | Required; > 0 (FR-028) |
| Number of Terms | `int` | Required; > 0 positive integer (FR-028) |
| Monthly Deduction Amount | `decimal` | Required; > 0 (FR-028) |
| Payment Start Date | `DateOnly` (month + year picker) | Required (FR-026) |

**Events / Actions**:
- `OnSubmit` → `LoanService.CreateLoanAsync(CreateLoanCommand)` → redirect to employee detail
- `[Cancel]` → navigate to employee detail

---

### 4.5 Edit Loan Page

**File**: `Pages/Employees/Loans/EditLoan.razor`  
**Route**: `/companies/{companyId:guid}/employees/{employeeId:guid}/loans/{loanId:guid}/edit`  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Guard**: If `TermsCompleted > 0` → display "This loan cannot be edited because at least one deduction has already been applied." and show no form (FR-030)

**Form Inputs**: Same as Add Loan (pre-populated), shown only when `TermsCompleted == 0`

---

## 5. Payslip Generation Pages

### 5.1 Generate Payslip Page

**File**: `Pages/Payslips/GeneratePayslip.razor`  
**Route**: `/companies/{companyId:guid}/employees/{employeeId:guid}/payslips/generate`  
**Authorization**: `[Authorize(Roles = "CompanyOwner")]`

**Injected Services**:
```csharp
@inject IPayslipService PayslipService
@inject IEmployeeService EmployeeService
@inject NavigationManager Nav
```

**Workflow Stages**:

**Stage 1 — Period Selection**:
| Field | Type | Validation |
|-------|------|------------|
| Pay Period Month | `int` (dropdown 1–12) | Required |
| Pay Period Year | `int` | Required; reasonable range |

Action: `[Preview]` → calls `PayslipService.PreviewPayslipAsync(PreviewPayslipQuery)` → transitions to Stage 2

**Stage 2 — Preview & Confirm**:

Displays calculated payslip values (FR-019):
```
Company Name:         Acme Ltd
Company Address:      123 Main St
Employee:             John Smith
Pay Period:           June 2025

─────────────────────────────────
Gross Earnings:       R 25,000.00
─────────────────────────────────
UIF Deduction:        R   177.12
Loan Deduction 1:     R   500.00   (Car Loan)
Loan Deduction 2:     R   200.00   (Staff Advance)
─────────────────────────────────
Total Deductions:     R   877.12
─────────────────────────────────
Net Pay:              R 24,122.88
─────────────────────────────────
```

**Duplicate check**: If payslip already exists for that period → show warning banner with `[Overwrite]` and `[Cancel]` options (FR-021).

Actions:
- `[Confirm & Generate]` → `PayslipService.GeneratePayslipAsync(GeneratePayslipCommand)` → on success: navigate to employee detail (payslip history auto-refreshes)
- `[Back]` → return to Stage 1
- `[Cancel]` → navigate to employee detail

**UI States**:
| State | Description |
|-------|-------------|
| `SelectPeriod` | Stage 1 form |
| `PreviewLoading` | Spinner during preview calculation |
| `PreviewReady` | Stage 2 calculated values + confirm button |
| `DuplicateWarning` | Existing payslip warning with overwrite option |
| `Generating` | Spinner during generation (PDF creation in progress) |
| `Error` | Generation failed — no partial save (FR-023) |
| `NoSalaryError` | Employee has no salary configured (FR-025) |

---

## 6. Shared Components

### 6.1 LoadingSpinner Component

**File**: `Shared/LoadingSpinner.razor`  
**Parameters**: `bool IsLoading`, `string? Message`  
**Usage**: Wrap async content in all data-loading pages

### 6.2 ErrorBanner Component

**File**: `Shared/ErrorBanner.razor`  
**Parameters**: `string? ErrorMessage`, `EventCallback OnDismiss`  
**Usage**: Standardised error display across all pages

### 6.3 ConfirmDialog Component

**File**: `Shared/ConfirmDialog.razor`  
**Parameters**: `string Title`, `string Message`, `EventCallback OnConfirm`, `EventCallback OnCancel`  
**Usage**: Delete confirmations, payslip overwrite confirmation

### 6.4 PageTitle Component

**File**: `Shared/PageTitle.razor`  
**Parameters**: `string Title`, `string? Subtitle`  
**Usage**: Consistent heading styling across pages

---

## Navigation Map

```
/login ──────────────────────────────────────── Register → /register
/register ───────────────────────────────────── Login → /login
/ (dashboard) ──────────────────────────────── company cards
  └─ /companies/create
  └─ /companies/{id}
       ├─ /companies/{id}/edit
       └─ employees table
            └─ /companies/{id}/employees/create
            └─ /companies/{id}/employees/{eid}
                 ├─ /companies/{id}/employees/{eid}/edit
                 ├─ /companies/{id}/employees/{eid}/payslips/generate
                 └─ /companies/{id}/employees/{eid}/loans/create
                      └─ /companies/{id}/employees/{eid}/loans/{lid}/edit
```

---

## Authorization Summary

| Page / Route | Role Required |
|---|---|
| `/login`, `/register` | Anonymous |
| `/logout` | Authenticated |
| All other routes | `CompanyOwner` |

All service methods additionally filter by `UserId` (FR-008, FR-013) — ownership is enforced at the data layer, not just at the route level (FR-005, FR-008).
