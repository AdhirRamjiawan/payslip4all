# Tasks: wwwroot Static Files — Hosting Fix

**Branch**: `003-wwwroot-static-files-fix`
**Feature**: Static files served correctly in all hosting environments
**Plan**: [plan.md](plan.md) | **Spec**: [spec.md](spec.md)

---

## Phase 1 — Tests (RED)

### Task 1.1 — Write failing integration tests for static assets
**File**: `tests/Payslip4All.Web.Tests/Integration/StaticFilesIntegrationTests.cs`
**Status**: [X]

Create integration tests using `WebApplicationFactory<Program>`:
- `CssIsolationBundle_IsServed_InDevelopmentEnvironment` — GET `/Payslip4All.Web.styles.css` returns 200 with `text/css`
- `CssIsolationBundle_IsServed_InProductionEnvironment` — GET `/Payslip4All.Web.styles.css` returns 200 with `text/css` when factory overrides env to `Production`
- `BootstrapCss_IsServed` — GET `/css/bootstrap/bootstrap.min.css` returns 200 with `text/css`
- `SiteCss_IsServed` — GET `/css/site.css` returns 200 with `text/css`
- `Favicon_IsServed` — GET `/favicon.png` returns 200 with `image/png`

Tests MUST be RED (fail) before any implementation changes — the `Production` env test for the CSS isolation bundle will fail because `UseStaticWebAssets()` is not yet called.

---

## Phase 2 — Implementation

### Task 2.1 — Add UseStaticWebAssets() to Program.cs
**File**: `src/Payslip4All.Web/Program.cs`
**Depends on**: Task 1.1 (tests written)
**Status**: [X]

Add `builder.WebHost.UseStaticWebAssets();` immediately after the Serilog bootstrap
and before `builder.Build()`. This loads the static web assets manifest in non-Development
environments so the CSS isolation bundle (`Payslip4All.Web.styles.css`) is found.

### Task 2.2 — Add UseForwardedHeaders() middleware
**File**: `src/Payslip4All.Web/Program.cs`
**Depends on**: Task 1.1 (tests written)
**Status**: [X]

1. Register `ForwardedHeadersOptions` in DI (before `builder.Build()`):
   - `options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto`
   - `options.KnownNetworks.Clear()` + `options.KnownProxies.Clear()`
2. Call `app.UseForwardedHeaders()` as the **first** middleware (before `UseHttpsRedirection()`).
   Using `Microsoft.AspNetCore.HttpOverrides` — no new NuGet package needed.

---

## Phase 3 — Validation

### Task 3.1 — Run full test suite (GREEN)
**Depends on**: Tasks 2.1, 2.2
**Status**: [X]

Run `dotnet test Payslip4All.sln` and verify:
- All 5 new static file tests pass
- All 164 existing tests still pass
- No build errors or warnings introduced

### Task 3.2 — Manual Test Gate
**Depends on**: Task 3.1
**Status**: [ ]

Present Manual Test Gate (Constitution Principle VI):
- Run app locally with `ASPNETCORE_ENVIRONMENT=Production dotnet run`
- Open browser, navigate to `/` — verify page loads with styles applied
- Verify no redirect loops occur
- Await engineer approval before committing
