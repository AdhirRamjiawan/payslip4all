# Feature Specification: wwwroot Hosting — CSS Bundle Physical Copy

**Feature Branch**: `004-wwwroot-hosting-manifest-fix`
**Created**: 2026-03-21
**Status**: Draft

## Problem Statement

The application fails to serve `Payslip4All.Web.styles.css` (the Blazor CSS isolation
bundle) in hosted environments because:

1. **The bundle is generated, not source** — it is produced by the Blazor SDK at build
   time into `obj/{config}/{tfm}/scopedcss/bundle/`. It does NOT exist in the source
   `wwwroot/` directory, so it is not served by `UseStaticFiles()` unless the
   `.staticwebassets.runtime.json` manifest is present.

2. **The manifest is never in the hosted output** — `.staticwebassets.runtime.json` is
   a development-time file. It is NOT included in `dotnet publish` output and NOT
   present on a production server. The previous fix (`builder.WebHost.UseStaticWebAssets()`)
   is therefore a no-op in the hosted environment.

3. **The publish output IS correct** — `dotnet publish` does copy the CSS bundle to
   `wwwroot/` in the publish directory. But if the hosting environment runs from build
   output (not publish), or if files were deployed without the `wwwroot/` folder, the
   bundle is missing.

## Root Cause

There is no reliable path for the CSS isolation bundle to reach the physical `wwwroot/`
directory in non-publish deployment scenarios. The project must be rebuilt or the MSBuild
pipeline must explicitly copy the file.

## Solution

Add an **MSBuild post-build target** to `Payslip4All.Web.csproj` that copies the
generated CSS isolation bundle from `obj/` to the physical `wwwroot/` directory after
every successful build. This makes the bundle physically present in `wwwroot/` at all
times, eliminating the dependency on the runtime manifest and working regardless of
the deployment mechanism used.

Add `wwwroot/Payslip4All.Web.styles.css` to `.gitignore` so the generated artifact
is not committed to source control.

Also remove `builder.WebHost.UseStaticWebAssets()` from `Program.cs` — it is now
redundant (the bundle is always physical) and misleads developers into thinking the
manifest file is needed.

## Architecture & TDD Alignment

All changes are confined to `Payslip4All.Web` (presentation/build layer). No Domain,
Application, or Infrastructure changes needed.

### User Story 1 — CSS bundle physically present after build (Priority: P1)

After running `dotnet build` on `Payslip4All.Web`, the file
`src/Payslip4All.Web/wwwroot/Payslip4All.Web.styles.css` must exist on disk and
contain the scoped CSS rules.

**Acceptance Scenarios**:

1. **Given** a clean build is performed, **When** `dotnet build` completes,
   **Then** `wwwroot/Payslip4All.Web.styles.css` exists and contains scoped CSS.

2. **Given** the CSS bundle is physically in `wwwroot/`, **When** the app runs in
   `Production` environment (no manifest), **Then** GET `/Payslip4All.Web.styles.css`
   returns `200 OK` with `Content-Type: text/css`.

3. **Given** `dotnet publish` produces its output, **When** the server hosts the
   published output, **Then** GET `/Payslip4All.Web.styles.css` returns `200 OK`.

---

## Out of Scope

- Changes to Domain, Application, or Infrastructure layers.
- CDN or asset fingerprinting.
- HTTPS/TLS configuration beyond what was done in feature 003.
- Service worker / PWA.
