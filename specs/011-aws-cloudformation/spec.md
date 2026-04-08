# Feature Specification: AWS CloudFormation Deployment

**Feature Branch**: `[011-aws-cloudformation]`  
**Created**: 2026-04-06  
**Status**: Draft  
**Input**: User description: "create aws cloud formation template for this web application. Use an ec2 instance for the web app. Use dynamodb for the persistence. Use the domain name payslip4all.co.za for the load balancer and ec2 instance. Try to use as much of the free tier as you can. Ensure dynamodb is regularly backed up"

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

- **Domain**: No new business rules are introduced; the deployment must preserve existing Payslip4All behaviour.
- **Application**: Any deployment-specific configuration exposed to the app must remain environment-driven and not change core use-case behaviour.
- **Infrastructure**: This feature provisions hosting, networking, DNS, persistence, observability, and backup capabilities for a single deployed environment.
- **Web**: The web application must be reachable through the public payslip4all.co.za entry point without requiring end users to know the instance address.
- **TDD Alignment**: Each functional requirement below is expressed as observable deployment behaviour so implementation can be driven by failing infrastructure, configuration, and startup acceptance tests first.

### User Story 1 - Deploy a working environment (Priority: P1)

As an operator, I want a single deployment template that stands up the web application with its required AWS resources so I can launch a working environment consistently without building infrastructure by hand.

**Why this priority**: A reliable first deployment is the minimum value of this feature. Without it, the rest of the hosting, domain, and backup requirements cannot be delivered.

**Independent Test**: Launch the stack in a new AWS account or environment with the required inputs and confirm the application starts successfully with persistence configured and no manual creation of core infrastructure resources.

**Acceptance Scenarios**:

1. **Given** an operator has the required AWS account inputs, **When** they deploy the template, **Then** the system provisions the web hosting environment and persistence layer needed for one working Payslip4All instance without hand-creating core AWS resources.
2. **Given** the deployment completes successfully, **When** the operator replaces the web server instance through the same template, **Then** the shared infrastructure remains intact and application data is preserved.

---

### User Story 2 - Publish the application on payslip4all.co.za (Priority: P2)

As an operator, I want the deployed application exposed through payslip4all.co.za with consistent environment naming so I can direct users to a stable public address and identify the corresponding compute resource easily.

**Why this priority**: Public reachability and clear operational naming are essential for going live and supporting the environment after deployment.

**Independent Test**: Complete a deployment with domain inputs, then confirm that users can browse to payslip4all.co.za successfully and operators can identify the associated server using payslip4all.co.za-based naming metadata.

**Acceptance Scenarios**:

1. **Given** the operator supplies the required domain prerequisites, **When** the deployment finishes, **Then** incoming web traffic resolves through payslip4all.co.za to the public application entry point.
2. **Given** an operator is reviewing the running environment, **When** they inspect the deployed compute resource, **Then** its name or identifying metadata clearly associates it with payslip4all.co.za.

---

### User Story 3 - Protect DynamoDB data with automated recovery (Priority: P3)

As an operator, I want the DynamoDB-backed persistence layer protected by automated backups so I can recover application data after an operational failure without rebuilding the entire environment.

**Why this priority**: Persistent business data must remain recoverable even in a low-cost deployment, and this protection is explicitly required for the production environment.

**Independent Test**: Deploy the stack, create representative application data, verify automated backup protection is enabled, and perform a restore exercise that recovers the saved data to a recoverable target.

**Acceptance Scenarios**:

1. **Given** the environment is running, **When** data is written to the persistence layer, **Then** automated backup protection covers that data without requiring manual backup steps after deployment.
2. **Given** the operator needs to recover data, **When** they perform a restore using the defined backup mechanism, **Then** the data can be recovered without rebuilding the public entry point or recreating the application manually.

---

### Edge Cases

- What happens when the operator cannot validate ownership of payslip4all.co.za or provide the required domain prerequisites before deployment?
- How does the deployment behave when a chosen region cannot satisfy the lowest-cost default capacity or free-tier-eligible resource assumptions?
- What happens when the web server instance becomes unhealthy while the persistence layer remains healthy?
- How is restore handled so recovered DynamoDB data does not overwrite the live environment unintentionally during an operator-led recovery exercise?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a single AWS deployment template that provisions all mandatory infrastructure required to run one Payslip4All web environment.
- **FR-002**: The deployment MUST host the web application on one EC2 instance behind a public load-balanced entry point.
- **FR-003**: The deployment MUST use DynamoDB as the persistence store for the application environment created by this feature.
- **FR-004**: The deployment MUST expose the application to end users through payslip4all.co.za as the primary public address.
- **FR-005**: The deployment MUST apply payslip4all.co.za-based naming or tags to the EC2 instance so operators can identify the instance that serves that environment.
- **FR-006**: The deployment MUST externalize environment-specific inputs, including domain prerequisites, secret references, operator access allowlists, and environment naming values, so they are not hardcoded in the template.
- **FR-007**: The public entry point MUST support secure user access and redirect non-secure traffic to the secure endpoint.
- **FR-008**: The load-balanced entry point MUST use health-based routing so unhealthy web instances do not continue receiving end-user traffic.
- **FR-009**: The deployment MUST include operator-visible health and deployment signals, including deployment outputs that identify the running environment, target-health visibility for the active web instance, and a health endpoint that confirms public application responsiveness.
- **FR-010**: The default deployment footprint MUST minimize ongoing cost by preferring free-tier-eligible or lowest-cost practical defaults unless a higher-cost option is required to satisfy another functional requirement.
- **FR-011**: The deployment MUST allow the EC2 instance to be replaced or recreated through the same template without losing DynamoDB data or requiring recreation of shared infrastructure such as routing and persistence resources.
- **FR-012**: The DynamoDB persistence layer MUST have automated backup protection enabled with recoverability from at least one recovery point per day.
- **FR-013**: The feature MUST document the operator steps and constraints for restoring DynamoDB data from backups.
- **FR-014**: The feature MUST document any cost-related exceptions where the domain, load balancing, or backup requirements cannot remain fully within AWS free-tier allowances.

### Key Entities *(include if feature involves data)*

- **Deployment Stack**: The complete infrastructure definition for one Payslip4All environment, including hosting, networking, DNS integration, persistence, observability, and recovery settings.
- **Public Entry Point**: The user-facing endpoint for payslip4all.co.za that receives web traffic, enforces secure access, and routes requests to the running application instance.
- **Application Instance**: The single EC2-hosted web server identified with payslip4all.co.za-based metadata and treated as replaceable compute.
- **Persistence Store**: The DynamoDB-backed application data boundary whose lifecycle is independent from the application instance.
- **Backup Protection Policy**: The automated recovery configuration that defines how DynamoDB data is protected and restored.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An operator can launch a working environment from the provided template in 45 minutes or less after required AWS account, domain, and secret prerequisites are ready.
- **SC-002**: Users can reach the deployed application through payslip4all.co.za and receive the first complete page load within 10 seconds during a standard smoke test.
- **SC-003**: Replacing the web server instance results in no loss of DynamoDB-backed application data for 100% of data created before the replacement event.
- **SC-004**: Operators can complete a DynamoDB restore exercise from automated backups within 60 minutes without rebuilding the public entry point.
- **SC-005**: The documented operator workflow from prepared prerequisites to submitting the deployment requires no more than five manual actions.

## Assumptions

- The public user-facing address is payslip4all.co.za, while the EC2 instance uses payslip4all.co.za-derived naming metadata for identification rather than a second public DNS endpoint on the same apex domain.
- The deployment targets a single-environment, single-instance footprint optimized for low cost rather than high availability across multiple compute instances.
- Required AWS account prerequisites such as domain ownership, certificate validation, and secret values are available before template execution begins.
- Daily automated recoverability is an acceptable minimum interpretation of "regularly backed up" for DynamoDB in this feature.
- The five manual operator actions counted for SC-005 are publishing the deployable artifact, confirming certificate readiness, confirming DNS authority, gathering external secret references, and submitting the deployment.
