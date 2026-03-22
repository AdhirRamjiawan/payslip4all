# Implementation Plan: wwwroot Hosting — CSS Bundle Physical Copy

**Branch**: `004-wwwroot-hosting-manifest-fix` | **Date**: 2026-03-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-wwwroot-hosting-manifest-fix/spec.md`

## Summary

The Blazor CSS isolation bundle (`Payslip4All.Web.styles.css`) is generated at build
time into `obj/{config}/{tfm}/scopedcss/bundle/` but never reaches the physical
`wwwroot/` directory until `dotnet publish` is run. Hosted environments without a
`dotnet publish` step — or those that copy only DLLs — cannot serve this file because
the runtime manifest (`.staticwebassets.runtime.json`) is absent outside development.

Fix: Add an MSBuild `AfterTargets="Build"` target to `Payslip4All.Web.csproj` that
copies the generated bundle to `wwwroot/` after every build. Gitignore the copied file.
Remove the now-redundant `builder.WebHost.UseStaticWebAssets()` call from `Program.cs`.

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: MSBuild (in-SDK), ASP.NET Core 8, xUnit  
**Storage**: N/A  
**Testing**: xUnit + `WebApplicationFactory<Program>` — existing `StaticFilesIntegrationTests`  
**Target Platform**: Linux/Windows server; IIS, Kestrel, published or non-published  
**Project Type**: Blazor Server web application  
**Performance Goals**: N/A — one file copy on build  
**Constraints**: No new NuGet packages; MSBuild only  
**Scale/Scope**: One csproj change, one Program.cs change, one .gitignore entry

## Constitution Check

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned before implementation? | ✅ Existing `CssIsolationBundle_IsServed_InProductionEnvironment` test is the RED gate |
| II | Clean Architecture | Touches ≤ 4 projects? Inward-only deps? | ✅ Only `Payslip4All.Web` (build config + Program.cs) |
| III | Blazor Web App | New UI surfaces in Razor components? | ✅ N/A — no new UI surfaces |
| IV | Basic Authentication | New pages carry `[Authorize]`? | ✅ N/A — no new pages |
| V | Database Support | Schema changes as EF Core migrations? | ✅ N/A — no schema changes |
| VI | Manual Test Gate | Gate prompt planned before each commit? | ✅ Yes |

## Project Structure

### Documentation (this feature)

```text
specs/004-wwwroot-hosting-manifest-fix/
├── plan.md         ← this file
├── research.md     ← Phase 0 output
└── spec.md         ← feature specification
```

### Source Code

```text
src/
└── Payslip4All.Web/
    ├── Payslip4All.Web.csproj   ← add AfterTargets="Build" MSBuild target
    └── Program.cs               ← remove UseStaticWebAssets() (now redundant)

.gitignore                       ← add wwwroot/Payslip4All.Web.styles.css
```

## Complexity Tracking

No constitution violations. All gates pass.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION]  
**Primary Dependencies**: [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]  
**Storage**: [if applicable, e.g., PostgreSQL, CoreData, files or N/A]  
**Testing**: [e.g., pytest, XCTest, cargo test or NEEDS CLARIFICATION]  
**Target Platform**: [e.g., Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]
**Project Type**: [e.g., library/cli/web-service/mobile-app/compiler/desktop-app or NEEDS CLARIFICATION]  
**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]  
**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]  
**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify compliance with each Payslip4All constitution principle before proceeding:

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | TDD | Are failing tests planned/written before any implementation task begins? | ☐ |
| II | Clean Architecture | Does the feature touch ≤ 4 projects (Domain/Application/Infrastructure/Web)? Does each layer only depend inward? | ☐ |
| III | Blazor Web App | Are all new UI surfaces Razor components? Is business logic kept out of `.razor` files? | ☐ |
| IV | Basic Authentication | Do new pages carry `[Authorize]`? Do new service methods filter by `UserId`? | ☐ |
| V | Database Support | Are all schema changes represented as named EF Core migrations? Is raw SQL avoided? | ☐ |
| VI | Manual Test Gate | Is the Manual Test Gate prompt planned at the end of each implementation task, before any `git commit`, `git merge`, or `git push`? | ☐ |

> **Any ☐ remaining = blocked.** Document justified exceptions in the Complexity Tracking table below.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
