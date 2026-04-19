# Data Model: nginx HTTPS Reverse Proxy

## Overview

This feature does not add business-domain persistence. Its data model describes the operator-facing gateway objects, runtime configuration inputs, and verification states required to serve Payslip4All securely through nginx.

## Entities

### 1. Gateway Site Configuration

**Purpose**: The source-controlled nginx artifact that defines how `payslip4all.co.za` is served publicly.

**Fields**:
- `domainName` — fixed public hostname (`payslip4all.co.za`)
- `httpRedirectEnabled` — whether port `80` requests are redirected to HTTPS
- `httpsListenerEnabled` — whether port `443` serves the site
- `configPath` — repository path of the nginx config artifact
- `defaultHostPolicy` — how non-matching hostnames are handled
- `websocketProxyEnabled` — whether upgrade headers for Blazor Server are enabled

**Relationships**:
- Uses one `TLS Certificate Binding`
- Proxies to one `Upstream Application Endpoint`
- Applies one `Proxy Header Policy`
- Depends on one `Deployment Prerequisite Set`

**Validation Rules**:
- `domainName` must remain `payslip4all.co.za` for this feature.
- Public app serving must not occur for unrelated hostnames.
- HTTPS and HTTP redirect behavior must both be defined.

### 2. TLS Certificate Binding

**Purpose**: The deployment-managed certificate material that nginx uses for HTTPS.

**Fields**:
- `certificateReference` — secret or file reference for the public certificate chain
- `privateKeyReference` — secret or file reference for the TLS private key
- `targetDirectory` — runtime location such as `/etc/nginx/certs`
- `rotationMode` — how renewed material is replaced
- `reloadRequired` — whether nginx must reload after certificate updates

**Relationships**:
- Secures one `Gateway Site Configuration`
- Depends on one `Deployment Prerequisite Set`

**Validation Rules**:
- Certificate material must not be committed to source control.
- The certificate must cover `payslip4all.co.za`.
- Rotation must preserve the public domain without redefining gateway behavior.

### 3. Upstream Application Endpoint

**Purpose**: The internal Payslip4All destination that receives proxied traffic from nginx.

**Fields**:
- `scheme` — planned as `http`
- `host` — planned as `127.0.0.1`
- `port` — planned as `8080`
- `healthPath` — `/health`
- `localOnlyBinding` — whether the endpoint avoids direct public exposure
- `availabilityState` — current upstream health state

**Relationships**:
- Receives traffic from one `Gateway Site Configuration`
- Is observed by one `Operator Verification Set`

**Validation Rules**:
- The endpoint must be externally configurable only where needed by deployment tooling.
- The endpoint must not become the public production address.
- The health path must remain probeable through the gateway and, where needed, locally.

### 4. Proxy Header Policy

**Purpose**: The set of request headers nginx must preserve or add so the app behaves correctly behind the proxy.

**Fields**:
- `hostHeaderMode` — preserves the original `Host`
- `forwardedForEnabled` — emits `X-Forwarded-For`
- `forwardedProtoEnabled` — emits `X-Forwarded-Proto`
- `forwardedHostEnabled` — emits `X-Forwarded-Host`
- `realIpEnabled` — emits `X-Real-IP`
- `upgradeHeadersEnabled` — forwards `Upgrade`/`Connection` for Blazor Server interactivity

**Relationships**:
- Applied by one `Gateway Site Configuration`
- Supports one `Upstream Application Endpoint`

**Validation Rules**:
- Must preserve enough context for secure redirects, cookies, form flows, and interactive Blazor connections.
- Must align with the existing forwarded-header expectations in `Program.cs`.

### 5. Deployment Prerequisite Set

**Purpose**: The operator-supplied dependencies needed before the gateway can be activated safely.

**Fields**:
- `dnsControlAvailable` — whether `payslip4all.co.za` can resolve to the gateway host
- `certificateMaterialAvailable` — whether HTTPS material is ready for deployment
- `nginxRuntimeAvailable` — whether the host can install and run nginx
- `upstreamAppAvailable` — whether Payslip4All can bind on the planned internal endpoint
- `portAccessConfigured` — whether ports `80` and `443` are reachable as intended
- `secretDeliveryMechanism` — how external certificate material reaches the host

**Relationships**:
- Supports one `Gateway Site Configuration`
- Supports one `TLS Certificate Binding`
- Supports one `Upstream Application Endpoint`

**Validation Rules**:
- DNS control and certificate availability must be confirmed before public cutover.
- Secret delivery must stay external to source control.
- Upstream app availability must be verifiable before nginx activation.

### 6. Operator Verification Set

**Purpose**: The observable checks used to confirm the gateway works after activation.

**Fields**:
- `httpsUrl` — `https://payslip4all.co.za`
- `httpRedirectCheck` — expected redirect from HTTP to HTTPS
- `healthCheckUrl` — public `/health` probe
- `interactiveBlazorCheck` — evidence that WebSocket/interactive behavior still works
- `wrongHostCheck` — expected rejection for unrelated hostnames
- `upstreamFailureCheck` — expected unavailable response when the app is down

**Relationships**:
- Observes one `Gateway Site Configuration`
- Observes one `Upstream Application Endpoint`

**Validation Rules**:
- Checks must map directly to FR-003 through FR-010 and SC-002 through SC-005.
- Verification must not require undocumented deployment behavior outside the repo-owned assets.

## State Model

### Gateway Activation Lifecycle

1. `PrerequisitesReady` — DNS, cert material, host access, and upstream inputs are available.
2. `ConfigRendered` — nginx config and deployment-managed certificate paths are prepared on the host.
3. `Validated` — nginx syntax/config validation and app endpoint checks pass.
4. `Serving` — nginx serves `https://payslip4all.co.za` and proxies to the app successfully.
5. `Degraded` — the gateway is reachable but the upstream app is unavailable, so users receive a generic unavailable response.
6. `RotatingCertificate` — certificate material is being replaced and nginx is reloaded without changing the public host.

**Transitions**:
- `PrerequisitesReady -> ConfigRendered`: operator or bootstrap tooling places config and certificate references on the host.
- `ConfigRendered -> Validated`: config syntax and upstream connectivity are checked successfully.
- `Validated -> Serving`: nginx starts or reloads and serves the public domain over HTTPS.
- `Serving -> Degraded`: the upstream app becomes unhealthy or unreachable.
- `Degraded -> Serving`: the upstream app recovers and proxying resumes.
- `Serving -> RotatingCertificate`: certificate material is renewed or replaced.
- `RotatingCertificate -> Serving`: nginx reload completes and HTTPS serving continues on the same host/domain.
