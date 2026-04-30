# YARP Gateway Contract

This document defines the canonical public-edge contract for `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/`.

## 1. Governed Scope

- Governing approval: Payslip4All Constitution v1.4.0, **Technology Stack & Constraints → Public HTTPS Edge**
- Approved dependency: `Yarp.ReverseProxy` 2.2.x inside `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web`
- Boundary rule: this contract must remain inside the existing four-project solution and must not create a separate gateway project

## 2. Public Entry Point

| Contract Item | Required Value |
|---------------|----------------|
| Public host | `payslip4all.co.za` |
| HTTP listener | `80` |
| HTTPS listener | `443` |
| Public gateway mode switch | `REVERSE_PROXY_ENABLED=true` |
| Upstream backend URL | `REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080` |
| Host filter | `REVERSE_PROXY_PUBLIC_HOST=payslip4all.co.za` |
| Single readiness smoke-check | `https://payslip4all.co.za/health` |

## 3. Activation Inputs

The following inputs are required before the contract can be activated:

| Input | Source | Requirement |
|-------|--------|-------------|
| DNS record for `payslip4all.co.za` | Operator-managed external DNS | Must resolve to the public host or Elastic IP |
| TLS secret reference | External secret manager | Must provide `fullchainPem` and `privkeyPem` values |
| Backend app artifact | Published application bundle | Must be available to bootstrap/start the backend service |
| Gateway certificate path | Runtime environment | Must point to `/etc/payslip4all/certs/payslip4all.pfx` in the hosted path |
| Gateway certificate password | Runtime environment | Must match the materialized `.pfx` file |

## 4. Certificate Activation Contract

- Certificate activation is fail-closed.
- Public traffic must not be served insecurely when certificate material is missing, invalid, or unreadable.
- The operator-visible activation error must be exactly:

```text
HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.
```

## 5. Request/Response Contract

### 5.1 Correct host over HTTP

**Request**

```http
GET / HTTP/1.1
Host: payslip4all.co.za
```

**Expected behavior**
- Response is a single redirect to the equivalent `https://payslip4all.co.za/...` URL.
- No backend app interaction is required before the redirect.

### 5.2 Correct host over HTTPS readiness

**Request**

```http
GET /health HTTP/1.1
Host: payslip4all.co.za
```

**Expected behavior**
- `/health` is the single readiness smoke-check contract.
- The request succeeds through the public HTTPS edge within 5 seconds when the edge is ready.
- Successful backend responses are returned to the caller.

### 5.3 Correct host over HTTPS interactive traffic

**Request**

```http
GET /some-app-route HTTP/1.1
Host: payslip4all.co.za
```

**Expected behavior**
- Request is forwarded to the configured backend app.
- Original host and HTTPS scheme are preserved.
- Backend-generated redirects stay on `https://payslip4all.co.za`.
- Blazor/SignalR interactions remain bound to the public host.
- Form submissions and follow-up navigation remain bound to the public host and do not expose the internal upstream address.

### 5.4 Wrong host

**Request**

```http
GET / HTTP/1.1
Host: unexpected.example.com
```

**Expected behavior**
- Gateway rejects the request with `421 Misdirected Request`.
- The unexpected hostname is not treated as a valid production entry point.

### 5.5 Unreachable upstream

**Request**

```http
GET / HTTP/1.1
Host: payslip4all.co.za
```

**Expected behavior**
- If the backend app is unavailable, the gateway returns `503 Service Unavailable` within 10 seconds.
- The response body is exactly `Service temporarily unavailable.` for user-facing purposes.
- No internal endpoint, local port, or stack trace detail is exposed.

## 6. Forwarding Requirements

The gateway must preserve the original request context required by the backend application:

- original host
- original scheme (`https`)
- original client IP / forwarding context

This contract is required so redirects, Blazor Server, SignalR, cookie security, forms, and follow-up navigation behave as if the user interacted directly with the public HTTPS host.

## 7. Deployment Boundary

- The gateway is implemented inside `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web`.
- Hosted deployment artifacts remain under `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/`.
- Operator guidance for this gateway lives under `/Users/adhirramjiawan/projects/payslip4all/infra/yarp/README.md`.
- Legacy nginx feature artifacts are historical only and do not define the contract for this branch.
