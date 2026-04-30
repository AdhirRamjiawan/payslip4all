# Payslip4All YARP gateway

This directory is a secondary, reference-only YARP deployment note for serving Payslip4All at `https://payslip4all.co.za` without nginx. This document lives at `infra/yarp/README.md`, and the single operator-facing entrypoint is `specs/017-yarp-https-proxy/quickstart.md`.

## Hosted edge contract

- Public production host: `payslip4all.co.za`
- Public listeners: `80` for HTTP and `443` for HTTPS
- Internal upstream application endpoint: `http://127.0.0.1:8080`
- Gateway mode switch: `REVERSE_PROXY_ENABLED=true`
- Upstream selector: `REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080`
- Public host filter: `REVERSE_PROXY_PUBLIC_HOST=payslip4all.co.za`
- TLS certificate path: `Kestrel__Certificates__Default__Path=/etc/payslip4all/certs/payslip4all.pfx`

## Certificate handling

The hosted AWS path still expects `TlsCertificateSecretArn` to provide `fullchainPem` and `privkeyPem`. Bootstrap converts those values into a `.pfx` certificate so the YARP gateway can terminate TLS directly through Kestrel.

## Operator prerequisites

Before activation, confirm:

1. `payslip4all.co.za` resolves to the target host or Elastic IP.
2. The Payslip4All backend app can bind to `127.0.0.1:8080`.
3. The YARP gateway can bind to `0.0.0.0:80` and `0.0.0.0:443`.
4. Certificate material is delivered from an external source such as AWS Secrets Manager.

## Hosted AWS alignment

The deployment assets under `infra/aws/cloudformation/` and `bootstrap-payslip4all.sh` now use two services on the same EC2 instance:

- `payslip4all.service` for the backend app on `ASPNETCORE_URLS=http://127.0.0.1:8080`
- `payslip4all-gateway.service` for the public YARP edge

## Gateway behavior

- Correct-host HTTP requests redirect to HTTPS.
- Wrong-host requests receive `421`.
- Unreachable upstream responses collapse to a generic `503 Service temporarily unavailable.` body.
- YARP forwards the public host and HTTPS scheme to the backend app so forwarded-header-aware Blazor behavior continues to work.

## Discoverability

See also:

- `specs/017-yarp-https-proxy/quickstart.md`
- `specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md`
- `infra/aws/cloudformation/payslip4all-web.yaml`
- `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`
- `infra/aws/cloudformation/README.md`
