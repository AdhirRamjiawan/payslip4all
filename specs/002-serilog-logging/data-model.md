# Data Model: Serilog File Logging & Global Exception Handling

**Branch**: `002-serilog-logging` | **Phase**: 1 — Design

---

## Database Entities

**None.** This feature introduces no new database tables, columns, or EF Core migrations.
All logging is to the file system via Serilog's file sink.

---

## Log Entry Structure (Structured Log Schema)

Although not a database entity, the structured log entry is a contract between the
middleware and any log-analysis tooling. Each log entry emitted by
`GlobalExceptionMiddleware` will contain:

| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `Timestamp` | `DateTimeOffset` | Serilog automatic | UTC |
| `Level` | `string` | `Error` for unhandled exceptions | Serilog level name |
| `Message` | `string` | Formatted from template | Human-readable summary |
| `Exception` | `string` | Full stack trace | Multi-line; rendered by Serilog |
| `RequestPath` | `string` | `HttpContext.Request.Path` | e.g., `/portal/companies` |
| `RequestMethod` | `string` | `HttpContext.Request.Method` | e.g., `GET`, `POST` |
| `UserId` | `string` | `ClaimTypes.NameIdentifier` or `"anonymous"` | Authenticated user's ID |
| `MachineName` | `string` | `Serilog.Enrichers.Environment` | Host name |
| `ThreadId` | `int` | `Serilog.Enrichers.Thread` | Useful for Blazor concurrent circuits |

### Sample Log Line (text format)

```
2026-03-19 20:15:34.123 +00:00 [ERR] Unhandled exception on GET /portal/companies
UserId: a1b2c3d4-e5f6-7890-abcd-ef1234567890 RequestPath: /portal/companies
System.InvalidOperationException: Object reference not set...
   at Payslip4All.Web.Pages.Companies.CompanyList.OnInitializedAsync() in ...
```

---

## Configuration Schema (`appsettings.json`)

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/payslip4all-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 31,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

```json
// appsettings.Development.json override
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

---

## Middleware Design

### `GlobalExceptionMiddleware`

```
Payslip4All.Web/Middleware/GlobalExceptionMiddleware.cs
```

**Responsibilities**:
1. Wrap `await _next(context)` in a `try/catch`.
2. On exception: extract `RequestPath`, `Method`, `UserId` from `HttpContext`.
3. Push structured properties via `LogContext.PushProperty(...)`.
4. Call `_logger.LogError(ex, "Unhandled exception on {Method} {RequestPath}", ...)`.
5. If response not yet started: write a 500 response (redirect to `/Error` for browser requests; JSON `{ "error": "An unexpected error occurred." }` for API paths).
6. If response already started (Blazor circuit streaming): log and rethrow — Blazor's own error boundary handles the UI.

### `ApplicationBuilderExtensions`

```
Payslip4All.Web/Extensions/ApplicationBuilderExtensions.cs
```

Provides `app.UseGlobalExceptionHandler()` extension method for clean `Program.cs` registration.

---

## File System Layout (new files)

```text
src/Payslip4All.Web/
├── Extensions/
│   └── ApplicationBuilderExtensions.cs       [NEW]
├── Middleware/
│   └── GlobalExceptionMiddleware.cs          [NEW]
├── appsettings.json                          [MODIFIED — add Serilog section]
├── appsettings.Development.json              [MODIFIED — add Serilog MinimumLevel override]
└── Program.cs                               [MODIFIED — UseSerilog(), UseGlobalExceptionHandler()]

tests/Payslip4All.Web.Tests/
├── Middleware/
│   └── GlobalExceptionMiddlewareTests.cs     [NEW]
└── Integration/
    └── LoggingIntegrationTests.cs            [NEW]
```

No changes to Domain, Application, or Infrastructure layers.
