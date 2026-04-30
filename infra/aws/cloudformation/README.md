# AWS CloudFormation Deployment

This guide is a secondary, reference-only AWS deployment note for Payslip4All. The single operator-facing entrypoint is `specs/017-yarp-https-proxy/quickstart.md`, and the canonical public-edge contract lives in `specs/017-yarp-https-proxy/contracts/yarp-gateway-contract.md`.

## What this deployment creates

- One EC2 web host for Payslip4All
- One Elastic IP attached directly to the EC2 instance
- One security group that exposes the YARP gateway on ports `80` and `443`
- One IAM instance profile so the app can use the AWS SDK credential chain and SSM Session Manager
- A DynamoDB-backed runtime with `PERSISTENCE_PROVIDER=dynamodb`
- Automated DynamoDB point-in-time recovery for regular backups in hosted AWS

The CloudFormation template lives at `infra/aws/cloudformation/payslip4all-web.yaml`. The bootstrap logic mirrored by the stack user data lives at `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`.

## Prerequisites

You must prepare these values before launching the stack:

| Parameter | Why it is required |
|-----------|--------------------|
| `EnvironmentName` | Distinguishes production from any future non-production environment |
| `VpcId` | Places the EC2 instance in an existing VPC |
| `SubnetId` | Places the EC2 instance in an existing public subnet with internet access |
| `InstanceType` | Defaults to a low-cost option such as `t3.micro`, but can be overridden |
| `ArtifactSource` | Points the bootstrap process at the published Payslip4All bundle |
| `DynamoDbTablePrefix` | Namespaces the application-owned table set |
| `AppConfigSecretArn` | Optional secret reference for the rendered custom app-configuration JSON artifact |
| `HostedPaymentsSecretArn` | Optional legacy secret reference for direct environment-style overrides |
| `TlsCertificateSecretArn` | External TLS certificate secret that supplies the `fullchainPem` and `privkeyPem` values that bootstrap converts into the YARP certificate |

## Runtime environment variables

The EC2 bootstrap process writes these values into the service environment:

- `PERSISTENCE_PROVIDER=dynamodb`
- `DYNAMODB_REGION` set from the stack region
- `DYNAMODB_TABLE_PREFIX`
- `DYNAMODB_ENABLE_PITR=true`
- `ASPNETCORE_URLS=http://127.0.0.1:8080`
- `REVERSE_PROXY_ENABLED=true`
- `REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080`

If `AppConfigSecretArn` is provided, bootstrap renders the secret to `/etc/payslip4all/app-config.secrets.json` and the app resolves it with the precedence `environment variables > rendered AWS-secret config > checked-in appsettings > code defaults`.

If `HostedPaymentsSecretArn` is provided, the instance also appends the secret values to the app environment file as direct environment-style overrides. That legacy path therefore still wins over the rendered app-config secret because environment variables remain the highest-precedence deployment source.

## Architecture and traffic flow

1. An Elastic IP is attached directly to the EC2 instance.
2. Public HTTP and HTTPS traffic reaches the YARP gateway on ports `80` and `443`.
3. YARP redirects `http://payslip4all.co.za` to `https://payslip4all.co.za`.
4. YARP terminates TLS and reverse-proxies requests to the app on `127.0.0.1:8080`.
5. Operators connect to the instance through SSM Session Manager instead of SSH.
6. The web app starts with `PERSISTENCE_PROVIDER=dynamodb`, then provisions and uses the application-owned DynamoDB tables.

This is a no-ALB, no-Route53, no-ACM deployment flow. DNS publishing for `payslip4all.co.za` and certificate delivery are handled outside the stack so the public edge stays small and repository-owned.

## Operator-visible signals

Operators should verify the deployment with this explicit signal set:

- CloudFormation outputs: `ApplicationUrl`, `ElasticIpAddress`, `InstanceId`, `InstanceSecurityGroupId`, `SsmStartSessionCommand`, `AppConfigSecretReference`, `AppConfigSecretsFilePath`, `HostedPaymentsSecretReference`, `TlsCertificateSecretReference`, `GatewayServiceName`, `BackupProtectionMode`, and `RestoreRunbook`
- `https://payslip4all.co.za/health`
- the `http://payslip4all.co.za` redirect to HTTPS
- successful `payslip4all-gateway.service` startup on the host
- the `Name` tag on the EC2 instance
- SSM Session Manager access to the instance

These signals are intentionally minimal: enough to verify stack identity, secure app reachability, operator access, and backup posture without turning this feature into a separate platform project.

## Five manual pre-launch actions

1. Publish or upload the application artifact that `ArtifactSource` will reference.
2. Stage the TLS certificate secret for YARP so it exposes `fullchainPem` and `privkeyPem`.
3. Reserve or confirm the Elastic IP workflow, then point `payslip4all.co.za` at that public address.
4. Gather the external secret references required for the rendered app-config secret, any legacy direct environment overrides, and TLS certificate delivery.
5. Launch `infra/aws/cloudformation/payslip4all-web.yaml` with the required parameters.

## Post-launch verification

1. Wait for the stack to complete.
2. Open the `ApplicationUrl` output after DNS for `payslip4all.co.za` is live.
3. Confirm 3 consecutive requests to `/health` each succeed within 5 seconds.
4. Confirm `http://payslip4all.co.za` redirects once to `https://payslip4all.co.za`.
5. Start an SSM session using the `SsmStartSessionCommand` output.
6. Confirm `payslip4all-gateway.service` is active.
7. Confirm the app starts with `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, `DYNAMODB_TABLE_PREFIX`, `ASPNETCORE_URLS=http://127.0.0.1:8080`, `REVERSE_PROXY_ENABLED=true`, `REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080`, and `REVERSE_PROXY_ACTIVITY_TIMEOUT_SECONDS=10`.
8. If `AppConfigSecretArn` is set, confirm `/etc/payslip4all/app-config.secrets.json` exists with mode `600`.
9. If certificate material is missing or invalid, expect the exact fail-closed activation error `HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.`
10. If the upstream becomes unavailable, confirm the public edge returns `503 Service temporarily unavailable.` within 10 seconds.

## Custom app-config secret contract

Use `AppConfigSecretArn` for repo-owned custom settings that need AWS Secrets Manager support without flattening them into environment variable names.

- The secret payload must be a JSON object with flat ASP.NET Core keys such as `ConnectionStrings:DefaultConnection`, `Auth:Cookie:ExpireDays`, and `HostedPayments:PayFast:MerchantId`.
- The covered catalog includes persistence-provider selection, relational connection strings, `DYNAMODB_*`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `Auth:Cookie:ExpireDays`, and `HostedPayments:PayFast:*`.
- Keep TLS certificate keys in the separate `TlsCertificateSecretArn` secret.
- Mixed-source deployments are supported: some covered keys may stay in checked-in appsettings, others may come from `/etc/payslip4all/app-config.secrets.json`, and emergency overrides may still come from environment variables.

Example payload:

```json
{
  "ConnectionStrings:DefaultConnection": "Data Source=/opt/payslip4all/payslip4all.db",
  "Auth:Cookie:ExpireDays": "30",
  "HostedPayments:PayFast:MerchantId": "10047421",
  "HostedPayments:PayFast:MerchantKey": "merchant-key",
  "HostedPayments:PayFast:PublicNotifyUrl": "https://payslip4all.co.za/api/payments/payfast/notify"
}
```

## Mixed-source and failure validation

- To validate mixed-source behaviour, place one covered setting only in checked-in appsettings, one only in `AppConfigSecretArn`, and one in both the rendered secret and a direct environment override, then confirm the effective order remains `environment variables > rendered AWS-secret config > checked-in appsettings`.
- Missing or unreadable `AppConfigSecretArn` references must fail bootstrap before the service starts.
- Malformed or incomplete rendered values must fail safely at app startup or before the affected feature executes, without printing secret contents.
- Validate operator diagnostics by checking the bootstrap output and service logs for key names or groups only; secret values should never appear.

## Cost notes

This deployment is optimized for lower cost than the previous managed-edge version.

- The template defaults to a small EC2 instance type to use as much of the free tier as practical.
- DynamoDB uses the app's on-demand table model, which is cost-efficient for low traffic.
- The template avoids managed edge services by following the no-ALB, no-Route53, no-ACM pattern.
- The main recurring costs are the EC2 instance, Elastic IP while attached, any EBS storage, any DynamoDB traffic or backup storage above the free tier, and the operator-managed certificate workflow.

## Health checks and startup validation

- Stack creation completes once AWS creates the instance resources; application bootstrap continues on the instance after launch.
- YARP owns the public edge while the app stays local to `127.0.0.1:8080`.
- `payslip4all-gateway.service` must start successfully before the public edge is considered healthy.
- Startup fails fast if the DynamoDB configuration or TLS secret wiring is incomplete at runtime.
- The preferred hosted AWS path uses the IAM instance profile, not static AWS credentials.

## Replaceable compute behaviour

The CloudFormation stack keeps application data in DynamoDB and keeps the public endpoint stable through the Elastic IP. Updating the stack and replacing or recreating the EC2 instance should therefore keep application data available and restore the same public endpoint once the replacement instance boots successfully, the certificate secret is restaged, and the Elastic IP is attached. The public entry point remains the same when the instance is replaced correctly.

## DynamoDB backup and restore

The hosted AWS deployment enables **point-in-time recovery** so the Payslip4All table set is regularly backed up without daily manual jobs.

- `DYNAMODB_ENABLE_PITR=true` keeps backup protection on in hosted AWS.
- If `DYNAMODB_ENDPOINT` is set for a local emulator, the backup-protection hosted service skips PITR configuration.
- Restores should target new DynamoDB tables first so recovery does not overwrite the live environment in place.
- The recommended non-live naming pattern is `{live-table-name}-restore-YYYYMMDDHHMMSS`.
- Always restore to a new table before considering any cutover.

### Restore runbook

1. Identify the affected table set for the current `DYNAMODB_TABLE_PREFIX`.
2. Restore each required table to a new table name from the desired recovery point so every restore goes to a new table rather than the live one.
3. Validate the restored data before any cutover.
4. If a cutover is required, update the application's table prefix or restore mapping in a controlled maintenance window.
5. Re-run application verification and confirm the `ApplicationUrl` output still serves correctly after the data recovery workflow.

## Recommended verification

After launch:

1. Browse to `https://payslip4all.co.za`.
2. Confirm `/health` returns a healthy response through YARP.
3. Confirm the HTTP endpoint redirects to HTTPS in one step.
4. Confirm the EC2 instance is tagged with a payslip4all-derived name.
5. Confirm SSM Session Manager can open a shell on the instance.
6. Confirm the app starts with `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, `DYNAMODB_TABLE_PREFIX`, `ASPNETCORE_URLS=http://127.0.0.1:8080`, `REVERSE_PROXY_ENABLED=true`, and `REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080`.
