# Implementation Plan: Payslip Generation System

**Branch**: `001-payslip-generation` | **Date**: 2025-07-15 | **Spec**: [specs/001-payslip-generation/spec.md](./spec.md)  
**Input**: Feature specification from `/specs/001-payslip-generation/spec.md`

## Summary

Payslip4All is a Blazor Server web application that enables employers (Company Owners) to manage companies and employees, and generate legally-compliant PDF payslips (including UIF deductions and loan deductions) on a monthly basis.

The implementation follows a strict Clean Architecture across four projects (`Domain`, `Application`, `Infrastructure`, `Web`) with TDD enforced throughout. Authentication uses ASP.NET Core cookie auth with BCrypt password hashing. Persistent storage uses Entity Framework Core with configurable SQLite (dev) or MySQL (production) backends. PDF generation uses QuestPDF. Payslip generation is atomic: all changes (payslip record, PDF bytes, `TermsCompleted` increments) are committed in a single database transaction or none at all.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS)  
**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core 8, QuestPDF, BCrypt.Net-Next, Pomelo.EntityFrameworkCore.MySql, Microsoft.EntityFrameworkCore.Sqlite, bUnit, xUnit, Moq  
**Storage**: SQLite (development default) + MySQL (production) — provider selected via `appsettings.json`; single `PayslipDbContext`  
**Testing**: xUnit (unit + integration), bUnit (Blazor component tests), Moq (mocking); coverage threshold ≥ 80% on Domain + Application  
**Target Platform**: Linux/Windows/macOS server (.NET 8 runtime); browser clients (Blazor Server — no WASM)  
**Project Type**: Blazor Server web application (multi-tenant SaaS, single-tenancy per user account)  
**Performance Goals**: Payslip PDF generated and available within 3 seconds (SC-002); page loads ≤ 2 seconds for 10 companies × 50 employees (SC-004)  
**Constraints**: One payslip per employee per month (unique constraint); `TermsCompleted` increment must be atomic within transaction; no raw SQL; no EF Core attributes on domain entities  
**Scale/Scope**: ~10 companies per user, ~50 employees per company (SC-004 baseline); 14 Razor pages/components; 6 domain entities; 4 application service interfaces

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify compliance with each Payslip4All constitution principle before proceeding:

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned/written before any implementation task begins? All acceptance scenarios in the spec map to xUnit test cases (domain, application) and bUnit tests (Web). Coverage ≥ 80% on Domain + Application is a CI gate. | ✅ |
| II | Clean Architecture | Feature touches exactly 4 projects (Domain ← Application ← Infrastructure ← Web). Domain has zero external dependencies. Application defines all service interfaces. Infrastructure implements them. Web only calls Application interfaces. Cross-layer comms via DTOs only. | ✅ |
| III | Blazor Web App | All UI is Razor components (`.razor`). Pages use `@page`. Business logic delegated to injected Application services. Async/await used for all I/O. Loading states and error messages implemented for all async ops. Bootstrap 5 CSS. | ✅ |
| IV | Basic Authentication | All pages except `/login` and `/register` carry `[Authorize(Roles = "CompanyOwner")]`. All service methods filter by authenticated `UserId`. Cookie is `HttpOnly`/`Secure`. Auth errors are generic (FR-004). | ✅ |
| V | Database Support | All schema changes are named EF Core migrations. `MigrateAsync()` called on startup. LINQ only — no raw SQL. All EF config in `OnModelCreating`. Connection string from `appsettings.json`. Provider swap requires only `Program.cs` change. | ✅ |

> **All gates PASS.** No exceptions required.

## Post-Design Constitution Re-Check

After Phase 1 design (data model + contracts):

| # | Principle | Design Compliance Confirmation |
|---|-----------|-------------------------------|
| I | TDD | Each entity has clear validation rules → xUnit domain tests defined. Each service method has clear input/output contract → application service tests defined. Each Blazor page has defined UI states → bUnit tests mapped. |
| II | Clean Architecture | `PayslipCalculator` is pure Domain. `IPayslipService`, `ICompanyService`, `IEmployeeService`, `ILoanService`, `IAuthenticationService`, `IPdfGenerationService` all in Application. `PayslipDbContext`, repositories, `PdfGenerationService`, `PasswordHasher` all in Infrastructure. All Razor pages in Web with no direct DbContext. |
| III | Blazor Web App | 14 pages/components identified in UI contracts. All async states (loading, error, empty, data) specified per page. |
| IV | Basic Authentication | Ownership filter enforced at service layer (not just route). Loan edit/delete guards enforce `TermsCompleted` check before any mutation. |
| V | Database Support | Single `InitialSchema` migration planned. Unique constraint `UQ_Payslips_EmployeeId_PayPeriodMonth_PayPeriodYear` defined. Concurrency token on `TermsCompleted`. `OnDelete` behaviours explicitly configured. |

## Project Structure

### Documentation (this feature)

```text
specs/001-payslip-generation/
├── plan.md              ✅ This file
├── research.md          ✅ Phase 0 output
├── data-model.md        ✅ Phase 1 output
├── quickstart.md        ✅ Phase 1 output
├── contracts/
│   └── ui-contracts.md  ✅ Phase 1 output
└── tasks.md             ⏳ Phase 2 output (/speckit.tasks — not yet created)
```

### Source Code (repository root)

```text
src/
├── Payslip4All.Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Company.cs
│   │   ├── Employee.cs
│   │   ├── EmployeeLoan.cs
│   │   ├── Payslip.cs
│   │   └── PayslipLoanDeduction.cs
│   ├── Enums/
│   │   └── LoanStatus.cs
│   └── Services/
│       └── PayslipCalculator.cs
│
├── Payslip4All.Application/
│   ├── Interfaces/
│   │   ├── IAuthenticationService.cs
│   │   ├── ICompanyService.cs
│   │   ├── IEmployeeService.cs
│   │   ├── ILoanService.cs
│   │   ├── IPayslipService.cs
│   │   └── IPdfGenerationService.cs
│   ├── Interfaces/Repositories/
│   │   ├── IUserRepository.cs
│   │   ├── ICompanyRepository.cs
│   │   ├── IEmployeeRepository.cs
│   │   ├── ILoanRepository.cs
│   │   └── IPayslipRepository.cs
│   ├── Services/
│   │   ├── AuthenticationService.cs
│   │   ├── CompanyService.cs
│   │   ├── EmployeeService.cs
│   │   ├── LoanService.cs
│   │   └── PayslipGenerationService.cs
│   └── DTOs/
│       ├── Auth/          (RegisterCommand, LoginCommand, AuthResult)
│       ├── Company/       (CompanyDto, CreateCompanyCommand, UpdateCompanyCommand)
│       ├── Employee/      (EmployeeDto, CreateEmployeeCommand, UpdateEmployeeCommand)
│       ├── Loan/          (LoanDto, CreateLoanCommand, UpdateLoanCommand)
│       └── Payslip/       (PayslipDto, GeneratePayslipCommand, PreviewPayslipQuery, PayslipResult)
│
├── Payslip4All.Infrastructure/
│   ├── Persistence/
│   │   ├── PayslipDbContext.cs
│   │   ├── Migrations/
│   │   └── Repositories/
│   │       ├── UserRepository.cs
│   │       ├── CompanyRepository.cs
│   │       ├── EmployeeRepository.cs
│   │       ├── LoanRepository.cs
│   │       └── PayslipRepository.cs
│   ├── Auth/
│   │   ├── PasswordHasher.cs
│   │   └── CookieAuthenticationStateProvider.cs
│   └── Services/
│       └── PdfGenerationService.cs
│
└── Payslip4All.Web/
    ├── Pages/
    │   ├── Auth/
    │   │   ├── Login.razor
    │   │   ├── Register.razor
    │   │   └── Logout.razor
    │   ├── Dashboard.razor
    │   ├── Companies/
    │   │   ├── CreateCompany.razor
    │   │   ├── EditCompany.razor
    │   │   └── CompanyDetail.razor
    │   ├── Employees/
    │   │   ├── CreateEmployee.razor
    │   │   ├── EditEmployee.razor
    │   │   ├── EmployeeDetail.razor
    │   │   └── Loans/
    │   │       ├── CreateLoan.razor
    │   │       └── EditLoan.razor
    │   └── Payslips/
    │       └── GeneratePayslip.razor
    └── Shared/
        ├── LoadingSpinner.razor
        ├── ErrorBanner.razor
        ├── ConfirmDialog.razor
        └── PageTitle.razor

tests/
├── Payslip4All.Domain.Tests/
│   └── Services/
│       └── PayslipCalculatorTests.cs
├── Payslip4All.Application.Tests/
│   └── Services/
│       ├── AuthenticationServiceTests.cs
│       ├── CompanyServiceTests.cs
│       ├── EmployeeServiceTests.cs
│       ├── LoanServiceTests.cs
│       └── PayslipGenerationServiceTests.cs
├── Payslip4All.Infrastructure.Tests/
│   └── Repositories/
│       └── (SQLite in-memory integration tests)
└── Payslip4All.Web.Tests/
    └── Pages/
        ├── LoginTests.cs
        ├── RegisterTests.cs
        ├── DashboardTests.cs
        ├── CompanyDetailTests.cs
        ├── EmployeeDetailTests.cs
        └── GeneratePayslipTests.cs
```

**Structure Decision**: Clean Architecture with 4 source projects and 4 test projects. Domain and Application are pure C# class libraries. Infrastructure references EF Core, QuestPDF, and BCrypt. Web is the Blazor Server host. Test projects mirror source structure with xUnit + bUnit.

## NuGet Package Registry

| Project | Package | Version | Purpose |
|---------|---------|---------|---------|
| `Infrastructure` | `Microsoft.EntityFrameworkCore` | 8.x | ORM core |
| `Infrastructure` | `Microsoft.EntityFrameworkCore.Sqlite` | 8.x | SQLite provider |
| `Infrastructure` | `Pomelo.EntityFrameworkCore.MySql` | 8.x | MySQL provider |
| `Infrastructure` | `Microsoft.EntityFrameworkCore.Tools` | 8.x | Migration tooling |
| `Infrastructure` | `QuestPDF` | 2024.x | PDF generation |
| `Infrastructure` | `BCrypt.Net-Next` | 4.x | Password hashing |
| `Web` | `Microsoft.AspNetCore.Components.Authorization` | 8.x | Blazor auth integration |
| `Web.Tests` | `bunit` | 1.x | Blazor component testing |
| `*.Tests` | `xunit` | 2.x | Test framework |
| `*.Tests` | `Moq` | 4.x | Mocking |
| `*.Tests` | `Microsoft.NET.Test.Sdk` | 17.x | Test runner |
| `*.Tests` | `coverlet.collector` | 6.x | Coverage collection |

## Key Design Decisions

### Atomic Payslip Generation (FR-023, FR-032)
The `PayslipGenerationService.GeneratePayslipAsync` method wraps all work in a single EF Core transaction:
1. Load employee + active loans
2. Calculate payslip values (`PayslipCalculator` — pure domain)
3. Insert `Payslip` entity
4. Insert `PayslipLoanDeduction` snapshots for each active loan
5. Call `loan.IncrementTermsCompleted()` on each active loan (domain method transitions to Completed if final term)
6. Generate PDF via `IPdfGenerationService`
7. Store PDF bytes on payslip
8. `SaveChangesAsync()` + `CommitAsync()` — or `RollbackAsync()` on any exception

### UIF Calculation (FR-019)
`PayslipCalculator.CalculateUifDeduction(decimal grossSalary)` — pure static, no dependencies:
```
UIF = ROUND(MIN(grossSalary, 17712.00) × 0.01, 2, AwayFromZero)
```
Constants `UifEarningsCeiling = 17712.00m` and `UifContributionRate = 0.01m` defined in Domain.

### Provider Switching (Constitution V)
Single `PayslipDbContext`. Provider selected at startup:
```
appsettings.json: "DatabaseProvider": "sqlite" | "mysql"
```
Switching providers requires no code changes beyond `appsettings.json`.

### Ownership Filtering (FR-008, FR-013, FR-018)
All repository queries accept a `Guid userId` parameter and include a `.Where(x => x.Company.UserId == userId)` (or equivalent) clause. The Application service layer extracts the `userId` from the `ClaimsPrincipal` — never trusts route parameters alone.

## Complexity Tracking

> **No Constitution violations.** All gates pass without exception.

*(This table is intentionally empty — no justified exceptions are needed.)*
