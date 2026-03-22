# Feature Specification: File Logging in Published/Hosted Environments

**Feature Branch**: `005-file-logging-published-fix`
**Created**: 2026-03-22
**Status**: Draft

## Problem Statement

The published site does not write logs to file in hosted environments. The Serilog
file sink is configured with a **relative path** (`logs/payslip4all-.log` in
`appsettings.json`). A relative path is resolved against the **OS process working
directory** (CWD), not the application directory.

When the application is hosted via systemd, IIS, or any service manager that does
not set `WorkingDirectory` to the app's directory, the CWD defaults to `/` (systemd)
or some other unrelated directory. Serilog silently fails to create `/logs/` (no write
permission) and the file sink produces no output. No exception is raised; the console
sink continues working, masking the problem.

**Why it works locally**: `dotnet run` and running the binary directly from the
publish output directory both set the CWD to the app/publish directory by convention,
so the relative path resolves correctly. This creates a false impression that logging
is working.

## Root Cause

`Serilog.Sinks.File` resolves its `path` argument using `System.IO` which uses the
OS process CWD. ASP.NET Core sets its **content root** (used for configuration and
static files) but does NOT change the OS CWD. The two can differ in hosted scenarios.

`AppContext.BaseDirectory` is always the directory containing the executing assembly,
regardless of CWD or how the process was launched.

## Solution

Before `builder.Host.UseSerilog()` reads the configuration, inspect the configured
file sink path. If the path is relative, rewrite it to be absolute by combining it
with `AppContext.BaseDirectory`. Since `builder.Configuration` is an
`IConfiguration`/`ConfigurationManager`, setting a key mutates the in-memory value
that subsequent `ReadFrom.Configuration()` calls will read.

```csharp
// Normalise the Serilog file sink path to be absolute so logs are written
// to the app directory regardless of the OS process working directory.
const string logPathKey = "Serilog:WriteTo:1:Args:path";
var configuredLogPath = builder.Configuration[logPathKey];
if (!string.IsNullOrEmpty(configuredLogPath) && !Path.IsPathRooted(configuredLogPath))
{
    builder.Configuration[logPathKey] =
        Path.Combine(AppContext.BaseDirectory, configuredLogPath);
}
```

This approach:
- Works in all hosting scenarios (systemd, IIS, Docker, manual).
- Requires no new NuGet packages.
- Is overridable: a production `appsettings.Production.json` with an absolute path
  (`/var/log/payslip4all/payslip4all-.log`) bypasses the normalisation because
  `Path.IsPathRooted()` returns `true`.
- Is transparent to integration tests: `WebApplicationFactory.ConfigureAppConfiguration`
  adds overrides with higher configuration priority AFTER this mutation, so tests
  continue to redirect to their temp paths unaffected.

## Architecture & TDD Alignment

Changes are confined to `Payslip4All.Web` (presentation/bootstrap layer). No Domain,
Application, or Infrastructure changes needed.

### User Story 1 — Logs written to app directory regardless of CWD (Priority: P1)

**Acceptance Scenarios**:

1. **Given** the OS CWD is set to a directory other than the app directory,
   **When** the application starts and receives an HTTP request,
   **Then** a log file is created in `{AppBaseDirectory}/logs/` and contains entries.

2. **Given** `appsettings.json` has the default relative log path,
   **When** the effective Serilog path is resolved,
   **Then** `Path.IsPathRooted(resolvedPath)` is `true` and the path starts with
   `AppContext.BaseDirectory`.

3. **Given** `appsettings.Production.json` specifies an absolute log path,
   **When** the normalisation logic runs,
   **Then** the configured absolute path is preserved unchanged (no double-rooting).

---

## Out of Scope

- Log aggregation, remote sinks, or structured log storage.
- Changing the log path from `logs/` to a system path like `/var/log/`.
- Log rotation beyond existing 31-day retention setting.
- Changes to Domain, Application, or Infrastructure layers.
