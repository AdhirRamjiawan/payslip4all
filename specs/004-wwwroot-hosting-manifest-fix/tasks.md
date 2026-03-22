# Tasks: wwwroot Hosting — CSS Bundle Physical Copy

**Feature**: 004-wwwroot-hosting-manifest-fix  
**Branch**: `004-wwwroot-hosting-manifest-fix`

## Phase 1 — Verify TDD Gate (RED)

- [X] T1: Confirm `CssIsolationBundle_IsServed_InProductionEnvironment` is currently GREEN (passes with existing `UseStaticWebAssets()` via manifest in bin/)

## Phase 2 — Implementation

- [X] T2: Add MSBuild `AfterTargets="Build"` target to `src/Payslip4All.Web/Payslip4All.Web.csproj` that copies the generated CSS isolation bundle from `obj/` to physical `wwwroot/`
- [X] T3: Add `src/Payslip4All.Web/wwwroot/Payslip4All.Web.styles.css` to `.gitignore`
- [X] T4: Remove `builder.WebHost.UseStaticWebAssets()` from `src/Payslip4All.Web/Program.cs`
- [X] T5: Run `dotnet build src/Payslip4All.Web` and verify file is physically present in `wwwroot/`

## Phase 3 — Validation

- [X] T6: Run full test suite — all 169 tests must pass (TDD gate: `CssIsolationBundle_IsServed_InProductionEnvironment` stays GREEN)
- [ ] T7: Manual Test Gate — verify static file serving works end to end
