# Data Model: AWS CloudFormation Deployment

## Overview

This feature does not introduce new business-domain persistence. Its data model describes the operator-facing infrastructure objects, configuration inputs, and recovery policies that the CloudFormation template and bootstrap assets must represent.

## Entities

### 1. Deployment Stack

**Purpose**: The top-level AWS environment definition for one Payslip4All deployment.

**Fields**:
- `stackName` — operator-visible deployment name
- `environmentName` — environment identifier such as `prod` or `staging`
- `domainName` — public hostname served by the stack (`payslip4all.co.za`)
- `region` — AWS region where the resources are launched
- `artifactSource` — deployable application package or machine-image reference
- `status` — current deployment lifecycle state

**Relationships**:
- Has one `Public Entry Point`
- Has one `Application Instance`
- Has one `Runtime Access Profile`
- Has one `DynamoDB Runtime Configuration`
- Has one `Backup Protection Policy`

**Validation Rules**:
- Must target one environment only.
- Must expose one public domain only.
- Must keep the application instance replaceable without coupling stack identity to one machine.

### 2. Public Entry Point

**Purpose**: The public traffic boundary that serves `payslip4all.co.za`.

**Fields**:
- `domainName` — public hostname
- `tlsCertificateReference` — certificate identifier supplied to the stack
- `httpRedirectEnabled` — whether insecure traffic is redirected to secure traffic
- `healthCheckPath` — operator-defined application health path or probe target
- `routingState` — whether the load balancer currently considers the target healthy
- `signalOutputs` — operator-facing values that identify the public stack and health state

**Relationships**:
- Routes traffic to one `Application Instance`
- Belongs to one `Deployment Stack`

**Validation Rules**:
- Must be the only public application entry point for the deployment.
- Must enforce secure access.
- Must stop routing traffic to unhealthy application targets.

### 3. Application Instance

**Purpose**: The single EC2-hosted Payslip4All web server.

**Fields**:
- `instanceType` — operator-selected or default low-cost instance size
- `instanceName` — payslip4all.co.za-derived identification value
- `bootstrapMode` — how the instance receives app artifacts and runtime configuration
- `allowedIngressSources` — permitted upstream traffic sources
- `replaceable` — whether the instance can be recreated without losing persistence

**Relationships**:
- Receives traffic from one `Public Entry Point`
- Uses one `Runtime Access Profile`
- Reads one `DynamoDB Runtime Configuration`

**Validation Rules**:
- Must not be the system of record for application data.
- Must accept application traffic only from the load balancer and approved operator access ranges.
- Must expose enough metadata for operators to identify it as the payslip4all.co.za host.

### 4. Runtime Access Profile

**Purpose**: The AWS permissions boundary used by the EC2 instance at runtime.

**Fields**:
- `profileName` — operator-visible role/profile identifier
- `dynamoDbAccess` — whether table creation and data operations are allowed
- `loggingAccess` — whether the app can publish operational logs and metrics
- `secretReadAccess` — whether the app can read deployment secrets from AWS-managed stores

**Relationships**:
- Belongs to one `Application Instance`
- Supports one `Deployment Stack`

**Validation Rules**:
- Must grant the minimum permissions required to run Payslip4All with DynamoDB.
- Must allow the app to use the AWS SDK credential chain without embedded long-lived credentials.

### 5. External Secret Reference

**Purpose**: The operator-supplied reference used to inject sensitive runtime values without embedding reusable secrets in version-controlled assets.

**Fields**:
- `referenceType` — secret-store mechanism such as AWS-managed secret reference
- `scope` — which runtime settings it provides
- `rotationOwner` — who manages secret value updates
- `bootstrapConsumptionMode` — how the EC2 bootstrap flow reads the referenced secret

**Relationships**:
- Supports one `Deployment Stack`
- Supports one `Runtime Access Profile`

**Validation Rules**:
- Must not include plaintext reusable secret values in source-controlled artifacts.
- Must be optional only when the deployment genuinely has no secret-backed runtime settings to inject.

### 6. DynamoDB Runtime Configuration

**Purpose**: The set of runtime values the app needs in order to use the existing DynamoDB provider path.

**Fields**:
- `persistenceProvider` — must be `dynamodb`
- `region` — runtime DynamoDB region value
- `tablePrefix` — logical namespace for application tables
- `credentialMode` — IAM role or explicit credentials
- `startupValidationState` — whether the app has the required values to boot successfully

**Relationships**:
- Used by one `Application Instance`
- Protected by one `Backup Protection Policy`

**Validation Rules**:
- `persistenceProvider` must always be `dynamodb` for this feature.
- `region` must be supplied.
- Explicit AWS credentials, if used, must be complete pairs and not partial values.

### 7. Backup Protection Policy

**Purpose**: The automated recoverability configuration for the application's DynamoDB tables.

**Fields**:
- `recoveryMode` — continuous or scheduled protection method
- `recoveryWindow` — amount of time data remains restorable
- `restoreTargetStrategy` — how recovered data is materialized
- `operatorRunbookReference` — restore procedure location

**Relationships**:
- Protects one `DynamoDB Runtime Configuration`
- Supports one `Deployment Stack`

**Validation Rules**:
- Must provide automated recoverability without manual daily intervention.
- Must support safe restore exercises that do not overwrite the live tables in place.

### 8. Operator Signal Set

**Purpose**: The minimum deployment and health signals the operator uses to verify launch success.

**Fields**:
- `applicationUrl` — public HTTPS address exposed through stack outputs
- `instanceId` — EC2 runtime identifier
- `loadBalancerReference` — ALB identifier or ARN
- `securityGroupReference` — network identifiers used during troubleshooting
- `healthEndpoint` — public or proxied `/health` probe location
- `targetHealthState` — ALB routing view of instance health
- `backupMode` — backup protection state surfaced to operators

**Relationships**:
- Belongs to one `Deployment Stack`
- Observes one `Public Entry Point`
- Observes one `Application Instance`
- Reflects one `Backup Protection Policy`

**Validation Rules**:
- Must be sufficient to confirm stack identity, application reachability, and recovery mode.
- Must stay minimal enough that the feature does not become a full monitoring-platform project.

### 9. Operator Launch Workflow

**Purpose**: The measurable set of manual steps an operator performs before stack creation succeeds.

**Fields**:
- `artifactPublished` — whether the deployable bundle is ready
- `certificateReady` — whether the ACM certificate is issued
- `dnsAuthorityConfirmed` — whether Route 53 prerequisites are satisfied
- `secretReferencesGathered` — whether external secret references are ready
- `stackLaunchSubmitted` — whether the operator executed the CloudFormation deployment

**Relationships**:
- Belongs to one `Deployment Stack`

**Validation Rules**:
- Must not exceed five manual pre-launch actions.
- Must map directly to the workflow documented in the deployment guide and quickstart.

## State Model

### Deployment Lifecycle

1. `Prepared` — operator has gathered domain, certificate, artifact, secret, and access inputs.
2. `Provisioning` — CloudFormation is creating or updating infrastructure resources.
3. `Bootstrapping` — EC2 is starting Payslip4All with DynamoDB configuration and initial table provisioning.
4. `Healthy` — the ALB routes secure traffic to the instance and the application is reachable at `payslip4all.co.za`.
5. `Recovering` — operator is restoring DynamoDB data from the defined backup mechanism.

**Transitions**:
- `Prepared -> Provisioning`: operator launches the CloudFormation stack with valid inputs.
- `Provisioning -> Bootstrapping`: AWS resources are created and the instance begins startup.
- `Bootstrapping -> Healthy`: the app passes health checks and serves traffic through the ALB.
- `Healthy -> Recovering`: operator initiates a DynamoDB restore workflow.
- `Recovering -> Healthy`: restored data is validated and the environment returns to normal service.
- Any state can regress if DNS, TLS, permissions, or DynamoDB runtime configuration becomes invalid.
