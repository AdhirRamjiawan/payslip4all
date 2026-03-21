# Feature Specification: wwwroot Static Files — Hosting Fix

**Feature Branch**: `003-wwwroot-static-files-fix`
**Created**: 2026-03-21
**Status**: Draft

## Problem Statement

Files under `wwwroot/` are not being served correctly when the application is run
in a non-Development hosting environment (Staging, Production, or any environment
where `ASPNETCORE_ENVIRONMENT ≠ Development`).

Root causes identified by investigation:

1. **CSS isolation bundle missing outside Development** — `Payslip4All.Web.styles.css`
   is generated at build time and is NOT present in the source `wwwroot/` directory.
   ASP.NET Core only loads the static web assets manifest (`.staticwebassets.runtime.json`)
   automatically in Development mode. In any other environment without a prior
   `dotnet publish`, the CSS isolation file cannot be found by `UseStaticFiles()`.
   The request falls through to `MapFallbackToPage("/_Host")`, which returns the
   Blazor app HTML instead — the browser discards it as invalid CSS.

2. **No reverse-proxy header forwarding** — The app does not call `UseForwardedHeaders()`,
   so if deployed behind nginx or Apache, the scheme/host/IP headers (`X-Forwarded-For`,
   `X-Forwarded-Proto`, `X-Forwarded-Host`) are ignored. This can cause `UseHttpsRedirection()`
   to incorrectly redirect requests that already arrived over HTTPS, producing redirect loops
   or mixed-content issues that prevent static assets from loading.

3. **`UseStaticFiles()` positioned after `UseHttpsRedirection()`** — for hosted scenarios
   where the app sits behind a TLS-terminating proxy that sends plain HTTP to the app,
   `UseHttpsRedirection()` will redirect all requests (including static file requests)
   unless forwarded headers are configured first.

## Architecture & TDD Alignment

All changes are confined to `Payslip4All.Web` (presentation layer). No Domain, Application,
or Infrastructure changes are needed.

### User Story 1 — Static files served in all environments (Priority: P1)

A deployed instance of Payslip4All (Staging or Production) MUST correctly serve all
files under `wwwroot/` — including the build-time-generated CSS isolation bundle
(`Payslip4All.Web.styles.css`), Bootstrap CSS, site.css, and favicon — regardless of
whether the app was started with `dotnet run` or as a published binary.

**Acceptance Scenarios**:

1. **Given** the app is running with `ASPNETCORE_ENVIRONMENT=Production` from its
   build output (not published), **When** a browser requests `/Payslip4All.Web.styles.css`,
   **Then** the server returns a `200 OK` with `Content-Type: text/css`.

2. **Given** the app is running with `ASPNETCORE_ENVIRONMENT=Production` from its
   build output, **When** a browser requests `/css/bootstrap/bootstrap.min.css`,
   **Then** the server returns a `200 OK` with `Content-Type: text/css`.

3. **Given** the app is deployed behind a reverse proxy that terminates TLS and forwards
   `X-Forwarded-Proto: https`, **When** a browser makes an HTTP request to the proxy,
   **Then** the app does NOT redirect the request (no infinite redirect loop).

---

### User Story 2 — Reverse proxy forwarding headers respected (Priority: P2)

When deployed behind nginx, Apache, or a cloud load balancer, the app must honour
`X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` headers so that
HTTPS redirect logic and authentication cookies work correctly.

**Acceptance Scenarios**:

1. **Given** `KnownProxies` or `KnownNetworks` is configured, **When** a request arrives
   with `X-Forwarded-Proto: https`, **Then** `HttpContext.Request.Scheme` is `https`.

2. **Given** a valid forwarded header, **When** the auth cookie is set, **Then** the
   `Secure` flag is honoured based on the forwarded scheme, not the raw transport.

---

## Key Entities

No new database entities.

## Out of Scope

- Changes to Domain, Application, or Infrastructure layers.
- CDN or asset fingerprinting.
- WebAssembly (this is Blazor Server).
- Service worker / PWA configuration.
