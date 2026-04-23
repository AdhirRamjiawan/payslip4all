# Quickstart: AWS Secrets-Sourced Custom Configuration

## Purpose

Validate that Payslip4All can resolve its covered custom configuration from AWS Secrets Manager in deployment environments while preserving the documented precedence `environment variables > AWS-secret-backed config > checked-in appsettings`.

## Prerequisites

- A published Payslip4All deployment artifact
- Access to the hosted AWS deployment path under `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/`
- An AWS Secrets Manager secret for **custom app configuration** (separate from the TLS certificate secret)
- IAM permission for the deployment host to call `secretsmanager:GetSecretValue` on that secret
- Knowledge of the covered setting catalog in `/Users/adhirramjiawan/projects/payslip4all/specs/014-aws-secrets-config/contracts/aws-secrets-configuration-contract.md`

## Scenario 1: Prepare the custom-config secret

1. Create or update a Secrets Manager secret whose JSON payload uses flat ASP.NET Core configuration keys.
2. Include only covered keys that you want to source from AWS Secrets Manager.
3. Keep TLS certificate material in its existing dedicated certificate secret; do not mix TLS keys into the custom app-config secret.
4. If you need a local or CI override path, set `PAYSLIP4ALL_AWS_SECRETS_CONFIG_PATH` to point the app at an alternate rendered JSON file.

### Example payload

```json
{
  "HostedPayments:PayFast:MerchantId": "10047421",
  "HostedPayments:PayFast:MerchantKey": "merchant-key",
  "HostedPayments:PayFast:Passphrase": "example-passphrase",
  "HostedPayments:PayFast:PublicNotifyUrl": "https://payslip4all.co.za/api/payments/payfast/notify",
  "HostedPayments:PayFast:UseSandbox": "false",
  "Auth:Cookie:ExpireDays": "30"
}
```

## Scenario 2: Wire the secret into the AWS deployment path

1. Supply the planned app-config secret reference parameter when launching or updating `/Users/adhirramjiawan/projects/payslip4all/infra/aws/cloudformation/payslip4all-web.yaml`.
2. Confirm the EC2 instance profile is allowed to read that secret.
3. Confirm bootstrap renders the intermediate secret-backed config artifact and leaves the service able to start.

## Scenario 3: Verify secrets-only resolution for covered keys

1. Remove duplicate values for the chosen covered keys from deployment env overrides where practical.
2. Start or update the deployment.
3. Confirm startup succeeds with the relevant values coming from the AWS-secret-backed source.
4. Exercise the affected features (for example hosted payments or the selected persistence path) and confirm behavior matches equivalent non-secret configuration.

## Scenario 4: Verify mixed-source precedence

1. Put one covered key in the AWS secret and the same key in an environment override.
2. Leave a different covered key only in the AWS secret.
3. Leave a third covered key only in checked-in appsettings.
4. Start the application and confirm:
   - the environment override wins over the AWS secret,
   - the AWS secret wins over checked-in appsettings,
   - appsettings still provides values when neither higher-priority source is present.

## Scenario 5: Verify unchanged legacy deployments

1. Deploy without the app-config secret reference.
2. Keep the current appsettings/env setup unchanged.
3. Confirm the application starts successfully with no new mandatory deployment input.
4. Re-run smoke tests for the same affected features and confirm user-visible behavior is unchanged.

## Scenario 6: Verify safe failure on missing or unreadable secrets

1. Configure a deployment with an invalid or unauthorized app-config secret reference.
2. Start the deployment.
3. Confirm bootstrap or startup fails before the unsafe feature becomes available.
4. Confirm operator diagnostics identify the failing source/key group without printing secret contents or AWS credential values.

## Scenario 7: Verify safe failure on malformed or incomplete secret-backed values

1. Put an invalid value into a covered key (for example a non-integer `Auth:Cookie:ExpireDays` or an invalid PayFast notify URL).
2. Start the application or trigger the affected flow.
3. Confirm the application rejects the configuration with an actionable operator-facing message.
4. Confirm user-facing output remains generic and secret-safe.

## Validation Outcome

The feature is ready for `speckit.tasks` and implementation when all seven scenarios above are supported by repository-owned deployment assets, startup validation, and tests without introducing undocumented manual workarounds.
