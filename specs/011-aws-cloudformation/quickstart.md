# Quickstart: AWS CloudFormation Deployment

## Purpose

Validate that the planned CloudFormation assets are sufficient to launch, verify, and recover a low-cost AWS-hosted Payslip4All environment using EC2, ALB, Route 53, and DynamoDB.

## Prerequisites

- AWS account with permission to create EC2, ALB, Route 53, IAM, CloudWatch, and DynamoDB resources
- Control of the `payslip4all.co.za` DNS zone or the ability to create the required Route 53 records
- An ACM certificate for `payslip4all.co.za` in the same AWS region as the load balancer
- A deployable Payslip4All application artifact
- Hosted-payment and application secrets stored outside source control
- A chosen DynamoDB region and table prefix
- An operator IP range for SSH administration, if SSH is enabled

## Scenario 1: Prepare deployment inputs

1. Publish or stage the deployment artifact referenced by `ArtifactSource`.
2. Confirm the ACM certificate for `payslip4all.co.za` is issued in the target region.
3. Confirm the Route 53 hosted zone is authoritative for `payslip4all.co.za`.
4. Gather the external secret references required by the deployment.
5. Launch the CloudFormation template with the required parameters, including the intended environment name and DynamoDB runtime values.

These five actions define the full manual pre-launch workflow for SC-005.

## Scenario 2: Launch the stack

1. Deploy the CloudFormation template with the prepared parameters.
2. Wait for stack completion.
3. Confirm the deployed resources include:
   - one EC2 application instance,
   - one internet-facing load balancer,
   - DNS wiring for `payslip4all.co.za`,
   - runtime configuration for DynamoDB,
   - automated DynamoDB recovery protection.

## Scenario 3: Verify public access through `payslip4all.co.za`

1. Open `https://payslip4all.co.za`.
2. Confirm the browser reaches Payslip4All through HTTPS.
3. Confirm insecure HTTP is redirected to HTTPS.
4. Confirm the instance is identified in AWS with payslip4all.co.za-derived metadata.

## Scenario 4: Verify operator-visible signals

1. Read the stack outputs.
2. Confirm the outputs expose:
    - the public application URL,
    - the EC2 instance identifier,
    - the load balancer identifier,
    - the instance and load-balancer security-group identifiers,
    - the backup protection mode or restore reference.
3. Inspect ALB target health and confirm the registered instance is healthy.
4. Call `/health` and confirm the application returns a healthy response.

## Scenario 5: Verify DynamoDB-backed startup

1. Inspect the running application's environment configuration.
2. Confirm the app is configured with:
   - `PERSISTENCE_PROVIDER=dynamodb`,
   - `DYNAMODB_REGION`,
   - `DYNAMODB_TABLE_PREFIX`.
3. Confirm the application starts successfully and the existing DynamoDB table provisioner can create or verify its required tables.

## Scenario 6: Verify replaceable compute

1. Replace or recreate the EC2 instance through the deployment workflow.
2. Confirm the public entry point remains the same.
3. Confirm application data remains available after the replacement.

## Scenario 7: Verify backup and restore readiness

1. Confirm automated DynamoDB recovery protection is enabled for the application's tables.
2. Record the documented restore steps.
3. Run a recovery exercise that restores data into a safe target and validate the restored data before any cutover decision.

## Scenario 8: Verify secret externalization

1. Inspect the CloudFormation template, bootstrap script, and deployment guide.
2. Confirm reusable hosted-payment or runtime secrets are referenced externally rather than embedded as plaintext values.
3. Confirm the deployment guide uses secret references and placeholders instead of reusable secret examples.

## Validation Outcome

The feature is ready for implementation when the planned assets and instructions support all eight scenarios above without requiring undocumented AWS setup steps or manual recreation of infrastructure outside the CloudFormation workflow.
