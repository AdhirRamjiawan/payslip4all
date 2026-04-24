# Data Model: AWS Secrets-Sourced Custom Configuration

## Overview

This feature does not add business-domain persistence. Its data model describes the operator-facing configuration catalog, source-resolution metadata, secret-mapping contract, and validation states required to support AWS Secrets Manager as a deployment-time source for Payslip4All custom configuration.

## Entities

### 1. Covered Configuration Setting

**Purpose**: A single repo-owned configuration key that Payslip4All explicitly consumes and that is therefore in scope for AWS-secret sourcing.

**Fields**:
- `key` — canonical ASP.NET Core configuration path (for example `HostedPayments:PayFast:MerchantId`)
- `group` — logical section such as `Persistence`, `AuthCookie`, or `HostedPaymentsPayFast`
- `currentConsumers` — startup location(s) or options class that read the key
- `requiredMode` — `Always`, `ProviderConditional`, `FeatureConditional`, or `Optional`
- `defaultValue` — code/appsettings default, if any
- `allowedSources` — ordered set of supported sources (`AppSettings`, `AwsSecret`, `Environment`)
- `sensitivity` — `Secret`, `Operational`, or `NonSecretConfig`
- `validationRule` — parse/format/completeness rule the resolved value must satisfy

**Relationships**:
- Belongs to one `Configuration Source Set`
- May be represented by one `AWS Secret Mapping Definition`
- May produce zero or more `Configuration Validation Failures`

**Validation Rules**:
- `key` must stay stable across appsettings, secret payload, and env override documentation.
- `allowedSources` must preserve `Environment > AwsSecret > AppSettings`.
- Secret-sensitive values must never appear in diagnostic payloads.

### 2. Configuration Source Set

**Purpose**: The ordered source stack used to resolve a covered setting for a specific deployment.

**Fields**:
- `appSettingsSource` — checked-in JSON inputs (`appsettings.json`, environment-specific appsettings)
- `awsSecretArtifactPath` — optional rendered secrets file path, such as `/etc/payslip4all/app-config.secrets.json`
- `environmentOverrideSource` — standard process/service environment variables
- `resolvedSource` — winning source for a given key after precedence evaluation
- `resolutionMode` — `SecretsDisabled`, `SecretsEnabled`, or `MixedSources`

**Relationships**:
- Resolves many `Covered Configuration Settings`
- Uses zero or one `Materialized Secret Artifact`
- Can emit many `Configuration Validation Failures`

**Validation Rules**:
- `resolvedSource` must always be deterministic.
- `SecretsDisabled` deployments must remain valid when the secret artifact is absent.
- Mixed-source deployments must allow some keys to resolve from secrets and others from env/appsettings without changing feature behavior.

### 3. AWS Secret Mapping Definition

**Purpose**: The documented operator contract that maps a covered setting to a value in AWS Secrets Manager.

**Fields**:
- `cloudFormationParameter` — secret-reference input, planned as the app-config secret ARN parameter/output path
- `secretJsonKey` — flat JSON key stored in the secret payload
- `settingKey` — target `Covered Configuration Setting.key`
- `deploymentScope` — environment or stack scope that uses the mapping
- `requiredWhen` — condition under which the mapped key must be present
- `notes` — operator-facing explanation or migration guidance

**Relationships**:
- Maps one `Covered Configuration Setting`
- Is materialized by one `Materialized Secret Artifact`

**Validation Rules**:
- `secretJsonKey` must equal `settingKey` in the planned contract.
- Only covered settings may appear in the mapping table.
- Mapping documentation must distinguish deployment-owned defaults from operator-supplied overrides.

### 4. Materialized Secret Artifact

**Purpose**: The bootstrap-rendered configuration artifact that lets the app consume AWS-secret-backed settings as a normal configuration source.

**Fields**:
- `path` — runtime location of the rendered artifact
- `format` — JSON object using ASP.NET Core configuration keys
- `owner` — system account/permission boundary that protects the file
- `sourceSecretReference` — originating Secrets Manager ARN or logical reference
- `renderedKeys` — set of covered keys present in the artifact
- `renderStatus` — `NotConfigured`, `Rendered`, or `RenderFailed`

**Relationships**:
- Realizes many `AWS Secret Mapping Definitions`
- Feeds one `Configuration Source Set`

**Validation Rules**:
- The artifact must be optional so non-secret deployments remain unchanged.
- File permissions must prevent casual disclosure on the host.
- The artifact must contain only documented covered keys; TLS certificate material stays in its separate secret path.

### 5. Configuration Validation Failure

**Purpose**: An operator-visible failure state that blocks startup or the affected flow when resolved configuration is unsafe.

**Fields**:
- `settingKey` — affected covered setting or section
- `source` — `AwsSecret`, `Environment`, `AppSettings`, or `BootstrapSecretReference`
- `failureKind` — `Missing`, `Unreadable`, `Invalid`, `Incomplete`, or `UnknownSourceError`
- `blocksStartup` — whether the whole app should refuse to start
- `operatorMessage` — sanitized actionable message
- `userVisibleMessage` — safe generic message, if a runtime flow is blocked after startup

**Relationships**:
- References one `Covered Configuration Setting`
- Arises from one `Configuration Source Set`

**Validation Rules**:
- `operatorMessage` must identify the failing key/group and source category without exposing the secret value.
- `blocksStartup` must be true for startup-critical configuration failures.
- `userVisibleMessage` must remain generic and secret-safe.

## Covered Setting Groups

### Persistence Group

- `PERSISTENCE_PROVIDER`
- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:MySqlConnection`
- `DYNAMODB_REGION`
- `DYNAMODB_ENDPOINT`
- `DYNAMODB_TABLE_PREFIX`
- `DYNAMODB_ENABLE_PITR`
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`

### Auth Cookie Group

- `Auth:Cookie:ExpireDays`

### Hosted Payments Group

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

## State Model

### Secret-Backed Configuration Lifecycle

1. `BaselineLoaded` — checked-in appsettings are loaded.
2. `SecretReferenceResolved` — deployment/bootstrap knows whether an app-config secret reference was supplied.
3. `SecretMaterialized` — the optional AWS secret payload is fetched and rendered into the intermediate artifact.
4. `EffectiveConfigurationBuilt` — appsettings, rendered secret artifact, and env vars are composed in the required order.
5. `Validated` — covered settings pass required-source, completeness, and format checks.
6. `Running` — application starts or the affected feature remains available.
7. `Blocked` — bootstrap or app validation fails safely before unsafe behavior is exposed.

**Transitions**:
- `BaselineLoaded -> SecretReferenceResolved`: bootstrap inspects deployment inputs.
- `SecretReferenceResolved -> SecretMaterialized`: secret fetch succeeds and the artifact is rendered.
- `SecretReferenceResolved -> EffectiveConfigurationBuilt`: no secret reference is configured, so the app continues with baseline sources.
- `SecretMaterialized -> EffectiveConfigurationBuilt`: the rendered secret source is inserted between appsettings and env vars.
- `EffectiveConfigurationBuilt -> Validated`: covered settings are parsed and checked.
- `Validated -> Running`: all startup-critical requirements pass.
- `SecretReferenceResolved -> Blocked`: secret fetch/read fails.
- `EffectiveConfigurationBuilt -> Blocked`: a required setting is missing, incomplete, or invalid after source resolution.
- `Running -> Blocked`: a feature-conditional validation path (such as hosted payments) detects unusable resolved configuration before executing the affected flow.
