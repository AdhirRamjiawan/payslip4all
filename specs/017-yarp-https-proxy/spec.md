# Feature Specification: YARP HTTPS Reverse Proxy Migration

**Feature Branch**: `017-yarp-https-proxy`  
**Created**: 2026-04-30  
**Status**: Approved  
**Input**: User description: "execute and suggest next agent" for the YARP-based HTTPS reverse proxy migration that replaces the older nginx-oriented deployment approach.

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

- **Domain**: No payroll rules, calculations, or legal document content change in this feature.
- **Application**: Existing application use cases remain unchanged; the feature only changes how the running application is exposed at the public HTTPS edge.
- **Infrastructure**: This feature replaces the older nginx-oriented deployment definition with a feature-owned YARP edge contract at `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md`, while the hosted assets under `/Users/adhirramjiawan/projects/payslip4all/infra/` implement that contract for deployment.
- **Web**: The web application must support a YARP gateway mode that serves the public host, redirects HTTP to HTTPS, filters unexpected hosts, and forwards request context to the backend app correctly.
- **TDD Alignment**: Each requirement is expressed as observable runtime or deployment behavior so failing infrastructure, startup, and reverse-proxy integration tests can be written before implementation tasks begin.

### Governed Dependency Alignment

- **Approved dependency**: `Yarp.ReverseProxy` 2.2.x in `Payslip4All.Web`.
- **Approving constitution citation**: Payslip4All Constitution v1.4.0, **Technology Stack & Constraints** → **Public HTTPS Edge**, and **Development Workflow & Quality Gates** → **Branching & Review**.
- **Approved scope**: This dependency is approved only for the repository-owned public HTTPS edge role in this feature and does not authorize a separate gateway project or broader non-edge usage.

### User Story 1 - Serve the public site securely through YARP (Priority: P1)

As an operator, I want Payslip4All to be reachable at `https://payslip4all.co.za` through a YARP-hosted HTTPS edge so end users can access the live application through one trusted public address without nginx.

**Why this priority**: Secure public reachability is the minimum value of the migration. Without it, the YARP replacement does not satisfy the production-entry-point requirement.

**Independent Test**: Start the gateway in YARP mode with the required certificate and host settings, then verify `https://payslip4all.co.za/health` succeeds within 5 seconds and `http://payslip4all.co.za` redirects once to the equivalent HTTPS URL.

**Acceptance Scenarios**:

1. **Given** the required DNS and certificate prerequisites are available, **When** a user browses to `https://payslip4all.co.za`, **Then** the application responds successfully through the YARP edge over HTTPS.
2. **Given** a user browses to `http://payslip4all.co.za`, **When** the request reaches the YARP edge, **Then** the user is redirected to the equivalent `https://payslip4all.co.za` URL before interacting with the application.
3. **Given** the required DNS and certificate prerequisites are available, **When** an operator requests `https://payslip4all.co.za/health`, **Then** the readiness check succeeds within 5 seconds and confirms the public HTTPS edge is active.
4. **Given** gateway mode is enabled but the certificate material is missing, invalid, or unreadable, **When** the operator starts or deploys the gateway, **Then** the public HTTPS edge remains unavailable, serves no insecure fallback traffic, and surfaces the activation error `HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.`

---

### User Story 2 - Route public traffic to the backend application through the YARP edge (Priority: P2)

As an operator, I want the public YARP gateway to forward incoming requests to the backend Payslip4All application so the app can stay on an internal-only endpoint while preserving secure links, redirects, and submitted data.

**Why this priority**: Reverse-proxy routing is required for the secure public address to be useful. Without correct forwarding, the public host exists but the application does not function for end users.

**Independent Test**: Start the backend app on its internal endpoint, send representative page and form requests through the public YARP host, and confirm the backend receives the original request context needed for Blazor Server and application redirects.

**Acceptance Scenarios**:

1. **Given** the Payslip4All backend app is running on its configured internal endpoint, **When** a user requests a page through `https://payslip4all.co.za`, **Then** the YARP edge forwards the request and returns the application response from the public domain.
2. **Given** a user completes a normal interaction such as sign-in or form submission through the public domain, **When** the request passes through the YARP edge, **Then** the backend application receives enough original request context to preserve secure links, redirects, and submitted data correctly.
3. **Given** the backend application endpoint is unavailable, **When** a user requests the public domain, **Then** the YARP edge returns an HTTP `503 Service Unavailable` response within 10 seconds with the generic body `Service temporarily unavailable.` and does not reveal internal endpoint details.
4. **Given** a request arrives with a hostname other than `payslip4all.co.za`, **When** the YARP edge evaluates the request, **Then** the request is rejected with HTTP `421 Misdirected Request` and the unrelated hostname is not treated as the production entry point.

---

### User Story 3 - Maintain a reusable repository-owned YARP deployment definition (Priority: P3)

As an operator, I want the YARP gateway definition and activation guidance stored in the repository with clear prerequisites so the public edge can be reviewed, versioned, and reused without falling back to the older nginx feature.

**Why this priority**: A source-controlled, YARP-specific deployment contract reduces setup drift and makes the nginx-to-YARP migration auditable, but it depends on the secure routing behavior delivered by the higher-priority stories.

**Independent Test**: Starting from `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md`, verify an operator can identify the canonical YARP gateway contract, required activation inputs, hosted AWS alignment, and the `/health` smoke-check process without reverse-engineering the implementation.

**Acceptance Scenarios**:

1. **Given** an operator has a repository checkout, **When** they open `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md`, **Then** they are directed to the canonical YARP public-edge contract at `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md` and to the hosted AWS artifacts that must implement it.
2. **Given** environment-specific certificate or upstream values must change, **When** the operator follows the authoritative guidance in `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md`, **Then** they can identify which activation inputs are operator-supplied versus repository-owned without reintroducing nginx-specific artifacts or changing the canonical contract ownership.
3. **Given** the repository still contains the older nginx feature history under `/Users/adhirramjiawan/projects/payslip4all/specs/013-nginx-https-proxy`, **When** this feature is planned and implemented, **Then** all new public-edge decisions resolve in favor of YARP.

---

### Edge Cases

- If `payslip4all.co.za` points at the host before the certificate material has been staged for YARP/Kestrel, or if that material is invalid or unreadable, the gateway must fail closed, must not fall back to serving production traffic insecurely, and must surface the operator-visible activation error `HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.`
- If the backend app on the internal-only upstream endpoint is stopped, unreachable, or too slow to answer, the YARP edge must return the same generic HTTP `503 Service Unavailable` response within 10 seconds without exposing internal endpoint details.
- If a request arrives with a hostname other than `payslip4all.co.za`, the YARP edge must reject it with HTTP `421 Misdirected Request`.
- When the TLS certificate material is renewed or replaced, the public host remains unchanged and the operator updates only the externally supplied certificate inputs rather than the repository-owned YARP contract.
- If repository artifacts under `/infra/` restate gateway behavior, those artifacts are implementation-facing and must conform to the canonical feature contract in `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md` rather than redefine it.
- If repository artifacts still reference nginx behavior during this migration, those references are historical only and must not define new public-edge requirements for this branch.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST define its single source-of-truth YARP public-edge contract at `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md`, and repository artifacts under `/Users/adhirramjiawan/projects/payslip4all/infra/` MUST implement that contract without redefining conflicting public-edge behavior for this branch.
- **FR-002**: The YARP public edge MUST treat `payslip4all.co.za` as the primary public host for this feature.
- **FR-003**: The YARP public edge MUST accept secure requests for `payslip4all.co.za`, deliver the application through that HTTPS endpoint, and expose the readiness smoke check at `https://payslip4all.co.za/health`.
- **FR-004**: The YARP public edge MUST redirect non-secure requests for `payslip4all.co.za` to the equivalent secure URL.
- **FR-005**: The public YARP edge MUST forward end-user requests from `payslip4all.co.za` to a configured internal Payslip4All application endpoint without requiring end users to know or access that internal endpoint directly.
- **FR-006**: Forwarded requests MUST preserve the original public host and secure-scheme context so that application-generated redirects stay on `https://payslip4all.co.za`, Blazor/SignalR interactions continue without switching to or exposing the internal upstream address, and form submissions plus follow-up navigation remain bound to the public host.
- **FR-007**: When the internal application endpoint is unavailable, the YARP public edge MUST return HTTP `503 Service Unavailable` with the generic body `Service temporarily unavailable.` within 10 seconds and MUST NOT expose internal network details in the user-facing response.
- **FR-008**: Environment-specific activation values, including certificate references, MUST be externally configurable. For the repository-owned hosted deployment contract in this feature, the upstream application address MUST be `http://127.0.0.1:8080`. Any explicitly documented override for another environment MUST still target a non-public internal-only backend endpoint, MUST NOT become an end-user entry point, and MUST preserve the same public host, HTTPS redirect, wrong-host rejection, generic `503 Service Unavailable` response body, 10-second failure bound, and fail-closed certificate activation behavior.
- **FR-009**: The feature MUST provide one authoritative operator-facing entrypoint at `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md` that documents the operator prerequisites, activation inputs, `/health` readiness smoke-check steps, and certificate-activation failure expectations needed to use the YARP public edge safely. Supporting documents MAY elaborate on hosted implementation details but MUST NOT replace or contradict this entrypoint.
- **FR-010**: The public edge MUST restrict application serving to the intended public host by rejecting unrelated hostnames with HTTP `421 Misdirected Request` so they are not treated as the production entry point.
- **FR-011**: Repository-owned hosted deployment artifacts for the current AWS-hosted path MUST reflect YARP instead of nginx, MUST keep the hosted default upstream set to `http://127.0.0.1:8080`, and MUST stay consistent with the canonical feature contract plus the authoritative operator-facing quickstart.
- **FR-012**: The feature MUST stay within the existing four-project Clean Architecture boundary and MUST NOT require a fifth project for the public edge.
- **FR-013**: If certificate activation prerequisites are missing, invalid, or unreadable, the YARP public edge MUST fail closed before serving public traffic, MUST remain not ready for activation, and MUST surface the operator-visible activation error `HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.`

### Key Entities *(include if feature involves data)*

- **YARP Public Edge Configuration**: The source-controlled runtime and deployment contract that defines the public host, HTTPS behavior, and request forwarding rules for Payslip4All.
- **Gateway TLS Certificate Binding**: The externally supplied certificate material and runtime binding that allow the YARP edge to terminate HTTPS for `payslip4all.co.za`.
- **Upstream Application Endpoint**: The internal Payslip4All destination that receives forwarded traffic from the YARP edge.
- **Deployment Prerequisite Set**: The operator-supplied DNS, certificate, environment-variable, and hosted deployment inputs required to activate the feature safely.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a timed operator validation run, an operator starting at `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/quickstart.md` can locate the canonical YARP contract, identify every required activation input for this feature, and complete the documented `/health` smoke-check procedure in 10 minutes or less without consulting any other operator guide as the primary source.
- **SC-002**: In smoke testing, 100% of requests sent to `http://payslip4all.co.za` are redirected to `https://payslip4all.co.za` in a single navigation step.
- **SC-003**: During the task-level smoke procedure for this feature, 3 consecutive requests to `https://payslip4all.co.za/health` each complete successfully within 5 seconds, measured from request start until the response completes.
- **SC-004**: In representative smoke testing, 100% of public-domain interactions covering initial page load, authenticated navigation, and form submission complete successfully without direct access to the internal application endpoint.
- **SC-005**: When the backend application is unavailable, users receive an unavailable response from the public domain within 10 seconds and no internal endpoint details are shown.
- **SC-006**: Repository planning and implementation artifacts for this branch contain no new nginx-oriented public-edge requirements.

## Assumptions

- Control of the `payslip4all.co.za` DNS record is already available before this feature is activated.
- TLS certificate material can be supplied by the deployment environment and is managed separately from application source code.
- A single public host is sufficient for this feature; additional aliases or subdomains are out of scope unless requested later.
- The Payslip4All backend app already has or can be assigned a local-only endpoint such as `http://127.0.0.1:8080`.
- The existing hosted AWS path continues to use one EC2 instance, one Elastic IP, and repository-owned deployment assets; this feature changes the public edge technology from nginx assumptions to YARP assumptions.
