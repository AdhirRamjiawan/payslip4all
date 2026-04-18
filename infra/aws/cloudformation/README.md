# AWS CloudFormation Deployment

This guide describes the repository-owned AWS deployment path for Payslip4All using a single EC2 instance, an Elastic IP, SSM Session Manager access, and DynamoDB.

## What this deployment creates

- One EC2 web host for Payslip4All
- One Elastic IP attached directly to the EC2 instance
- One security group that exposes HTTP on port 80
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

## Runtime environment variables

The EC2 bootstrap process writes these values into the service environment:

- `PERSISTENCE_PROVIDER=dynamodb`
- `DYNAMODB_REGION` set from the stack region
- `DYNAMODB_TABLE_PREFIX`
- `DYNAMODB_ENABLE_PITR=true`
- `ASPNETCORE_URLS=http://0.0.0.0:80`

If `HostedPaymentsSecretArn` is provided, the instance also appends the secret values to the app environment file using AWS Secrets Manager. Keep reusable secrets out of the template itself.

## Architecture and traffic flow

1. An Elastic IP is attached directly to the EC2 instance.
2. Public HTTP traffic reaches the app on port 80.
3. Operators connect to the instance through SSM Session Manager instead of SSH.
4. The web app starts with `PERSISTENCE_PROVIDER=dynamodb`, then provisions and uses the application-owned DynamoDB tables.

The EC2 instance uses a `Name` tag such as `payslip4all-web-prod`. This template intentionally avoids Route 53, ACM, and Application Load Balancer resources so stack creation stays as small and direct as possible.

## Operator-visible signals

Operators should verify the deployment with this explicit signal set:

- CloudFormation outputs: `ApplicationUrl`, `ElasticIpAddress`, `InstanceId`, `InstanceSecurityGroupId`, `SsmStartSessionCommand`, `HostedPaymentsSecretReference`, `BackupProtectionMode`, and `RestoreRunbook`
- The public `/health` endpoint through the Elastic IP
- The `Name` tag on the EC2 instance
- SSM Session Manager access to the instance

These signals are intentionally minimal: enough to verify stack identity, app reachability, operator access, and backup posture without turning this feature into a separate platform project.

## Manual pre-launch actions

1. Publish or upload the application artifact that `ArtifactSource` will reference.
2. Confirm the chosen subnet is public and has outbound internet access.
3. Gather the external secret references required for hosted-payment and application runtime configuration.
4. Launch `infra/aws/cloudformation/payslip4all-web.yaml` with the required parameters.

These actions are the complete manual pre-launch workflow for this feature.

## Post-launch verification

1. Wait for the stack to complete.
2. Open the `ApplicationUrl` output.
3. Confirm the instance responds on `/health`.
4. Start an SSM session using the `SsmStartSessionCommand` output.
5. Confirm the app starts with `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, and `DYNAMODB_TABLE_PREFIX`.

When inspecting the instance over SSM:

- `/etc/payslip4all/payslip4all.env` is written with mode `0600`, so read it with `sudo`.
- The systemd unit lives at `/etc/systemd/system/payslip4all.service`.
- Bootstrap output is appended to `/var/log/payslip4all-bootstrap.log`; if the unit file is missing, also inspect `/var/log/cloud-init-output.log`.

## Cost notes

This deployment is optimized for lower cost than the previous ALB-based version.

- The template defaults to a small EC2 instance type to use as much of the free tier as practical.
- DynamoDB uses the app's on-demand table model, which is cost-efficient for low traffic.
- The template no longer creates an Application Load Balancer or Route 53 record.
- The main recurring costs are the EC2 instance, Elastic IP while attached, any EBS storage, and any DynamoDB traffic or backup storage above the free tier.

## Health checks and startup validation

- Stack creation completes once AWS creates the instance resources; application bootstrap continues on the instance after launch.
- The web app enables request logging so direct instance traffic is visible in structured application logs.
- Startup fails fast if the DynamoDB configuration is incomplete at runtime.
- The preferred hosted AWS path uses the IAM instance profile, not static AWS credentials.

## Replaceable compute behaviour

The CloudFormation stack keeps application data in DynamoDB and keeps the public endpoint stable through the Elastic IP. Updating the stack and replacing or recreating the EC2 instance should therefore keep application data available and restore the same public endpoint once the replacement instance boots successfully and the Elastic IP is attached.

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

1. Browse to the `ApplicationUrl` output.
2. Confirm `/health` returns a healthy response through the Elastic IP.
3. Confirm the EC2 instance is tagged with a payslip4all-derived name.
4. Confirm SSM Session Manager can open a shell on the instance.
5. Confirm the app starts with `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION`, and `DYNAMODB_TABLE_PREFIX`.
6. Confirm the hosted AWS environment reports point-in-time recovery enabled for the Payslip4All tables.
