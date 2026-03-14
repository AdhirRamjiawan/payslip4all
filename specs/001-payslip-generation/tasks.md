# Tasks: Payslip Generation System

**Feature Branch**: `001-payslip-generation`  
**Input**: Design documents from `/specs/001-payslip-generation/`  
**Prerequisites**: plan.md ✅ · spec.md ✅ · data-model.md ✅ · contracts/ui-contracts.md ✅ · research.md ✅ · quickstart.md ✅ · constitution.md ✅

**TDD Mandate (Constitution Principle I)**: All test tasks MUST be written and confirmed **failing** before the corresponding implementation tasks begin. Test tasks are **mandatory** — not optional. Coverage threshold ≥ 80% on `Payslip4All.Domain` and `Payslip4All.Application` is a CI gate.

## Format: `[ID] [P?] [Story?] Description with exact file path`

- **[P]**: Task can run in parallel (touches different files, no unresolved dependencies)
- **[US#]**: User story this task belongs to (US1–US4)
- Setup and Foundation phases carry no story label

---

## Phase 1: Project Setup

**Purpose**: Scaffold the full solution structure with all projects, NuGet packages, and core configuration. No business logic.

- [x] T001 Create .NET 8 solution `Payslip4All.sln` with 4 source projects (`Payslip4All.Domain`, `Payslip4All.Application`, `Payslip4All.Infrastructure`, `Payslip4All.Web`) and 4 test projects (`Payslip4All.Domain.Tests`, `Payslip4All.Application.Tests`, `Payslip4All.Infrastructure.Tests`, `Payslip4All.Web.Tests`) under `src/` and `tests/` respectively, adding all project references per Clean Architecture dependency direction
- [x] T002 [P] Add NuGet packages to `src/Payslip4All.Infrastructure/Payslip4All.Infrastructure.csproj`: `Microsoft.EntityFrameworkCore` 8.x, `Microsoft.EntityFrameworkCore.Sqlite` 8.x, `Pomelo.EntityFrameworkCore.MySql` 8.x, `Microsoft.EntityFrameworkCore.Tools` 8.x, `QuestPDF` 2024.x, `BCrypt.Net-Next` 4.x
- [x] T003 [P] Add NuGet packages to `src/Payslip4All.Web/Payslip4All.Web.csproj`: `Microsoft.AspNetCore.Components.Authorization` 8.x
- [x] T004 [P] Add NuGet packages to all 4 test projects (`*.Tests.csproj`): `xunit` 2.x, `Moq` 4.x, `Microsoft.NET.Test.Sdk` 17.x, `coverlet.collector` 6.x, `xunit.runner.visualstudio` 2.x; add `bunit` 1.x to `Payslip4All.Web.Tests` only
- [x] T005 Create `src/Payslip4All.Web/appsettings.json` with keys: `DatabaseProvider` (`"sqlite"`), `ConnectionStrings:DefaultConnection` (`"Data Source=payslip4all.db"`), `ConnectionStrings:MySqlConnection` (`""`), `Auth:Cookie:ExpireDays` (`30`), `BCrypt:WorkFactor` (`12`); create matching `appsettings.Development.json`
- [x] T006 Configure EF Core provider switching in `src/Payslip4All.Web/Program.cs`: read `DatabaseProvider` from config, register `PayslipDbContext` as Scoped with `UseSqlite` or `UseMySql(ServerVersion.AutoDetect)` branch
- [x] T007 Configure ASP.NET Core cookie authentication in `src/Payslip4All.Web/Program.cs`: `AddAuthentication().AddCookie(...)` with `LoginPath = "/login"`, `LogoutPath = "/logout"`, `HttpOnly = true`, `SecurePolicy = Always`, `ExpireTimeSpan` from config; add `app.UseAuthentication()` and `app.UseAuthorization()` middleware
- [x] T008 Register all application services, repositories, and infrastructure implementations in `src/Payslip4All.Web/Program.cs` DI container (placeholder registrations referencing types that will be created in Phase 2–3; keeps compiler-happy stubs acceptable until types exist)
- [x] T009 Set QuestPDF community licence (`QuestPDF.Settings.License = LicenseType.Community`) in `src/Payslip4All.Web/Program.cs` application startup

**Checkpoint**: Solution builds cleanly (`dotnet build`). All 8 projects present. No business logic yet.

---

## Phase 2: Foundation (Blocking Prerequisites)

**Purpose**: All domain entities, DTOs, service interfaces, repository interfaces, DbContext, repository implementations, initial migration, and the base auth state provider. **No user story implementation can begin until this phase is complete.**

⚠️ **CRITICAL BLOCK**: Phases 3–6 depend on every task in this phase being done.

### Domain Entities & Enums

- [x] T010 [P] Create `LoanStatus` enum (`Active = 0`, `Completed = 1`) in `src/Payslip4All.Domain/Enums/LoanStatus.cs`
- [x] T011 [P] Create `User` entity (properties: `Id Guid`, `Email string`, `PasswordHash string`, `CreatedAt DateTimeOffset`; constructor sets `Id = Guid.NewGuid()`, `CreatedAt = DateTimeOffset.UtcNow`) in `src/Payslip4All.Domain/Entities/User.cs` — zero EF Core attributes
- [x] T012 [P] Create `Company` entity (properties: `Id Guid`, `Name string`, `Address string?`, `UserId Guid`, `CreatedAt DateTimeOffset`) in `src/Payslip4All.Domain/Entities/Company.cs` — zero EF Core attributes
- [x] T013 [P] Create `Employee` entity (properties: `Id Guid`, `FirstName string`, `LastName string`, `IdNumber string`, `EmployeeNumber string`, `StartDate DateOnly`, `Occupation string`, `UifReference string?`, `MonthlyGrossSalary decimal`, `CompanyId Guid`, `CreatedAt DateTimeOffset`) in `src/Payslip4All.Domain/Entities/Employee.cs` — zero EF Core attributes
- [x] T014 [P] Create `EmployeeLoan` entity in `src/Payslip4All.Domain/Entities/EmployeeLoan.cs` with properties (`Id Guid`, `Description string`, `TotalLoanAmount decimal`, `NumberOfTerms int`, `MonthlyDeductionAmount decimal`, `PaymentStartDate DateOnly`, `TermsCompleted int`, `Status LoanStatus`, `EmployeeId Guid`, `CreatedAt DateTimeOffset`) and domain methods: `IncrementTermsCompleted()` (increments counter, transitions to `Completed` when `TermsCompleted == NumberOfTerms`, throws if already Completed) and `IsActiveForPeriod(int month, int year) bool` — zero EF Core attributes
- [x] T015 [P] Create `Payslip` entity (properties: `Id Guid`, `PayPeriodMonth int`, `PayPeriodYear int`, `GrossEarnings decimal`, `UifDeduction decimal`, `TotalLoanDeductions decimal`, `TotalDeductions decimal`, `NetPay decimal`, `PdfContent byte[]?`, `EmployeeId Guid`, `GeneratedAt DateTimeOffset`) in `src/Payslip4All.Domain/Entities/Payslip.cs` — zero EF Core attributes
- [x] T016 [P] Create `PayslipLoanDeduction` entity (properties: `Id Guid`, `PayslipId Guid`, `EmployeeLoanId Guid`, `Description string`, `Amount decimal`) in `src/Payslip4All.Domain/Entities/PayslipLoanDeduction.cs` — zero EF Core attributes

### Application DTOs

- [x] T017 [P] Create Auth DTOs in `src/Payslip4All.Application/DTOs/Auth/`: `RegisterCommand.cs` (Email, Password, ConfirmPassword), `LoginCommand.cs` (Email, Password), `AuthResult.cs` (Success bool, ErrorMessage string?, UserId Guid?, UserEmail string?)
- [x] T018 [P] Create Company DTOs in `src/Payslip4All.Application/DTOs/Company/`: `CompanyDto.cs` (Id, Name, Address, UserId, CreatedAt, EmployeeCount int), `CreateCompanyCommand.cs` (Name, Address?, UserId), `UpdateCompanyCommand.cs` (Id, Name, Address?, UserId)
- [x] T019 [P] Create Employee DTOs in `src/Payslip4All.Application/DTOs/Employee/`: `EmployeeDto.cs` (all employee fields + CompanyId), `CreateEmployeeCommand.cs`, `UpdateEmployeeCommand.cs` (both include all employee input fields + CompanyId + UserId ownership check field)
- [x] T020 [P] Create Loan DTOs in `src/Payslip4All.Application/DTOs/Loan/`: `LoanDto.cs` (all loan fields including TermsCompleted, Status), `CreateLoanCommand.cs` (Description, TotalLoanAmount, NumberOfTerms, MonthlyDeductionAmount, PaymentStartDate, EmployeeId, UserId), `UpdateLoanCommand.cs` (same fields + LoanId)
- [x] T021 [P] Create Payslip DTOs in `src/Payslip4All.Application/DTOs/Payslip/`: `PayslipDto.cs` (all payslip fields + LoanDeductions list), `GeneratePayslipCommand.cs` (EmployeeId, PayPeriodMonth, PayPeriodYear, OverwriteExisting bool, UserId), `PreviewPayslipQuery.cs` (EmployeeId, PayPeriodMonth, PayPeriodYear, UserId), `PayslipResult.cs` (Success bool, PayslipDto?, ErrorMessage string?, IsDuplicate bool)

### Application Service & Repository Interfaces

- [x] T022 [P] Create `IPasswordHasher` interface (methods: `Hash(string password) string`, `Verify(string password, string hash) bool`) in `src/Payslip4All.Application/Interfaces/IPasswordHasher.cs`
- [x] T023 [P] Create `IAuthenticationService` interface (methods: `RegisterAsync(RegisterCommand) Task<AuthResult>`, `LoginAsync(LoginCommand) Task<AuthResult>`) in `src/Payslip4All.Application/Interfaces/IAuthenticationService.cs`
- [x] T024 [P] Create `ICompanyService` interface (methods: `GetCompaniesForUserAsync(Guid userId) Task<IReadOnlyList<CompanyDto>>`, `GetCompanyByIdAsync(Guid id, Guid userId) Task<CompanyDto?>`, `CreateCompanyAsync(CreateCompanyCommand) Task<CompanyDto>`, `UpdateCompanyAsync(UpdateCompanyCommand) Task<CompanyDto?>`, `DeleteCompanyAsync(Guid id, Guid userId) Task<bool>`) in `src/Payslip4All.Application/Interfaces/ICompanyService.cs`
- [x] T025 [P] Create `IEmployeeService` interface (methods: `GetEmployeesForCompanyAsync(Guid companyId, Guid userId) Task<IReadOnlyList<EmployeeDto>>`, `GetEmployeeByIdAsync(Guid id, Guid userId) Task<EmployeeDto?>`, `CreateEmployeeAsync(CreateEmployeeCommand) Task<EmployeeDto>`, `UpdateEmployeeAsync(UpdateEmployeeCommand) Task<EmployeeDto?>`, `DeleteEmployeeAsync(Guid id, Guid userId) Task<bool>`) in `src/Payslip4All.Application/Interfaces/IEmployeeService.cs`
- [x] T026 [P] Create `ILoanService` interface (methods: `GetLoansForEmployeeAsync(Guid employeeId, Guid userId) Task<IReadOnlyList<LoanDto>>`, `GetLoanByIdAsync(Guid id, Guid userId) Task<LoanDto?>`, `CreateLoanAsync(CreateLoanCommand) Task<LoanDto>`, `UpdateLoanAsync(UpdateLoanCommand) Task<LoanDto?>`, `DeleteLoanAsync(Guid id, Guid userId) Task<bool>`) in `src/Payslip4All.Application/Interfaces/ILoanService.cs`
- [x] T027 [P] Create `IPayslipService` interface (methods: `PreviewPayslipAsync(PreviewPayslipQuery) Task<PayslipResult>`, `GeneratePayslipAsync(GeneratePayslipCommand) Task<PayslipResult>`, `GetPayslipsForEmployeeAsync(Guid employeeId, Guid userId) Task<IReadOnlyList<PayslipDto>>`, `GetPdfAsync(Guid payslipId, Guid userId) Task<byte[]?>`) in `src/Payslip4All.Application/Interfaces/IPayslipService.cs`
- [x] T028 [P] Create `IPdfGenerationService` interface (method: `GeneratePayslip(PayslipDocument document) byte[]`; define `PayslipDocument` record with CompanyName, CompanyAddress, EmployeeName, EmployeeNumber, Occupation, PayPeriod, GrossEarnings, UifDeduction, LoanDeductions, TotalDeductions, NetPay) in `src/Payslip4All.Application/Interfaces/IPdfGenerationService.cs`
- [x] T029 [P] Create `IUserRepository` interface (methods: `GetByEmailAsync(string email) Task<User?>`, `AddAsync(User user) Task`, `ExistsAsync(string email) Task<bool>`) in `src/Payslip4All.Application/Interfaces/Repositories/IUserRepository.cs`
- [x] T030 [P] Create `ICompanyRepository` interface (methods: `GetAllByUserIdAsync(Guid userId) Task<IReadOnlyList<Company>>`, `GetByIdAsync(Guid id, Guid userId) Task<Company?>`, `GetByIdWithEmployeesAsync(Guid id, Guid userId) Task<Company?>`, `AddAsync(Company company) Task`, `UpdateAsync(Company company) Task`, `DeleteAsync(Company company) Task`, `HasEmployeesAsync(Guid id) Task<bool>`) in `src/Payslip4All.Application/Interfaces/Repositories/ICompanyRepository.cs`
- [x] T031 [P] Create `IEmployeeRepository` interface (methods: `GetAllByCompanyIdAsync(Guid companyId, Guid userId) Task<IReadOnlyList<Employee>>`, `GetByIdAsync(Guid id, Guid userId) Task<Employee?>`, `GetByIdWithLoansAsync(Guid id, Guid userId) Task<Employee?>`, `AddAsync(Employee emp) Task`, `UpdateAsync(Employee emp) Task`, `DeleteAsync(Employee emp) Task`, `HasPayslipsAsync(Guid id) Task<bool>`) in `src/Payslip4All.Application/Interfaces/Repositories/IEmployeeRepository.cs`
- [x] T032 [P] Create `ILoanRepository` interface (methods: `GetAllByEmployeeIdAsync(Guid employeeId, Guid userId) Task<IReadOnlyList<EmployeeLoan>>`, `GetByIdAsync(Guid id, Guid userId) Task<EmployeeLoan?>`, `AddAsync(EmployeeLoan loan) Task`, `UpdateAsync(EmployeeLoan loan) Task`, `DeleteAsync(EmployeeLoan loan) Task`) in `src/Payslip4All.Application/Interfaces/Repositories/ILoanRepository.cs`
- [x] T033 [P] Create `IPayslipRepository` interface (methods: `GetAllByEmployeeIdAsync(Guid employeeId, Guid userId) Task<IReadOnlyList<Payslip>>`, `GetByIdAsync(Guid id, Guid userId) Task<Payslip?>`, `ExistsAsync(Guid employeeId, int month, int year) Task<bool>`, `AddAsync(Payslip payslip) Task`, `DeleteAsync(Payslip payslip) Task`) in `src/Payslip4All.Application/Interfaces/Repositories/IPayslipRepository.cs`

### Infrastructure: DbContext, Repositories, Migration

- [x] T034 Create `PayslipDbContext` in `src/Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs` with `DbSet<>` properties for all 6 entities and full `OnModelCreating` configuration: all field constraints (MaxLength, HasPrecision, IsRequired), all FK/navigation relationships with explicit `OnDelete` behaviours, all indexes (unique and non-unique) including `UQ_Payslips_EmployeeId_PayPeriodMonth_PayPeriodYear`, and `IsConcurrencyToken()` on `EmployeeLoan.TermsCompleted`
- [x] T035 [P] Implement `UserRepository` in `src/Payslip4All.Infrastructure/Persistence/Repositories/UserRepository.cs` implementing `IUserRepository` using `PayslipDbContext`; email comparisons use `ToLower()`
- [x] T036 [P] Implement `CompanyRepository` in `src/Payslip4All.Infrastructure/Persistence/Repositories/CompanyRepository.cs` implementing `ICompanyRepository`; all queries include `.Where(c => c.UserId == userId)` ownership filter
- [x] T037 [P] Implement `EmployeeRepository` in `src/Payslip4All.Infrastructure/Persistence/Repositories/EmployeeRepository.cs` implementing `IEmployeeRepository`; all queries include ownership filter via company join (`.Where(e => e.Company.UserId == userId)`)
- [x] T038 [P] Implement `LoanRepository` in `src/Payslip4All.Infrastructure/Persistence/Repositories/LoanRepository.cs` implementing `ILoanRepository`; all queries include ownership filter via employee-company join
- [x] T039 [P] Implement `PayslipRepository` in `src/Payslip4All.Infrastructure/Persistence/Repositories/PayslipRepository.cs` implementing `IPayslipRepository`; all queries include ownership filter via employee-company join
- [x] T040 Create `PasswordHasher` in `src/Payslip4All.Infrastructure/Auth/PasswordHasher.cs` implementing `IPasswordHasher` using `BCrypt.Net.BCrypt.HashPassword` (work factor from config) and `BCrypt.Net.BCrypt.Verify`
- [x] T041 Run `dotnet ef migrations add InitialSchema --project src/Payslip4All.Infrastructure --startup-project src/Payslip4All.Web` to generate the `InitialSchema` migration in `src/Payslip4All.Infrastructure/Persistence/Migrations/`

### Base Authentication State Provider

- [x] T042 Create base `CookieAuthenticationStateProvider` in `src/Payslip4All.Infrastructure/Auth/CookieAuthenticationStateProvider.cs` implementing `AuthenticationStateProvider`: reads `ClaimsPrincipal` from `IHttpContextAccessor.HttpContext?.User`; returns `AuthenticationState` with anonymous identity when HttpContext is null; includes `public void NotifyAuthenticationStateChanged(ClaimsPrincipal user)` helper to trigger Blazor auth state refresh

**Checkpoint**: `dotnet build` passes. All interfaces, entities, DbContext, repositories, and base auth provider exist. DB migration file generated. User story implementation can now begin.

---

## Phase 3: User Story 1 — Employer Registration & Login (Priority: P1) 🎯 MVP

**Goal**: An employer can register an account, log in, and be redirected to an authenticated dashboard. An unauthenticated visitor is redirected to `/login`.

**Independent Test**: Register a new account → log out → log back in → dashboard loads. Second account cannot access first account's session.

### Tests for US1 (Constitution Principle I — TDD: write first, confirm failing, then implement)

- [x] T04X [P] [US1] Write failing xUnit tests for `AuthenticationService` in `tests/Payslip4All.Application.Tests/Services/AuthenticationServiceTests.cs`: registration with unique email succeeds and returns `AuthResult.Success = true`; registration with duplicate email returns generic error without revealing existence; login with correct credentials returns success; login with wrong password returns generic error; `PasswordHash` stored in DB is never the plain-text password
- [x] T04X [P] [US1] Write failing bUnit tests for `Register.razor` in `tests/Payslip4All.Web.Tests/Pages/RegisterTests.cs`: form renders email + password + confirm-password inputs; valid submission calls `IAuthenticationService.RegisterAsync`; mismatched password shows validation error; service error shows generic error banner; success redirects to `/`
- [x] T04X [P] [US1] Write failing bUnit tests for `Login.razor` in `tests/Payslip4All.Web.Tests/Pages/LoginTests.cs`: form renders email + password inputs; valid submission calls `IAuthenticationService.LoginAsync`; failed login shows generic error banner (never field-specific); success redirects to `/`

### Implementation for US1

- [x] T04X [US1] Implement `AuthenticationService` in `src/Payslip4All.Application/Services/AuthenticationService.cs` implementing `IAuthenticationService`: `RegisterAsync` normalises email to lowercase, checks uniqueness via `IUserRepository`, hashes password via `IPasswordHasher`, persists `User`, returns `AuthResult`; `LoginAsync` fetches user by email, verifies hash, returns `AuthResult` with `UserId` and `UserEmail`; both methods return generic error messages on failure
- [x] T04X [US1] Implement `Register.razor` page in `src/Payslip4All.Web/Pages/Auth/Register.razor` at route `/register`: anonymous-only; `EditForm` with `DataAnnotationsValidator`; calls `IAuthenticationService.RegisterAsync`; on success signs in via `HttpContext.SignInAsync` with `ClaimTypes.NameIdentifier`, `ClaimTypes.Email`, `ClaimTypes.Role = "CompanyOwner"` claims then redirects to `/`; shows `<ErrorBanner>` on failure; shows `<LoadingSpinner>` during submit
- [x] T04X [US1] Implement `Login.razor` page in `src/Payslip4All.Web/Pages/Auth/Login.razor` at route `/login`: anonymous-only; `EditForm`; calls `IAuthenticationService.LoginAsync`; on success signs in via `HttpContext.SignInAsync` then redirects to `/` (or `returnUrl`); shows generic `<ErrorBanner>` on failure; shows `<LoadingSpinner>` during submit
- [x] T04X [US1] Implement `Logout.razor` page in `src/Payslip4All.Web/Pages/Auth/Logout.razor` at route `/logout`: requires authentication; immediately calls `HttpContext.SignOutAsync` then redirects to `/login`; no visible UI rendered

**Checkpoint**: Register → Login → Logout flow is fully functional and all US1 tests pass.

---

## Phase 4: User Story 2 — Company Management (Priority: P2)

**Goal**: An authenticated employer can create, view, edit, and delete companies. Ownership isolation ensures no employer sees another's companies.

**Independent Test**: Create a company → verify on dashboard → edit name → delete company (no employees) → confirm deletion succeeds. Attempt to delete a company with employees → confirm system blocks it.

### Tests for US2 (TDD: write first, confirm failing, then implement)

- [x] T05X [P] [US2] Write failing xUnit tests for `CompanyService` in `tests/Payslip4All.Application.Tests/Services/CompanyServiceTests.cs`: `GetCompaniesForUserAsync` returns only companies belonging to requesting user; `CreateCompanyAsync` with valid name persists and returns DTO; `CreateCompanyAsync` with empty name throws validation error; `UpdateCompanyAsync` with wrong userId returns null (ownership); `DeleteCompanyAsync` with employees present returns false and does not delete; `DeleteCompanyAsync` with no employees returns true
- [x] T05X [P] [US2] Write failing bUnit tests for `Dashboard.razor` in `tests/Payslip4All.Web.Tests/Pages/DashboardTests.cs`: loading spinner shown initially; company cards render name and address; empty state shows Add Company CTA; error state shows error banner
- [x] T05X [P] [US2] Write failing bUnit tests for `CompanyDetail.razor` in `tests/Payslip4All.Web.Tests/Pages/CompanyDetailTests.cs`: renders company name; employee table shown when employees exist; empty employee state shown when none; delete button with no employees navigates away on confirm; delete button with employees shows error banner

### Implementation for US2

- [x] T05X [US2] Implement `CompanyService` in `src/Payslip4All.Application/Services/CompanyService.cs` implementing `ICompanyService`: all methods extract `userId` from parameter (never trusts ID alone); `GetCompaniesForUserAsync` includes employee count; `DeleteCompanyAsync` checks `ICompanyRepository.HasEmployeesAsync` before deleting; maps domain entities to DTOs
- [x] T05X [US2] Implement `Dashboard.razor` page in `src/Payslip4All.Web/Pages/Dashboard.razor` at route `/` (`@page "/"`): `[Authorize(Roles = "CompanyOwner")]`; on `OnInitializedAsync` loads companies via `ICompanyService`; renders loading/empty/error/data states; company cards show Name, Address, employee count, Edit and View buttons
- [x] T05X [P] [US2] Implement `CreateCompany.razor` page in `src/Payslip4All.Web/Pages/Companies/CreateCompany.razor` at route `/companies/create`: `[Authorize(Roles = "CompanyOwner")]`; `EditForm` with Name (required) and Address (optional); on submit calls `ICompanyService.CreateCompanyAsync`; redirects to `/` on success; shows `<ErrorBanner>` on failure
- [x] T05X [P] [US2] Implement `EditCompany.razor` page in `src/Payslip4All.Web/Pages/Companies/EditCompany.razor` at route `/companies/{companyId:guid}/edit`: `[Authorize(Roles = "CompanyOwner")]`; on load fetches company (shows not-found if null); pre-populates form; on submit calls `ICompanyService.UpdateCompanyAsync`; redirects to `/companies/{companyId}` on success
- [x] T05X [US2] Implement `CompanyDetail.razor` page in `src/Payslip4All.Web/Pages/Companies/CompanyDetail.razor` at route `/companies/{companyId:guid}`: `[Authorize(Roles = "CompanyOwner")]`; loads company + employees; renders employee table with View Employee and Add Employee links; Edit Company and Delete Company buttons; delete uses `<ConfirmDialog>`; blocks delete if employees exist with error banner

**Checkpoint**: Full company CRUD works. All US2 tests pass. Dashboard loads only the authenticated user's companies.

---

## Phase 5: User Story 3 — Employee Management (Priority: P3)

**Goal**: An authenticated employer can add, view, edit, and delete employees under a company. Employer can add loans to employees and manage them.

**Independent Test**: Add an employee → edit their salary → add a loan → verify loan appears on employee detail. Attempt to delete an employee with payslips → confirm system blocks it. Attempt to edit a loan with `TermsCompleted > 0` → confirm system blocks form.

### Tests for US3 (TDD: write first, confirm failing, then implement)

- [x] T05X [P] [US3] Write failing xUnit tests for `EmployeeService` in `tests/Payslip4All.Application.Tests/Services/EmployeeServiceTests.cs`: `CreateEmployeeAsync` with valid data persists employee; `CreateEmployeeAsync` with `MonthlyGrossSalary <= 0` throws validation error; `GetEmployeesForCompanyAsync` returns only employees for the specified company owned by userId; `DeleteEmployeeAsync` with payslips present returns false; ownership filter returns null for wrong userId
- [x] T05X [P] [US3] Write failing xUnit tests for `LoanService` in `tests/Payslip4All.Application.Tests/Services/LoanServiceTests.cs`: `CreateLoanAsync` with valid data persists loan with `Status = Active` and `TermsCompleted = 0`; `UpdateLoanAsync` when `TermsCompleted > 0` returns null (locked); `DeleteLoanAsync` when `TermsCompleted > 0` returns false (locked); `GetLoansForEmployeeAsync` returns all loans for employee with ownership check
- [x] T06X [P] [US3] Write failing bUnit tests for `EmployeeDetail.razor` in `tests/Payslip4All.Web.Tests/Pages/EmployeeDetailTests.cs`: renders employee header with name and salary; active loans table renders with edit/delete buttons when `TermsCompleted == 0`; edit/delete buttons absent when `TermsCompleted > 0`; payslip history table rendered in reverse-chronological order; delete employee blocked when payslips exist

### Implementation for US3

- [x] T06X [US3] Implement `EmployeeService` in `src/Payslip4All.Application/Services/EmployeeService.cs` implementing `IEmployeeService`: validates `MonthlyGrossSalary > 0`; `DeleteEmployeeAsync` checks `IEmployeeRepository.HasPayslipsAsync` before deleting; maps to DTOs; all queries enforce userId ownership
- [x] T06X [US3] Implement `LoanService` in `src/Payslip4All.Application/Services/LoanService.cs` implementing `ILoanService`: `CreateLoanAsync` sets `Status = Active`, `TermsCompleted = 0`; `UpdateLoanAsync` returns null if `loan.TermsCompleted > 0`; `DeleteLoanAsync` returns false if `loan.TermsCompleted > 0`; maps to DTOs; all queries enforce ownership
- [x] T06X [P] [US3] Implement `CreateEmployee.razor` page in `src/Payslip4All.Web/Pages/Employees/CreateEmployee.razor` at route `/companies/{companyId:guid}/employees/create`: `[Authorize(Roles = "CompanyOwner")]`; `EditForm` with all required fields per FR-016; validates `MonthlyGrossSalary > 0` client-side; on submit calls `IEmployeeService.CreateEmployeeAsync`; redirects to `/companies/{companyId}/employees/{newId}` on success
- [x] T06X [P] [US3] Implement `EditEmployee.razor` page in `src/Payslip4All.Web/Pages/Employees/EditEmployee.razor` at route `/companies/{companyId:guid}/employees/{employeeId:guid}/edit`: `[Authorize(Roles = "CompanyOwner")]`; pre-populates form from loaded employee; on submit calls `IEmployeeService.UpdateEmployeeAsync`; redirects to employee detail on success
- [x] T06X [US3] Implement `EmployeeDetail.razor` page in `src/Payslip4All.Web/Pages/Employees/EmployeeDetail.razor` at route `/companies/{companyId:guid}/employees/{employeeId:guid}`: `[Authorize(Roles = "CompanyOwner")]`; loads employee + payslip history (reverse-chronological) + active loans + completed loans; Edit Employee, Delete Employee (guarded by payslip check with `<ConfirmDialog>`), Add Loan, Generate Payslip navigation links; loan Edit/Delete buttons only shown when `loan.TermsCompleted == 0`; PDF download link per payslip row
- [x] T06X [P] [US3] Implement `CreateLoan.razor` page in `src/Payslip4All.Web/Pages/Employees/Loans/CreateLoan.razor` at route `/companies/{companyId:guid}/employees/{employeeId:guid}/loans/create`: `[Authorize(Roles = "CompanyOwner")]`; `EditForm` with Description, TotalLoanAmount, NumberOfTerms, MonthlyDeductionAmount, PaymentStartDate; on submit calls `ILoanService.CreateLoanAsync`; redirects to employee detail on success
- [x] T06X [P] [US3] Implement `EditLoan.razor` page in `src/Payslip4All.Web/Pages/Employees/Loans/EditLoan.razor` at route `/companies/{companyId:guid}/employees/{employeeId:guid}/loans/{loanId:guid}/edit`: `[Authorize(Roles = "CompanyOwner")]`; on load fetches loan; if `loan.TermsCompleted > 0` displays "This loan cannot be edited because at least one deduction has already been applied." with no form; otherwise shows pre-populated form; on submit calls `ILoanService.UpdateLoanAsync`

**Checkpoint**: Full employee CRUD and loan management works. All US3 tests pass. Loan immutability guards enforced.

---

## Phase 6: User Story 4 — Monthly Payslip Generation & PDF Download (Priority: P4)

**Goal**: An authenticated employer selects a pay period, previews calculated payslip values (UIF, loan deductions, net pay), confirms generation, and downloads a PDF. Generation is atomic — either all changes commit or none do.

**Independent Test**: For an employee with salary R25,000 and one active loan (R500/month): preview shows `UIF = R177.12`, `Net Pay = R24,322.88`; confirm generates payslip record + increments loan `TermsCompleted`; PDF downloads as valid file. Attempt duplicate generation → duplicate warning shown. At final loan term, loan transitions to `Completed` after generation.

### Tests for US4 (TDD: write first, confirm failing, then implement)

- [x] T06X [P] [US4] Write failing xUnit tests for `PayslipCalculator` in `tests/Payslip4All.Domain.Tests/Services/PayslipCalculatorTests.cs`: `CalculateUifDeduction(25000)` returns `177.12m`; `CalculateUifDeduction(17712)` returns `177.12m` (at ceiling); `CalculateUifDeduction(10000)` returns `100.00m` (below ceiling); `CalculateUifDeduction(0)` throws `ArgumentException`; `CalculateUifDeduction(-1)` throws `ArgumentException`; `CalculateNetPay` correctly subtracts UIF + all loan deductions from gross; `CalculateTotalDeductions` correctly sums UIF + loan deductions
- [x] T06X [P] [US4] Write failing xUnit tests for `EmployeeLoan.IncrementTermsCompleted()` domain method in `tests/Payslip4All.Domain.Tests/Entities/EmployeeLoanTests.cs`: incrementing below `NumberOfTerms` keeps `Status = Active`; incrementing at final term transitions `Status = Completed`; calling `IncrementTermsCompleted` on a `Completed` loan throws `InvalidOperationException`; `IsActiveForPeriod` returns true for active loan with valid month/year; `IsActiveForPeriod` returns false for completed loan; `IsActiveForPeriod` returns false when `periodDate < PaymentStartDate`
- [x] T07X [P] [US4] Write failing xUnit tests for `PayslipGenerationService` in `tests/Payslip4All.Application.Tests/Services/PayslipGenerationServiceTests.cs`: `GeneratePayslipAsync` creates `Payslip` record and increments `TermsCompleted` on each active loan in the same transaction; duplicate month/year returns `PayslipResult.IsDuplicate = true` without `OverwriteExisting`; with `OverwriteExisting = true` overwrites; employee with `MonthlyGrossSalary == 0` returns error; loan at final term transitions to `Completed` after generation; `PreviewPayslipAsync` returns calculated values without persisting anything
- [x] T07X [P] [US4] Write failing bUnit tests for `GeneratePayslip.razor` in `tests/Payslip4All.Web.Tests/Pages/GeneratePayslipTests.cs`: Stage 1 form renders month/year inputs; Preview button calls `IPayslipService.PreviewPayslipAsync`; Stage 2 shows calculated gross, UIF, loan deductions, net pay; Confirm button calls `IPayslipService.GeneratePayslipAsync`; duplicate warning shows `<ConfirmDialog>` with overwrite option; generation error shows `<ErrorBanner>`

### Implementation for US4

- [x] T07X [US4] Implement `PayslipCalculator` static class in `src/Payslip4All.Domain/Services/PayslipCalculator.cs`: constants `UifEarningsCeiling = 17_712.00m` and `UifContributionRate = 0.01m`; `CalculateUifDeduction(decimal grossSalary) decimal` — `Math.Round(Math.Min(grossSalary, UifEarningsCeiling) * UifContributionRate, 2, MidpointRounding.AwayFromZero)`, throws `ArgumentException` if `grossSalary <= 0`; `CalculateTotalDeductions(decimal uifDeduction, IEnumerable<decimal> loanDeductions) decimal`; `CalculateNetPay(decimal grossEarnings, decimal uifDeduction, IEnumerable<decimal> loanDeductions) decimal`
- [x] T07X [US4] Implement `PayslipGenerationService` in `src/Payslip4All.Application/Services/PayslipGenerationService.cs` implementing `IPayslipService`: `PreviewPayslipAsync` loads employee + active loans via repositories, uses `PayslipCalculator` to compute values, returns `PayslipResult` without writing to DB; `GeneratePayslipAsync` wraps the full operation in an `IDbContextTransaction`: (1) check duplicate via `IPayslipRepository.ExistsAsync` — return `IsDuplicate` if exists and `!OverwriteExisting`, delete old if `OverwriteExisting`; (2) load employee with active loans; (3) calculate values; (4) create and add `Payslip` entity; (5) create `PayslipLoanDeduction` snapshots; (6) call `loan.IncrementTermsCompleted()` on each active loan; (7) call `IPdfGenerationService.GeneratePayslip(...)` and store bytes; (8) `SaveChangesAsync` + `CommitAsync`; rollback on any exception; `GetPayslipsForEmployeeAsync` returns payslips ordered by year desc, month desc; `GetPdfAsync` returns `PdfContent` bytes
- [x] T07X [US4] Implement `PdfGenerationService` in `src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs` implementing `IPdfGenerationService`: uses QuestPDF fluent API to generate an A4 payslip document with company name + address header, employee details section, pay period, gross earnings row, UIF deduction row, each loan deduction row (description + amount), total deductions, net pay; returns `byte[]` from `Document.Create(...).GeneratePdf()`
- [x] T07X [US4] Implement `GeneratePayslip.razor` page in `src/Payslip4All.Web/Pages/Payslips/GeneratePayslip.razor` at route `/companies/{companyId:guid}/employees/{employeeId:guid}/payslips/generate`: `[Authorize(Roles = "CompanyOwner")]`; two-stage workflow: Stage 1 (SelectPeriod) — month dropdown + year input + `[Preview]` button; Stage 2 (PreviewReady) — renders all calculated values from `IPayslipService.PreviewPayslipAsync`, `[Confirm & Generate]` button, `[Back]` button; `DuplicateWarning` state shows `<ConfirmDialog>` with overwrite option; `Generating` state shows `<LoadingSpinner>`; `Error` state shows `<ErrorBanner>`; `NoSalaryError` state shown when employee has no salary; on success navigates to employee detail
- [x] T07X [US4] Add PDF download handler in `src/Payslip4All.Web/Pages/Payslips/`: implement a minimal API endpoint or `@page` component that streams payslip PDF bytes as `application/pdf` response when a valid `payslipId` and authenticated `userId` are provided; wire Download link on `EmployeeDetail.razor` to this endpoint

**Checkpoint**: End-to-end payslip generation and PDF download works. All US4 tests pass. Atomic transaction verified — no partial saves on error.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Shared UI components used across all stories, startup migration, navigation/layout, and final integration smoke test.

- [x] T07X [P] Create `LoadingSpinner.razor` shared component in `src/Payslip4All.Web/Shared/LoadingSpinner.razor`: parameters `bool IsLoading`, `string? Message`; renders Bootstrap spinner when `IsLoading = true`
- [x] T07X [P] Create `ErrorBanner.razor` shared component in `src/Payslip4All.Web/Shared/ErrorBanner.razor`: parameters `string? ErrorMessage`, `EventCallback OnDismiss`; renders dismissible Bootstrap alert when `ErrorMessage` is non-null
- [x] T07X [P] Create `ConfirmDialog.razor` shared component in `src/Payslip4All.Web/Shared/ConfirmDialog.razor`: parameters `string Title`, `string Message`, `EventCallback OnConfirm`, `EventCallback OnCancel`; Bootstrap modal dialog
- [x] T08X [P] Create `PageTitle.razor` shared component in `src/Payslip4All.Web/Shared/PageTitle.razor`: parameters `string Title`, `string? Subtitle`; renders consistent Bootstrap heading with optional subtitle
- [x] T08X Update `src/Payslip4All.Web/Shared/MainLayout.razor` with Bootstrap 5 navigation: left sidebar or top navbar with links to Dashboard and Logout; hide nav for anonymous routes (`/login`, `/register`); include `<AuthorizeView>` for conditional nav items
- [x] T08X Add `MigrateAsync()` call on startup in `src/Payslip4All.Web/Program.cs`: after `app.Build()`, resolve `PayslipDbContext` from DI scope and call `database.MigrateAsync()` to apply pending migrations automatically
- [x] T08X Add global error boundary in `src/Payslip4All.Web/App.razor` using Blazor `<ErrorBoundary>` wrapper to catch unhandled exceptions and render a user-friendly error message instead of a blank page
- [x] T08X Verify `dotnet test` passes all tests across all 4 test projects with ≥ 80% coverage on `Payslip4All.Domain` and `Payslip4All.Application`; run `dotnet test --collect:"XPlat Code Coverage"` and confirm no CI gate failures

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup            → No dependencies; start immediately
Phase 2: Foundation       → Depends on Phase 1 completion; BLOCKS Phases 3–6
Phase 3: US1 Auth (P1)   → Depends on Phase 2; recommended first story
Phase 4: US2 Company (P2) → Depends on Phase 2; can overlap with Phase 3
Phase 5: US3 Employee (P3)→ Depends on Phase 2; can overlap with Phases 3–4
Phase 6: US4 Payslip (P4) → Depends on Phase 2; depends on US1 auth being in place
Final Phase: Polish        → Depends on all story phases; can start shared components earlier
```

### User Story Dependencies

| Story | Hard Dependencies | Notes |
|-------|-------------------|-------|
| US1 (Auth) | Phase 2 complete | No story dependencies; pure auth flow |
| US2 (Company) | Phase 2 complete | Needs authenticated user from US1 in integration but independently testable with mocks |
| US3 (Employee) | Phase 2 complete | Needs Company entities (Phase 2 Foundation); independently testable with mocks |
| US4 (Payslip) | Phase 2 complete | Needs Employee + Loan entities; `PayslipCalculator` is pure domain — testable first |

### Within Each Story: Mandatory TDD Order

1. **Write tests** (all `[P]` test tasks for the story) → confirm they FAIL
2. Implement domain/application logic
3. Implement infrastructure / service classes
4. Implement Blazor pages
5. Run tests → all must pass
6. Commit; do not proceed to next story until all tests are green

---

## Parallel Execution Examples

### Phase 2: Foundation (run together)

```
Parallel group A — Domain entities (T010–T016):
  T010 LoanStatus enum
  T011 User entity
  T012 Company entity
  T013 Employee entity
  T014 EmployeeLoan entity
  T015 Payslip entity
  T016 PayslipLoanDeduction entity

Parallel group B — DTOs (T017–T021):
  T017 Auth DTOs
  T018 Company DTOs
  T019 Employee DTOs
  T020 Loan DTOs
  T021 Payslip DTOs

Parallel group C — Service interfaces (T022–T033):
  (all [P] interface tasks)

Sequential: T034 PayslipDbContext (after entities) →
  Parallel group D — Repositories (T035–T040):
    T035 UserRepository
    T036 CompanyRepository
    T037 EmployeeRepository
    T038 LoanRepository
    T039 PayslipRepository
    T040 PasswordHasher
  Sequential: T041 Migration (after DbContext + entities complete)
```

### Phase 3: US1 — run test tasks in parallel, then implement sequentially

```
Parallel: T043 AuthenticationServiceTests | T044 RegisterTests | T045 LoginTests
(Confirm all 3 FAIL before proceeding)
Sequential: T046 AuthenticationService → T047 Register.razor → T048 Login.razor → T049 Logout.razor
```

### Phase 6: US4 — domain tests parallelisable before any implementation

```
Parallel: T068 PayslipCalculatorTests | T069 EmployeeLoanTests | T070 PayslipGenerationServiceTests | T071 GeneratePayslipTests
(Confirm all FAIL)
Sequential: T072 PayslipCalculator → T073 PayslipGenerationService → T074 PdfGenerationService → T075 GeneratePayslip.razor → T076 PDF download handler
```

---

## Implementation Strategy

### MVP First (User Story 1 + Minimal Company View)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundation (entities, DbContext, repositories, migration)
3. Complete Phase 3: US1 (register + login + logout)
4. **STOP and VALIDATE**: Auth flow works end-to-end
5. Add minimal Dashboard shell (stub company list) → demostrable running app
6. Proceed to US2 → US3 → US4

### Incremental Delivery Order

| Step | Deliverable | Validates |
|------|-------------|-----------|
| 1 | Phase 1 + 2 | Solution builds; DB schema created; migration runs |
| 2 | Phase 3 (US1) | Register/Login/Logout; authenticated session |
| 3 | Phase 4 (US2) | Company CRUD; ownership isolation confirmed |
| 4 | Phase 5 (US3) | Employee CRUD; loan add/lock/delete |
| 5 | Phase 6 (US4) | Payslip generation; PDF download; atomic transaction |
| 6 | Final Phase | Polish; migration on startup; error boundaries |

---

## Summary

| Metric | Count |
|--------|-------|
| **Total tasks** | **84** |
| Phase 1 — Setup | 9 |
| Phase 2 — Foundation | 33 |
| Phase 3 — US1 Auth | 7 |
| Phase 4 — US2 Company | 8 |
| Phase 5 — US3 Employee | 10 |
| Phase 6 — US4 Payslip | 9 |
| Final Phase — Polish | 8 |
| **Tasks with [P] (parallelisable)** | **51** |
| **Test tasks (TDD — before implementation)** | **12** (T043–T045, T050–T052, T058–T060, T068–T071) |
| **Suggested MVP scope** | Phase 1 + Phase 2 + Phase 3 (US1 only) |
