# Research: nginx HTTPS Reverse Proxy

## Decision 1: Terminate TLS at nginx and externalize certificate material

- **Decision**: Terminate HTTPS at nginx and load the certificate/key from deployment-managed files such as `/etc/nginx/certs/fullchain.pem` and `/etc/nginx/certs/privkey.pem`, with the file contents populated from an external secret source rather than committed to the repo.
- **Rationale**: The feature requires a source-controlled gateway definition without embedding secrets. The existing app already honors forwarded headers and `UseHttpsRedirection()`, so nginx can own TLS while the app stays on internal HTTP. This also keeps certificate rotation operationally separate from the application binary.
- **Alternatives considered**:
  - **Terminate TLS inside ASP.NET Core only**: Rejected because it weakens the purpose of the nginx gateway and keeps certificates coupled to the app process.
  - **Use a new ALB/ACM-only design instead of nginx**: Rejected because the requested feature is specifically an nginx configuration under `infra/`.

## Decision 2: Move the app behind nginx on a local-only upstream endpoint

- **Decision**: Plan the upstream Payslip4All app to bind to `http://127.0.0.1:8080`, leaving nginx to own public ports `80` and `443`.
- **Rationale**: This keeps nginx as the only public entry point, aligns with FR-005/FR-010, and reduces accidental exposure of the app's internal endpoint. It fits the current single-host deployment model described in `infra/aws/cloudformation/`.
- **Alternatives considered**:
  - **Keep the app on `0.0.0.0:80` and place nginx on another host**: Rejected because it expands the scope beyond the current EC2-hosted deployment path.
  - **Use a Unix domain socket immediately**: Rejected because it adds bootstrap and permissions complexity without clear value for the current single-instance plan.

## Decision 3: Preserve forwarded headers and Blazor WebSocket upgrade headers

- **Decision**: Require nginx to forward `Host`, `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`, `X-Real-IP`, `Upgrade`, and `Connection` headers when proxying to the app.
- **Rationale**: `Program.cs` already configures forwarded-header handling and `UseHttpsRedirection()`, and Blazor Server requires WebSocket/SignalR upgrade support for interactive pages. Preserving scheme and host is also necessary for secure cookies, absolute redirects, and provider callback URLs.
- **Alternatives considered**:
  - **Forward only `X-Forwarded-For` and `X-Forwarded-Proto`**: Rejected because host-sensitive behaviors and WebSocket upgrades would remain under-specified.
  - **Add application-side custom header translation**: Rejected because the repo already follows standard ASP.NET Core reverse-proxy patterns.

## Decision 4: Enforce HTTP→HTTPS redirect and strict host filtering at nginx

- **Decision**: Use a dedicated HTTP server block that redirects requests for `payslip4all.co.za` to the equivalent HTTPS URL, and include a catch-all/default behavior that does not serve unrelated hostnames as the production site.
- **Rationale**: Redirecting at nginx is simpler and more efficient than letting insecure requests reach the app. Host filtering directly supports FR-010 and reduces host-header confusion or accidental production aliasing.
- **Alternatives considered**:
  - **Let ASP.NET Core perform all redirects and host handling**: Rejected because the public gateway should own protocol and hostname enforcement.
  - **Accept arbitrary hosts and rely on DNS alone**: Rejected because the spec explicitly requires restricting application serving to the intended public host.

## Decision 5: Return generic unavailable responses when the upstream app is down

- **Decision**: Configure nginx timeouts and upstream error handling so failed upstream requests become a generic `503 Service Unavailable` style response without exposing the internal address or port.
- **Rationale**: FR-007 requires a non-success response that hides internal network details. A gateway-controlled fallback page or error mapping keeps failures user-safe and operator-debuggable.
- **Alternatives considered**:
  - **Expose nginx default `502 Bad Gateway` output unchanged**: Rejected because default error pages can reveal too much about the gateway or upstream topology.
  - **Retry aggressively for long periods**: Rejected because SC-005 requires an unavailable response within 10 seconds.

## Decision 6: Treat `infra/nginx/` as the primary artifact and update the existing AWS path only as needed

- **Decision**: Place the gateway config and its operator guidance under `infra/nginx/`, then update `infra/aws/cloudformation/payslip4all-web.yaml`, `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`, and `infra/aws/cloudformation/README.md` only where needed to install/configure nginx on the current hosted-AWS path.
- **Rationale**: The repo already keeps deployable infrastructure assets and operator docs under `infra/aws/cloudformation/`, so the nginx feature should extend that flow instead of inventing a second deployment story.
- **Alternatives considered**:
  - **Create a brand-new AWS deployment stack just for nginx**: Rejected because it duplicates existing deployment ownership.
  - **Keep the config outside `infra/` and document it only in README**: Rejected because FR-001 and FR-009 require a source-controlled infra artifact with activation guidance.

## Decision 7: Validate the feature through existing .NET test patterns plus operator smoke checks

- **Decision**: Plan failing tests first in `Payslip4All.Web.Tests` for nginx config/documentation expectations and reverse-proxy-aware application behavior, then use deployment smoke checks to verify HTTPS, redirect, `/health`, wrong-host handling, and upstream failure behavior.
- **Rationale**: The constitution requires TDD, and the repo already validates infrastructure and deployment docs through xUnit/WebApplicationFactory tests. Reusing those patterns avoids adding a new test stack during planning.
- **Alternatives considered**:
  - **Rely on manual nginx testing only**: Rejected because it violates Principle I (TDD).
  - **Introduce a dedicated infrastructure test framework now**: Rejected because the repo already has established .NET-based validation patterns.
