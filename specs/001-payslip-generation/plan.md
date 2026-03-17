# Implementation Plan: Modernise PDF Payslip Layout

**Branch**: `001-payslip-generation` | **Date**: 2026-07-15 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/001-payslip-generation/spec.md`

---

## Summary

Replace the flat-text `PdfGenerationService` with a **structured, tabular QuestPDF layout** that
clearly separates the six visual sections of a South-African payslip:

1. **Header** — company logo area, company name, period, payslip reference  
2. **Employer Details** — name, address, UIF reference, SARS PAYE number  
3. **Employee Details** — full name, employee number, ID number, occupation, start date, UIF reference  
4. **Income Table** — line items (Basic Salary, Allowances) with amounts, gross total  
5. **Deductions Table** — UIF, loan deductions, other deductions, total deductions  
6. **Net Pay Summary** — prominent net-pay box, payment date, "This is a computer-generated payslip" footer  

The `PayslipDocument` record (in `Payslip4All.Application`) will be extended with new fields
required for SA compliance and to carry the richer layout data. No domain entity changes are needed;
all new data is either derived from existing entities or supplied by the employer (UIF/SARS numbers
on `Company`).

**Technical approach**: QuestPDF 2024.10.4 Fluent API — two-column panels for employer/employee
details, `Table` API for income and deductions, `Row` + `Background` for the net-pay summary band.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS)  
**Primary Dependencies**: QuestPDF 2024.10.4 (already in `Payslip4All.Infrastructure.csproj`); no new NuGet packages required  
**Storage**: SQLite (dev) / MySQL (prod) via EF Core — no schema changes for the PDF layout itself; optional `Company` fields (`UifNumber`, `SarsPayeNumber`) require migration `AddCompanyUifAndSarsFields`. Phase 6 adds migration `RemovePdfContentColumn` which drops the `PdfContent BLOB` from the `Payslips` table — PDFs are now generated on the fly at download time from stored numeric data, eliminating binary blob storage entirely.
**Testing**: xUnit + Moq (unit tests for `PdfGenerationService` validating byte-array output and document structure; bUnit not needed — no new Blazor components for this feature)  
**Target Platform**: PDF file generation on the server; delivered as a byte array via the existing `IPdfGenerationService` contract  
**Project Type**: Internal service (Infrastructure layer); surface area is the `IPdfGenerationService` interface in Application  
**Performance Goals**: PDF generation ≤ 500 ms for a typical payslip (1–10 loan deductions); acceptable for an on-demand user action  
**Constraints**: Must not change `IPdfGenerationService` method signature — callers (`PayslipGenerationService`) remain unmodified except for constructing the richer `PayslipDocument`; PDF must be A4, print-safe (no web-only fonts)  
**Scale/Scope**: One payslip per generation request; no batching in scope

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Failing tests for `PdfGenerationService` rendering logic and the extended `PayslipDocument` mapping will be written first; byte-array smoke tests + structural assertion tests planned | ✅ |
| II | Clean Architecture | Touches Infrastructure (`PdfGenerationService`), Application (`PayslipDocument` DTO + optional `Company` fields); all inward-only dependencies preserved; still exactly 4 projects | ✅ |
| III | Blazor Web App | No new Razor components; existing download button already wires to `IPayslipService.GetPdfAsync` — no UI change needed for this scope | ✅ |
| IV | Basic Authentication | No new pages or service methods; existing ownership filtering in `PayslipGenerationService` unchanged | ✅ |
| V | Database Support | `AddCompanyUifAndSarsFields` migration adds two nullable `Company` columns. `RemovePdfContentColumn` migration (Phase 6) drops `PdfContent BLOB` from `Payslips`. Both migrations are committed alongside the code changes that require them. | ✅ |
| VI | Manual Test Gate | T023 (Phase 5) — PDF layout visually verified in browser. T031 (Phase 6) — on-the-fly download verified post-`RemovePdfContentColumn` migration. Both gates passed 2026-03-17. | ✅ |

**Post-Design Re-check** (after Phase 1): ✅ No new violations introduced. `PayslipDocument` is a
DTO/record in the Application layer — it has no EF Core dependency. The two new nullable `Company`
fields are optional and additive; the migration is purely `ALTER TABLE ADD COLUMN` compatible with
both SQLite and MySQL.

**Post-Phase 6 re-check** (2026-03-17): ✅ No new constitution violations introduced. `GetPdfAsync`
reconstructs `PayslipDocument` from stored data and calls `_pdfService.GeneratePayslip` — Clean
Architecture layer boundaries remain intact. The `IPdfGenerationService` and `IPayslipService`
method signatures are unchanged.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-payslip-generation/
├── plan.md              ← This file
├── research.md          ← Phase 0: QuestPDF API, SA compliance, typography
├── data-model.md        ← Phase 1 + Phase 6: PDF layout model + entity reconciliation
├── quickstart.md        ← Phase 1: How to build & visually test the PDF
├── contracts/
│   ├── http-endpoints.md   ← Existing (payslip download endpoint)
│   └── ui-contracts.md     ← Existing (Blazor page contracts)
└── tasks.md             ← Phases 1–6 task list (31/31 tasks complete ✅)
```

### Source Code

```text
src/
├── Payslip4All.Domain/
│   └── Entities/
│       ├── Company.cs            ← +UifNumber? (max 50), +SarsPayeNumber? (max 30)
│       └── Payslip.cs            ← PdfContent byte[]? removed (Phase 6)
│
├── Payslip4All.Application/
│   ├── DTOs/
│   │   └── PayslipDocument.cs    ← PayslipDocument record (moved from IPdfGenerationService.cs; +6 SA fields)
│   └── Services/
│       └── PayslipGenerationService.cs  ← GetPdfAsync now builds PayslipDocument on the fly (Phase 6)
│
└── Payslip4All.Infrastructure/
    ├── Services/
    │   └── PdfGenerationService.cs    ← Full rewrite: tabular QuestPDF layout (6 sections)
    └── Migrations/
        ├── <timestamp>_AddCompanyUifAndSarsFields.cs   ← Phase 3: UIF/SARS columns on Company
        └── <timestamp>_RemovePdfContentColumn.cs       ← Phase 6: drops PdfContent BLOB from Payslips

tests/
├── Payslip4All.Domain.Tests/          ← No changes required
├── Payslip4All.Application.Tests/
│   └── Services/
│       └── PayslipGenerationServiceTests.cs  ← PayslipDocument mapping tests (GetPdfAsync; Phase 6)
└── Payslip4All.Infrastructure.Tests/
    └── Services/
        ├── PdfGenerationServiceTests.cs   ← Byte-array + structural tests
        └── PdfBenchmarkTests.cs           ← Performance guard (< 500 ms / < 3 000 ms median)
```

**Structure Decision**: Clean Architecture Option 2 (web application layers) — Domain, Application,
and Infrastructure layers modified. `Payslip4All.Web` and `Payslip4All.Domain.Tests` are
untouched. All dependencies remain strictly inward-only.

---

## Complexity Tracking

> No constitution violations — table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| — | — | — |
