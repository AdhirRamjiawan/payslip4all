# Payslip4All nginx gateway

This directory contains the repository-owned nginx gateway definition for serving Payslip4All at `https://payslip4all.co.za`.

## Files

- `infra/nginx/payslip4all.conf` — nginx site configuration for HTTPS termination, HTTP→HTTPS redirect, reverse proxying, host filtering, and generic upstream failure handling.
- `infra/aws/cloudformation/payslip4all-web.yaml` — the hosted AWS path that installs nginx, stages the same gateway configuration, and publishes the app through an Elastic IP.
- `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh` — the standalone bootstrap script that mirrors the hosted AWS nginx setup.

## Fixed gateway behaviour

- Public production host: `payslip4all.co.za`
- Public listeners: `80` for redirect and `443` for HTTPS
- Upstream application endpoint: `127.0.0.1:8080`
- TLS runtime files: `/etc/nginx/certs/fullchain.pem` and `/etc/nginx/certs/privkey.pem`
- Wrong-host handling: unrelated hosts receive a gateway rejection instead of the production app

## Operator prerequisites

Before activation, confirm:

1. `payslip4all.co.za` can resolve to the target host or hosted AWS Elastic IP.
2. nginx is installed and can read `/etc/nginx/certs/fullchain.pem` and `/etc/nginx/certs/privkey.pem`.
3. The Payslip4All app can bind to `127.0.0.1:8080`.
4. Certificate material is delivered from an external source such as AWS Secrets Manager or another deployment-managed secret/file mechanism.

## Activation inputs

Operators must be able to supply or stage these inputs without committing certificate contents to source control:

| Input | Purpose |
|-------|---------|
| `payslip4all.co.za` DNS record | Publishes the fixed production host |
| `/etc/nginx/certs/fullchain.pem` | Public certificate chain consumed by nginx |
| `/etc/nginx/certs/privkey.pem` | Private key consumed by nginx |
| `127.0.0.1:8080` | Local-only upstream used by the app service |
| Optional AWS Secrets Manager secret | Hosted AWS delivery path for `fullchainPem` and `privkeyPem` values |

## Hosted AWS alignment

The existing hosted AWS path under `infra/aws/cloudformation/` now assumes nginx is the public edge:

- the app service runs with `ASPNETCORE_URLS=http://127.0.0.1:8080`,
- the EC2 security group exposes only nginx on ports `80` and `443`,
- bootstrap stages certificate files from an external secret reference,
- bootstrap validates the generated nginx config with `nginx -t` before restarting services.

## Recommended operator workflow

1. Stage the repository-owned gateway config from `infra/nginx/payslip4all.conf` onto the host (or let the hosted AWS bootstrap render the same content).
2. Place `fullchain.pem` and `privkey.pem` in `/etc/nginx/certs/`.
3. Start or restart the Payslip4All app on `127.0.0.1:8080`.
4. Validate the gateway with `nginx -t`.
5. Reload nginx and smoke-test `https://payslip4all.co.za` plus the HTTP redirect path.

## Smoke checks

- `https://payslip4all.co.za` returns the application successfully.
- `http://payslip4all.co.za` redirects once to HTTPS.
- `https://payslip4all.co.za/health` returns the app health payload.
- Interactive Blazor Server flows continue to work through forwarded headers and WebSocket upgrade support.
- wrong-host validation confirms unrelated hostnames are rejected without serving the production app.
- stopping the upstream returns a generic `503 Service temporarily unavailable.` response that does not disclose `127.0.0.1:8080`.

## Certificate replacement

Certificate rotation keeps the same public host and nginx routing rules:

1. Replace the external certificate source.
2. refresh `/etc/nginx/certs/fullchain.pem` and `/etc/nginx/certs/privkey.pem`,
3. run `nginx -t`,
4. reload nginx.
