# Research: File Logging in Published/Hosted Environments

## Decision 1: Normalise log path using `AppContext.BaseDirectory` in Program.cs

**Decision**: Before `builder.Host.UseSerilog()` reads configuration, check if
`Serilog:WriteTo:1:Args:path` is a relative path. If so, rewrite it to an absolute
path by prepending `AppContext.BaseDirectory`.

**Rationale**:
- `AppContext.BaseDirectory` is the directory containing the executing assembly DLL.
  It is always absolute and set by the CLR at startup — independent of the OS CWD.
- In a `dotnet publish` output, `AppContext.BaseDirectory` is the publish directory
  (e.g., `/var/www/payslip4all/`). The `logs/` subdirectory is created there.
- In development, `AppContext.BaseDirectory` is `bin/Debug/net8.0/` — the same
  directory the existing tests already use as their working context.
- Serilog's File sink creates missing directories automatically, so `logs/` does not
  need to be pre-created.
- `builder.Configuration[key] = value` mutates the in-memory `ConfigurationManager`,
  which is read by `ReadFrom.Configuration()` inside the `UseSerilog` callback.
  `WebApplicationFactory` adds its in-memory overrides AFTER this mutation (higher
  priority), so test path overrides continue to work correctly.

**Why not `ContentRootPath`?**
- `builder.Environment.ContentRootPath` is set during `WebApplication.CreateBuilder()`
  but is only reliable after the host is fully built. `AppContext.BaseDirectory` is
  available immediately and does not change.
- In published deployments, ContentRootPath and AppContext.BaseDirectory are the same.

**Why not environment variables?**
- Requires ops configuration in addition to code. The fix should work out of the box.

**Alternatives considered**:
- Configure `WorkingDirectory=` in the systemd unit — shifts burden to ops, doesn't
  fix the root cause for other hosting platforms (IIS, Docker). ❌
- Use an absolute path in `appsettings.json` — hardcodes a server path; breaks
  development portability. ❌
- Use `Directory.SetCurrentDirectory(AppContext.BaseDirectory)` — changes global CWD;
  side-effects on relative path resolution elsewhere in the app. ❌

---

## Decision 2: Hardcode the WriteTo sink index as 1

**Decision**: The normalisation code references the file sink as `WriteTo:1:Args:path`,
matching the array index in `appsettings.json`.

**Rationale**:
- The config has exactly two sinks: index 0 = Console, index 1 = File. This is stable
  and well-documented (the Serilog integration tests already rely on this index).
- Searching for the sink by `Name == "File"` is possible but more complex; the index
  is simple and correct for this project's single-file-sink design.
- A comment in Program.cs documents the index convention, so future developers are
  aware of the dependency.

---

## Decision 3: TDD gate — new test for CWD-independence

**Decision**: Add one new integration test:
`LogFile_IsCreated_InAppBaseDirectory_WhenCwdIsChanged`.

This test:
1. Saves and restores `Environment.CurrentDirectory`.
2. Changes CWD to `Path.GetTempPath()` (simulates wrong CWD from service manager).
3. Creates a `WebApplicationFactory` WITHOUT overriding the log path.
4. Makes an HTTP request.
5. Asserts the log file is created in `AppContext.BaseDirectory/logs/`, NOT in
   `Path.GetTempPath()/logs/`.
6. Cleans up the created log file (the file is in `AppContext.BaseDirectory` which is
   the test bin dir — gitignored via the existing `logs/` gitignore entry).

This is the RED gate that verifies the problem exists before the fix and turns GREEN
after.

**Concern — CWD mutation in tests**:
- xUnit runs tests sequentially within a collection. All integration tests are in
  `[Collection("WebIntegration")]`, so the CWD change is serialised.
- `Environment.CurrentDirectory` is restored in `finally` to prevent bleed-through.

---

## Decision 4: No changes to `appsettings.json`

**Decision**: Leave `appsettings.json` log path as `"logs/payslip4all-.log"`.

**Rationale**:
- The relative path is a sensible human-readable default.
- An absolute path in `appsettings.json` would hardcode a server directory,
  breaking portability.
- The normalisation in Program.cs makes the relative path safe in all environments.
- Teams that want a custom path (e.g., `/var/log/payslip4all/`) can override it in
  `appsettings.Production.json` with an absolute path, which the normalisation
  correctly skips.
