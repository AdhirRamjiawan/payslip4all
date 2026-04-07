# AWS CloudFormation Deployment

This guide describes the repository-owned AWS deployment path for Payslip4All using a single EC2 instance, an Application Load Balancer, Route 53, and DynamoDB.

## What this deployment creates

- One internet-facing Application Load Balancer for `payslip.co.za`
- One EC2 web host tagged with payslip.co.za-derived metadata
- One Route 53 alias record that points `payslip.co.za` to the ALB
- One IAM instance profile so the app can use the AWS SDK credential chain
- A DynamoDB-backed runtime with `PERSISTENCE_PROVIDER=dynamodb`
- Automated DynamoDB point-in-time recovery for regular backups in hosted AWS

The CloudFormation template lives at `infra/aws/cloudformation/payslip4all-web.yaml`. The bootstrap logic mirrored by the stack user data lives at `infra/aws/cloudformation/user-data/bootstrap-payslip4all.sh`.

## Prerequisites

You must prepare these values before launching the stack:

| Parameter | Why it is required |
|-----------|--------------------|
| `EnvironmentName` | Distinguishes production from any future non-production environment |
| `DomainName` | Public hostname, expected to be `payslip.co.za` |
| `HostedZoneId` | Lets Route 53 publish the ALB alias record |
| `CertificateArn` | Enables HTTPS on the public listener through ACM |
| `InstanceType` | Defaults to a low-cost option such as `t3.micro`, but can be overridden |
| `ArtifactSource` | Points the bootstrap process at the published Payslip4All bundle |
| `AllowedSshCidr` | Restricts operator shell access to approved IP ranges |
| `DynamoDbRegion` | Required by the application's DynamoDB startup validation |
| `DynamoDbTablePrefix` | Namespaces the application-owned table set |
| `HostedPaymentsSecretArn` | Optional secret reference for hosted-payment and app-specific configuration |

## Runtime environment variables

The EC2 bootstrap process writes these values into the service environment:

- `PERSISTENCE_PROVIDER=dynamodb`
- `DYNAMODB_REGION`
- `DYNAMODB_TABLE_PREFIX`
- `DYNAMODB_ENABLE_PITR`
- `ASPNETCORE_URLS=http://0.0.0.0:80`

If `HostedPaymentsSecretArn` is provided, the instance also appends the secret values to the app environment file using AWS Secrets Manager. Keep reusable secrets out of the template itself.

## Architecture and traffic flow

1. Route 53 publishes `payslip.co.za` as an alias to the Application Load Balancer.
2. The ALB terminates TLS with ACM and redirects HTTP traffic to HTTPS.
3. The ALB forwards only healthy requests to the EC2 instance on port 80.
4. Payslip4All responds to the ALB health check at `/health`.
5. The web app starts with `PERSISTENCE_PROVIDER=dynamodb`, then provisions and uses the application-owned DynamoDB tables.

The EC2 instance uses a payslip.co.za-derived `Name` tag such as `payslip-co-za-web-prod`. The public domain belongs to the ALB only; the instance is identified operationally through tags and instance IDs rather than a second public DNS record.

## Operator-visible signals

Operators should verify the deployment with this explicit signal set:

- CloudFormation outputs: `ApplicationUrl`, `InstanceId`, `LoadBalancerArn`, `InstanceSecurityGroupId`, `LoadBalancerSecurityGroupId`, `HostedPaymentsSecretReference`, `BackupProtectionMode`, and `RestoreRunbook`
- ALB target health for the registered EC2 instance
- The public `/health` endpoint
- The payslip.co.za-derived `Name` tags on the ALB and EC2 instance

These signals are intentionally minimal: enough to verify stack identity, app reachability, network wiring, and backup posture without turning this feature into a separate observability project.

## Five manual pre-launch actions

1. Publish or upload the application artifact that `ArtifactSource` will reference.
2. Confirm the ACM certificate for `payslip.co.za` is issued in the target AWS region.
3. Confirm the Route 53 hosted zone is authoritative for `payslip.co.za`.
4. Gather the external secret references required for hosted-payment and application runtime configuration.
5. Launch `infra/aws/cloudformation/payslip4all-web.yaml` with the required parameters.

These five actions are the complete manual pre-launch workflow for this feature.

## Post-launch verification

1. Wait for the stack to complete.
2. Open `https://payslip.co.za`.
3. Confirm the ALB returns the app and the EC2 instance is tagged for the same environment.
4. Inspect ALB target health and confirm the registered instance is healthy.
5. Call `/health` and confirm the application returns a healthy response.

## Free-tier and cost notes

This deployment is optimized for low cost, but it is **not fully free tier** because the required edge services are billable.

- The template defaults to a small EC2 instance type to use as much of the free tier as practical.
- DynamoDB uses the app's on-demand table model, which is cost-efficient for low traffic.
- **Application Load Balancer (ALB)** is a recurring cost outside the free tier.
- **Route 53** hosted zones and DNS queries are also outside the free tier.
- ACM certificates are not the primary cost concern here; the paid pieces are the ALB, Route 53, and any DynamoDB traffic or backup storage above the free tier.

## Health checks and startup validation

- The ALB target group probes `/health`.
- The app trusts forwarded headers and redirects to HTTPS correctly when fronted by the ALB.
- The web app enables request logging so behind-ALB traffic is visible in structured application logs.
- Startup fails fast if `DYNAMODB_REGION` is missing or if only one of `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` is supplied.
- The preferred hosted AWS path uses the IAM instance profile, not static AWS credentials.

## Replaceable compute behaviour

The CloudFormation stack keeps networking, DNS, ALB resources, and DynamoDB-backed application data independent of the EC2 instance lifecycle. Updating the stack and replacing or recreating the EC2 instance should therefore keep application data available and ensure the public entry point remains the same, assuming the replacement instance boots successfully and rejoins the target group.

## DynamoDB backup and restore

The hosted AWS deployment enables **point-in-time recovery** by default so the Payslip4All table set is regularly backed up without daily manual jobs.

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
5. Re-run application verification and confirm `https://payslip.co.za` still serves correctly after the data recovery workflow.

Aim to complete the restore drill, including validation, within the feature's 60-minute recovery target.

## Recommended verification

After launch:

1. Browse to `https://payslip.co.za`.
2. Confirm HTTP requests are redirected to HTTPS.
3. Confirm `/health` returns a healthy response through the stack.
4. Confirm the EC2 instance is tagged with a payslip.co.za-derived name.
5. Confirm the app starts with `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, and `DYNAMODB_TABLE_PREFIX`.
6. Confirm the hosted AWS environment reports point-in-time recovery enabled for the Payslip4All tables.
