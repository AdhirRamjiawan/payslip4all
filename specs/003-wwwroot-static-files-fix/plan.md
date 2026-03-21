# Implementation Plan: wwwroot Static Files — Hosting Fix

**Branch**: `003-wwwroot-static-files-fix` | **Date**: 2026-03-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-wwwroot-static-files-fix/spec.md`

## Summary

`wwwroot` static files (particularly the build-time CSS isolation bundle
`Payslip4All.Web.styles.css`) are not served when the app runs outside Development
mode, because the static web assets manifest is only loaded automatically in
Development. Separately, the absence of `UseForwardedHeaders()` breaks HTTPS redirect
logic behind a reverse proxy.

Fix approach:
1. Call `builder.WebHost.UseStaticWebAssets()` unconditionally so the build-time
   manifest is loaded in Staging and Production environments as well.
2. Add `UseForwardedHeaders()` as the very first middleware so that forwarded
   scheme/host/IP headers are applied before any redirect or cookie logic.
3. Write integration tests (RED before GREEN) verifying each static asset returns
   `200 OK` with the correct `Content-Type`, in both Development and Production
   environment configurations.

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: ASP.NET Core 8 (`Microsoft.NET.Sdk.Web`), Blazor Server, xUnit, bUnit  
**Storage**: N/A (no database changes)  
**Testing**: xUnit + `WebApplicationFactory<Program>` integration tests  
**Target Platform**: Linux/Windows server; IIS, Kestrel, or reverse-proxy-fronted Kestrel  
**Project Type**: Blazor Server web application  
**Performance Goals**: No new performance targets — static file serving is already O(1)  
**Constraints**: No new NuGet packages (all required APIs are in-box ASP.NET Core)  
**Scale/Scope**: Touches only `Program.cs` in `Payslip4All.Web` + one test file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify compliance with each Payslip4All constitution principle before proceeding:

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned/written before any implementation task begins? | ✅ Yes — integration tests for each asset written RED before code changes |
| II | Clean Architecture | Does the feature touch ≤ 4 projects? Does each layer only depend inward? | ✅ Yes — only `Payslip4All.Web` + `Payslip4All.Web.Tests`; no cross-layer changes |
| III | Blazor Web App | Are all new UI surfaces Razor components? Is business logic kept out of `.razor` files? | ✅ N/A — no new UI surfaces; this is middleware configuration only |
| IV | Basic Authentication | Do new pages carry `[Authorize]`? Do new service methods filter by `UserId`? | ✅ N/A — no new pages or service methods |
| V | Database Support | Are all schema changes represented as named EF Core migrations? | ✅ N/A — no schema changes |
| VI | Manual Test Gate | Is the Manual Test Gate prompt planned before any `git commit`, `git merge`, or `git push`? | ✅ Yes — gate presented before each commit |

> **Any ☐ remaining = blocked.** Document justified exceptions in the Complexity Tracking table below.

## Project Structure

### Documentation (this feature)

```text
specs/003-wwwroot-static-files-fix/
├── plan.md         ← this file
├── research.md     ← Phase 0 output (below)
└── spec.md         ← feature specification
```

### Source Code

```text
src/
└── Payslip4All.Web/
    └── Program.cs        ← add UseStaticWebAssets() + UseForwardedHeaders()

tests/
└── Payslip4All.Web.Tests/
    └── Integration/
        └── StaticFilesIntegrationTests.cs   ← new: assert static assets return 200
```

**Structure Decision**: Single-project change confined to Web layer; tests in existing
`Payslip4All.Web.Tests` integration test directory.

## Complexity Tracking

No constitution violations. All gates pass.
