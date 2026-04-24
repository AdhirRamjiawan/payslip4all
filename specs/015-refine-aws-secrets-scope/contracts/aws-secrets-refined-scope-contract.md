# Contract: AWS Secrets Refined Scope

## Purpose

Define the deployment-facing contract for the constitution-compliant AWS Secrets-backed app-config artifact after feature 015 narrows the broader feature 014 catalog.

## Contract Summary

- **Secret reference**: the hosted AWS deployment path may still provide one optional custom app-config secret reference.
- **Secret payload shape**: a flat JSON object whose keys are standard ASP.NET Core configuration paths.
- **Resolution order**: `environment variables > rendered AWS-secret config > checked-in appsettings > code defaults`.
- **Refinement rule**: eligible repo-owned app settings remain supported; DynamoDB runtime keys and AWS credential keys are explicitly excluded.
- **Failure policy**: any excluded key present in the rendered app-config artifact blocks startup with actionable but secret-safe diagnostics.

## Supported AWS Secret-Backed Catalog

### Persistence Selection and Relational Settings

- `PERSISTENCE_PROVIDER`
- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:MySqlConnection`

> When `PERSISTENCE_PROVIDER=dynamodb`, the provider-specific DynamoDB runtime and AWS credential inputs listed under **Excluded Catalog** still must come from non-app-config deployment sources.

### Authentication

- `Auth:Cookie:ExpireDays`

### Hosted Payments

- `HostedPayments:PayFast:ProviderKey`
- `HostedPayments:PayFast:UseSandbox`
- `HostedPayments:PayFast:MerchantId`
- `HostedPayments:PayFast:MerchantKey`
- `HostedPayments:PayFast:Passphrase`
- `HostedPayments:PayFast:PublicNotifyUrl`
- `HostedPayments:PayFast:SandboxBaseUrl`
- `HostedPayments:PayFast:LiveBaseUrl`
- `HostedPayments:PayFast:SandboxValidationUrl`
- `HostedPayments:PayFast:LiveValidationUrl`
- `HostedPayments:PayFast:ItemName`

## Excluded Catalog

These keys MUST NOT appear in the AWS Secrets-backed app-config artifact:

### DynamoDB Runtime

- `DYNAMODB_REGION`
- `DYNAMODB_ENDPOINT`
- `DYNAMODB_TABLE_PREFIX`
- `DYNAMODB_ENABLE_PITR`

### AWS Credentials

- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`

## Alternate Sources for Excluded Keys

- **DynamoDB runtime keys**: service environment variables or deployment/bootstrap-managed runtime wiring
- **AWS credentials**: IAM instance profile / AWS SDK default credential chain, or paired environment variables when explicitly required

## Secret Payload Format

The secret payload MUST be a JSON object with flat keys and scalar values:

```json
{
  "PERSISTENCE_PROVIDER": "dynamodb",
  "Auth:Cookie:ExpireDays": "30",
  "HostedPayments:PayFast:MerchantId": "10047421",
  "HostedPayments:PayFast:MerchantKey": "merchant-key",
  "HostedPayments:PayFast:PublicNotifyUrl": "https://payslip4all.co.za/api/payments/payfast/notify"
}
```

## Non-Compliant Example

The following payload is invalid because it contains excluded keys:

```json
{
  "PERSISTENCE_PROVIDER": "dynamodb",
  "DYNAMODB_REGION": "af-south-1",
  "AWS_ACCESS_KEY_ID": "AKIA...",
  "HostedPayments:PayFast:MerchantId": "10047421"
}
```

## Materialization Contract

1. Bootstrap fetches the optional custom app-config secret from AWS Secrets Manager.
2. Bootstrap renders the payload into `/etc/payslip4all/app-config.secrets.json`.
3. Startup parses the artifact and validates it against the supported and excluded catalogs.
4. If the artifact is compliant, eligible keys are inserted between checked-in appsettings and environment variables.
5. If the artifact contains excluded keys, startup stops before those values are consumed.

## Validation Contract

### Scope Validation

- The artifact MUST be a JSON object with flat scalar values.
- The artifact MAY omit eligible keys that continue to come from appsettings or environment variables.
- The artifact MUST NOT contain any excluded key.

### Resolved Value Validation

- Eligible settings MUST keep the same completeness/type/business validation rules used for non-secret sources.
- Hosted-payment settings remain feature-conditional validations.
- Provider and connection-string rules remain startup-critical validations.

## Diagnostics Contract

Operator diagnostics MUST identify:

- the excluded key or excluded group found in the artifact,
- that the AWS Secrets-backed app-config scope is non-compliant,
- the required corrective action (remove the excluded key from the artifact and supply it via an allowed alternate source).

Operator diagnostics MUST NOT identify:

- the secret value,
- AWS credential contents,
- raw secret payload bodies.

## Migration Contract from Feature 014

1. Audit the current feature 014 app-config secret for excluded keys.
2. Remove every excluded DynamoDB runtime and AWS credential key from that payload.
3. Keep supported settings in place when they still need AWS Secrets backing.
4. Supply excluded runtime inputs through environment/bootstrap wiring and prefer IAM/default credential chain for AWS credentials.
5. Re-deploy and verify the app-config artifact now passes scope validation and eligible settings still resolve with the documented precedence.
