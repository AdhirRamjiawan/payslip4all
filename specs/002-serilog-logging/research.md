# Research: Serilog File Logging & Global Exception Handling

**Branch**: `002-serilog-logging` | **Phase**: 0 — Research

---

## Decision 1: Serilog vs Microsoft.Extensions.Logging (built-in)

**Decision**: Use `Serilog.AspNetCore` with `Serilog.Sinks.File`.

**Rationale**:
- The user explicitly specified Serilog.
- Serilog provides structured logging with first-class file sinks, daily rolling, retention configuration, and enrichers out of the box.
- `Microsoft.Extensions.Logging` alone does not ship a file sink; it would require a third-party add-on anyway (e.g., `NLog`, `log4net`).
- Serilog integrates cleanly with ASP.NET Core via `UseSerilog()` and reads from `appsettings.json` via `Serilog.Settings.Configuration` (bundled in `Serilog.AspNetCore`).

**Alternatives considered**:
- `NLog`: capable, but Serilog has better structured-logging ergonomics and is more idiomatic in .NET 8.
- `log4net`: legacy; not recommended for new .NET 8 projects.
- Built-in `EventLog` sink: Windows-only; ruled out for cross-platform compatibility.

---

## Decision 2: NuGet Packages Required

| Package | Version (minimum) | Purpose |
|---------|--------------------|---------|
| `Serilog.AspNetCore` | 8.x | Host builder integration, request logging, `ILogger<T>` bridge |
| `Serilog.Sinks.File` | 5.x (bundled via `Serilog.AspNetCore`) | Rolling daily file output |

**Note**: `Serilog.AspNetCore` 8.x transitively pulls in `Serilog.Sinks.File`, `Serilog.Settings.Configuration`, and `Serilog.Enrichers.Environment`. No separate `Serilog.Sinks.File` package entry is strictly necessary, but explicitly referencing it is fine for clarity.

**Constitution amendment note**: Serilog is not listed in the constitution's technology table. The user's explicit feature request constitutes approval; a complexity-tracking entry documents the deviation in `plan.md`.

---

## Decision 3: Global Exception Middleware vs UseExceptionHandler

**Decision**: Custom `GlobalExceptionMiddleware` class.

**Rationale**:
- `app.UseExceptionHandler("/Error")` redirects the user to a Razor Page — it doesn't easily capture the exception for structured logging without re-executing the request pipeline.
- A custom middleware allows us to: (a) catch the exception, (b) log it with all structured properties, (c) write an appropriate response, and (d) short-circuit the pipeline — all in one place.
- For the `/payslips/{id}/download` Minimal API endpoint, a middleware-based approach handles JSON vs HTML responses uniformly via `Accept` header inspection or path prefix.

**Alternatives considered**:
- `UseExceptionHandler(exceptionHandlerApp => ...)` lambda form: works but complicates structured logging because the exception is retrieved via `IExceptionHandlerFeature`, adding indirection.
- `ProblemDetails` middleware (ASP.NET Core 7+): good for APIs; less natural for Blazor Server pages where the error UI is already handled by Blazor's `ErrorBoundary`.

---

## Decision 4: Middleware Placement in Pipeline

**Decision**: Register `GlobalExceptionMiddleware` as the **first** custom middleware after `UseHttpsRedirection` and `UseStaticFiles` (which must remain early for performance). Placed before `UseRouting`, `UseAuthentication`, `UseAuthorization`.

```
app.UseStaticFiles()          // serve static assets fast, before exception handler
app.UseGlobalExceptionHandler() // catch anything below
app.UseRouting()
app.UseAuthentication()
app.UseAuthorization()
app.MapBlazorHub()
app.MapFallbackToPage("/_Host")
```

**Rationale**: Placing it after `UseStaticFiles` avoids logging 404s for missing `.css`/`.js` files. Placing it before routing/auth ensures all application-level exceptions are captured regardless of auth state.

---

## Decision 5: Log File Configuration Defaults

| Setting | Default | Config Key |
|---------|---------|------------|
| Log directory | `logs/` (relative to app content root) | `Serilog:WriteTo:0:Args:path` |
| File name pattern | `logs/payslip4all-.log` | same |
| Rolling interval | `Day` | `Serilog:WriteTo:0:Args:rollingInterval` |
| Retained files | `31` | `Serilog:WriteTo:0:Args:retainedFileCountLimit` |
| Minimum level | `Information` (prod), `Debug` (dev) | `Serilog:MinimumLevel:Default` |
| Output template | `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}` | `Serilog:WriteTo:0:Args:outputTemplate` |

---

## Decision 6: Enrichers

Use the following enrichers (all available via `Serilog.AspNetCore`):

- `FromLogContext` — allows per-request properties pushed via `LogContext.PushProperty(...)` in the middleware (e.g., `UserId`, `RequestPath`).
- `WithMachineName` — adds `MachineName` for multi-host deployments.
- `WithThreadId` — useful for diagnosing concurrent Blazor Server circuit issues.

---

## Decision 7: Project Layer Assignment

| Concern | Layer | File |
|---------|-------|------|
| Serilog host bootstrap | Web | `Program.cs` |
| `GlobalExceptionMiddleware` | Web | `Middleware/GlobalExceptionMiddleware.cs` |
| Middleware registration extension | Web | `Extensions/ApplicationBuilderExtensions.cs` |
| `appsettings.json` Serilog section | Web | `appsettings.json` / `appsettings.Development.json` |
| Tests (middleware unit) | Web.Tests | `Middleware/GlobalExceptionMiddlewareTests.cs` |
| Tests (integration — log file written) | Web.Tests | `Integration/LoggingIntegrationTests.cs` |

No changes to `Payslip4All.Domain`, `Payslip4All.Application`, or `Payslip4All.Infrastructure`.

---

## Decision 8: Test Strategy

| Test type | What it covers |
|-----------|----------------|
| Unit (`GlobalExceptionMiddlewareTests`) | Middleware catches exception, calls `ILogger.LogError`, returns 500; authenticated vs anonymous user ID enrichment |
| Integration (`LoggingIntegrationTests`) | Use `WebApplicationFactory<Program>` with a temp log path; trigger a 500, assert log file is created and contains exception details |
| Existing suite | All 152 pre-existing tests MUST remain green after changes |

Serilog's `Serilog.Sinks.InMemory` sink (or a mock `ILogger`) will be used in unit tests to avoid real file I/O.
