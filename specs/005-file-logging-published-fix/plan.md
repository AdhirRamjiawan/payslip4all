# Implementation Plan: File Logging in Published/Hosted Environments

**Branch**: `005-file-logging-published-fix` | **Date**: 2026-03-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-file-logging-published-fix/spec.md`

## Summary

The Serilog file sink path `logs/payslip4all-.log` is relative, so it resolves
against the **OS process working directory** (CWD) — not the application directory.
In hosted environments (systemd without `WorkingDirectory=`, IIS, Docker), the CWD
may not be the app directory, causing Serilog to silently fail to create the log file.

Fix: In `Program.cs`, before `builder.Host.UseSerilog()` reads configuration, rewrite
the file sink path to be absolute using `AppContext.BaseDirectory` if the configured
path is relative. One new integration test provides the RED → GREEN TDD gate.

## Technical Context

**Language/Version**: C# 12 / .NET 8
**Primary Dependencies**: Serilog.AspNetCore 10.x (already installed)
**Storage**: N/A
**Testing**: xUnit + WebApplicationFactory<Program> — existing [Collection("WebIntegration")]
**Target Platform**: Linux systemd, IIS, Docker, manual dotnet publish
**Project Type**: Blazor Server web application
**Performance Goals**: N/A — one-time path resolution at startup
**Constraints**: No new NuGet packages; existing config structure preserved
**Scale/Scope**: 4 lines of code in Program.cs, 1 new test method

## Constitution Check

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned before implementation? | YES — new test is the RED gate |
| II | Clean Architecture | Touches ≤ 4 projects? Inward-only deps? | YES — only Payslip4All.Web |
| III | Blazor Web App | New UI surfaces in Razor components? | N/A — no UI changes |
| IV | Basic Authentication | New pages carry [Authorize]? | N/A — no new pages |
| V | Database Support | Schema changes as EF Core migrations? | N/A — no schema changes |
| VI | Manual Test Gate | Gate prompt planned before each commit? | YES |

## Project Structure

### Documentation (this feature)

```
specs/005-file-logging-published-fix/
├── plan.md         <- this file
├── research.md     <- Phase 0 output
└── spec.md         <- feature specification
```

### Source Code

```
src/
└── Payslip4All.Web/
    └── Program.cs    <- add path normalisation before UseSerilog()

tests/
└── Payslip4All.Web.Tests/
    └── Integration/
        └── LoggingIntegrationTests.cs  <- add 1 new test method
```

## Complexity Tracking

No constitution violations. All gates pass.
