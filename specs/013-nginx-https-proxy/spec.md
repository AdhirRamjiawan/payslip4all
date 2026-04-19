# Feature Specification: Secure Public Gateway Configuration

**Feature Branch**: `013-nginx-https-proxy`  
**Created**: 2026-04-19  
**Status**: Draft  
**Input**: User description: "In the infra folder, create an nginx config for the payslip application. Use the domain payslip4all.co.za, configure the application for HTTPS and ensure that nginx is configured as a reverse proxy."

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

- **Domain**: No payroll rules, calculations, or legal document content change in this feature.
- **Application**: Existing application use cases remain unchanged; the feature only affects how the running application is exposed publicly.
- **Infrastructure**: This feature adds a source-controlled public gateway configuration in `infra/` that defines domain routing, secure access, reverse proxy behaviour, and deployment prerequisites.
- **Web**: The web application must behave correctly when requests arrive through the secure public host rather than a directly exposed application endpoint.
- **TDD Alignment**: Each requirement is written as observable deployment behaviour so failing configuration validation checks, redirect tests, and reverse-proxy smoke tests can be created before implementation.

### User Story 1 - Serve the public site securely (Priority: P1)

As an operator, I want Payslip4All to be reachable at payslip4all.co.za over a secure connection so end users can access the live application through a trusted public address.

**Why this priority**: Secure public access is the minimum value of this feature. Without it, the deployment cannot safely expose the application to real users.

**Independent Test**: Apply the provided gateway configuration in a deployment environment with the required domain and certificate prerequisites, then verify secure requests succeed and non-secure requests redirect.

**Acceptance Scenarios**:

1. **Given** the required domain and secure-connection prerequisites are available, **When** a user browses to `https://payslip4all.co.za`, **Then** the application responds successfully from that public address over a secure connection.
2. **Given** a user attempts to access `http://payslip4all.co.za`, **When** the request reaches the public gateway, **Then** the user is redirected to the equivalent secure URL before interacting with the application.

---

### User Story 2 - Route public traffic to the running application (Priority: P2)

As an operator, I want the public gateway to forward incoming requests to the running Payslip4All service so the application can stay behind one controlled public entry point.

**Why this priority**: Reverse proxy routing is required for the secure public address to be useful. Without correct forwarding, the domain exists but the application does not function for end users.

**Independent Test**: Start the application on its intended internal endpoint, send representative page and form requests through payslip4all.co.za, and confirm the application completes them without exposing the internal endpoint directly.

**Acceptance Scenarios**:

1. **Given** the Payslip4All application is running on its configured internal endpoint, **When** a user requests a page through `https://payslip4all.co.za`, **Then** the gateway forwards the request and returns the application response from the public domain.
2. **Given** a user completes a normal interaction such as sign-in or form submission through the public domain, **When** the request passes through the gateway, **Then** the application receives enough original request context to preserve secure links, redirects, and submitted data correctly.
3. **Given** the internal application endpoint is unavailable, **When** a user requests the public domain, **Then** the gateway returns an unavailable response and does not reveal internal endpoint details.

---

### User Story 3 - Maintain a reusable deployment gateway definition (Priority: P3)

As an operator, I want the public gateway definition stored in the repository's infra folder with clear activation prerequisites so it can be reviewed, versioned, and reused in deployment workflows.

**Why this priority**: A reusable, source-controlled infrastructure artifact reduces setup drift and makes future deployment changes auditable, but it depends on the secure routing behaviour delivered by the higher-priority stories.

**Independent Test**: Inspect the repository, locate the gateway artifact in `infra/`, and verify an operator can identify the required public domain, secure-connection inputs, and upstream application inputs without reverse-engineering the file.

**Acceptance Scenarios**:

1. **Given** an operator has a repository checkout, **When** they inspect the `infra/` folder, **Then** they can locate the public gateway configuration artifact for the Payslip4All deployment.
2. **Given** environment-specific upstream or secure-connection values must change, **When** the operator updates the documented deployment inputs, **Then** the public domain and routing behaviour remain consistent without redefining the overall gateway behaviour.

---

### Edge Cases

- What happens when `payslip4all.co.za` points to the public gateway before the secure-connection materials are ready for use?
- How does the public gateway respond when the upstream Payslip4All application is stopped, unreachable, or slow to respond?
- What happens when a request arrives with a hostname other than `payslip4all.co.za`?
- How is secure access preserved when certificate or key material is renewed or replaced without changing the public domain?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST provide a source-controlled public gateway configuration artifact in the `infra/` folder for the Payslip4All deployment.
- **FR-002**: The public gateway configuration MUST treat `payslip4all.co.za` as the primary public host for this feature.
- **FR-003**: The public gateway MUST accept secure requests for `payslip4all.co.za` and deliver the application through that secure endpoint.
- **FR-004**: The public gateway MUST redirect non-secure requests for `payslip4all.co.za` to the equivalent secure URL.
- **FR-005**: The public gateway MUST forward end-user requests from `payslip4all.co.za` to a configured internal Payslip4All application endpoint without requiring end users to know or access that internal endpoint directly.
- **FR-006**: Forwarded requests MUST preserve the original request context needed for the application to recognize the public host and secure scheme during normal page loads, redirects, and form submissions.
- **FR-007**: When the internal application endpoint is unavailable, the public gateway MUST return a non-success response and MUST NOT expose internal network details in the user-facing response.
- **FR-008**: Environment-specific activation values, including secure-connection material references and the upstream application address, MUST be externally configurable rather than hardcoded, except for the required public domain in this feature.
- **FR-009**: The feature MUST document the operator prerequisites and activation inputs needed to use the public gateway configuration safely in a deployment environment.
- **FR-010**: The public gateway configuration MUST restrict application serving to the intended public host so unrelated hostnames are not treated as the production entry point.

### Key Entities *(include if feature involves data)*

- **Public Gateway Configuration**: The source-controlled deployment artifact that defines the public host, secure-access behaviour, and request forwarding rules for Payslip4All.
- **Secure Access Policy**: The set of rules that determine how secure and non-secure requests are handled for `payslip4all.co.za`.
- **Upstream Application Endpoint**: The internal Payslip4All destination that receives forwarded traffic from the public gateway.
- **Deployment Prerequisite Set**: The operator-supplied domain, secure-connection, and upstream values required to activate the configuration in an environment.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An operator can locate the public gateway artifact and identify its required activation inputs within 5 minutes of opening the repository.
- **SC-002**: In smoke testing, 100% of requests sent to `http://payslip4all.co.za` are redirected to `https://payslip4all.co.za` in a single navigation step.
- **SC-003**: During a standard smoke test, users can load the first complete application page through `https://payslip4all.co.za` within 5 seconds for at least 95% of requests.
- **SC-004**: In representative smoke testing, 100% of public-domain interactions covering initial page load, authenticated navigation, and form submission complete successfully without direct access to the internal application endpoint.
- **SC-005**: When the upstream application is unavailable, users receive an unavailable response from the public domain within 10 seconds and no internal endpoint details are shown.

## Assumptions

- Control of the `payslip4all.co.za` DNS records is already available before this feature is activated.
- Secure-connection materials can be supplied by the deployment environment and are managed separately from application source code.
- A single public host is sufficient for this feature; additional aliases or subdomains are out of scope unless requested later.
- The Payslip4All application already has a reachable internal address or port that can be provided as an environment-specific deployment input.
