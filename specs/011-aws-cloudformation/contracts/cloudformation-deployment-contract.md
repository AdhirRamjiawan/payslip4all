# Contract: CloudFormation Deployment

## Purpose

Define the operator-facing contract for the AWS CloudFormation assets that deploy Payslip4All on EC2 with DynamoDB persistence and a public `payslip.co.za` entry point.

## Audience

- Operators deploying Payslip4All to AWS
- Maintainers reviewing infrastructure changes

## Scope

This contract covers the CloudFormation template, its bootstrap inputs, required outputs, and recovery expectations. It does not define new runtime business behaviour inside Payslip4All.

## Required Template Inputs

The CloudFormation assets MUST accept or document operator-supplied values for:

| Input | Purpose |
|-------|---------|
| `EnvironmentName` | Distinguishes environments such as production or staging |
| `DomainName` | Public application hostname, expected to be `payslip.co.za` for the target environment |
| `HostedZoneId` or equivalent DNS prerequisite | Allows the public hostname to resolve to the load balancer |
| `CertificateArn` | Enables HTTPS on the public entry point |
| `InstanceType` | Allows low-cost default compute with explicit override when needed |
| `ArtifactSource` | Identifies what application build the instance should run |
| `AllowedSshCidr` | Restricts operator shell access |
| `DynamoDbRegion` | Satisfies the runtime requirement for the DynamoDB provider |
| `DynamoDbTablePrefix` | Namespaces the application's table set |
| Secret references for hosted-payment and app configuration | Supplies sensitive runtime values without hardcoding them in the template |

## Required Infrastructure Behaviour

The CloudFormation assets MUST:

1. create one EC2-based Payslip4All web host,
2. create an internet-facing load-balanced entry point for `payslip.co.za`,
3. enforce secure HTTPS access and redirect insecure HTTP traffic,
4. route application traffic to the EC2 instance only when health checks pass,
5. configure the app to run with `PERSISTENCE_PROVIDER=dynamodb`,
6. provide `DYNAMODB_REGION` and `DYNAMODB_TABLE_PREFIX` to the running app,
7. prefer IAM-based AWS access from the instance over embedded static AWS credentials,
8. identify the EC2 instance with payslip.co.za-derived metadata,
9. avoid provisioning unnecessary paid services when a lower-cost supported alternative exists,
10. enable automated DynamoDB recovery protection suitable for regular backups.

## Required Outputs

The CloudFormation assets MUST expose operator-usable outputs for:

- the stack's public application URL,
- the EC2 instance identifier,
- the load balancer identifier,
- the security-group identifiers needed for operator troubleshooting,
- the secret/reference locations required for subsequent updates,
- the recovery configuration reference or confirmation that DynamoDB protection is enabled.

## Required Operator-Visible Signals

The deployment MUST make these signals available to the operator:

1. stack outputs that expose the public application URL, instance identifier, load balancer identifier, and backup protection mode,
2. ALB target health for the registered EC2 instance,
3. a health endpoint (`/health`) that allows first-pass application verification,
4. deployment guidance that tells the operator how to inspect those signals together.

## Security Rules

The implementation MUST:

- keep reusable secrets out of version-controlled plaintext files,
- restrict direct instance access to explicitly approved operator ranges,
- avoid exposing the application instance directly as the public production endpoint,
- use the AWS SDK credential chain or AWS-managed secret references where possible,
- preserve HTTPS as the default user path.

The implementation MUST NOT:

- commit reusable hosted-payment or runtime secrets into the CloudFormation template,
- commit reusable hosted-payment or runtime secrets into the bootstrap script,
- document plaintext reusable secret examples in the deployment guide.

## Backup and Restore Rules

The deployment MUST:

- provide automated DynamoDB recoverability without requiring manual daily backup jobs,
- preserve the ability to restore into a non-live target for safe recovery testing,
- document the operator steps required to perform a restore and validate the result,
- keep restore behaviour compatible with the repository's application-owned table namespace.

## Manual Launch Workflow Limit

The deployment documentation MUST keep the manual pre-launch workflow to no more than these five operator actions:

1. publish the application artifact,
2. confirm ACM certificate issuance,
3. confirm Route 53 authority for `payslip.co.za`,
4. gather external secret references,
5. launch the CloudFormation stack with parameters.

## Acceptance Conditions

This contract is satisfied when:

1. An operator can deploy Payslip4All to AWS through one CloudFormation-driven workflow.
2. End users reach the app at `https://payslip.co.za` rather than through the EC2 instance directly.
3. The running app boots successfully using the existing DynamoDB provider path and environment variables.
4. Replacing the EC2 instance does not discard DynamoDB-backed application data.
5. The deployment provides an operator-verifiable automated DynamoDB recovery path.
6. The operator can verify deployment success using the required signal set without consulting undocumented AWS steps.
7. The deployment assets and documentation do not embed reusable plaintext secrets.
