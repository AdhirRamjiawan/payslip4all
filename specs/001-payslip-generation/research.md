# Research: Payslip Generation System (001)

**Phase**: 0 — Outline & Research  
**Date**: 2025-07-15  
**Status**: Complete — all NEEDS CLARIFICATION items resolved

---

## 1. EF Core Multi-Provider Strategy (MySQL + SQLite)

### Decision
Use a single `PayslipDbContext` with provider selected at startup via `appsettings.json` key `"DatabaseProvider": "sqlite" | "mysql"`. Both providers are registered in `Program.cs`; the factory reads the key and calls either `UseSqlite(...)` or `UseMySql(...)`.

### Implementation Pattern

```csharp
// appsettings.json
{
  "DatabaseProvider": "sqlite",          // or "mysql"
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=payslip4all.db",
    "MySqlConnection": "Server=...;Database=...;User=...;Password=...;"
  }
}

// Program.cs
var provider = builder.Configuration["DatabaseProvider"] ?? "sqlite";
var connStr = provider == "mysql"
    ? builder.Configuration.GetConnectionString("MySqlConnection")
    : builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<PayslipDbContext>(options =>
{
    if (provider == "mysql")
        options.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
    else
        options.UseSqlite(connStr);
});
```

### Migration Strategy
- Migrations are generated once targeting SQLite (the dev default).
- For MySQL, run `dotnet ef migrations add <Name> --context PayslipDbContext` after switching provider in appsettings.
- Alternatively, maintain two migration directories (`Migrations/Sqlite/`, `Migrations/MySql/`) and pass `--output-dir` — chosen approach: **single migration set targeting the lowest-common-denominator DDL** (no provider-specific column types in model; rely on EF Core conventions).
- `MigrateAsync()` is called on startup; it is idempotent and safe for both providers.

### Rationale
Single context + startup-time provider selection is the official EF Core multi-provider pattern. It avoids code duplication and satisfies the constitution requirement that "migrating to SQL Server MUST require only a provider swap in `Program.cs`."

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|-----------------|
| Two separate DbContexts per provider | Duplicates all entity config; violates single-context rule |
| Compile-time `#if` directives | Untestable at runtime; requires recompile to switch |
| Separate migration histories | Adds operational overhead; not needed for SQLite ↔ MySQL DDL compat |

---

## 2. QuestPDF for Payslip PDF Generation

### Decision
Use **QuestPDF** (v2024.x) for all PDF generation in `Payslip4All.Infrastructure`.

### Rationale
- **License**: Community licence is free for companies with annual revenue under USD 1M; MIT-friendly open-source model; commercial licence available if revenue exceeds threshold.
- **API**: Fluent, code-first document layout — no template files, no external tools, fully in-process.
- **Performance**: Generates a typical payslip PDF (single A4 page) in < 100 ms.
- **Alternatives**: iTextSharp (AGPL, licence-incompatible for commercial use), PDFSharp (limited layout), Syncfusion (paid SaaS), FastReport (paid).

### Integration Pattern

```csharp
// Infrastructure/Services/PdfGenerationService.cs
public class PdfGenerationService : IPdfGenerationService
{
    public byte[] GeneratePayslip(PayslipDocument document)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Content().Column(col =>
                {
                    col.Item().Text(document.CompanyName).Bold().FontSize(16);
                    col.Item().Text(document.CompanyAddress);
                    // ... employee details, line items, net pay
                });
            });
        }).GeneratePdf();
    }
}
```

### Interface Contract
```csharp
// Application/Interfaces/IPdfGenerationService.cs
public interface IPdfGenerationService
{
    byte[] GeneratePayslip(PayslipDocument document);
}
```

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|-----------------|
| iTextSharp/iText7 | AGPL licence requires open-sourcing the application |
| PDFSharp | No fluent layout engine; manual coordinate placement |
| Syncfusion (free tier) | Requires Syncfusion licence banner; not clean |
| HTML → Chrome headless | Requires external process; startup latency > 3 s |

---

## 3. Cookie Authentication in Blazor Server

### Decision
Use **ASP.NET Core cookie authentication** (`AddAuthentication().AddCookie(...)`) with a custom `AuthenticationStateProvider` that reads the `ClaimsPrincipal` from the HttpContext.

### Blazor Server vs WASM Considerations
Blazor Server runs server-side; the SignalR circuit inherits the authenticated `HttpContext`. The constitution mandates **Blazor Server** only; WASM is out of scope. This simplifies auth because:
- The auth cookie is sent with every HTTP request (initial page load and reconnection).
- `HttpContextAccessor` provides the `ClaimsPrincipal` for the circuit lifetime.
- No JWT/token plumbing is needed.

### Implementation Pattern

```csharp
// Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // prod
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddScoped<AuthenticationStateProvider,
    RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();
// OR a simpler custom implementation:
builder.Services.AddScoped<AuthenticationStateProvider,
    CookieAuthenticationStateProvider>();
```

```csharp
// Infrastructure/Auth/CookieAuthenticationStateProvider.cs
public class CookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieAuthenticationStateProvider(IHttpContextAccessor accessor)
        => _httpContextAccessor = accessor;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User
                   ?? new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(user));
    }
}
```

### Sign-in / Sign-out

```csharp
// In AuthenticationService (Application layer, called from Razor page)
await HttpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie")),
    new AuthenticationProperties { IsPersistent = true, ExpiresUtc = ... });

await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
```

### Roles
Claims include `ClaimTypes.Role` with value `"CompanyOwner"` or `"SiteAdministrator"`. Pages use `[Authorize(Roles = "CompanyOwner")]`.

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|-----------------|
| ASP.NET Core Identity (full) | Adds migration complexity; spec only needs simple email+password |
| JWT Bearer tokens | Session-less; redundant overhead for Blazor Server's persistent circuit |
| Session-based (non-cookie) | Not compatible with Blazor Server's reconnect model |

---

## 4. BCrypt Password Hashing

### Decision
Use **BCrypt.Net-Next** (`BCrypt.Net.BCrypt.HashPassword` / `BCrypt.Net.BCrypt.Verify`).

### Implementation

```csharp
// Infrastructure/Auth/PasswordHasher.cs
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
```

### NuGet Package
`BCrypt.Net-Next` (v4.0.3+) — actively maintained, MIT licence.

### Work Factor
12 (≈ 250–400 ms on modern hardware). Configurable via `appsettings.json` (`"BCrypt:WorkFactor": 12`).

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|-----------------|
| PBKDF2 (`KeyDerivation`) | More manual implementation; BCrypt simpler and well-tested |
| Argon2 | No first-class .NET package; overkill for this use case |
| SHA-256 | **Explicitly prohibited by constitution** |

---

## 5. UIF Calculation Domain Rule Implementation

### Decision
Implement UIF calculation as a **pure static method on a domain service** `PayslipCalculator` in `Payslip4All.Domain`.

### Formula
```
UIF Deduction = MIN(MonthlyGrossSalary, 17712.00m) × 0.01m
```

South African UIF employee contribution: 1% of gross earnings, capped at the earnings ceiling of R17,712/month (current legislated threshold).

### Implementation Pattern

```csharp
// Domain/Services/PayslipCalculator.cs
public static class PayslipCalculator
{
    public const decimal UifEarningsCeiling = 17_712.00m;
    public const decimal UifContributionRate = 0.01m;

    public static decimal CalculateUifDeduction(decimal monthlyGrossSalary)
    {
        if (monthlyGrossSalary <= 0)
            throw new ArgumentException("Gross salary must be positive.", nameof(monthlyGrossSalary));

        return Math.Round(
            Math.Min(monthlyGrossSalary, UifEarningsCeiling) * UifContributionRate,
            2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateNetPay(
        decimal grossEarnings,
        decimal uifDeduction,
        IEnumerable<decimal> loanDeductions)
    {
        var totalDeductions = uifDeduction + loanDeductions.Sum();
        return grossEarnings - totalDeductions;
    }

    public static decimal CalculateTotalDeductions(
        decimal uifDeduction,
        IEnumerable<decimal> loanDeductions)
        => uifDeduction + loanDeductions.Sum();
}
```

### Rationale
- Pure static method → zero dependencies → trivially unit-testable (no mocks required).
- Constants defined in Domain so they appear in xUnit tests as named values, not magic numbers.
- `MidpointRounding.AwayFromZero` matches standard SA payroll rounding convention.

---

## 6. TermsCompleted Atomic Increment in EF Core

### Decision
Wrap the entire payslip generation operation — payslip insert + all `TermsCompleted` increments + loan status transition — in a **single EF Core transaction** using `IDbContextTransaction`.

### Implementation Pattern

```csharp
// Application/Services/PayslipGenerationService.cs
public async Task<PayslipResult> GeneratePayslipAsync(GeneratePayslipCommand cmd)
{
    await using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 1. Load employee with active loans
        var employee = await _repository.GetEmployeeWithLoansAsync(cmd.EmployeeId);
        
        // 2. Calculate payslip values (pure domain logic)
        var activeLoans = employee.GetActiveLoansForPeriod(cmd.Month, cmd.Year);
        var uifDeduction = PayslipCalculator.CalculateUifDeduction(employee.MonthlyGrossSalary);
        var loanDeductions = activeLoans.Select(l => l.MonthlyDeductionAmount).ToList();
        
        // 3. Create payslip entity
        var payslip = new Payslip { /* ... calculated values ... */ };
        await _payslipRepository.AddAsync(payslip);
        
        // 4. Increment TermsCompleted for each active loan
        foreach (var loan in activeLoans)
        {
            loan.IncrementTermsCompleted(); // domain method on EmployeeLoan
        }
        
        // 5. Generate PDF (Infrastructure)
        var pdfBytes = _pdfService.GeneratePayslip(/* ... */);
        payslip.PdfContent = pdfBytes;
        
        // 6. Save all changes in one transaction
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return new PayslipResult(payslip);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### EmployeeLoan Domain Method

```csharp
// Domain/Entities/EmployeeLoan.cs
public void IncrementTermsCompleted()
{
    if (Status == LoanStatus.Completed)
        throw new InvalidOperationException("Cannot increment a completed loan.");
    
    TermsCompleted++;
    
    if (TermsCompleted == NumberOfTerms)
        Status = LoanStatus.Completed;
}
```

### Concurrency Guard
Use **optimistic concurrency** via EF Core `[ConcurrencyToken]` on `TermsCompleted` (configured in `OnModelCreating` — no attributes on domain entity). If two concurrent requests attempt to increment the same loan, EF will throw `DbUpdateConcurrencyException`; the application layer catches it and returns a conflict error.

### Rationale
- Transaction ensures atomicity: payslip + PDF stored OR nothing persisted (satisfies FR-023).
- Domain method encapsulates business rule: only `EmployeeLoan` may modify its own `TermsCompleted`.
- Optimistic concurrency via row version or ConcurrencyToken covers FR-032's "atomic" requirement under concurrent load.

---

## 7. bUnit Testing Patterns for Blazor Components

### Decision
Use **bUnit** (v1.x) with **xUnit** and **Moq** for all Blazor component tests. Place component tests in `tests/Payslip4All.Web.Tests/`.

### Installation
```xml
<!-- tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj -->
<PackageReference Include="bunit" Version="1.*" />
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
```

### Key Patterns

**1. Basic component render test:**
```csharp
[Fact]
public void LoginForm_RendersEmailAndPasswordInputs()
{
    using var ctx = new TestContext();
    var cut = ctx.RenderComponent<Login>();
    cut.Find("input[type='email']").Should().NotBeNull();
    cut.Find("input[type='password']").Should().NotBeNull();
}
```

**2. Service injection with mock:**
```csharp
[Fact]
public async Task CompanyList_ShowsUserCompanies()
{
    using var ctx = new TestContext();
    var mockService = new Mock<ICompanyService>();
    mockService.Setup(s => s.GetCompaniesForUserAsync(It.IsAny<Guid>()))
               .ReturnsAsync(new List<CompanyDto> { new() { Name = "Acme Ltd" } });

    ctx.Services.AddSingleton(mockService.Object);
    var cut = ctx.RenderComponent<CompanyList>();

    cut.Find("td").TextContent.Should().Contain("Acme Ltd");
}
```

**3. Authentication state:**
```csharp
[Fact]
public void ProtectedPage_RedirectsWhenUnauthenticated()
{
    using var ctx = new TestContext();
    ctx.AddTestAuthorization(); // bUnit built-in
    var cut = ctx.RenderComponent<Dashboard>();
    // Verify redirect or "Not authorized" render
}
```

**4. Cascading auth state for authorized tests:**
```csharp
ctx.AddTestAuthorization()
   .SetAuthorized("test@example.com")
   .SetRoles("CompanyOwner");
```

**5. Form submission:**
```csharp
[Fact]
public async Task AddEmployee_SubmitsFormAndCallsService()
{
    using var ctx = new TestContext();
    var mockService = new Mock<IEmployeeService>();
    ctx.Services.AddSingleton(mockService.Object);

    var cut = ctx.RenderComponent<AddEmployee>();
    cut.Find("input[name='firstName']").Change("John");
    cut.Find("form").Submit();

    mockService.Verify(s => s.CreateEmployeeAsync(It.IsAny<CreateEmployeeCommand>()), Times.Once);
}
```

### Test Project Structure
```
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

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|-----------------|
| Playwright / Selenium | E2E only; too slow for unit-level component testing; can be added later |
| AngleSharp only | Lower-level; no Blazor lifecycle support |
| TestServer integration | Correct for API tests, not component-level isolation |

---

## Summary of All Resolved Decisions

| Topic | Decision | Package |
|-------|----------|---------|
| EF Core multi-provider | Startup-time config switch | `Pomelo.EntityFrameworkCore.MySql` + `Microsoft.EntityFrameworkCore.Sqlite` |
| PDF generation | QuestPDF community licence | `QuestPDF` |
| Cookie auth | Custom `AuthenticationStateProvider` over `IHttpContextAccessor` | Built-in ASP.NET Core |
| Password hashing | BCrypt.Net-Next, work factor 12 | `BCrypt.Net-Next` |
| UIF calculation | Pure static domain method `PayslipCalculator` | None (Domain project) |
| Atomic increment | Single EF Core transaction + optimistic concurrency | Built-in EF Core |
| Blazor testing | bUnit + xUnit + Moq | `bunit`, `xunit`, `Moq` |

---

## 4. QuestPDF Layout Modernisation (PDF Payslip Redesign)

**Phase**: 0 — Research for PDF layout modernisation  
**Date**: 2026-07-15  
**Status**: Complete — all items resolved

---

### 4.1 QuestPDF Fluent API — Table Layout

#### Decision
Use the QuestPDF `Table` API (available since v2022.x, stable in 2024.10.x) for both the
**Income** and **Deductions** sections. Use `Row` + `RelativeItem` for the two-column
employer/employee header panel.

#### Key API Patterns

```csharp
// ── Two-column panel (employer left, employee right) ──────────────────────
container.Row(row =>
{
    row.RelativeItem().Border(1).BorderColor("#DDDDDD").Padding(10).Column(left =>
    {
        left.Item().Text("EMPLOYER DETAILS").Bold().FontSize(8).FontColor("#555555");
        left.Item().Text(doc.CompanyName).Bold().FontSize(11);
        left.Item().Text(doc.CompanyAddress ?? "").FontSize(9);
        left.Item().PaddingTop(4).Text($"UIF Ref: {doc.CompanyUifNumber ?? "—"}").FontSize(9);
        left.Item().Text($"SARS PAYE: {doc.CompanySarsPayeNumber ?? "—"}").FontSize(9);
    });
    row.ConstantItem(12); // gutter
    row.RelativeItem().Border(1).BorderColor("#DDDDDD").Padding(10).Column(right =>
    {
        right.Item().Text("EMPLOYEE DETAILS").Bold().FontSize(8).FontColor("#555555");
        right.Item().Text(doc.EmployeeName).Bold().FontSize(11);
        right.Item().Text($"Employee #: {doc.EmployeeNumber}").FontSize(9);
        right.Item().Text($"ID Number: {doc.EmployeeIdNumber ?? "—"}").FontSize(9);
        right.Item().Text($"Occupation: {doc.Occupation}").FontSize(9);
    });
});

// ── Table (Income / Deductions) ───────────────────────────────────────────
container.Table(table =>
{
    table.ColumnsDefinition(cols =>
    {
        cols.RelativeColumn(5);   // Description — fills remaining space
        cols.ConstantColumn(90);  // Amount (right-aligned, fixed)
    });

    table.Header(header =>
    {
        header.Cell().Background("#1A3C5E").Padding(6)
              .Text("DESCRIPTION").Bold().FontSize(9).FontColor(Colors.White);
        header.Cell().Background("#1A3C5E").Padding(6).AlignRight()
              .Text("AMOUNT (R)").Bold().FontSize(9).FontColor(Colors.White);
    });

    bool alt = false;
    foreach (var item in lineItems)
    {
        var bg = alt ? "#F5F5F5" : Colors.White;
        table.Cell().Background(bg).PaddingHorizontal(6).PaddingVertical(4)
             .Text(item.Description).FontSize(9);
        table.Cell().Background(bg).PaddingHorizontal(6).PaddingVertical(4)
             .AlignRight().Text($"{item.Amount:N2}").FontSize(9);
        alt = !alt;
    }

    // Total row
    table.Cell().Background("#E8EEF4").PaddingHorizontal(6).PaddingVertical(5)
         .Text("TOTAL").Bold().FontSize(9);
    table.Cell().Background("#E8EEF4").PaddingHorizontal(6).PaddingVertical(5)
         .AlignRight().Text($"{total:N2}").Bold().FontSize(9);
});
```

#### Rationale
- **Table API** provides automatic column alignment, header repetition on page overflow, and
  alternating row styling with zero manual coordinate math.
- **ConstantColumn(90)** for the amount column ensures monetary values are consistently
  right-aligned regardless of description length.
- **RelativeColumn(5)** for description absorbs all remaining width.

#### Alternatives Considered
| Alternative | Rejected Because |
|-------------|-----------------|
| Manual `Row` per line item | Verbose; no header repetition on page break |
| PDF column grid (raw coordinates) | QuestPDF is flow-based, not coordinate-based |
| `Stack` + `Border` per row | No automatic column alignment |

---

### 4.2 Page Structure & Section Separation

#### Decision
Use a single `page.Content().Column(...)` with discrete section-drawing helper methods.
Each section has a coloured header band (`Background("#1A3C5E")`) followed by white content.

```csharp
page.Header().Height(70).Row(row =>
{
    row.RelativeItem().Column(col =>
    {
        col.Item().Text(doc.CompanyName).Bold().FontSize(18).FontColor("#1A3C5E");
        col.Item().Text($"PAYSLIP — {doc.PayPeriod}").FontSize(10).FontColor("#555555");
    });
    row.ConstantItem(120).AlignRight().Column(col =>
    {
        col.Item().AlignRight().Text($"Ref: {doc.PayslipReference ?? "—"}").FontSize(8).FontColor("#888888");
        col.Item().AlignRight().Text($"Generated: {DateTime.Today:dd MMM yyyy}").FontSize(8).FontColor("#888888");
    });
});

page.Footer().Height(20).AlignCenter()
    .Text("This is a computer-generated document and is valid without a signature.")
    .FontSize(7).FontColor("#AAAAAA").Italic();
```

---

### 4.3 Net Pay Summary Band

#### Decision
A full-width coloured `Row` with contrasting text — no table, pure emphasis band:

```csharp
container.PaddingTop(8).Row(row =>
{
    row.RelativeItem().Background("#1A3C5E").Padding(12).Row(inner =>
    {
        inner.RelativeItem()
             .Text("NET PAY").Bold().FontSize(13).FontColor(Colors.White);
        inner.ConstantItem(140).AlignRight()
             .Text($"R {doc.NetPay:N2}").Bold().FontSize(16).FontColor("#FFD700");
    });
});
```

---

### 4.4 South African Payslip — Legal Field Requirements

#### Decision
The following fields are legally expected on a South African payslip (BCEA s.33 / UIF Act):

| Section | Field | Source |
|---------|-------|--------|
| Employer | Company name | `Company.Name` |
| Employer | Physical address | `Company.Address` |
| Employer | UIF reference number | `Company.UifNumber` *(new field)* |
| Employer | SARS PAYE number | `Company.SarsPayeNumber` *(new field)* |
| Employee | Full name | `Employee.FirstName + LastName` |
| Employee | Employee number | `Employee.EmployeeNumber` |
| Employee | SA ID number | `Employee.IdNumber` |
| Employee | Occupation / Job title | `Employee.Occupation` |
| Employee | UIF reference | `Employee.UifReference` *(existing, may be null)* |
| Period | Pay period (month + year) | Derived from `Payslip.PayPeriodMonth/Year` |
| Earnings | Basic salary | `Employee.MonthlyGrossSalary` (currently only one income line) |
| Earnings | Gross earnings total | `Payslip.GrossEarnings` |
| Deductions | UIF deduction (1% of min(gross, R17 712)) | `Payslip.UifDeduction` |
| Deductions | Each loan deduction with description | `PayslipLoanDeduction.*` |
| Deductions | Total deductions | `Payslip.TotalDeductions` |
| Summary | Net pay | `Payslip.NetPay` |
| Footer | "Computer-generated" disclaimer | Hardcoded |

**Fields not required by BCEA but commonly expected**: employer address, generation date.  
**PAYE (income tax)**: Payslip4All currently does **not** calculate PAYE — this is out of scope.
The deductions table will only show UIF + loan deductions for now. The layout accommodates future
PAYE addition by treating each deduction as a generic `(Description, Amount)` line item.

---

### 4.5 Typography & Colour Palette

#### Decision

**Font**: QuestPDF default font (Lato) — print-safe, readable at 8–9 pt for table content.

| Element | Size | Weight | Colour |
|---------|------|--------|--------|
| Company name (header) | 18 pt | Bold | `#1A3C5E` (navy) |
| Page subtitle (period) | 10 pt | Normal | `#555555` |
| Section header text | 9 pt | Bold | `#FFFFFF` (on navy band) |
| Section header band | — | — | `#1A3C5E` background |
| Table header text | 9 pt | Bold | `#FFFFFF` |
| Table body text | 9 pt | Normal | `#111111` |
| Table alt-row background | — | — | `#F5F5F5` |
| Total row background | — | — | `#E8EEF4` |
| Total row text | 9 pt | Bold | `#111111` |
| Net pay label | 13 pt | Bold | `#FFFFFF` (on navy) |
| Net pay amount | 16 pt | Bold | `#FFD700` (gold) |
| Footer disclaimer | 7 pt | Italic | `#AAAAAA` |
| Detail labels | 8 pt | Normal | `#555555` |
| Detail values | 9–11 pt | Varies | `#111111` |

**Rationale**: Navy (`#1A3C5E`) is the primary accent — professional, print-safe, and high-contrast
against white text. Gold (`#FFD700`) for the net pay amount creates a focal point. The palette is
accessible (contrast ratio ≥ 4.5:1 for all body text on white).

**Alternatives Considered**:
| Palette | Rejected Because |
|---------|-----------------|
| Pure black & white | Sections are hard to distinguish without colour cues |
| Bootstrap primary blue | QuestPDF has no Bootstrap integration; hex is cleaner |
| Custom embedded font | Adds binary asset; Lato is high-quality and built-in |

---

### 4.6 PayslipDocument DTO — Required Extensions

#### Decision
Extend the `PayslipDocument` record in `IPdfGenerationService.cs` with:

```csharp
public record PayslipDocument(
    // ── Employer ──────────────────────────────────────────────────────────
    string  CompanyName,
    string? CompanyAddress,
    string? CompanyUifNumber,          // NEW — UIF employer reference
    string? CompanySarsPayeNumber,     // NEW — SARS PAYE employer number

    // ── Employee ──────────────────────────────────────────────────────────
    string  EmployeeName,
    string  EmployeeNumber,
    string? EmployeeIdNumber,          // NEW — SA ID number
    string? EmployeeUifReference,      // NEW — UIF employee reference
    string  Occupation,

    // ── Period ────────────────────────────────────────────────────────────
    string  PayPeriod,                 // e.g. "July 2026"
    string? PayslipReference,          // NEW — display ID (e.g. "PS-2026-07-001")

    // ── Earnings ──────────────────────────────────────────────────────────
    IReadOnlyList<(string Description, decimal Amount)> IncomeLineItems,  // NEW
    decimal GrossEarnings,

    // ── Deductions ────────────────────────────────────────────────────────
    decimal UifDeduction,
    IReadOnlyList<(string Description, decimal Amount)> LoanDeductions,
    decimal TotalDeductions,

    // ── Summary ───────────────────────────────────────────────────────────
    decimal NetPay
);
```

**Backward-compatibility strategy**: `IncomeLineItems` is new; the caller
(`PayslipGenerationService`) will populate it with a single entry
`("Basic Salary", employee.MonthlyGrossSalary)` for now. Future allowance support requires
no interface change — simply append more items to the list.

New nullable `Company` fields (`UifNumber`, `SarsPayeNumber`) require:
- Two new `string?` properties on `Company` entity (Domain layer)
- EF Core migration: `AddCompanyUifAndSarsFields`
- Update of `CompanyDto` and company CRUD forms in the Web layer (out of scope for this plan;
  the PDF renders `null` values as `"—"`)

---

### 4.7 Manual Test Gate (Constitution Principle VI)

Although the constitution does not formally define a "Principle VI", the team convention
(**Manual Test Gate**) requires the PDF to be visually inspected before the PR is merged.

**Procedure**:
1. Run the application locally and generate a payslip for an employee with ≥ 1 loan.
2. Download the PDF and visually verify all 6 sections are present and readable.
3. Print to paper (or print-preview) to confirm A4 margins and font sizes are correct.
4. Screenshot the PDF and attach it to the PR description.

This gate is **documented here** as required by the plan template's Constitution Check note.

---

### Updated Summary of All Resolved Decisions

| Topic | Decision |
|-------|----------|
| QuestPDF layout engine | `Table` API for income/deductions; `Row.RelativeItem` for employer/employee panel |
| Page sections | 6 sections: header, employer, employee, income, deductions, net pay |
| Colour palette | Navy `#1A3C5E` / white / gold `#FFD700` / light grey alternating rows |
| Typography | QuestPDF default Lato; 7–18 pt hierarchy; no embedded fonts |
| SA compliance fields | UIF ref, SARS PAYE, ID number, occupation, pay period, disclaimer footer |
| DTO extension | `PayslipDocument` gains 6 new optional/list fields; backward-compatible |
| Domain changes | `Company` gets 2 nullable fields + EF Core migration |
| New packages | None — QuestPDF 2024.10.4 already present |
