# Data Model: Payslip Generation System (001)

**Phase**: 1 — Design  
**Date**: 2026-03-15 | **Last amended**: 2026-03-17  
**Status**: Complete — reconciled with Phase 6 implementation (C1–C4, I1, I2 from analysis report)

---

## Entity Relationship Overview

```
User (1) ──────────────────────── (0..*) Company
                                              │
                                         (0..*) Employee
                                              │
                                    ┌─────────┴──────────┐
                                    │                    │
                               (0..*) Payslip      (0..*) EmployeeLoan
                                    │
                             (0..*) PayslipLoanDeduction
                                    │
                      (references) ─┘─ EmployeeLoan
```

**Cardinality summary**:
- `User` → `Company`: One-to-Many (a user owns zero or more companies)
- `Company` → `Employee`: One-to-Many (a company has zero or more employees)
- `Employee` → `Payslip`: One-to-Many (an employee has zero or more payslips)
- `Employee` → `EmployeeLoan`: One-to-Many (an employee has zero or more loans)
- `Payslip` → `PayslipLoanDeduction`: One-to-Many (a payslip has zero or more loan line items)
- `EmployeeLoan` → `PayslipLoanDeduction`: One-to-Many (a loan may appear on many payslips)

---

## Entities

### User

Represents an employer account. Owns companies and is the data isolation boundary.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK, non-nullable | Generated on creation (`Guid.NewGuid()`) |
| `Email` | `string` | Unique, non-nullable, max 256 chars | Normalised to lowercase before storage |
| `PasswordHash` | `string` | Non-nullable, max 60 chars | BCrypt hash (60-char fixed output) |
| `CreatedAt` | `DateTimeOffset` | Non-nullable | UTC; set on creation, never updated |

**Indexes**:
- `IX_Users_Email` — unique index on `Email` (enforces FR-001 uniqueness)

**EF Core Configuration** (`OnModelCreating`):
```csharp
builder.Entity<User>(e =>
{
    e.HasKey(u => u.Id);
    e.Property(u => u.Email).HasMaxLength(256).IsRequired();
    e.HasIndex(u => u.Email).IsUnique();
    e.Property(u => u.PasswordHash).HasMaxLength(60).IsRequired();
    e.Property(u => u.CreatedAt).IsRequired();
});
```

**Validation Rules**:
- Email must match RFC 5321 format (validated in Application layer)
- Password must be at least 8 characters before hashing (spec does not specify min; 8 is sensible default)
- `CreatedAt` defaults to `DateTimeOffset.UtcNow` on domain object construction

---

### Company

Represents a business entity owned by a User. The top-level container for employees and payslips.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK, non-nullable | Generated on creation |
| `Name` | `string` | Non-nullable, max 200 chars | Employer/trading name; required (FR-011) |
| `Address` | `string?` | Nullable, max 500 chars | Street address printed on payslips; optional at creation but non-empty if provided |
| `UserId` | `Guid` | FK → `User.Id`, non-nullable | Cascade delete on User delete |
| `CreatedAt` | `DateTimeOffset` | Non-nullable | UTC |

**Indexes**:
- `IX_Companies_UserId` — non-unique index on `UserId` (all company queries filter by user)

**EF Core Configuration**:
```csharp
builder.Entity<Company>(e =>
{
    e.HasKey(c => c.Id);
    e.Property(c => c.Name).HasMaxLength(200).IsRequired();
    e.Property(c => c.Address).HasMaxLength(500);
    e.HasOne<User>()
     .WithMany()
     .HasForeignKey(c => c.UserId)
     .OnDelete(DeleteBehavior.Cascade);
    e.HasIndex(c => c.UserId);
});
```

**Validation Rules**:
- `Name` must be non-empty string (FR-011)
- `Address`, if provided, must be non-empty string (FR-011)
- **Deletion guard**: Cannot delete if `Employees.Any()` or `Payslips.Any()` via any employee (FR-010) — enforced in Application layer before delete call

---

### Employee

Represents a person employed by a Company. Stores personal and financial details for payslip generation.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK, non-nullable | Generated on creation |
| `FirstName` | `string` | Non-nullable, max 100 chars | Required (FR-016) |
| `LastName` | `string` | Non-nullable, max 100 chars | Required (FR-016) |
| `IdNumber` | `string` | Non-nullable, max 20 chars | National ID document number; required (FR-016) |
| `EmployeeNumber` | `string` | Non-nullable, max 50 chars | Employer-assigned identifier; required (FR-016) |
| `StartDate` | `DateOnly` | Non-nullable | Employment commencement date; required (FR-016) |
| `Occupation` | `string` | Non-nullable, max 150 chars | Job title / role; required (FR-016) |
| `UifReference` | `string?` | Nullable, max 50 chars | UIF registration reference; optional |
| `MonthlyGrossSalary` | `decimal` | Non-nullable, precision (18,2) | Must be > 0 (FR-017); required (FR-016) |
| `CompanyId` | `Guid` | FK → `Company.Id`, non-nullable | Cascade delete on Company delete |
| `CreatedAt` | `DateTimeOffset` | Non-nullable | UTC |

**Indexes**:
- `IX_Employees_CompanyId` — non-unique index on `CompanyId`
- `IX_Employees_EmployeeNumber_CompanyId` — unique composite index (employee numbers unique within a company)

**EF Core Configuration**:
```csharp
builder.Entity<Employee>(e =>
{
    e.HasKey(emp => emp.Id);
    e.Property(emp => emp.FirstName).HasMaxLength(100).IsRequired();
    e.Property(emp => emp.LastName).HasMaxLength(100).IsRequired();
    e.Property(emp => emp.IdNumber).HasMaxLength(20).IsRequired();
    e.Property(emp => emp.EmployeeNumber).HasMaxLength(50).IsRequired();
    e.Property(emp => emp.Occupation).HasMaxLength(150).IsRequired();
    e.Property(emp => emp.UifReference).HasMaxLength(50);
    e.Property(emp => emp.MonthlyGrossSalary).HasPrecision(18, 2).IsRequired();
    e.HasOne<Company>()
     .WithMany()
     .HasForeignKey(emp => emp.CompanyId)
     .OnDelete(DeleteBehavior.Cascade);
    e.HasIndex(emp => emp.CompanyId);
    e.HasIndex(emp => new { emp.EmployeeNumber, emp.CompanyId }).IsUnique();
});
```

**Validation Rules**:
- All required fields: `FirstName`, `LastName`, `IdNumber`, `EmployeeNumber`, `StartDate`, `Occupation`, `MonthlyGrossSalary` (FR-016)
- `MonthlyGrossSalary` must be > 0 (FR-017)
- **Deletion guard**: Cannot delete if `Payslips.Any()` for this employee (FR-015)

---

### EmployeeLoan

Represents a loan repayment arrangement for an Employee. Controls deduction line items on payslips.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK, non-nullable | Generated on creation |
| `Description` | `string` | Non-nullable, max 300 chars | What the loan is for (FR-026) |
| `TotalLoanAmount` | `decimal` | Non-nullable, precision (18,2) | Must be > 0 (FR-028) |
| `NumberOfTerms` | `int` | Non-nullable | Must be positive integer (FR-028) |
| `MonthlyDeductionAmount` | `decimal` | Non-nullable, precision (18,2) | Fixed deduction per payslip; must be > 0 (FR-028) |
| `PaymentStartDate` | `DateOnly` | Non-nullable | Month + year deductions begin (FR-026) |
| `TermsCompleted` | `int` | Non-nullable, default 0 | Atomically incremented per payslip generation (FR-032); NOT user-editable |
| `Status` | `LoanStatus` | Non-nullable, enum (Active=0, Completed=1) | Transitions Active → Completed when `TermsCompleted == NumberOfTerms` (FR-029) |
| `EmployeeId` | `Guid` | FK → `Employee.Id`, non-nullable | Cascade delete on Employee delete (only when `TermsCompleted == 0`) |
| `CreatedAt` | `DateTimeOffset` | Non-nullable | UTC |

**Enum**:
```csharp
// Domain/Enums/LoanStatus.cs
public enum LoanStatus { Active = 0, Completed = 1 }
```

**State Transitions**:
```
Created ──→ Active (TermsCompleted = 0)
Active  ──→ Active (TermsCompleted increments 0..N-1 per payslip)
Active  ──→ Completed (TermsCompleted reaches NumberOfTerms)
Completed: terminal state — no further transitions
```

**Active Loan Predicate** (domain method):
```csharp
public bool IsActiveForPeriod(int month, int year)
{
    var periodDate = new DateOnly(year, month, 1);
    return Status == LoanStatus.Active
        && periodDate >= PaymentStartDate
        && TermsCompleted < NumberOfTerms;
}
```

**Mutability Rules** (FR-030, FR-031):
- Editable when `TermsCompleted == 0`
- Read-only (all fields locked) when `TermsCompleted > 0`
- Deletable only when `TermsCompleted == 0`

**Concurrency Token** (for atomic increment, FR-032):
```csharp
e.Property(l => l.TermsCompleted).IsConcurrencyToken();
```

**Indexes**:
- `IX_EmployeeLoans_EmployeeId` — non-unique index on `EmployeeId`
- `IX_EmployeeLoans_EmployeeId_Status` — composite index for active loan queries

**EF Core Configuration**:
```csharp
builder.Entity<EmployeeLoan>(e =>
{
    e.HasKey(l => l.Id);
    e.Property(l => l.Description).HasMaxLength(300).IsRequired();
    e.Property(l => l.TotalLoanAmount).HasPrecision(18, 2).IsRequired();
    e.Property(l => l.MonthlyDeductionAmount).HasPrecision(18, 2).IsRequired();
    e.Property(l => l.TermsCompleted).IsRequired().HasDefaultValue(0).IsConcurrencyToken();
    e.Property(l => l.Status).IsRequired().HasConversion<int>();
    e.HasOne<Employee>()
     .WithMany()
     .HasForeignKey(l => l.EmployeeId)
     .OnDelete(DeleteBehavior.Restrict); // prevent delete if TermsCompleted > 0
    e.HasIndex(l => l.EmployeeId);
    e.HasIndex(l => new { l.EmployeeId, l.Status });
});
```

---

### Payslip

Represents the official earnings record for one Employee for one calendar month.

> **11 fields** (ownership queries traverse `Employee → Company`; no denormalised `CompanyId` needed):

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK, non-nullable | Generated on creation |
| `PayPeriodMonth` | `int` | Non-nullable, 1–12 | Calendar month |
| `PayPeriodYear` | `int` | Non-nullable, 2000–9999 | Calendar year |
| `GrossEarnings` | `decimal` | Non-nullable, precision (18,2) | Snapshot of `Employee.MonthlyGrossSalary` at generation time |
| `UifDeduction` | `decimal` | Non-nullable, precision (18,2) | `MIN(GrossEarnings, 17712) × 0.01` |
| `TotalLoanDeductions` | `decimal` | Non-nullable, precision (18,2) | Sum of all `PayslipLoanDeduction.Amount` |
| `TotalDeductions` | `decimal` | Non-nullable, precision (18,2) | `UifDeduction + TotalLoanDeductions` |
| `NetPay` | `decimal` | Non-nullable, precision (18,2) | `GrossEarnings − TotalDeductions` (i.e., Gross − UIF − sum of all active loan deductions; see FR-019) |
| `EmployeeId` | `Guid` | FK → `Employee.Id`, non-nullable | Cascade delete NOT set (FR-015 guards this) |
| `GeneratedAt` | `DateTimeOffset` | Non-nullable | UTC; when the payslip was created |
| _PayslipLoanDeduction rows_ | (child entity) | One-to-Many via `PayslipId` | Snapshot of each active loan deduction at generation time |

> **Note on PDF storage**: `PdfContent byte[]` was removed in migration `RemovePdfContentColumn` (Phase 6).
> PDFs are now generated on the fly at download time from stored numeric data — no binary blob is persisted.
> Ownership is verified by joining `Payslip → Employee → Company → UserId`.

**Unique Constraint**:
- `UQ_Payslips_EmployeeId_PayPeriodMonth_PayPeriodYear` — enforces one payslip per employee per month (FR-021)

**EF Core Configuration**:
```csharp
builder.Entity<Payslip>(e =>
{
    e.HasKey(p => p.Id);
    e.Property(p => p.GrossEarnings).HasPrecision(18, 2).IsRequired();
    e.Property(p => p.UifDeduction).HasPrecision(18, 2).IsRequired();
    e.Property(p => p.TotalLoanDeductions).HasPrecision(18, 2).IsRequired();
    e.Property(p => p.TotalDeductions).HasPrecision(18, 2).IsRequired();
    e.Property(p => p.NetPay).HasPrecision(18, 2).IsRequired();
    e.HasIndex(p => p.EmployeeId);
    e.HasIndex(p => new { p.EmployeeId, p.PayPeriodMonth, p.PayPeriodYear })
     .IsUnique()
     .HasDatabaseName("UQ_Payslips_EmployeeId_PayPeriodMonth_PayPeriodYear");
    e.HasMany(p => p.LoanDeductions)
     .WithOne()
     .HasForeignKey(d => d.PayslipId)
     .OnDelete(DeleteBehavior.Cascade);
});
```

**Validation Rules**:
- `PayPeriodMonth` in range 1–12; `PayPeriodYear` > 2000
- `GrossEarnings` > 0 (FR-025 prevents generation for zero-salary employees)
- Duplicate check: Application layer checks unique constraint before insert; on `DbUpdateException` returns conflict to caller (FR-021)

---

### PayslipLoanDeduction

Junction/snapshot entity: captures each active loan's deduction line item on a specific payslip. Preserves the loan description and amount as they were at generation time (immutable after payslip creation).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK, non-nullable | Generated on creation |
| `PayslipId` | `Guid` | FK → `Payslip.Id`, non-nullable | Cascade delete on Payslip delete |
| `EmployeeLoanId` | `Guid` | FK → `EmployeeLoan.Id`, non-nullable | Restrict delete (loan may not be deleted if deductions exist) |
| `Description` | `string` | Non-nullable, max 300 chars | Snapshot of `EmployeeLoan.Description` at generation time |
| `Amount` | `decimal` | Non-nullable, precision (18,2) | Snapshot of `EmployeeLoan.MonthlyDeductionAmount` at generation time |

**EF Core Configuration**:
```csharp
builder.Entity<PayslipLoanDeduction>(e =>
{
    e.HasKey(d => d.Id);
    e.Property(d => d.Description).HasMaxLength(300).IsRequired();
    e.Property(d => d.Amount).HasPrecision(18, 2).IsRequired();
    e.HasOne<Payslip>()
     .WithMany(p => p.LoanDeductions)
     .HasForeignKey(d => d.PayslipId)
     .OnDelete(DeleteBehavior.Cascade);
    e.HasOne<EmployeeLoan>()
     .WithMany()
     .HasForeignKey(d => d.EmployeeLoanId)
     .OnDelete(DeleteBehavior.Restrict);
    e.HasIndex(d => d.PayslipId);
});
```

---

## Migration Names

| Migration | Phase | Contents |
|-----------|-------|----------|
| `InitialSchema` | 1 | All 6 tables: Users, Companies, Employees, EmployeeLoans, Payslips, PayslipLoanDeductions |
| `SeedApplicationRoles` | 1 | Seeds `CompanyOwner` and `SiteAdministrator` role constants |
| `AddCompanyUifAndSarsFields` | 3 | Adds nullable `UifNumber` (max 50) and `SarsPayeNumber` (max 30) columns to `Companies` table |
| `RemovePdfContentColumn` | 6 | Drops the `PdfContent BLOB` column from `Payslips` table — PDFs are now generated on the fly |

All schema changes are captured as named migrations in `Payslip4All.Infrastructure/Migrations/`.

---

## Domain Layer Notes

- All entities live in `Payslip4All.Domain/Entities/`.
- `LoanStatus` enum lives in `Payslip4All.Domain/Enums/`.
- `PayslipCalculator` static class lives in `Payslip4All.Domain/Services/`.
- **Zero EF Core attributes on domain entities** (no `[Key]`, `[Required]`, `[MaxLength]`).
- All EF Core configuration is in `Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs` `OnModelCreating`.
- Navigation properties on domain entities use `IReadOnlyList<T>` with a private backing `List<T>` field — domain entities control their own collections.

---

## PDF Layout Model (Modernised Payslip)

**Phase**: 1 — Design (PDF Modernisation)  
**Date**: 2026-07-15

This section documents the **presentation-layer data model** for the modernised PDF payslip.
It is distinct from the domain entities above — these are the DTO/record types that feed the
QuestPDF renderer.

---

### PayslipDocument (Application DTO — extended)

Located at: `src/Payslip4All.Application/DTOs/PayslipDocument.cs`

> **Implemented record** (20 constructor parameters — 14 required + 6 optional SA-compliance fields with defaults):

```csharp
public record PayslipDocument(
    // ── Core fields (required, positional) ────────────────────────────────
    string CompanyName,                // Section: Header + Employer Details
    string? CompanyAddress,            // Section: Employer Details
    string EmployeeName,               // Section: Employee Details
    string EmployeeNumber,             // Section: Employee Details
    string Occupation,                 // Section: Employee Details
    string PayPeriod,                  // Section: Header subtitle
    decimal GrossEarnings,             // Section: Income Table total
    decimal UifDeduction,              // Section: Deductions Table
    IReadOnlyList<(string Description, decimal Amount)> LoanDeductions,  // Section: Deductions Table
    decimal TotalDeductions,           // Section: Deductions Table total
    decimal NetPay,                    // Section: Net Pay Summary
    // ── SA-compliance fields (optional, defaulted) ────────────────────────
    string? CompanyUifNumber = null,       // Section: Employer Details
    string? CompanySarsPayeNumber = null,  // Section: Employer Details
    string EmployeeIdNumber = "",          // Section: Employee Details (non-null, default "")
    DateOnly EmployeeStartDate = default,  // Section: Employee Details
    string? EmployeeUifReference = null,   // Section: Employee Details
    DateOnly PaymentDate = default         // Section: Net Pay Summary footer
);
```

**Field nullability notes**:
- `EmployeeIdNumber` is `string` (non-nullable) with default `""` — renders as an empty cell if not set
- All `string?` fields render `"—"` when null in the PDF
- `LoanDeductions` is always non-null; an empty list means no loan rows are rendered

**Validation rules** enforced by `PdfGenerationService` at render time:
- `GrossEarnings > 0`
- `NetPay > 0`
- `TotalDeductions = UifDeduction + sum(LoanDeductions)` *(assertion, not throw)*

---

### PDF Visual Layout Map

```
┌─────────────────────────────────────────────────────────────┐
│  HEADER                                                     │
│  CompanyName (18pt bold navy)    Ref: PayslipReference      │
│  PAYSLIP — PayPeriod (10pt)      Generated: DD MMM YYYY     │
├─────────────────────────────────────────────────────────────┤
│  ┌────────────────────────┐  ┌──────────────────────────┐   │
│  │ EMPLOYER DETAILS        │  │ EMPLOYEE DETAILS          │  │
│  │ CompanyName             │  │ EmployeeName              │  │
│  │ CompanyAddress          │  │ Employee #: EmployeeNum   │  │
│  │ UIF Ref: CompanyUif     │  │ ID Number: EmployeeIdNum  │  │
│  │ SARS PAYE: CompanySars  │  │ Occupation: Occupation    │  │
│  │                         │  │ UIF Ref: EmpUifRef        │  │
│  └────────────────────────┘  └──────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│  ░░░ INCOME                                          ░░░░   │  ← navy band
├──────────────────────────────────┬──────────────────────────┤
│  Basic Salary                    │            R XX,XXX.XX   │
│  (future allowances here)        │            R     XXX.XX  │  ← alternating grey
├──────────────────────────────────┼──────────────────────────┤
│  GROSS EARNINGS                  │  BOLD  R XX,XXX.XX       │  ← blue-tint total row
├─────────────────────────────────────────────────────────────┤
│  ░░░ DEDUCTIONS                                      ░░░░   │  ← navy band
├──────────────────────────────────┬──────────────────────────┤
│  UIF Contribution                │                R XXX.XX  │
│  Loan: <description>             │                R XXX.XX  │  ← alternating grey
├──────────────────────────────────┼──────────────────────────┤
│  TOTAL DEDUCTIONS                │  BOLD  R     XXX.XX      │  ← blue-tint total row
├─────────────────────────────────────────────────────────────┤
│  ████████████████  NET PAY       │  R XX,XXX.XX  ████████   │  ← full-width navy band
│                                  │  (gold 16pt bold)         │
├─────────────────────────────────────────────────────────────┤
│  FOOTER: "This is a computer-generated document..."  7pt    │
└─────────────────────────────────────────────────────────────┘
```

---

### Extended Company Entity Fields

Two new nullable fields on `Company` (Domain entity — no behaviour change):

| Field | Type | C# | Max Length | Notes |
|-------|------|----|------------|-------|
| `UifNumber` | `string?` | `public string? UifNumber { get; set; }` | 50 chars | Employer UIF registration number |
| `SarsPayeNumber` | `string?` | `public string? SarsPayeNumber { get; set; }` | 30 chars | SARS PAYE employer reference |

**EF Core migration**: `AddCompanyUifAndSarsFields`  
Both columns are nullable `TEXT`; no index required; no cascade impact.

```csharp
// OnModelCreating addition (appended to existing Company entity config)
e.Property(c => c.UifNumber).HasMaxLength(50);
e.Property(c => c.SarsPayeNumber).HasMaxLength(30);
```

---

### Section Rendering Components (Infrastructure — not domain types)

The following private helper methods will be defined inside `PdfGenerationService`:

| Method | Responsibility |
|--------|----------------|
| `RenderHeader(IContainer, PayslipDocument)` | Top header band: company name, period, reference, generated date |
| `RenderDetailsPanel(IContainer, PayslipDocument)` | Two-column employer / employee detail boxes |
| `RenderSectionBand(IContainer, string title)` | Reusable coloured section header band |
| `RenderLineItemTable(IContainer, IEnumerable<LineItem>, decimal total)` | Generic income/deductions table with alternating rows + total row |
| `RenderNetPaySummary(IContainer, decimal netPay)` | Full-width navy + gold net pay band |
| `RenderFooter(IContainer)` | Disclaimer text |
| `GetDeductionLineItems(PayslipDocument)` | Combines UIF + loan deductions into a flat list |

These are pure rendering helpers — no business logic; no DI; no DB access.

---

### Mapping: Domain → PayslipDocument (in `PayslipGenerationService.GetPdfAsync`)

> **Phase 6 note**: `PayslipDocument` is now constructed in `GetPdfAsync` (on-the-fly at download
> time) rather than in `GeneratePayslipAsync`. The source data comes from the persisted `Payslip`
> record and its loaded `Employee → Company` navigation properties.

| PayslipDocument field | Source |
|----------------------|--------|
| `CompanyName` | `payslip.Employee.Company?.Name ?? "Company"` |
| `CompanyAddress` | `payslip.Employee.Company?.Address` |
| `CompanyUifNumber` | `payslip.Employee.Company?.UifNumber` |
| `CompanySarsPayeNumber` | `payslip.Employee.Company?.SarsPayeNumber` |
| `EmployeeName` | `$"{employee.FirstName} {employee.LastName}"` |
| `EmployeeNumber` | `employee.EmployeeNumber` |
| `EmployeeIdNumber` | `employee.IdNumber` |
| `EmployeeUifReference` | `employee.UifReference` |
| `Occupation` | `employee.Occupation` |
| `PayPeriod` | `$"{monthName} {payslip.PayPeriodYear}"` |
| `GrossEarnings` | `payslip.GrossEarnings` |
| `UifDeduction` | `payslip.UifDeduction` |
| `LoanDeductions` | `payslip.LoanDeductions.Select(d => (d.Description, d.Amount))` |
| `TotalDeductions` | `payslip.TotalDeductions` |
| `NetPay` | `payslip.NetPay` |
| `EmployeeStartDate` | `employee.StartDate` |
| `PaymentDate` | `new DateOnly(year, month, DateTime.DaysInMonth(year, month))` |
