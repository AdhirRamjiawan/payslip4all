# Research: AWS CloudFormation Deployment

## Decision 1: Use a single EC2 instance behind an internet-facing ALB

- **Decision**: Deploy Payslip4All on one Linux EC2 instance and place it behind an internet-facing Application Load Balancer.
- **Rationale**: The feature explicitly requires both an EC2-hosted web app and a load-balanced public entry point. A single instance keeps compute cost low while the ALB satisfies HTTPS termination, health checks, and stable routing through `payslip4all.co.za`.
- **Alternatives considered**:
  - **Expose EC2 directly with an Elastic IP**: Rejected because it does not satisfy the load-balancer requirement or health-based routing requirement.
  - **Use an Auto Scaling group with multiple instances**: Rejected because it adds cost and operational complexity beyond the feature's low-cost single-environment scope.

## Decision 2: Keep the EC2 instance in a public subnet and restrict inbound traffic by security group

- **Decision**: Place the EC2 instance in a public subnet, allow application traffic only from the ALB security group, and allow SSH only from an operator-supplied CIDR.
- **Rationale**: This avoids a NAT gateway and its recurring cost while still allowing the instance to reach AWS APIs and deployment artifact sources. The security-group design preserves a low-cost footprint without opening the app directly to the internet.
- **Alternatives considered**:
  - **Private subnet plus NAT gateway**: Rejected because the NAT gateway materially increases monthly cost in a feature that is explicitly cost-sensitive.
  - **Private subnet plus multiple VPC endpoints**: Rejected because it adds design and implementation complexity that is unnecessary for the single-instance scope.

## Decision 3: Point `payslip4all.co.za` at the ALB and use payslip4all.co.za-derived tags for the EC2 instance

- **Decision**: Route the public apex domain `payslip4all.co.za` to the ALB and identify the EC2 instance with a payslip4all.co.za-derived name or tag such as `payslip4all-co-za-web-prod`.
- **Rationale**: One public DNS name cannot safely represent both the load balancer and the instance at the same time. Using the apex domain only for the public entry point keeps DNS valid while still giving operators a clear way to recognize the backing instance.
- **Alternatives considered**:
  - **Assign `payslip4all.co.za` directly to the EC2 instance as well**: Rejected because it conflicts with the ALB requirement and creates ambiguous routing.
  - **Create a second public name for the instance**: Rejected because the spec does not ask for a second public endpoint and that would weaken the single-entry-point design.

## Decision 4: Use an ACM certificate and Route 53 alias records for the public domain

- **Decision**: Use an ACM public certificate in the same AWS region as the ALB and create a Route 53 alias record from `payslip4all.co.za` to the ALB.
- **Rationale**: ACM certificates integrate directly with ALB listeners and do not add certificate-management cost. Route 53 aliasing provides the cleanest supported way to serve an apex domain through an ALB.
- **Alternatives considered**:
  - **Terminate TLS on the EC2 instance only**: Rejected because HTTPS redirection and load-balancer health checks are simpler and safer when TLS terminates at the ALB.
  - **Use an external DNS provider with manual records**: Rejected because the feature is specifically about an AWS-managed deployment template.

## Decision 5: Use the existing application-owned DynamoDB provisioning path

- **Decision**: Configure the stack to run Payslip4All with `PERSISTENCE_PROVIDER=dynamodb` and let the existing `DynamoDbTableProvisioner` create the required tables at startup with on-demand billing.
- **Rationale**: The repository already has a full DynamoDB provider path and startup provisioning service. Reusing it avoids duplicating table definitions and keeps CloudFormation focused on the infrastructure boundary rather than application-owned schema details.
- **Alternatives considered**:
  - **Declare every DynamoDB table in CloudFormation**: Rejected because it would duplicate table structure already encoded in application startup logic and increase maintenance drift.
  - **Keep SQLite or MySQL for the first AWS deployment**: Rejected because the feature explicitly requires DynamoDB persistence.

## Decision 6: Prefer an IAM instance profile over static AWS credentials

- **Decision**: Grant the EC2 instance an IAM role with the DynamoDB, logging, and secret-read permissions it needs, and rely on the AWS SDK credential chain instead of storing AWS access keys in the template.
- **Rationale**: `Program.cs` and the DynamoDB client path already support the AWS SDK default credential chain when explicit keys are absent. IAM roles are safer, simpler to rotate, and better aligned with the spec's no-hardcoded-secrets requirement.
- **Alternatives considered**:
  - **Pass `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` into the instance**: Rejected because it creates unnecessary credential-management risk.
  - **Use long-lived shared credentials files on the instance**: Rejected because it is harder to automate safely through CloudFormation.

## Decision 7: Enable DynamoDB point-in-time recovery on all application tables

- **Decision**: Turn on DynamoDB point-in-time recovery for the Payslip4All table set and document operator-led restore steps that restore into new tables.
- **Rationale**: PITR satisfies the requirement for regular backups with minimal daily operational overhead and supports safer recovery drills because restores create new tables instead of overwriting live ones.
- **Alternatives considered**:
  - **Use AWS Backup with scheduled backup plans only**: Rejected as the default because it adds cost and administration overhead across every table in this low-cost deployment.
  - **Rely on ad-hoc manual on-demand backups**: Rejected because it does not satisfy the "regularly backed up" requirement.

## Decision 8: Be explicit that the deployment is low-cost optimized, not zero-cost

- **Decision**: Design the template to minimize cost with a single instance, on-demand DynamoDB billing, and no NAT gateway, while documenting that ALB, Route 53, and backup features are expected recurring costs outside the free tier.
- **Rationale**: The spec asks to use as much of the free tier as possible, but the required public-domain and load-balancer features inherently introduce some paid AWS services. Making that tradeoff explicit keeps the plan honest and implementation-safe.
- **Alternatives considered**:
  - **Claim the whole deployment can stay inside the free tier**: Rejected because the mandatory edge services make that unrealistic.
  - **Drop the load balancer to chase lower cost**: Rejected because it would violate the feature requirements.

## Decision 9: Treat deployment artifacts as a repository-owned infrastructure surface

- **Decision**: Store the CloudFormation template and its bootstrap script under `infra/aws/cloudformation/` and validate them through the repository's existing .NET test suites.
- **Rationale**: The infrastructure assets should evolve with the application code they configure, and keeping validation in the existing test projects avoids introducing a new testing stack during this feature.
- **Alternatives considered**:
  - **Keep infrastructure files outside the repository**: Rejected because the feature requires the deployment template to ship with the application.
  - **Introduce a separate infrastructure test framework immediately**: Rejected because the repo already has established xUnit-based validation patterns.

## Decision 10: Make the operator-visible signal set explicit and minimal

- **Decision**: Treat the minimum required operator-visible signals as CloudFormation outputs (`ApplicationUrl`, `InstanceId`, `LoadBalancerArn`, security-group identifiers, backup mode), ALB target health, and the public `/health` endpoint documented in the deployment guide.
- **Rationale**: The analysis pass showed that “operator-visible health and deployment signals” was otherwise open to interpretation. Fixing the signal set keeps the requirement testable without expanding the feature into a broader observability platform project.
- **Alternatives considered**:
  - **Leave signals loosely defined**: Rejected because it makes FR-009 and related tests ambiguous.
  - **Require full alarm dashboards and a dedicated monitoring stack**: Rejected because it would expand scope beyond the requested deployment feature.

## Decision 11: Enforce external secret handling as a first-class deployment rule

- **Decision**: The template, bootstrap flow, and deployment docs must all avoid embedding reusable secrets directly; hosted-payment and app-specific secrets must come from operator-supplied external references.
- **Rationale**: Secret handling was already implied by the spec, but tightening it into an explicit design rule makes FR-006 testable and keeps the deployment guidance aligned with the constitution’s no-secrets policy.
- **Alternatives considered**:
  - **Allow plaintext example secrets in the bootstrap flow**: Rejected because it normalizes unsafe operational practices.
  - **Rely only on prose warnings without contract coverage**: Rejected because it leaves a high-risk requirement under-tested.

## Decision 12: Constrain the operator launch workflow to five manual actions

- **Decision**: The deployment design treats the manual pre-launch workflow as exactly five operator actions: publish artifact, confirm ACM issuance, confirm Route 53 authority, gather secret references, and launch the stack with parameters.
- **Rationale**: Success criterion SC-005 needs a concrete workflow boundary to be measurable. Enumerating the five actions keeps the deployment guide honest and lets tasks and tests verify the limit directly.
- **Alternatives considered**:
  - **Count manual actions informally**: Rejected because it makes SC-005 difficult to validate.
  - **Ignore the action count and optimize only for technical correctness**: Rejected because SC-005 is a user-facing outcome, not an optional note.
