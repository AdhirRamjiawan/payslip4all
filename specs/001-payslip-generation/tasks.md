---
description: "Task list for Payslip Generation System (001)"
---

# Tasks: Payslip Generation System

**Input**: Design documents from `/specs/001-payslip-generation/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Feature Status**: ✅ **Implemented** (66/66 tasks complete — all tasks done)

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for all features in
this project (xUnit for unit/integration, bUnit for Blazor components). Tests MUST be
written and confirmed failing before implementation tasks begin. Do not mark test tasks
as optional — TDD is non-negotiable.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Complexity Deviations** (from plan.md Complexity Tracking):

| ID | Deviation | Tasks Affected |
|----|-----------|----------------|
| C1 | `PayslipLoanDeduction` snapshot entity (5th table) | T011, T018, T019, T058 |
| C2 | MySQL provider (`Pomelo.EntityFrameworkCore.MySql`) alongside SQLite | T002, T022 |
| C3 | Auth pages use Razor Pages (`.cshtml`/`.cshtml.cs`) not Blazor components | T030, T031, T032 |
| C4 | `SiteAdministrator` role seeded but not enforced (deferred to `002-admin-portal`) | T019 |

> **Gate III** (Constitution Principle III — Blazor Web App): ✅ **(with C3 deviation)** — Auth pages (Login/Register/Logout) use Razor Pages because `HttpContext.SignInAsync()` / `SignOutAsync()` cannot be called from the Blazor Server render thread. All other UI (12+ pages) remain Blazor `.razor` components.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to ([US1]–[US4])
- Exact file paths are included in every description

## Tech Stack

**Language/Runtime**: C# 12 / .NET 8 (LTS)
**Framework**: Blazor Server (ASP.NET Core 8)
**ORM**: Entity Framework Core 8
**Database**: SQLite (dev default) / MySQL via `Pomelo.EntityFrameworkCore.MySql` (provider swap via `appsettings.json` key `"DatabaseProvider"`)
**PDF**: QuestPDF (community licence)
**Auth**: ASP.NET Core cookie authentication + custom `CookieAuthenticationStateProvider`; BCrypt.Net-Next (work factor 12)
**Testing**: xUnit 2.x + Moq 4.x (unit/integration), bUnit 1.x (Blazor components), SQLite in-memory (infrastructure integration), coverlet (≥ 80% coverage on Domain + Application)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution + project scaffolding, CI pipeline, and tooling configuration.

- [x] T001 Create .NET 8 solution `Payslip4All.sln` with four source projects (`Payslip4All.Domain`, `Payslip4All.Application`, `Payslip4All.Infrastructure`, `Payslip4All.Web`) and four test projects (`Payslip4All.Domain.Tests`, `Payslip4All.Application.Tests`, `Payslip4All.Infrastructure.Tests`, `Payslip4All.Web.Tests`) matching the layout in plan.md
- [x] T002 Add NuGet package references: `BCrypt.Net-Next`, `QuestPDF`, `Microsoft.EntityFrameworkCore.Sqlite`, `Pomelo.EntityFrameworkCore.MySql`, `Microsoft.EntityFrameworkCore.Tools` to `src/Payslip4All.Infrastructure/Payslip4All.Infrastructure.csproj`; add `bunit`, `xunit`, `Moq`, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` to `tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj`; add `xunit`, `Moq`, `Microsoft.NET.Test.Sdk` to remaining test projects
- [x] T003 [P] Create GitHub Actions CI workflow at `.github/workflows/ci.yml` running `dotnet restore`, `dotnet build --no-restore --warnaserror`, `dotnet test --collect:"XPlat Code Coverage"` on push and PR to `main`
- [x] T004 [P] Add `.editorconfig` at repo root enforcing zero build warnings (`dotnet_diagnostic.CS0` through `CS9` as warnings treated as errors) and set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props`
- [x] T005 [P] Configure `src/Payslip4All.Web/appsettings.json` with keys `DatabaseProvider`, `ConnectionStrings:DefaultConnection` (SQLite), `ConnectionStrings:MySqlConnection`, `Auth:Cookie:ExpireDays`, `BCrypt:WorkFactor`; create `appsettings.Development.json` with SQLite defaults per quickstart.md

**Checkpoint**: `dotnet build` succeeds with zero warnings; CI workflow exists.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain model, application interfaces, infrastructure wiring, and shared UI scaffolding that ALL user stories depend on. No user story work begins until this phase is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Domain Entities & Services

- [x] T006 [P] Create `User` entity (Id, Email, PasswordHash, CreatedAt — no EF attributes) in `src/Payslip4All.Domain/Entities/User.cs`
- [x] T007 [P] Create `Company` entity (Id, Name, Address?, UserId, CreatedAt) in `src/Payslip4All.Domain/Entities/Company.cs`
- [x] T008 [P] Create `Employee` entity (Id, FirstName, LastName, IdNumber, EmployeeNumber, StartDate, Occupation, UifReference?, MonthlyGrossSalary, CompanyId, CreatedAt) in `src/Payslip4All.Domain/Entities/Employee.cs`
- [x] T009 [P] Create `LoanStatus` enum (Active=0, Completed=1) in `src/Payslip4All.Domain/Enums/LoanStatus.cs` and `EmployeeLoan` entity with `IsActiveForPeriod(int month, int year)` and `IncrementTermsCompleted()` domain methods in `src/Payslip4All.Domain/Entities/EmployeeLoan.cs`
- [x] T010 [P] Create `Payslip` entity (all 13 fields per data-model.md including `CompanyId` denormalised FK and `PdfContent byte[]?`) in `src/Payslip4All.Domain/Entities/Payslip.cs`
- [x] T011 [P] Create `PayslipLoanDeduction` snapshot entity (Id, PayslipId, EmployeeLoanId, Description snapshot, Amount snapshot) in `src/Payslip4All.Domain/Entities/PayslipLoanDeduction.cs`
- [x] T012 [P] Create `PayslipCalculator` pure static domain service (`CalculateUifDeduction`, `CalculateNetPay`, `CalculateTotalDeductions` — constants `UifEarningsCeiling = 17712m`, `UifContributionRate = 0.01m`) in `src/Payslip4All.Domain/Services/PayslipCalculator.cs`

### Application Interfaces & DTOs

- [x] T013 [P] Create `IPasswordHasher` (`Hash`, `Verify`) and `IAuthenticationService` (`RegisterAsync`, `LoginAsync`) interfaces in `src/Payslip4All.Application/Interfaces/`
- [x] T014 [P] Create `ICompanyService` (`CreateCompanyAsync`, `GetCompaniesForUserAsync`, `GetCompanyAsync`, `UpdateCompanyAsync`, `DeleteCompanyAsync`) and repository interfaces `IUserRepository`, `ICompanyRepository` in `src/Payslip4All.Application/Interfaces/` and `src/Payslip4All.Application/Interfaces/Repositories/`
- [x] T015 [P] Create `IEmployeeService` (`CreateEmployeeAsync`, `GetEmployeesForCompanyAsync`, `GetEmployeeAsync`, `UpdateEmployeeAsync`, `DeleteEmployeeAsync`), `ILoanService` (`CreateLoanAsync`, `GetLoansForEmployeeAsync`, `GetLoanAsync`, `UpdateLoanAsync`, `DeleteLoanAsync`), and `IEmployeeRepository`, `ILoanRepository` interfaces in `src/Payslip4All.Application/Interfaces/` and `src/Payslip4All.Application/Interfaces/Repositories/`
- [x] T016 [P] Create `IPayslipService` (`PreviewPayslipAsync`, `GeneratePayslipAsync`, `GetPayslipsForEmployeeAsync`, `GetPdfAsync`), `IPdfGenerationService` (`GeneratePayslip`), and `IPayslipRepository` interfaces in `src/Payslip4All.Application/Interfaces/` and `src/Payslip4All.Application/Interfaces/Repositories/`
- [x] T017 [P] Create all DTOs: `RegisterCommand`, `LoginCommand`, `CreateCompanyCommand`, `UpdateCompanyCommand`, `CreateEmployeeCommand`, `UpdateEmployeeCommand`, `CreateLoanCommand`, `UpdateLoanCommand`, `GeneratePayslipCommand`, `PreviewPayslipQuery` and result/response DTOs in `src/Payslip4All.Application/DTOs/Commands/` and `src/Payslip4All.Application/DTOs/Queries/`

### Infrastructure: Persistence

- [x] T018 Create `PayslipDbContext` with `OnModelCreating` fully configured for all 6 entities (all indexes, unique constraints, FK cascade rules, precision settings, concurrency token on `EmployeeLoan.TermsCompleted`, `UQ_Payslips_EmployeeId_PayPeriodMonth_PayPeriodYear` unique index) per data-model.md in `src/Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs`
- [x] T019 Add EF Core `InitialSchema` migration covering all 6 tables (`Users`, `Companies`, `Employees`, `EmployeeLoans`, `Payslips`, `PayslipLoanDeductions`) via `dotnet ef migrations add InitialSchema --project src/Payslip4All.Infrastructure --startup-project src/Payslip4All.Web` and validate migration output in `src/Payslip4All.Infrastructure/Persistence/Migrations/`

### Infrastructure: Auth & Services

- [x] T020 [P] Implement `PasswordHasher` wrapping BCrypt.Net-Next (work factor read from config, default 12) in `src/Payslip4All.Infrastructure/Auth/PasswordHasher.cs`
- [x] T021 [P] Implement `CookieAuthenticationStateProvider` reading `HttpContext.User` via `IHttpContextAccessor` in `src/Payslip4All.Infrastructure/Auth/CookieAuthenticationStateProvider.cs`

### Web: Program.cs & Shared Components

- [x] T022 Configure `src/Payslip4All.Web/Program.cs`: register `AddDbContext<PayslipDbContext>` with startup-time SQLite/MySQL provider switch from `"DatabaseProvider"` config key; register `AddAuthentication().AddCookie(...)` with `HttpOnly`, `SecurePolicy.Always`, `SlidingExpiration`, login path `/Auth/Login`; register all application services and repositories as `Scoped`; register `CookieAuthenticationStateProvider`; call `MigrateAsync()` on startup; add Minimal API `GET /payslips/{payslipId:guid}/download` endpoint with `[Authorize(Roles = "CompanyOwner")]` ownership filter via `IPayslipService.GetPdfAsync`
- [x] T023 [P] Create shared Blazor components: `LoadingSpinner.razor` (params: `bool IsLoading`, `string? Message`), `ErrorBanner.razor` (params: `string? ErrorMessage`, `EventCallback OnDismiss`), `ConfirmDialog.razor` (params: `string Title`, `string Message`, `EventCallback OnConfirm`, `EventCallback OnCancel`), `PageTitle.razor` (params: `string Title`, `string? Subtitle`) in `src/Payslip4All.Web/Shared/`

**Checkpoint**: Foundation complete — `dotnet build` succeeds, migrations exist, Program.cs wires up the app skeleton. User story implementation can now begin.

---

## Phase 3: User Story 1 — Employer Registration & Login (Priority: P1) 🎯 MVP

**Goal**: An employer can register a new account, log in with their credentials, view their personal dashboard, and log out. All protected pages redirect unauthenticated visitors to `/Auth/Login`.

**Independent Test**: Register a new account → log out → log back in → confirm dashboard loads. Second account cannot see first account's data.

### Tests for User Story 1 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation.**

- [x] T024 [P] [US1] Write failing `PayslipCalculatorTests` covering `CalculateUifDeduction` (below ceiling, above ceiling, exactly at ceiling, zero salary throws, negative throws) and `CalculateNetPay`/`CalculateTotalDeductions` in `tests/Payslip4All.Domain.Tests/Services/PayslipCalculatorTests.cs`
- [x] T025 [P] [US1] Write failing `AuthServiceTests` covering `RegisterAsync` (success, duplicate email returns generic error, password hashed before storage), `LoginAsync` (valid credentials, invalid credentials returns generic error, case-insensitive email lookup) in `tests/Payslip4All.Application.Tests/Services/AuthServiceTests.cs`
- [x] T026 [P] [US1] Write failing bUnit `LoginTests` covering form renders email + password inputs, invalid credentials shows error banner, successful login redirects to `/` in `tests/Payslip4All.Web.Tests/Pages/LoginTests.cs`
- [x] T027 [P] [US1] Write failing bUnit `RegisterTests` covering form renders required fields, duplicate email shows generic error (not revealing existence), password mismatch shows error, successful registration redirects to `/` in `tests/Payslip4All.Web.Tests/Pages/RegisterTests.cs`

### Implementation for User Story 1

> **⚠️ C3 DEVIATION — Auth pages are Razor Pages, not Blazor components**: `Login`, `Register`, and `Logout` are implemented as `.cshtml`/`.cshtml.cs` Razor Pages (not `.razor` Blazor components). This is required because `HttpContext.SignInAsync()` / `SignOutAsync()` cannot be called on the Blazor Server SignalR render thread. Navigation Map: `/Auth/Login`, `/Auth/Register`, `/Auth/Logout`. All other pages (12+) remain `.razor` Blazor components. See plan.md Complexity Tracking C3.

- [x] T028 [US1] Implement `AuthenticationService` (register normalises email to lowercase, hashes password via `IPasswordHasher`, checks uniqueness via `IUserRepository`, issues cookie claims on success; login does constant-time BCrypt verify, returns generic error on failure) in `src/Payslip4All.Application/Services/AuthenticationService.cs`
- [x] T029 [US1] Implement `UserRepository` (`GetByEmailAsync`, `AddAsync`, `ExistsByEmailAsync` using `PayslipDbContext`) in `src/Payslip4All.Infrastructure/Repositories/UserRepository.cs`
- [x] T030 [P] [US1] Create `Login.cshtml` / `Login.cshtml.cs` Razor Page [C3 — `.cshtml`, NOT `.razor`] (`LoginModel.OnGetAsync` renders form; `LoginModel.OnPostAsync` calls `IAuthenticationService.LoginAsync`, on success calls `HttpContext.SignInAsync` + redirects to `returnUrl ?? "/"`, on failure sets `ErrorMessage` for generic error banner; routed at `/Auth/Login`) in `src/Payslip4All.Web/Pages/Auth/Login.cshtml`
- [x] T031 [P] [US1] Create `Register.cshtml` / `Register.cshtml.cs` Razor Page [C3 — `.cshtml`, NOT `.razor`] (`RegisterModel.OnGetAsync` renders form; `RegisterModel.OnPostAsync` calls `IAuthenticationService.RegisterAsync`, on success calls `HttpContext.SignInAsync` + redirects to `/`, on failure sets generic `ErrorMessage` — never reveals whether email exists; routed at `/Auth/Register`) in `src/Payslip4All.Web/Pages/Auth/Register.cshtml`
- [x] T032 [US1] Create `Logout.cshtml` / `Logout.cshtml.cs` Razor Page [C3 — `.cshtml`, NOT `.razor`] (`LogoutModel.OnGetAsync` calls `HttpContext.SignOutAsync` and redirects to `/Auth/Login` — no visible UI; requires authentication; routed at `/Auth/Logout`) in `src/Payslip4All.Web/Pages/Auth/Logout.cshtml`

**Checkpoint**: User Story 1 complete — register, login, and logout work end-to-end via Razor Pages at `/Auth/Login`, `/Auth/Register`, `/Auth/Logout`. All `AuthServiceTests`, `PayslipCalculatorTests`, `LoginTests`, and `RegisterTests` pass. Protected pages redirect unauthenticated visitors. (C3 deviation confirmed: auth pages are `.cshtml` Razor Pages, not `.razor` Blazor components.)

---

## Phase 4: User Story 2 — Company Management (Priority: P2)

**Goal**: An authenticated employer can create one or more companies, view their company list on the dashboard, edit company details, and delete companies (with a guard against deletion when employees exist). No other employer can see or mutate their companies.

**Independent Test**: Create company → verify on dashboard → edit name/address → attempt delete with employees (blocked) → delete empty company → confirm removed. Second employer sees zero companies.

### Tests for User Story 2 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation.**

- [x] T033 [P] [US2] Write failing `CompanyServiceTests` covering `CreateCompanyAsync` (success, empty name rejected), `GetCompaniesForUserAsync` (ownership filter — returns only current user's companies), `UpdateCompanyAsync` (success, ownership enforced), `DeleteCompanyAsync` (success when empty, blocked when employees exist) in `tests/Payslip4All.Application.Tests/Services/CompanyServiceTests.cs`
- [x] T034 [P] [US2] Write failing bUnit `CompanyListTests` covering dashboard shows owned companies, empty state renders Add Company CTA, delete blocked when employees shows error banner in `tests/Payslip4All.Web.Tests/Pages/CompanyListTests.cs`

### Implementation for User Story 2

- [x] T035 [US2] Implement `CompanyService` (all CRUD methods; all reads filter by `userId`; `DeleteCompanyAsync` checks `IEmployeeRepository.AnyForCompanyAsync` before delete; returns descriptive error on blocked delete) in `src/Payslip4All.Application/Services/CompanyService.cs`
- [x] T036 [US2] Implement `CompanyRepository` (`GetAllForUserAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` using `PayslipDbContext`; all queries include `WHERE UserId = @userId`) in `src/Payslip4All.Infrastructure/Repositories/CompanyRepository.cs`
- [x] T037 [P] [US2] Create `Dashboard.razor` (`@page "/"`, `[Authorize(Roles = "CompanyOwner")]`, loads companies via `ICompanyService.GetCompaniesForUserAsync`, renders company cards with Name/Address/employee count, empty state CTA, `LoadingSpinner`, `ErrorBanner`, navigation to `/companies/create` and `/companies/{id}`) in `src/Payslip4All.Web/Pages/Dashboard.razor`
- [x] T038 [P] [US2] Create `CreateCompany.razor` (`@page "/companies/create"`, `[Authorize]`, form with Company Name (required, max 200) and Address (optional, max 500), `LoadingSpinner` on submit, `ErrorBanner` on server error, inline validation, redirect to `/` on success, Cancel → `/`) in `src/Payslip4All.Web/Pages/Companies/CreateCompany.razor`
- [x] T039 [P] [US2] Create `EditCompany.razor` (`@page "/companies/{companyId:guid}/edit"`, `[Authorize]`, loads company on init (404 if not owned), pre-populated form, `UpdateCompanyAsync` on submit, redirect to `/companies/{companyId}` on success, Cancel navigates back) in `src/Payslip4All.Web/Pages/Companies/EditCompany.razor`
- [x] T040 [US2] Create `CompanyDetail.razor` (`@page "/companies/{companyId:guid}"`, `[Authorize]`, loads company + employees, NotFound state if not owned, employee table with View links, Add Employee CTA, Edit Company button, Delete Company button with `ConfirmDialog` + error banner on deletion failure due to existing employees, `LoadingSpinner`) in `src/Payslip4All.Web/Pages/Companies/CompanyDetail.razor`

**Checkpoint**: User Story 2 complete — full company lifecycle works. `CompanyServiceTests` and `CompanyListTests` pass.

---

## Phase 5: User Story 3 — Employee Management (Priority: P3)

**Goal**: An authenticated employer adds, edits, and removes employees under a specific company. The EmployeeDetail page also exposes loan management (create, edit when `TermsCompleted == 0`, delete when `TermsCompleted == 0`) and displays payslip history. Deletion is blocked when the employee has existing payslips.

**Independent Test**: Add employee to a company → edit details → add a loan → view employee detail (shows loan under Active Loans) → attempt delete of employee with payslips (blocked) → delete employee with no payslips → confirm removed.

### Tests for User Story 3 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation.**

- [x] T041 [P] [US3] Write failing `EmployeeServiceTests` covering `CreateEmployeeAsync` (success, duplicate employee number in same company rejected), `GetEmployeesForCompanyAsync` (ownership via company chain), `UpdateEmployeeAsync`, `DeleteEmployeeAsync` (success with no payslips, blocked when payslips exist) in `tests/Payslip4All.Application.Tests/Services/EmployeeServiceTests.cs`
- [x] T042 [P] [US3] Write failing `LoanServiceTests` covering `CreateLoanAsync` (success, invalid amounts rejected), `UpdateLoanAsync` (blocked when `TermsCompleted > 0`), `DeleteLoanAsync` (blocked when `TermsCompleted > 0` or `PayslipLoanDeductions` exist) in `tests/Payslip4All.Application.Tests/Services/LoanServiceTests.cs`
- [x] T043 [P] [US3] Write failing bUnit `EmployeeListTests` covering employee table renders in company detail, empty state shows Add Employee CTA, delete blocked with payslips shows error banner in `tests/Payslip4All.Web.Tests/Pages/EmployeeListTests.cs`

### Implementation for User Story 3

- [x] T044 [US3] Implement `EmployeeService` (all CRUD methods; ownership verified through `Company.UserId`; `DeleteEmployeeAsync` checks `IPayslipRepository.AnyForEmployeeAsync` before deleting; duplicate `EmployeeNumber` within same company rejected) in `src/Payslip4All.Application/Services/EmployeeService.cs`
- [x] T045 [US3] Implement `LoanService` (`CreateLoanAsync`, `GetLoansForEmployeeAsync`, `GetLoanAsync`, `UpdateLoanAsync` blocked when `TermsCompleted > 0`, `DeleteLoanAsync` blocked when `TermsCompleted > 0`) in `src/Payslip4All.Application/Services/LoanService.cs`
- [x] T046 [P] [US3] Implement `EmployeeRepository` (`GetAllForCompanyAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `AnyForCompanyAsync`) in `src/Payslip4All.Infrastructure/Repositories/EmployeeRepository.cs`
- [x] T047 [P] [US3] Implement `LoanRepository` (`GetAllForEmployeeAsync`, `GetByIdAsync`, `GetActiveLoansForPeriodAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`) in `src/Payslip4All.Infrastructure/Repositories/LoanRepository.cs`
- [x] T048 [P] [US3] Create `CreateEmployee.razor` (`@page "/companies/{companyId:guid}/employees/create"`, `[Authorize]`, all 8 required fields + optional UIF Reference per ui-contracts.md, `MonthlyGrossSalary` must be > 0, inline field-level validation, redirect to `/companies/{companyId}/employees/{newId}` on success) in `src/Payslip4All.Web/Pages/Employees/CreateEmployee.razor`
- [x] T049 [P] [US3] Create `EditEmployee.razor` (`@page "/companies/{companyId:guid}/employees/{employeeId:guid}/edit"`, `[Authorize]`, loads employee on init, pre-populated form, `UpdateEmployeeAsync` on submit, Cancel → employee detail) in `src/Payslip4All.Web/Pages/Employees/EditEmployee.razor`
- [x] T050 [US3] Create `EmployeeDetail.razor` (`@page "/companies/{companyId:guid}/employees/{employeeId:guid}"`, `[Authorize]`, Employee header section; Payslip History table (reverse-chronological, Month/Year/Gross/Net Pay/Download link); Active Loans table (Description/Terms/Status, Edit + Delete buttons visible only when `TermsCompleted == 0`); Completed Loans read-only section; Generate Payslip button; Delete Employee with `ConfirmDialog` + error banner on blocked delete; `LoadingSpinner`) in `src/Payslip4All.Web/Pages/Employees/EmployeeDetail.razor`
- [x] T051 [P] [US3] Create `CreateLoan.razor` (`@page "/companies/{companyId:guid}/employees/{employeeId:guid}/loans/create"`, `[Authorize]`, Description, Total Loan Amount, Number of Terms (positive integer), Monthly Deduction Amount, Payment Start Date month+year picker — all required with validation, Cancel → employee detail) in `src/Payslip4All.Web/Pages/Employees/Loans/CreateLoan.razor`
- [x] T052 [US3] Create `EditLoan.razor` (`@page "/companies/{companyId:guid}/employees/{employeeId:guid}/loans/{loanId:guid}/edit"`, `[Authorize]`, loads loan on init; if `TermsCompleted > 0`: renders read-only error message "This loan cannot be edited because at least one deduction has already been applied." with no form; otherwise: pre-populated editable form) in `src/Payslip4All.Web/Pages/Employees/Loans/EditLoan.razor`

**Checkpoint**: User Story 3 complete — full employee and loan lifecycle works. `EmployeeServiceTests`, `LoanServiceTests`, and `EmployeeListTests` pass.

---

## Phase 6: User Story 4 — Monthly Payslip Generation & PDF Download (Priority: P4)

**Goal**: An authenticated employer selects an employee, picks a pay period (month + year), reviews the calculated payslip preview (gross, UIF deduction, all active loan deductions, net pay), confirms generation, receives a persisted payslip record with a downloadable PDF. Generation is atomic — no partial saves on failure.

**Independent Test**: Generate payslip for an employee with at least one active loan → verify preview shows correct UIF calculation and loan line items → confirm → download PDF → verify file opens and shows correct values → attempt same month (duplicate warning shown → overwrite → confirm).

### Tests for User Story 4 (REQUIRED — TDD, constitution Principle I)

> **MANDATORY: Write these tests FIRST, confirm they FAIL, then begin implementation.**

- [x] T053 [P] [US4] Write failing `PayslipGenerationServiceTests` covering `PreviewPayslipAsync` (correct UIF, active loans included, completed loans excluded), `GeneratePayslipAsync` (atomic insert + `TermsCompleted` incremented + loan status transitions to `Completed` on final term, duplicate month returns conflict error, zero salary blocked, `DbUpdateConcurrencyException` propagated), `GetPdfAsync` (ownership filter — returns null for wrong user) in `tests/Payslip4All.Application.Tests/Services/PayslipGenerationServiceTests.cs`
- [x] T054 [P] [US4] Write failing bUnit `PayslipGenerateTests` covering Stage 1 period selector renders, Stage 2 preview shows gross/UIF/loan lines/net pay, duplicate warning shows Overwrite + Cancel options, Generating state shows spinner, Error state shows banner (no partial save), NoSalaryError state in `tests/Payslip4All.Web.Tests/Pages/PayslipGenerateTests.cs`
- [x] T055 [P] [US4] Write failing infrastructure integration tests using SQLite in-memory: payslip insert + `TermsCompleted` increment committed in single transaction, rollback on failure, unique constraint on `(EmployeeId, PayPeriodMonth, PayPeriodYear)` enforced, `GetPdfAsync` ownership filter correct in `tests/Payslip4All.Infrastructure.Tests/Repositories/PayslipRepositoryIntegrationTests.cs`

### Implementation for User Story 4

- [x] T056 [US4] Implement `PayslipGenerationService` (`PreviewPayslipAsync` — loads employee + active loans via `IsActiveForPeriod`, calls `PayslipCalculator`, returns preview DTO; `GeneratePayslipAsync` — wraps payslip insert + all `loan.IncrementTermsCompleted()` calls + PDF generation in a single `IDbContextTransaction`, catches `DbUpdateConcurrencyException` for concurrent-generation guard, catches `DbUpdateException` for duplicate-month conflict; `GetPdfAsync` — queries with ownership join through `Employee → Company → UserId`; `GetPayslipsForEmployeeAsync` — reverse-chronological order) in `src/Payslip4All.Application/Services/PayslipGenerationService.cs`
- [x] T057 [US4] Implement `PayslipRepository` (`AddAsync`, `GetByIdAsync`, `GetAllForEmployeeAsync` ordered by year desc/month desc, `GetByEmployeePeriodAsync` for duplicate check, `AnyForEmployeeAsync`, `GetWithOwnershipAsync` for PDF download ownership filter, `UpdateAsync`) in `src/Payslip4All.Infrastructure/Repositories/PayslipRepository.cs`
- [x] T058 [US4] Implement `PdfGenerationService` using QuestPDF (`QuestPDF.Settings.License = LicenseType.Community`; A4 page; sections: company name + address header, employee details block, pay period, earnings table with Gross Earnings, deductions table with UIF Deduction line + one row per `PayslipLoanDeduction` (description + amount), Total Deductions, Net Pay; returns `byte[]`) in `src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs`
- [x] T059 [US4] Create `GeneratePayslip.razor` (`@page "/companies/{companyId:guid}/employees/{employeeId:guid}/payslips/generate"`, `[Authorize(Roles = "CompanyOwner")]`; two-stage workflow: Stage 1 — month (1–12 dropdown) + year input + Preview button; Stage 2 — preview table showing Company Name, Company Address, Employee name, Pay Period, Gross Earnings, each loan deduction line (Description + Amount), UIF Deduction, Total Deductions, Net Pay + Confirm & Generate / Back / Cancel buttons; `DuplicateWarning` state with Overwrite + Cancel; `Generating` spinner state; `Error` banner state; `NoSalaryError` state) in `src/Payslip4All.Web/Pages/Payslips/GeneratePayslip.razor`
- [x] T060 [US4] Verify and finalise the `GET /payslips/{payslipId:guid}/download` Minimal API endpoint in `src/Payslip4All.Web/Program.cs` (added in T022): ownership check via `IPayslipService.GetPdfAsync(payslipId, userId)` returns `Results.NotFound()` for wrong user or missing PDF, `Results.File(pdf, "application/pdf", $"payslip-{payslipId}.pdf")` on success, `Results.Unauthorized()` if claims missing

**Checkpoint**: User Story 4 complete — payslip generation, preview, atomic commit, loan term tracking, and PDF download all work. `PayslipGenerationServiceTests`, `PayslipGenerateTests`, and `PayslipRepositoryIntegrationTests` pass.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, coverage gate, zero-warning gate, and end-to-end quickstart walkthrough.

- [x] T061 [P] Audit DI registrations in `src/Payslip4All.Web/Program.cs` — confirm all services (`AuthenticationService`, `CompanyService`, `EmployeeService`, `LoanService`, `PayslipGenerationService`, `PdfGenerationService`) and all repositories (`UserRepository`, `CompanyRepository`, `EmployeeRepository`, `LoanRepository`, `PayslipRepository`) registered as `Scoped`; `PasswordHasher` registered as `Scoped`; `CookieAuthenticationStateProvider` registered as `Scoped<AuthenticationStateProvider>`
- [x] T062 [P] Run `dotnet test --collect:"XPlat Code Coverage"` and validate coverage ≥ 80% on `Payslip4All.Domain` and `Payslip4All.Application`; add any missing edge-case tests to reach threshold
- [x] T063 [P] Run `dotnet build --warnaserror` across entire solution and resolve any residual build warnings to achieve zero-warning gate per constitution constraint
- [x] T064 Execute full `quickstart.md` walkthrough end-to-end: clone → `dotnet restore` → `dotnet build` → `dotnet run` → Register → Login → Add Company → Add Employee → Add Loan → Generate Payslip → Download PDF → verify all steps complete in < 5 minutes (SC-001), PDF generates in < 3 s (SC-002)
- [x] T065 [P] Benchmark `PdfGenerationService.GeneratePayslipPdfAsync` for a representative payslip (1 loan deduction): run ≥ 3 times via a test or manual timing, assert median elapsed time < 3 000 ms; document result in a comment in `tests/Payslip4All.Application.Tests/` or as an xUnit Fact with a stopwatch assertion (SC-002)
- [x] T066 [P] Seed database with 10 companies × 50 employees (via a test-data seed script or migration); manually or via integration test measure `/` (dashboard) and `/companies/{id}` page-load times; assert each < 2 000 ms; document result (SC-004)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — **BLOCKS all user stories**
- **US1 (Phase 3)**: Depends on Phase 2 — no dependency on US2/US3/US4
- **US2 (Phase 4)**: Depends on Phase 2 — no dependency on US1/US3/US4 (Dashboard requires auth, but auth infrastructure from Phase 2 is sufficient)
- **US3 (Phase 5)**: Depends on Phase 2 — no dependency on US1/US2 (but CompanyRepository must exist for ownership checks)
- **US4 (Phase 6)**: Depends on Phase 2 — logically depends on US1 (auth) + US2 (company) + US3 (employee) being complete for end-to-end use, but can be implemented independently if stubs are used
- **Polish (Phase 7)**: Depends on all user story phases being complete

### User Story Dependencies

| Story | Blocks | Depends On | Notes |
|-------|--------|------------|-------|
| US1 (P1) | Nothing | Phase 2 | Auth infrastructure only |
| US2 (P2) | Nothing | Phase 2 | Company CRUD; Dashboard needs auth cookie but Phase 2 provides it |
| US3 (P3) | Nothing | Phase 2 | Employee + Loan CRUD; ownership chain through Company |
| US4 (P4) | Nothing | Phase 2 | Full generation logic; integration test uses stubs for company/employee |

### Within Each User Story

1. **Tests MUST be written and confirmed FAILING** before implementation (TDD — non-negotiable, constitution Principle I)
2. Domain entities + application interfaces (Phase 2) before any service implementation
3. Application service implementation before infrastructure repository implementation
4. Repositories before Blazor pages (pages inject services, not repos directly)
5. Story considered complete only when ALL tests pass and `dotnet build --warnaserror` is green

### Parallel Opportunities

- All Phase 1 tasks marked `[P]` can run concurrently
- All Phase 2 domain entity tasks (T006–T012) can run concurrently (different files)
- All Phase 2 interface tasks (T013–T017) can run concurrently (different files)
- Once Phase 2 is complete, US1/US2/US3 can proceed in parallel by different developers
- Within each story, all test tasks marked `[P]` can run concurrently
- Within each story, all `[P]`-marked implementation tasks can run concurrently

---

## Parallel Example: User Story 3 (Employee Management)

```bash
# Step 1 — Write ALL failing tests in parallel (different test files):
Task: "Write failing EmployeeServiceTests in tests/Payslip4All.Application.Tests/Services/EmployeeServiceTests.cs"
Task: "Write failing LoanServiceTests in tests/Payslip4All.Application.Tests/Services/LoanServiceTests.cs"
Task: "Write failing EmployeeListTests in tests/Payslip4All.Web.Tests/Pages/EmployeeListTests.cs"

# Step 2 — Confirm all tests FAIL (dotnet test), then implement services in parallel:
Task: "Implement EmployeeService in src/Payslip4All.Application/Services/EmployeeService.cs"
Task: "Implement LoanService in src/Payslip4All.Application/Services/LoanService.cs"

# Step 3 — Implement repos in parallel:
Task: "Implement EmployeeRepository in src/Payslip4All.Infrastructure/Repositories/EmployeeRepository.cs"
Task: "Implement LoanRepository in src/Payslip4All.Infrastructure/Repositories/LoanRepository.cs"

# Step 4 — Build pages in parallel:
Task: "Create CreateEmployee.razor in src/Payslip4All.Web/Pages/Employees/CreateEmployee.razor"
Task: "Create EditEmployee.razor in src/Payslip4All.Web/Pages/Employees/EditEmployee.razor"
Task: "Create CreateLoan.razor in src/Payslip4All.Web/Pages/Employees/Loans/CreateLoan.razor"

# Step 5 — Complete pages with dependencies:
Task: "Create EmployeeDetail.razor in src/Payslip4All.Web/Pages/Employees/EmployeeDetail.razor"
Task: "Create EditLoan.razor in src/Payslip4All.Web/Pages/Employees/Loans/EditLoan.razor"
```

---

## Parallel Example: User Story 4 (Payslip Generation)

```bash
# Step 1 — Write ALL failing tests in parallel:
Task: "Write failing PayslipGenerationServiceTests in tests/Payslip4All.Application.Tests/Services/PayslipGenerationServiceTests.cs"
Task: "Write failing PayslipGenerateTests in tests/Payslip4All.Web.Tests/Pages/PayslipGenerateTests.cs"
Task: "Write failing PayslipRepositoryIntegrationTests in tests/Payslip4All.Infrastructure.Tests/Repositories/PayslipRepositoryIntegrationTests.cs"

# Step 2 — Confirm FAIL, then implement in order:
Task: "Implement PayslipGenerationService in src/Payslip4All.Application/Services/PayslipGenerationService.cs"
# (depends on PayslipCalculator from Phase 2 + repositories from next step)

# Step 3 — Implement infrastructure in parallel:
Task: "Implement PayslipRepository in src/Payslip4All.Infrastructure/Repositories/PayslipRepository.cs"
Task: "Implement PdfGenerationService in src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs"

# Step 4 — Complete UI + endpoint:
Task: "Create GeneratePayslip.razor in src/Payslip4All.Web/Pages/Payslips/GeneratePayslip.razor"
Task: "Finalise /payslips/{id}/download endpoint in src/Payslip4All.Web/Program.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (**CRITICAL** — blocks all stories)
3. Complete Phase 3: User Story 1 (Registration + Login)
4. **STOP and VALIDATE**: Register → Login → Dashboard loads → Logout → tests pass
5. Deploy/demo if ready

### Incremental Delivery

| Sprint | Deliverable | Validates |
|--------|-------------|-----------|
| Sprint 1 | Phase 1 + Phase 2 | Build passes, migrations run, skeleton app starts |
| Sprint 2 | + US1 (Phase 3) | Register, login, logout — auth end-to-end |
| Sprint 3 | + US2 (Phase 4) | Company CRUD — employer can manage companies |
| Sprint 4 | + US3 (Phase 5) | Employee + Loan CRUD — full employee register |
| Sprint 5 | + US4 (Phase 6) | Payslip generation + PDF download — core value delivered |
| Sprint 6 | Polish (Phase 7) | Coverage gate, zero warnings, quickstart walkthrough |

### Parallel Team Strategy

With three developers (after Phase 2 is complete):

- **Developer A**: US1 (Phase 3) — auth pages and service
- **Developer B**: US2 (Phase 4) — company pages and service
- **Developer C**: US3 (Phase 5) — employee/loan pages and services
- All three then converge on US4 (Phase 6) — payslip generation and PDF

---

## Notes

- `[P]` tasks touch different files — safe to run concurrently
- `[USx]` label maps each task to a specific user story for traceability
- **TDD is non-negotiable** (constitution Principle I) — write failing tests before every implementation task
- Commit after each logical task group; ensure `dotnet build --warnaserror` passes before committing
- The `/payslips/{id}/download` endpoint is a Minimal API endpoint in `Program.cs`, not a Blazor page
- `PayslipLoanDeduction` is a **snapshot entity** — values are copied at generation time, not referenced live (supports accurate historical re-render) [C1]
- `TermsCompleted` is a **concurrency token** — `DbUpdateConcurrencyException` on concurrent payslip generation for the same loan must be handled
- Deletion guards: Company (has employees), Employee (has payslips), Loan (`TermsCompleted > 0`) — enforced in Application services, not at DB level only
- All service methods filter by `userId` — ownership enforced at data layer (constitution Principle IV)
- **[C3] Auth pages are Razor Pages** (`.cshtml`/`.cshtml.cs`), NOT Blazor components (`.razor`). Login → `src/Payslip4All.Web/Pages/Auth/Login.cshtml`, Register → `src/Payslip4All.Web/Pages/Auth/Register.cshtml`, Logout → `src/Payslip4All.Web/Pages/Auth/Logout.cshtml`. Navigation Map: `/Auth/Login`, `/Auth/Register`, `/Auth/Logout`. Required because Blazor Server runs over SignalR and cannot call `HttpContext.SignInAsync()` / `SignOutAsync()` from the render thread. Constitution Gate III status: ✅ **(with C3 deviation)**
- **[C4] `SiteAdministrator` role** is seeded in the `InitialSchema` migration (via `ApplicationRoles.cs` constants) but is not yet enforced in any page or service. Enforcement deferred to feature `002-admin-portal`. Seeding now avoids a future migration to add a role that belongs to the identity foundation
