# Phase 1 Data Model: YARP HTTPS Reverse Proxy Migration

This feature adds no payroll-domain entities and no persistence schema. The relevant design model is runtime-, deployment-, and operator-facing.

## Entities

### 1. YarpPublicEdgeConfiguration

| Field | Type | Required | Validation / Rule |
|-------|------|----------|-------------------|
| `Enabled` | boolean | Yes | Controls whether `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs` starts in gateway mode |
| `PublicHost` | string | Yes when `Enabled=true` | Must be non-empty; production value is `payslip4all.co.za` |
| `UpstreamBaseUrl` | absolute URL | Yes when `Enabled=true` | Must be an absolute `http://` or `https://` URL; hosted default is `http://127.0.0.1:8080` |
| `ReadinessPath` | path | Yes | Fixed to `/health` as the single smoke-check contract |
| `RedirectHttpToHttps` | boolean | Yes | Must redirect correct-host HTTP requests in one navigation step |
| `WrongHostStatusCode` | integer | Yes | Fixed to `421` |
| `UnavailableStatusCode` | integer | Yes | Fixed to `503` |
| `UnavailableBody` | string | Yes | Exact generic body is `Service temporarily unavailable.` |
| `FailureDeadlineSeconds` | integer | Yes | Fixed upper bound is `10` seconds for upstream unavailability behavior |

**State transitions**:
- `Disabled` → `Enabled` when deployment sets valid reverse-proxy configuration.
- `Enabled` → `Active` when the gateway can bind and forward traffic correctly.
- `Enabled` → `Blocked` when startup validation or TLS activation prerequisites fail.

### 2. GatewayTlsCertificateBinding

| Field | Type | Required | Validation / Rule |
|-------|------|----------|-------------------|
| `SecretReference` | string | Yes for hosted deployment | Identifies the external certificate source, such as Secrets Manager |
| `FullChainPemKey` | string | Yes | Must resolve to the full certificate chain in the secret payload |
| `PrivateKeyPemKey` | string | Yes | Must resolve to the private key in the secret payload |
| `PfxPath` | file path | Yes | Hosted path is `/etc/payslip4all/certs/payslip4all.pfx` |
| `PfxPasswordSource` | secure runtime value | Yes | Must stay outside source control and match the staged `.pfx` |
| `ActivationErrorMessage` | string | Yes | Exact operator-visible text: `HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.` |
| `FailClosed` | boolean | Yes | Must remain `true`; no insecure fallback traffic is allowed |

**State transitions**:
- `Secret declared` → `Secret retrieved` when bootstrap can read the external payload.
- `Secret retrieved` → `Pfx materialized` when PEM inputs are converted successfully.
- `Pfx materialized` → `Gateway TLS active` when Kestrel can bind HTTPS.
- Any missing/invalid/unreadable certificate material → `Activation blocked` with the exact operator-visible error.

### 3. UpstreamApplicationEndpoint

| Field | Type | Required | Validation / Rule |
|-------|------|----------|-------------------|
| `ServiceName` | string | Yes | Hosted service is `payslip4all.service` |
| `BaseUrl` | absolute URL | Yes | Hosted default is `http://127.0.0.1:8080` |
| `PubliclyReachable` | boolean | Yes | Must remain `false` for hosted default topology |
| `PersistenceProvider` | string | Yes | Hosted path continues to use `dynamodb` unless another internal-only environment is explicitly documented |
| `EnvironmentFilePath` | file path | Yes | Must be deployment-managed and must not embed source-controlled secrets |

### 4. GatewayServiceDefinition

| Field | Type | Required | Validation / Rule |
|-------|------|----------|-------------------|
| `ServiceName` | string | Yes | Hosted service is `payslip4all-gateway.service` |
| `ListenUrls` | string | Yes | Hosted value is `http://0.0.0.0:80;https://0.0.0.0:443` |
| `PublicHost` | string | Yes | Must equal `payslip4all.co.za` |
| `UpstreamBaseUrl` | absolute URL | Yes | Must point to `UpstreamApplicationEndpoint.BaseUrl` |
| `ReadinessPath` | path | Yes | Must expose `/health` through the public host |
| `EnvironmentFilePath` | file path | Yes | Must carry reverse-proxy and Kestrel certificate settings |

### 5. ForwardedRequestPolicy

| Field | Type | Required | Validation / Rule |
|-------|------|----------|-------------------|
| `AllowedHost` | string | Yes | `payslip4all.co.za` only |
| `ForwardedHeaders` | set | Yes | Must preserve original host, scheme, and client IP context |
| `PreserveRedirectHost` | boolean | Yes | Backend-generated redirects must stay on `https://payslip4all.co.za` |
| `SupportBlazorSignalR` | boolean | Yes | Real-time Blazor/SignalR flows must remain bound to the public host |
| `PreserveFormNavigation` | boolean | Yes | Form submissions and follow-up navigation must not switch to the internal upstream address |
| `WrongHostStatusCode` | integer | Yes | `421` |
| `UnavailableStatusCode` | integer | Yes | `503` |
| `UnavailableBody` | string | Yes | `Service temporarily unavailable.` |

### 6. DeploymentPrerequisiteSet

| Field | Type | Required | Validation / Rule |
|-------|------|----------|-------------------|
| `DnsReady` | boolean | Yes | `payslip4all.co.za` must resolve to the target public address |
| `CertificateMaterialReady` | boolean | Yes | Required before activation; missing material blocks readiness |
| `BackendArtifactReady` | boolean | Yes | Published web artifact must be available before service startup |
| `HostedAwsInputsReady` | boolean | Hosted path only | CloudFormation/bootstrap inputs must be known and staged |
| `HealthSmokeCheckReady` | boolean | Yes | `/health` must be reachable through the public HTTPS host |

## Relationships

- `GatewayServiceDefinition.UpstreamBaseUrl` targets `UpstreamApplicationEndpoint.BaseUrl`.
- `GatewayServiceDefinition` enforces `YarpPublicEdgeConfiguration`.
- `GatewayTlsCertificateBinding` must reach `Gateway TLS active` before `GatewayServiceDefinition` can become publicly active.
- `ForwardedRequestPolicy` is applied by the gateway to preserve redirect, Blazor/SignalR, and form/navigation behavior.
- `DeploymentPrerequisiteSet` gates both the backend and gateway activation path.

## Validation Summary

- No new database tables, EF Core migrations, or DynamoDB repository contracts are introduced.
- All validations are startup-, deployment-, or operator-smoke-check validations.
- Certificate activation is explicitly fail-closed and bound to an exact operator-visible error message.
- `/health` is the single readiness contract for public-edge validation.
