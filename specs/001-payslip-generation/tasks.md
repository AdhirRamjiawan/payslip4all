---
description: "Task list — Modernise PDF Payslip Layout (001)"
---

# Tasks: Modernise PDF Payslip Layout

**Input**: Design documents from `/specs/001-payslip-generation/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Feature**: Replace the flat-text `PdfGenerationService` with a structured, tabular QuestPDF layout
that clearly separates six visual sections of a South-African payslip. The `PayslipDocument` record
is extended with six new fields for SA compliance. `Company` gains two optional fields (`UifNumber`,
`SarsPayeNumber`) backed by an EF Core migration.

**Tests**: Per constitution Principle I (TDD), tests are **REQUIRED** for all features in this
project (xUnit for unit tests). Test tasks MUST be written and confirmed failing before any
implementation tasks in their phase begin. This is non-negotiable.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.
Phases map to the user stories from `spec.md` that this modernization touches.

**Scope boundary**: No new Blazor components or route changes. No change to the
`IPdfGenerationService.GeneratePayslip` method signature. PDF is A4, server-side, byte-array return.

**Constitution Principle VI**: A Manual Test Gate task closes the final phase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (task operates on a *different file* from other [P]-marked tasks
  in the same group and has no dependency on an incomplete task in the same group)
- **[Story]**: Which user story this task belongs to ([US2] = Company Management,
  [US4] = Monthly Payslip Generation & PDF Download). Setup and Foundational phases carry no story label.
- Exact file paths are included in every task description.

## Tech Stack

**Language/Runtime**: C# 12 / .NET 8 (LTS)
**PDF library**: QuestPDF 2024.10.4 — Fluent API, Table API, Row + Background  
**ORM**: EF Core 8 (SQLite dev / MySQL prod — single migration set)  
**Testing**: xUnit 2.x + Moq 4.x; no new bUnit tests (no new Blazor components)  
**Projects touched**: `Payslip4All.Domain`, `Payslip4All.Application`,
`Payslip4All.Infrastructure`, `Payslip4All.Infrastructure.Tests`,
`Payslip4All.Application.Tests`

---

## Phase 1: Setup

**Purpose**: Confirm the baseline is healthy before any changes are made.

- [X] T001 Verify feature branch baseline — run `dotnet build --warnaserror && dotnet test` from repo root and confirm zero build errors and all existing tests pass (including `PdfBenchmarkTests`)

**Checkpoint**: Green build + green test suite confirmed. All subsequent tasks start from this baseline.

---

## Phase 2: Foundational — PayslipDocument Relocation

**Purpose**: Extract `PayslipDocument` from the interface file into its own DTO file so it can be
extended cleanly. This is a pure refactor with no behavioural change; it unblocks both Phase 3 and
Phase 4.

**⚠️ CRITICAL**: Both Phase 3 and Phase 4 depend on the new file location. Complete this phase
before beginning any user-story work.

- [X] T002 Create `src/Payslip4All.Application/DTOs/PayslipDocument.cs` by moving the `PayslipDocument` record out of `src/Payslip4All.Application/Interfaces/IPdfGenerationService.cs` — keep the same 11 constructor parameters, add `namespace Payslip4All.Application.DTOs`, and ensure the file compiles independently
- [X] T003 Update `src/Payslip4All.Application/Interfaces/IPdfGenerationService.cs` to remove the inline `PayslipDocument` record definition and add `using Payslip4All.Application.DTOs;` — verify `dotnet build` is still green after this change

**Checkpoint**: `PayslipDocument` lives in `src/Payslip4All.Application/DTOs/PayslipDocument.cs`
and is referenced from `IPdfGenerationService.cs`. `dotnet build` is green.

---

## Phase 3: User Story 2 — Employer UIF & SARS Fields (Priority: P2)

**Goal**: `Company` entity carries two new optional fields (`UifNumber`, `SarsPayeNumber`); an EF
Core migration persists them to both SQLite and MySQL providers.

**Independent Test**: Can be verified in isolation — create a `Company` with UIF and SARS values,
persist, reload from the database via EF Core, and assert values round-trip correctly.
No PDF generation required.

**Acceptance criteria tied to spec.md**: Satisfies FR-009 (employer can update company details) and
the Employer Details section of the new PDF layout (plan.md §1 — Section 2).

### Tests for User Story 2 (REQUIRED — TDD, write FIRST, confirm FAILING)

> **MANDATORY**: Write these tests before T005 / T006 / T007. Run `dotnet test` and confirm red.

- [X] T004 [P] [US2] Write failing unit tests for `Company.UifNumber` and `Company.SarsPayeNumber` nullable string properties (assert property exists, accepts null, accepts ≤50 char value, rejects >50 chars in Application-layer guard) in `tests/Payslip4All.Domain.Tests/Entities/CompanyTests.cs`

### Implementation for User Story 2

- [X] T005 [P] [US2] Add `public string? UifNumber { get; set; }` (max 50 chars) and `public string? SarsPayeNumber { get; set; }` (max 30 chars) properties to `src/Payslip4All.Domain/Entities/Company.cs` — nullable, no default value, compatible with existing constructor
- [X] T006 [US2] Update `OnModelCreating` in `src/Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs` to configure `Company.UifNumber` (`HasMaxLength(50)`, nullable) and `Company.SarsPayeNumber` (`HasMaxLength(30)`, nullable) on the `Company` entity builder
- [X] T007 [US2] Generate and commit EF Core migration `AddCompanyUifAndSarsFields` by running `dotnet ef migrations add AddCompanyUifAndSarsFields --project src/Payslip4All.Infrastructure --startup-project src/Payslip4All.Web` — confirm two `AddColumn` statements appear in the generated file at `src/Payslip4All.Infrastructure/Migrations/<timestamp>_AddCompanyUifAndSarsFields.cs`

**Checkpoint**: T004 tests now pass. `dotnet test` is green. Migration file committed alongside
entity and DbContext changes (constitution Principle V).

---

## Phase 4: User Story 4 — Structured PDF Layout (Priority: P4)

**Goal**: `PdfGenerationService` produces an A4 PDF with six distinct, labelled sections using the
QuestPDF Table API, two-column panels, and a highlighted Net Pay band. `PayslipDocument` carries
six new fields. `PayslipGenerationService` populates those fields from `Employee` and `Company`
navigation properties.

**Independent Test**: Construct a `PayslipDocument` with all fields populated (including new fields)
and call `GeneratePayslip` — assert the result is a non-empty byte array and that extracting text
from the PDF (via QuestPDF's `GeneratePdf()` round-trip or text-extraction helper) contains the
expected section labels and key values. End-to-end: generate a payslip via Blazor UI and verify
the downloaded PDF visually shows all six sections.

**Acceptance criteria tied to spec.md**: Satisfies US4 scenario 2 ("correctly formatted PDF file
is produced") and plan.md layout specification (sections 1–6).

### Tests for User Story 4 (REQUIRED — TDD, write FIRST, confirm FAILING)

> **MANDATORY**: Write T008 and T009 before touching any implementation file. Run `dotnet test` and
> confirm both new test classes are **red** before proceeding to T010.

- [X] T008 [US4] Write failing tests for `PdfGenerationService` in `tests/Payslip4All.Infrastructure.Tests/Services/PdfGenerationServiceTests.cs` — include: (a) smoke test asserting `GeneratePayslip` returns a non-empty `byte[]` for a fully-populated `PayslipDocument` with all 17 fields; (b) section-presence tests asserting the PDF byte array is non-null and its length exceeds a minimum threshold; (c) no-loan-deductions test asserting generation succeeds when `LoanDeductions` is an empty list; (d) performance guard asserting generation completes in under 500 ms (single run, not median)
- [X] T009 [P] [US4] Write failing mapping tests for `PayslipGenerationService` in `tests/Payslip4All.Application.Tests/Services/PayslipGenerationServiceTests.cs` — mock `IEmployeeRepository` to return an `Employee` with a fully-populated `Company` (including `UifNumber`, `SarsPayeNumber`) and assert that the `PayslipDocument` passed to `IPdfGenerationService.GeneratePayslip` has: `CompanyUifNumber == employee.Company.UifNumber`, `CompanySarsPayeNumber == employee.Company.SarsPayeNumber`, `EmployeeIdNumber == employee.IdNumber`, `EmployeeStartDate == employee.StartDate`, `EmployeeUifReference == employee.UifReference`, and `PaymentDate` equals the last calendar day of the pay period month

### Implementation for User Story 4

- [X] T010 [US4] Extend `PayslipDocument` record in `src/Payslip4All.Application/DTOs/PayslipDocument.cs` with six new constructor parameters appended after `NetPay`: `string? CompanyUifNumber`, `string? CompanySarsPayeNumber`, `string EmployeeIdNumber`, `DateOnly EmployeeStartDate`, `string? EmployeeUifReference`, `DateOnly PaymentDate` — keep all 11 existing parameters in their original positions to avoid breaking callers before T013 and T019 update them
- [X] T011 [US4] Replace the entire body of `PdfGenerationService.GeneratePayslip` in `src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs` with a QuestPDF scaffold: set `QuestPDF.Settings.License = LicenseType.Community;`, create `Document.Create(...)` with `page.Size(PageSizes.A4)`, `page.Margin(2, Unit.Centimetre)`, and a top-level `page.Content().Column(col => { ... })` containing six placeholder `col.Item().Text("SECTION N")` calls — one per section — so the document compiles and T008 smoke test turns green
- [X] T012 [US4] Implement **Section 1 — Header** inside `PdfGenerationService.GeneratePayslip` in `src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs`: replace the Section 1 placeholder with a `Row` containing company name (bold, 14pt, left-aligned) and a right-aligned column showing pay period and a payslip reference derived as `$"REF-{document.PayPeriod.Replace(" ", "").ToUpperInvariant()}"`, followed by a full-width horizontal rule (`LineHorizontal(1)`)
- [X] T013 [US4] Implement **Section 2 — Employer Details** inside `PdfGenerationService.GeneratePayslip` in `src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs`: replace the Section 2 placeholder with a section heading "EMPLOYER DETAILS" (bold, 9pt, grey background padding) and a two-column `Row` — left column: Company Name and Address; right column: UIF Reference (shows `document.CompanyUifNumber ?? "—"`) and SARS PAYE Number (shows `document.CompanySarsPayeNumber ?? "—"`)
- [X] T014 [US4] Implement **Section 3 — Employee Details** inside `PdfGenerationService.GeneratePayslip` in `src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs`: replace the Section 3 placeholder with a section heading "EMPLOYEE DETAILS" and a two-column `Row` — left column: Full Name, Employee Number, ID Number; right column: Occupation, Start Date (`document.EmployeeStartDate.ToString("d MMM yyyy")`), UIF Reference (`document.EmployeeUifReference ?? "—"`)
- [X] T015 [US4] Implement **Section 4 — Income Table** inside `PdfGenerationService.GeneratePayslip` in `src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs`: replace the Section 4 placeholder with a section heading "INCOME", then a QuestPDF `Table` with columns `[Description | Amount]` — one row "Basic Salary / R{GrossEarnings:N2}", a divider row, and a bold "Gross Earnings / R{GrossEarnings:N2}" total row with light grey background
- [X] T016 [US4] Implement **Section 5 — Deductions Table** inside `PdfGenerationService.GeneratePayslip` in `src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs`: replace the Section 5 placeholder with a section heading "DEDUCTIONS", then a QuestPDF `Table` with columns `[Description | Amount]` — first row "UIF Deduction / R{UifDeduction:N2}", then one row per entry in `LoanDeductions` using `(desc, amt)` tuple, then a divider, then a bold "Total Deductions / R{TotalDeductions:N2}" row with light grey background; if `LoanDeductions` is empty, render only the UIF row and total
- [X] T017 [US4] Implement **Section 6 — Net Pay Summary** inside `PdfGenerationService.GeneratePayslip` in `src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs`: replace the Section 6 placeholder with a full-width `Row` with dark grey `Background`, containing left-aligned label "NET PAY" (white, bold, 11pt) and right-aligned amount `R{document.NetPay:N2}` (white, bold, 16pt); below the band add "Payment Date: {document.PaymentDate:d MMM yyyy}" (right-aligned, 9pt) and a centred footer "This is a computer-generated payslip" (italic, 8pt, grey)
- [X] T018 [P] [US4] Update `PayslipGenerationService.GeneratePayslipAsync` in `src/Payslip4All.Application/Services/PayslipGenerationService.cs` to populate the six new `PayslipDocument` constructor parameters: `CompanyUifNumber: employee.Company?.UifNumber`, `CompanySarsPayeNumber: employee.Company?.SarsPayeNumber`, `EmployeeIdNumber: employee.IdNumber`, `EmployeeStartDate: employee.StartDate`, `EmployeeUifReference: employee.UifReference`, `PaymentDate: new DateOnly(command.PayPeriodYear, command.PayPeriodMonth, DateTime.DaysInMonth(command.PayPeriodYear, command.PayPeriodMonth))`
- [X] T019 [P] [US4] Update `PdfBenchmarkTests.BuildRepresentativeDocument()` in `tests/Payslip4All.Infrastructure.Tests/Services/PdfBenchmarkTests.cs` to supply all six new `PayslipDocument` constructor arguments: `CompanyUifNumber: "U123456"`, `CompanySarsPayeNumber: "7654321A"`, `EmployeeIdNumber: "9001015009087"`, `EmployeeStartDate: new DateOnly(2021, 3, 1)`, `EmployeeUifReference: "UIF-EMP-001"`, `PaymentDate: new DateOnly(2025, 1, 31)` — confirm benchmark still passes ≤ 3 000 ms median threshold

**Checkpoint**: All Phase 4 tests pass (T008, T009). `PdfBenchmarkTests` still passes.
`dotnet test` is fully green. PDF generates the six-section layout.

---

## Phase 5: Polish & Manual Test Gate

**Purpose**: Validate the full delivery end-to-end — automated pipeline health, migration applied to
the dev database, and a manual visual check of the generated PDF (constitution Principle VI).

- [X] T020 [P] Run `dotnet build --warnaserror` across the entire solution from the repo root and confirm zero build warnings and zero errors — resolve any CS warnings introduced by new properties or constructor changes
- [X] T021 [P] Run `dotnet test` from the repo root and confirm all tests pass, including the new `PdfGenerationServiceTests` and the updated `PdfBenchmarkTests` (median < 500 ms for single-run guard in T008 and < 3 000 ms median in the benchmark)
- [X] T022 Apply the `AddCompanyUifAndSarsFields` migration to the local SQLite dev database by running `dotnet ef database update --project src/Payslip4All.Infrastructure --startup-project src/Payslip4All.Web` and confirm `Companies` table now contains `UifNumber` and `SarsPayeNumber` columns (inspect via SQLite browser or `dotnet ef dbcontext info`)
- [ ] T023 **Manual Test Gate** (constitution Principle VI): start the app with `dotnet run --project src/Payslip4All.Web`, sign in as a registered employer, edit a company and enter a UIF reference and SARS PAYE number, navigate to an employee, generate a payslip for the current month, download the PDF, open it and visually confirm: (1) Header shows company name and pay period reference, (2) Employer Details shows UIF/SARS values, (3) Employee Details shows ID number, start date, UIF reference, (4) Income Table shows Basic Salary and Gross Earnings rows, (5) Deductions Table shows UIF row and any loan rows, (6) Net Pay band is prominent and shows correct net pay and payment date with footer text — mark T023 complete only when all six sections render correctly on screen

**Checkpoint**: All automated tests green. Migration applied. PDF visually validated. Feature complete.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
  └─→ Phase 2 (Foundational: PayslipDocument relocation) — BLOCKS Phase 3 and Phase 4
        ├─→ Phase 3 (US2: Company UIF/SARS fields) — independent of Phase 4
        └─→ Phase 4 (US4: Structured PDF Layout) — independent of Phase 3
              └─→ Phase 5 (Polish & Manual Test Gate)
```

### User Story Dependencies

- **Phase 2 (Foundational)**: No user story — pure refactor. Unblocks everything.
- **Phase 3 (US2)**: Depends only on Phase 2. Can begin as soon as T003 is done.
  Company entity, DbContext, and migration are self-contained; no Phase 4 dependency.
- **Phase 4 (US4)**: Depends on Phase 2. Can begin as soon as T003 is done.
  T010 (extend `PayslipDocument`) must complete before T011–T019 begin.
- **Phase 3 and Phase 4** can proceed in parallel (different files throughout).

### Within Phase 4

```
T008 (write PdfGenerationServiceTests.cs)  ─┐
T009 (write PayslipGenerationServiceTests.cs) ─┤ [P] with each other (different files)
                                               ↓
T010 (extend PayslipDocument.cs — adds 6 fields)
       ↓
T011 (scaffold PdfGenerationService.cs)
       ↓  (sequential — same file)
T012 → T013 → T014 → T015 → T016 → T017   ← (sections 1–6, sequential, same file)

T018 [P] (PayslipGenerationService.cs — different file, depends on T010)
T019 [P] (PdfBenchmarkTests.cs — different file, depends on T010)
  ↑ both can run in parallel with T011–T017 once T010 is done
```

### Parallel Opportunities by Phase

**Phase 2**: T002 → T003 are sequential (T003 references T002 output).

**Phase 3**:
- T004 [P] and T005 [P] are parallel (different files: `CompanyTests.cs` vs `Company.cs`)
- T006 depends on T005 (DbContext must configure the new properties)
- T007 depends on T006 (migration requires updated OnModelCreating)

**Phase 4**:
- T008 and T009 [P] are parallel (different test files)
- T011–T017 are sequential (same file, same method, sections build on each other)
- T018 [P] and T019 [P] can run in parallel with T011–T017 and with each other
  (different files; both only need T010 complete)

**Phase 5**:
- T020 [P] and T021 [P] are parallel (read-only build/test checks)
- T022 and T023 are sequential (T022 applies migration; T023 manual test requires running app)

---

## Parallel Example: Phase 4 (US4)

```bash
# After T009/T010 pass, launch these in parallel:

# Developer A — PDF Layout sections (sequential within this stream):
T011 → T012 → T013 → T014 → T015 → T016 → T017
File: src/Payslip4All.Infrastructure/Services/PdfGenerationService.cs

# Developer B — Application service mapping (parallel with A):
T018
File: src/Payslip4All.Application/Services/PayslipGenerationService.cs

# Developer C — Test/benchmark fix (parallel with A and B):
T019
File: tests/Payslip4All.Infrastructure.Tests/Services/PdfBenchmarkTests.cs
```

---

## Implementation Strategy

### MVP Scope (minimum to deliver a structured PDF)

1. Complete **Phase 1** (T001) — confirm baseline
2. Complete **Phase 2** (T002–T003) — relocate PayslipDocument
3. Complete **Phase 4 tests** (T008–T009) — write failing tests
4. Complete **Phase 4 implementation** in order: T010 → T011 → T012–T017 → T018 → T019
5. **STOP and validate**: `dotnet test` green; open generated PDF in browser
6. PDF is deliverable. Phase 3 (Company UIF/SARS fields) can follow as a second increment.

### Incremental Delivery

| Increment | Phases Completed | Deliverable |
|-----------|-----------------|-------------|
| 1 — PDF layout | Phase 1 + 2 + 4 | Structured 6-section PDF; new fields default to null |
| 2 — UIF/SARS data | Phase 3 | Employer can persist and display UIF/SARS numbers on PDF |
| 3 — Full validation | Phase 5 | Green CI + manual sign-off; feature declared done |

### Parallel Team Strategy

With two developers:
- Developer A: Phase 2 → Phase 4 (PDF layout, primary stream)
- Developer B: Phase 3 (Company entity + migration, can run while A works on Phase 4 post-T003)

---

## Summary

| Metric | Value |
|--------|-------|
| Total tasks | 23 |
| Phase 2 (Foundational) | 2 tasks |
| Phase 3 — US2 | 4 tasks (1 test + 3 impl) |
| Phase 4 — US4 | 12 tasks (2 tests + 10 impl) |
| Phase 5 (Polish + Gate) | 4 tasks |
| Tasks with [P] marker | 9 |
| Files modified (src) | 5 |
| Files modified/created (tests) | 3 |
| New EF Core migration | 1 (`AddCompanyUifAndSarsFields`) |
| New `PayslipDocument` fields | 6 |
| PDF sections implemented | 6 |

**Suggested MVP**: Phase 1 + Phase 2 + Phase 4 (sections 1–6 with null-safe defaults for UIF/SARS).
Run Phase 3 immediately after for full SA compliance data surfacing.

---

## Notes

- `[P]` tasks operate on different files — no same-file conflicts possible within the parallel group
- `[US2]` label = tasks that extend Company Management data model
- `[US4]` label = tasks that deliver or support the PDF layout output
- Each phase is independently completable and testable
- Constitution Principle I (TDD): T008 and T009 MUST be red before T010 begins
- Constitution Principle V (EF migration): T007 must be committed alongside T005 + T006
- Constitution Principle VI (Manual Test Gate): T023 is non-optional and cannot be auto-generated
- `dotnet ef` commands require the EF Core tools package; run from the repo root
- QuestPDF `LicenseType.Community` must be set before any document is generated (already in `PdfGenerationService`; also set in test constructors)
- PDF font: QuestPDF defaults to Lato (bundled); no system font dependency — print-safe on all platforms
