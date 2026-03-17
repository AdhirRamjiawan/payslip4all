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
**Storage**: SQLite (dev) / MySQL (prod) via EF Core — no schema changes for the PDF layout itself; optional `Company` fields (`UifNumber`, `SarsPayeNumber`) require a new EF Core migration  
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
| V | Database Support | If `Company` gains `UifNumber` / `SarsPayeNumber` fields, an EF Core migration (`AddCompanyUifAndSarsFields`) is required and MUST be committed alongside the code | ✅ |

> All gates pass. No exceptions required.

**Post-Design Re-check** (after Phase 1): ✅ No new violations introduced. `PayslipDocument` is a
DTO/record in the Application layer — it has no EF Core dependency. The two new nullable `Company`
fields are optional and additive; the migration is purely `ALTER TABLE ADD COLUMN` compatible with
both SQLite and MySQL.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-payslip-generation/
├── plan.md              ← This file
├── research.md          ← Phase 0: QuestPDF API, SA compliance, typography
├── data-model.md        ← Phase 1: PDF layout model + extended PayslipDocument
├── quickstart.md        ← Phase 1: How to build & visually test the PDF
├── contracts/
│   ├── http-endpoints.md   ← Existing (payslip download endpoint)
│   └── ui-contracts.md     ← Existing (Blazor page contracts)
└── tasks.md             ← Phase 2 output (speckit.tasks — not yet created)
```

### Source Code

```text
src/
├── Payslip4All.Domain/
│   └── Entities/
│       └── Company.cs            ← +UifNumber?, +SarsPayeNumber? (optional)
│
├── Payslip4All.Application/
│   └── Interfaces/
│       └── IPdfGenerationService.cs   ← PayslipDocument record extended
│
└── Payslip4All.Infrastructure/
    ├── Services/
    │   └── PdfGenerationService.cs    ← Full rewrite: tabular QuestPDF layout
    └── Migrations/
        └── <timestamp>_AddCompanyUifAndSarsFields.cs   ← New migration

tests/
├── Payslip4All.Domain.Tests/          ← No changes required
├── Payslip4All.Application.Tests/     ← PayslipDocument mapping tests
└── Payslip4All.Infrastructure.Tests/
    └── Services/
        └── PdfGenerationServiceTests.cs   ← NEW: byte-array + structural tests
```

**Structure Decision**: Clean Architecture Option 2 (web application layers) — only
Infrastructure and Application layers are modified. No Domain entity logic changes; the two
new `Company` fields are purely storage additions with no behaviour.

---

## Complexity Tracking

> No constitution violations — table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| — | — | — |
