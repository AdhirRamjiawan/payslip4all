# Quickstart: AWS Secrets Scope Refinement

## Purpose

Validate that a feature 014 deployment can migrate to the refined feature 015 AWS Secrets scope without losing support for eligible secret-backed settings.

## Prerequisites

- A published Payslip4All deployment artifact
- Access to `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/`
- Access to the current AWS app-config secret used by the deployment, if any
- Knowledge of the refined contract in `/Users/adhirramjiawan/projects/payslip4all/specs/015-refine-aws-secrets-scope/contracts/aws-secrets-refined-scope-contract.md`

## Scenario 1: Audit an existing feature 014 payload

1. Open the current app-config secret payload.
2. Check whether it contains any excluded keys:
   - `DYNAMODB_REGION`
   - `DYNAMODB_ENDPOINT`
   - `DYNAMODB_TABLE_PREFIX`
   - `DYNAMODB_ENABLE_PITR`
   - `AWS_ACCESS_KEY_ID`
   - `AWS_SECRET_ACCESS_KEY`
3. Record which excluded keys must move out of the AWS app-config artifact.
4. Leave supported keys in place if they should remain secret-backed.

## Scenario 2: Build the refined compliant payload

1. Remove all excluded keys from the app-config secret payload.
2. Keep only eligible settings that the application owns.
3. Save the payload as flat ASP.NET Core configuration keys.

### Example refined payload

```json
{
  "PERSISTENCE_PROVIDER": "dynamodb",
  "Auth:Cookie:ExpireDays": "30",
  "HostedPayments:PayFast:MerchantId": "10047421",
  "HostedPayments:PayFast:MerchantKey": "merchant-key",
  "HostedPayments:PayFast:Passphrase": "example-passphrase",
  "HostedPayments:PayFast:PublicNotifyUrl": "https://payslip4all.co.za/api/payments/payfast/notify",
  "HostedPayments:PayFast:UseSandbox": "false"
}
```

## Scenario 3: Supply excluded runtime inputs outside the payload

1. Provide DynamoDB runtime values through deployment/runtime environment variables.
2. Prefer IAM instance-profile / AWS SDK default credential-chain support for hosted AWS credentials.
3. If explicit credentials are required for a non-hosted path, supply both `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` outside the AWS app-config artifact.
4. Keep TLS certificate material in its separate certificate secret.

## Scenario 4: Verify eligible settings still resolve from AWS Secrets

1. Deploy or restart the app with the refined payload.
2. Confirm startup succeeds.
3. Exercise the settings that remain eligible:
   - auth cookie expiry behavior
   - hosted PayFast configuration
   - any relational/provider-selection values still intentionally sourced from the app-config secret
4. Confirm the app behaves the same as before for those supported settings.

## Scenario 5: Verify mixed-source precedence

1. Put one eligible key only in the AWS app-config secret.
2. Put another eligible key in both the AWS app-config secret and an environment override.
3. Leave a third eligible key only in checked-in appsettings.
4. Start the app and confirm:
   - the environment override wins,
   - the AWS app-config secret outranks checked-in appsettings,
   - appsettings still works when higher-priority sources are absent.

## Scenario 6: Verify scope violations fail safely

1. Reintroduce one excluded key such as `DYNAMODB_REGION` into the app-config secret.
2. Start the app.
3. Confirm startup fails before the deployment is treated as valid.
4. Confirm the diagnostic names the offending key or group and tells the operator to move it out of the app-config secret.
5. Confirm the diagnostic does not reveal secret values or credential contents.

## Scenario 7: Verify unchanged compliant deployments

1. Use a deployment whose app-config secret already contains only eligible keys, or no app-config secret at all.
2. Deploy without changing the supported keys.
3. Confirm startup and affected features behave the same as before.
4. Confirm no duplicate copies of eligible settings are required in other sources.

## Validation Outcome

The refinement is ready for `speckit.tasks` when:

- excluded DynamoDB runtime and AWS credential keys are rejected from the AWS app-config artifact,
- eligible settings still resolve with the documented precedence,
- operators have a clear migration path from feature 014,
- diagnostics remain actionable and secret-safe.
