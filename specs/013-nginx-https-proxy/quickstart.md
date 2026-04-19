# Quickstart: nginx HTTPS Reverse Proxy

## Purpose

Validate that the planned nginx gateway assets are sufficient to expose Payslip4All at `payslip4all.co.za` over HTTPS while preserving the existing ASP.NET Core reverse-proxy behavior.

## Prerequisites

- Control of the `payslip4all.co.za` DNS records
- A Linux host (or EC2 instance) that can run nginx and reach the Payslip4All app
- Deployment-managed TLS certificate and private key material for `payslip4all.co.za` (for the hosted AWS path, a secret exposing `fullchainPem` and `privkeyPem`)
- A deployable Payslip4All app configured to bind on the planned internal endpoint (`127.0.0.1:8080`)
- Network/security-group rules that allow inbound `80` and `443` to nginx
- Any external secret or file-delivery mechanism required to place certificate material on the host

## Scenario 1: Prepare gateway inputs

1. Confirm `payslip4all.co.za` will resolve to the nginx host.
2. Confirm the TLS certificate and key for `payslip4all.co.za` are available from the chosen deployment-managed source.
3. Confirm the Payslip4All app can start on the planned internal endpoint instead of binding publicly on port `80`.
4. Confirm nginx can be installed and started on the target host.

## Scenario 2: Stage the gateway configuration

1. Place the repository-owned nginx config from `infra/nginx/` on the target host.
2. Place or render the certificate and key into the expected runtime location (for example `/etc/nginx/certs/` with `fullchain.pem` and `privkey.pem`).
3. Configure the app environment so nginx owns ports `80` and `443`, while the app listens on the planned internal endpoint.
4. Validate the nginx configuration with `nginx -t` before reload/start.

## Scenario 3: Verify secure public access

1. Browse to `https://payslip4all.co.za`.
2. Confirm the browser reaches Payslip4All successfully over HTTPS.
3. Browse to `http://payslip4all.co.za`.
4. Confirm the request redirects to the equivalent HTTPS URL in a single navigation step.

## Scenario 4: Verify proxied app behavior

1. Open the public site and complete an interactive navigation flow through the nginx host.
2. Confirm the app still behaves as a Blazor Server application behind the proxy.
3. Confirm `/health` returns a healthy response through `https://payslip4all.co.za/health`.
4. Confirm generated redirects, secure cookies, and normal form submissions continue to use the public HTTPS host.

## Scenario 5: Verify host restriction

1. Send a request to the gateway host using a hostname other than `payslip4all.co.za`.
2. Confirm the gateway does not serve the production app for that unrelated hostname.
3. Confirm the rejection path does not disclose the internal app endpoint.

## Scenario 6: Verify upstream failure handling

1. Stop or make the internal Payslip4All endpoint unavailable.
2. Request `https://payslip4all.co.za`.
3. Confirm users receive an unavailable response within 10 seconds.
4. Confirm the response does not reveal `127.0.0.1:8080` or other internal network details.

## Scenario 7: Verify certificate replacement workflow

1. Replace or rotate the deployment-managed certificate material without changing the public domain.
2. Re-run `nginx -t` and reload nginx using the documented operator workflow.
3. Confirm `https://payslip4all.co.za` still serves successfully with the renewed certificate.

## Validation Outcome

The feature is ready for implementation when the planned assets and instructions support all seven scenarios above without requiring undocumented host-specific steps outside the repository-owned deployment flow.
