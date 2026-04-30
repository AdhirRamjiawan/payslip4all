# Quickstart: YARP HTTPS Reverse Proxy Migration

Use this quickstart as the single operator-facing entrypoint to validate the refreshed YARP public-edge plan for `/Users/adhirramjiawan/projects/payslip4all`.

## 1. Confirm prerequisites

1. Confirm `payslip4all.co.za` can be pointed at the target public host or Elastic IP.
2. Confirm external TLS certificate material is available and can provide the required `fullchainPem` and `privkeyPem` values.
3. Confirm the published application artifact for `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web` is available to the hosted deployment flow.
4. Confirm the backend app can run on the internal-only upstream `http://127.0.0.1:8080`.

## 2. Confirm the repository-owned public-edge assets

Review these files before implementation or deployment work:

- `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md` (canonical contract)
- `/Users/adhirramjiawan/projects/payslip4all/infra/yarp/README.md`
- `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/README.md`
- `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/payslip4all-web.yaml`
- `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`
- `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/Program.cs`
- `/Users/adhirramjiawan/projects/payslip4all/src/Payslip4All.Web/ReverseProxyModeOptions.cs`
- Supporting infra documents are reference-only and must point back to this quickstart.

## 3. Planned hosted runtime contract

### Backend service

```text
ASPNETCORE_URLS=http://127.0.0.1:8080
PERSISTENCE_PROVIDER=dynamodb
```

### Gateway service

```text
ASPNETCORE_URLS=http://0.0.0.0:80;https://0.0.0.0:443
REVERSE_PROXY_ENABLED=true
REVERSE_PROXY_PUBLIC_HOST=payslip4all.co.za
REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080
Kestrel__Certificates__Default__Path=/etc/payslip4all/certs/payslip4all.pfx
```

## 4. Single smoke-check and behavior validation

1. Start or deploy the backend app so it binds to `http://127.0.0.1:8080`.
2. Start or deploy the gateway service with reverse-proxy mode enabled and certificate inputs staged.
3. Use `https://payslip4all.co.za/health` as the single readiness smoke-check and confirm 3 consecutive requests each succeed within 5 seconds.
4. Verify `http://payslip4all.co.za` redirects once to the equivalent `https://payslip4all.co.za` URL.
5. Verify representative public-host interactions preserve forwarding context:
   - an application-generated redirect stays on `https://payslip4all.co.za`
   - a Blazor/SignalR interaction remains bound to the public host
   - a form submission and follow-up navigation do not reveal or switch to `http://127.0.0.1:8080`
6. Verify a request for an unrelated hostname returns HTTP `421 Misdirected Request`.
7. Stop or break the backend app temporarily and verify the public edge returns HTTP `503 Service Unavailable` with the exact generic body `Service temporarily unavailable.` within 10 seconds.

## 5. Certificate activation failure expectation

If certificate material is missing, invalid, or unreadable, the gateway must fail closed before serving public traffic. The expected operator-visible error is:

```text
HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.
```

Do not proceed with any deployment flow that serves insecure fallback traffic.

## 6. TDD checkpoints for implementation

Before implementation tasks begin, create failing tests that prove:

1. gateway mode is activated only when reverse-proxy settings and certificate prerequisites are valid,
2. `/health` remains the single public readiness smoke-check,
3. redirects, Blazor/SignalR flows, and form/navigation preserve the public host and HTTPS context,
4. wrong-host requests are rejected with `421`,
5. upstream failures collapse to the exact generic `503 Service temporarily unavailable.` response within 10 seconds,
6. hosted docs/bootstrap artifacts remain YARP-first and keep `http://127.0.0.1:8080` as the hosted default upstream.

## 7. Next planning handoff

After this refresh, the next step is to update `/Users/adhirramjiawan/projects/payslip4all/specs/017-yarp-https-proxy/tasks.md` and then implement the tasks with the constitution-required TDD-first workflow and Manual Test Gate.
