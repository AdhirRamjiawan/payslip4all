# Contract: AWS Secrets-Sourced Configuration

## Purpose

Define the deployment-facing contract for sourcing Payslip4All custom configuration from AWS Secrets Manager.

## Contract Summary

- **Secret reference**: the hosted AWS deployment path provides one optional custom app-config secret reference in addition to the existing TLS certificate secret.
- **Secret payload shape**: a flat JSON object whose keys are standard ASP.NET Core configuration paths.
- **Resolution order**: `environment variables > rendered AWS-secret config > checked-in appsettings > code defaults`.
- **Failure policy**: unreadable secret references fail bootstrap/startup; malformed or incomplete resolved values fail safely before the affected feature becomes available.

## Secret Payload Format

The secret payload MUST be a JSON object with flat keys:

```json
{
  "PERSISTENCE_PROVIDER": "dynamodb",
  "DYNAMODB_REGION": "af-south-1",
  "DYNAMODB_TABLE_PREFIX": "payslip4all",
  "Auth:Cookie:ExpireDays": "30",
  "HostedPayments:PayFast:MerchantId": "10047421",
  "HostedPayments:PayFast:MerchantKey": "merchant-key",
  "HostedPayments:PayFast:PublicNotifyUrl": "https://payslip4all.co.za/api/payments/payfast/notify"
}
```

## Covered Keys

### Persistence

- `PERSISTENCE_PROVIDER`
- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:MySqlConnection`
- `DYNAMODB_REGION`
- `DYNAMODB_ENDPOINT`
- `DYNAMODB_TABLE_PREFIX`
- `DYNAMODB_ENABLE_PITR`
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`

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

## Explicitly Excluded Keys

- `AllowedHosts`
- baseline `Logging:*`
- `Serilog:*`
- TLS certificate material (`fullchainPem`, `privkeyPem`) handled by the separate certificate secret
- dev/test-only fake hosted-payment settings

## Materialization Contract

1. Bootstrap fetches the optional custom app-config secret from AWS Secrets Manager.
2. Bootstrap renders the payload into a deployment-owned JSON configuration artifact at `/etc/payslip4all/app-config.secrets.json`.
3. Startup composes configuration in this order:
   1. checked-in appsettings
   2. rendered AWS-secret artifact
   3. environment variables
4. Environment variables therefore remain the emergency override mechanism.

## Validation Contract

### Startup-Critical Groups

These groups MUST block startup when selected/required values are unreadable, missing, or invalid after source resolution:

- persistence-provider selection and its required supporting keys
- relational connection strings for the selected provider
- DynamoDB runtime keys when `PERSISTENCE_PROVIDER=dynamodb`

### Feature-Conditional Groups

These groups MUST fail safely before the affected feature runs, while preserving secret-safe diagnostics:

- `HostedPayments:PayFast:*` when hosted PayFast flows are invoked

## Diagnostics Contract

Operator diagnostics MUST identify:

- which covered key or key group failed,
- whether the failure occurred while reading the AWS secret reference or while validating the resolved config,
- whether the failure kind was missing, unreadable, incomplete, or invalid.

Diagnostics MUST NOT expose:

- secret values,
- AWS access key material,
- TLS private key material,
- user-facing exception details that reveal sensitive configuration contents.
