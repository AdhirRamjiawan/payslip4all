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
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
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
