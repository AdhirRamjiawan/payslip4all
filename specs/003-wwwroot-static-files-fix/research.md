# Research: wwwroot Static Files — Hosting Fix

## Decision 1: How ASP.NET Core static web assets work across environments

**Decision**: Call `builder.WebHost.UseStaticWebAssets()` explicitly at builder setup
(before `builder.Build()`). This is safe in all environments.

**Rationale**:
- In Development, `WebApplication.CreateBuilder()` calls `UseStaticWebAssets()`
  automatically — the method becomes a no-op if called again.
- In non-Development environments WITHOUT `dotnet publish`, the static web assets
  manifest (`{AssemblyName}.staticwebassets.runtime.json` in the build output)
  is NOT loaded automatically. This means build-time-generated files like
  `Payslip4All.Web.styles.css` (CSS isolation bundle) are invisible to `UseStaticFiles()`.
- In non-Development environments WITH `dotnet publish`, files are physically
  copied to `wwwroot/` — `UseStaticWebAssets()` is a safe no-op because the
  manifest is not present in the publish output.
- Calling it explicitly costs nothing: it reads the manifest file if it exists
  and skips silently if it doesn't.

**API used** (ASP.NET Core 8, in-box):
```csharp
builder.WebHost.UseStaticWebAssets();
```
This is equivalent to `app.UseStaticWebAssets()` on `WebApplication` but is
applied earlier at the builder phase.

**Alternatives considered**:
- `app.UseStaticWebAssets()` on `WebApplication` — also works but is applied later.
  `builder.WebHost.UseStaticWebAssets()` is the preferred form per ASP.NET Core docs.
- Calling it only in non-Development (`if (!builder.Environment.IsDevelopment())`) —
  also works but is redundant checking since the method is idempotent.
- Manual `Content` items in `.csproj` — brittle; would require listing every generated
  asset path explicitly.

---

## Decision 2: Forwarded Headers Middleware

**Decision**: Add `UseForwardedHeaders()` as the **first** middleware (before all other
middleware including `UseHttpsRedirection()`), configured to forward `X-Forwarded-For`,
`X-Forwarded-Proto`, and `X-Forwarded-Host`.

**Rationale**:
- Without this, when the app sits behind nginx/Apache/Azure Front Door that terminates
  TLS, `Request.Scheme` stays `http` inside the app. `UseHttpsRedirection()` then
  redirects every request, causing an infinite redirect loop (browser hits the proxy
  over HTTPS → proxy forwards HTTP to app → app redirects to HTTPS → proxy forwards
  HTTP to app → ...).
- The `ForwardedHeaders` middleware MUST run before any middleware that inspects
  `Request.Scheme`, `Request.Host`, or `RemoteIpAddress` — i.e., before
  `UseHttpsRedirection()`.
- `ForwardedHeadersOptions.ForwardedHeaders` should include `ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto`.
- For production, `KnownProxies` or `KnownNetworks` should be configured to prevent
  header spoofing. For a single-proxy setup, clearing `KnownProxies` and adding the
  proxy IP is sufficient. For cloud environments (variable IPs), allow any: set
  `KnownNetworks.Clear()` + `KnownProxies.Clear()` (acceptable if the load balancer
  is the only ingress point).

**API used** (ASP.NET Core 8, `Microsoft.AspNetCore.HttpOverrides`, in-box):
```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();   // trust proxy from any IP (configure in production)
    options.KnownProxies.Clear();
});
// ...
app.UseForwardedHeaders(); // FIRST middleware, before UseHttpsRedirection()
```

**Alternatives considered**:
- Disabling `UseHttpsRedirection()` entirely — not acceptable; HTTPS is required.
- Using `CookieSecurePolicy.Always` only — doesn't fix redirect loops.
- Configuring forwarding headers via `appsettings.json` — possible but the code
  approach is simpler and more explicit for a single-proxy scenario.

---

## Decision 3: Integration test approach for static assets

**Decision**: Add `StaticFilesIntegrationTests` using the existing `WebApplicationFactory<Program>`
pattern (already used in `LoggingIntegrationTests`). Tests create an HTTP client and make
GET requests to each static asset path, asserting `StatusCode == 200` and correct
`Content-Type` header.

**Assets to test** (minimum set that proves the pipeline works):
- `/css/bootstrap/bootstrap.min.css` — physical file in source wwwroot
- `/css/site.css` — physical file in source wwwroot
- `/favicon.png` — physical file in source wwwroot
- `/Payslip4All.Web.styles.css` — build-time CSS isolation bundle (not in source wwwroot;
  only available via static web assets manifest OR after publish)

**Environment configurations tested**:
- Default `WebApplicationFactory<Program>` (inherits `ASPNETCORE_ENVIRONMENT=Development`
  from the test runner, where static web assets are auto-loaded) — establishes baseline.
- Factory with `ASPNETCORE_ENVIRONMENT=Production` override — verifies the fix works.

**Rationale**: The CSS isolation bundle test with `Production` environment is the TDD
RED case that fails before the fix and passes after. The other asset tests confirm no
regression was introduced.

**Alternatives considered**:
- Testing with a published output — requires a publish step in CI; too heavyweight.
- Manual testing only — violates Principle I (TDD).

---

## Decision 4: No new NuGet packages required

`ForwardedHeadersOptions` and `ForwardedHeaders` are in
`Microsoft.AspNetCore.HttpOverrides`, which ships as part of the ASP.NET Core shared
framework (`Microsoft.AspNetCore.App`). No additional package references needed.

`UseStaticWebAssets()` is in `Microsoft.AspNetCore.StaticWebAssets`, also part of the
shared framework.
