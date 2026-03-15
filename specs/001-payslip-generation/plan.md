# Implementation Plan: Payslip Generation System (001)

**Branch**: `001-payslip-generation` | **Date**: 2026-03-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-payslip-generation/spec.md`

## Summary

Build the core Payslip4All application: a Blazor Server web app that allows employers to register accounts, create companies, manage employees (including loan arrangements), and generate downloadable PDF payslips each month. The payslip calculation applies South African UIF (1% of gross, capped at R17,712) plus any active loan deductions. All operations are ownership-filtered per authenticated user. The system is implemented in strict Clean Architecture with TDD across all layers.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS)  
**Primary Dependencies**: ASP.NET Core 8 Blazor Server, Entity Framework Core 8, BCrypt.Net-Next (v4), QuestPDF (community licence), Pomelo.EntityFrameworkCore.MySql  
**Storage**: SQLite (local development default); MySQL via `Pomelo.EntityFrameworkCore.MySql` in production — provider selected at startup via `appsettings.json` key `"DatabaseProvider"`  
**Testing**: xUnit 2.x + Moq 4.x (domain & application layer unit tests); bUnit 1.x (Blazor component tests); SQLite in-memory (infrastructure integration tests)  
**Target Platform**: Server-side ASP.NET Core 8 (Blazor Server over SignalR); no WASM  
**Project Type**: Blazor Server web application  
**Performance Goals**: PDF generation < 3 s (SC-002); page loads < 2 s for 10 companies × 50 employees (SC-004); full register-to-payslip journey < 5 min (SC-001)  
**Constraints**: Cookie auth HttpOnly + Secure (production); no secrets in source; EF Core migrations only (no raw SQL); test coverage ≥ 80% on Domain + Application; zero build warnings  
**Scale/Scope**: ~10 companies per employer × 50 employees per company; medium scale; single-tenant per account

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify compliance with each Payslip4All constitution principle before proceeding:

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned/written before any implementation task begins? | ✅ |
| II | Clean Architecture | Does the feature touch ≤ 4 projects (Domain/Application/Infrastructure/Web)? Does each layer only depend inward? | ✅ |
| III | Blazor Web App | Are all new UI surfaces Razor components? Is business logic kept out of `.razor` files? | ✅ (with C3 deviation) |
| IV | Basic Authentication | Do new pages carry `[Authorize]`? Do new service methods filter by `UserId`? | ✅ |
| V | Database Support | Are all schema changes represented as named EF Core migrations? Is raw SQL avoided? | ✅ |

> **All gates pass.** Four justified deviations are tracked in the Complexity Tracking table (C1 — PayslipLoanDeduction snapshot entity; C2 — MySQL provider addition; C3 — Razor Pages auth deviation; C4 — SiteAdministrator deferral).

**Post-Phase 1 re-check**: All principles remain satisfied after design. Data model uses no EF Core attributes on domain entities; all configuration in `OnModelCreating`; all service interfaces defined in Application layer; all Blazor pages delegate business logic to injected services; unique constraint enforced via EF Core index; BCrypt used for password hashing.

## Project Structure

### Documentation (this feature)

```text
specs/001-payslip-generation/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── http-endpoints.md
│   └── ui-contracts.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
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
├── Payslip4All.Application/
│   ├── Interfaces/
│   │   ├── IAuthenticationService.cs
│   │   ├── ICompanyService.cs
│   │   ├── IEmployeeService.cs
│   │   ├── ILoanService.cs
│   │   ├── IPayslipService.cs
│   │   ├── IPdfGenerationService.cs
│   │   ├── IPasswordHasher.cs
│   │   └── Repositories/
│   │       ├── IUserRepository.cs
│   │       ├── ICompanyRepository.cs
│   │       ├── IEmployeeRepository.cs
│   │       ├── ILoanRepository.cs
│   │       └── IPayslipRepository.cs
│   ├── Services/
│   │   ├── AuthenticationService.cs
│   │   ├── CompanyService.cs
│   │   ├── EmployeeService.cs
│   │   ├── LoanService.cs
│   │   └── PayslipGenerationService.cs
│   └── DTOs/
│       ├── Commands/       # CreateCompanyCommand, CreateEmployeeCommand, etc.
│       └── Queries/        # PreviewPayslipQuery, etc.
├── Payslip4All.Infrastructure/
│   ├── Persistence/
│   │   ├── PayslipDbContext.cs
│   │   └── Migrations/
│   │       └── InitialSchema.cs
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   ├── CompanyRepository.cs
│   │   ├── EmployeeRepository.cs
│   │   ├── LoanRepository.cs
│   │   └── PayslipRepository.cs
│   ├── Auth/
│   │   ├── CookieAuthenticationStateProvider.cs
│   │   └── PasswordHasher.cs
│   └── Services/
│       └── PdfGenerationService.cs
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
    ├── Shared/
    │   ├── LoadingSpinner.razor
    │   ├── ErrorBanner.razor
    │   ├── ConfirmDialog.razor
    │   └── PageTitle.razor
    └── Program.cs

tests/
├── Payslip4All.Domain.Tests/
│   └── Services/
│       └── PayslipCalculatorTests.cs
├── Payslip4All.Application.Tests/
│   └── Services/
│       ├── AuthServiceTests.cs
│       ├── CompanyServiceTests.cs
│       ├── EmployeeServiceTests.cs
│       └── PayslipGenerationServiceTests.cs
├── Payslip4All.Infrastructure.Tests/
│   └── Repositories/
│       └── (integration tests using SQLite in-memory)
└── Payslip4All.Web.Tests/
    └── Pages/
        ├── LoginTests.cs
        ├── CompanyListTests.cs
        ├── EmployeeListTests.cs
        └── PayslipGenerateTests.cs
```

**Structure Decision**: Clean Architecture 4-project layout (Domain / Application / Infrastructure / Web) matching the mandated stack in the constitution. All UI built with Blazor Server `.razor` components. All business logic in Application services. Infrastructure handles EF Core, repositories, BCrypt, and QuestPDF. No additional projects introduced.

## Complexity Tracking

| ID | Violation | Why Needed | Simpler Alternative Rejected Because |
|----|-----------|------------|--------------------------------------|
| C1 | `PayslipLoanDeduction` snapshot entity (5th table beyond the 5 core entities) | Preserves the loan description and deduction amount as they were at generation time; required for accurate payslip PDF re-render after the loan is completed or edited | Storing a reference to `EmployeeLoan` directly would produce incorrect historical payslip values if the loan was ever edited before being frozen |
| C2 | MySQL provider (`Pomelo.EntityFrameworkCore.MySql`) alongside SQLite | Production deployments require MySQL; constitution mandates "only a provider swap in `Program.cs`"; both providers must be installed to enable the swap | Omitting MySQL support would block production deployment without a code change, violating the constitution's own provider-swap principle |
| C3 | Auth pages (`Login`, `Register`, `Logout`) use Razor Pages (`.cshtml` / `.cshtml.cs`) not Blazor components | Blazor Server components cannot call `HttpContext.SignInAsync()` / `SignOutAsync()` directly from the render thread (Blazor runs over SignalR, not a traditional HTTP request/response cycle); Razor Pages have direct `HttpContext` access required by ASP.NET Core cookie auth | Wrapping `SignInAsync` in a service injected into a Blazor component still requires `HttpContext` which is not reliably available in Blazor Server; three auth pages only — all other UI (12+ pages) remains Blazor components |
| C4 | `SiteAdministrator` role seeded but not enforced | Constitution Principle IV requires `SiteAdministrator` / `CompanyOwner` role distinction; `ApplicationRoles.cs` constants and the DB seed migration establish the role string so it exists before the admin portal ships | Deferring the constant to `002-admin-portal` would require a migration in that feature to add a role that arguably belongs to the identity foundation; seeding now is safe and low-risk |
