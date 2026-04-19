# AWS CloudFormation Deployment

This guide describes the repository-owned AWS deployment path for Payslip4All using a single EC2 instance, nginx, an Elastic IP, SSM Session Manager access, and DynamoDB.

## What this deployment creates

- One EC2 web host for Payslip4All
- One Elastic IP attached directly to the EC2 instance
- One security group that exposes nginx on ports `80` and `443`
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
| `HostedPaymentsSecretArn` | Optional secret reference for hosted-payment and app-specific configuration |
| `TlsCertificateSecretArn` | External TLS certificate secret that supplies the nginx `fullchainPem` and `privkeyPem` values |

## Runtime environment variables

The EC2 bootstrap process writes these values into the service environment:

- `PERSISTENCE_PROVIDER=dynamodb`
- `DYNAMODB_REGION` set from the stack region
- `DYNAMODB_TABLE_PREFIX`
- `DYNAMODB_ENABLE_PITR=true`
- `ASPNETCORE_URLS=http://127.0.0.1:8080`

If `HostedPaymentsSecretArn` is provided, the instance also appends the secret values to the app environment file using AWS Secrets Manager. Keep reusable secrets out of the template itself.

## Architecture and traffic flow

1. An Elastic IP is attached directly to the EC2 instance.
2. Public HTTP and HTTPS traffic reaches nginx on ports `80` and `443`.
3. nginx redirects `http://payslip4all.co.za` to `https://payslip4all.co.za`.
4. nginx terminates TLS and reverse-proxies requests to the app on `127.0.0.1:8080`.
5. Operators connect to the instance through SSM Session Manager instead of SSH.
6. The web app starts with `PERSISTENCE_PROVIDER=dynamodb`, then provisions and uses the application-owned DynamoDB tables.

This is a no-ALB, no-Route53, no-ACM deployment flow. DNS publishing for `payslip4all.co.za` and certificate delivery are handled outside the stack so the public edge stays small and repository-owned.

## Operator-visible signals

Operators should verify the deployment with this explicit signal set:

- CloudFormation outputs: `ApplicationUrl`, `ElasticIpAddress`, `InstanceId`, `InstanceSecurityGroupId`, `SsmStartSessionCommand`, `HostedPaymentsSecretReference`, `TlsCertificateSecretReference`, `NginxConfigPath`, `BackupProtectionMode`, and `RestoreRunbook`
- `https://payslip4all.co.za/health`
- the `http://payslip4all.co.za` redirect to HTTPS
- successful `nginx -t` validation on the host
- the `Name` tag on the EC2 instance
- SSM Session Manager access to the instance

These signals are intentionally minimal: enough to verify stack identity, secure app reachability, operator access, and backup posture without turning this feature into a separate platform project.

## Five manual pre-launch actions

1. Publish or upload the application artifact that `ArtifactSource` will reference.
2. Stage the TLS certificate secret for nginx so it exposes `fullchainPem` and `privkeyPem`.
3. Reserve or confirm the Elastic IP workflow, then point `payslip4all.co.za` at that public address.
4. Gather the external secret references required for hosted-payment and application runtime configuration.
5. Launch `infra/aws/cloudformation/payslip4all-web.yaml` with the required parameters.

## Post-launch verification

1. Wait for the stack to complete.
2. Open the `ApplicationUrl` output after DNS for `payslip4all.co.za` is live.
3. Confirm the instance responds on `/health`.
4. Confirm `http://payslip4all.co.za` redirects once to `https://payslip4all.co.za`.
5. Start an SSM session using the `SsmStartSessionCommand` output.
6. Run `nginx -t`.
7. Confirm the app starts with `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, `DYNAMODB_TABLE_PREFIX`, and `ASPNETCORE_URLS=http://127.0.0.1:8080`.

## Cost notes

This deployment is optimized for lower cost than the previous managed-edge version.

- The template defaults to a small EC2 instance type to use as much of the free tier as practical.
- DynamoDB uses the app's on-demand table model, which is cost-efficient for low traffic.
- The template avoids managed edge services by following the no-ALB, no-Route53, no-ACM pattern.
- The main recurring costs are the EC2 instance, Elastic IP while attached, any EBS storage, any DynamoDB traffic or backup storage above the free tier, and the operator-managed certificate workflow.

## Health checks and startup validation

- Stack creation completes once AWS creates the instance resources; application bootstrap continues on the instance after launch.
- nginx owns the public edge while the app stays local to `127.0.0.1:8080`.
- `nginx -t` must pass before nginx is restarted.
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
2. Confirm `/health` returns a healthy response through nginx.
3. Confirm the HTTP endpoint redirects to HTTPS in one step.
4. Confirm the EC2 instance is tagged with a payslip4all-derived name.
5. Confirm SSM Session Manager can open a shell on the instance.
6. Confirm the app starts with `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, `DYNAMODB_TABLE_PREFIX`, and `ASPNETCORE_URLS=http://127.0.0.1:8080`.
