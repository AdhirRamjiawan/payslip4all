# Data Model: AWS Secrets Scope Refinement

## Overview

This feature does not add business-domain persistence. Its data model describes the supported and excluded AWS app-config catalogs, the source-resolution stack, the scope-validation outcome, and the migration guidance needed to refine feature 014 into a constitution-compliant configuration contract.

## Entities

### 1. Eligible Secret-Backed Setting Group

**Purpose**: A repo-owned custom application setting group that may still be supplied through the AWS Secrets-backed app-config artifact.

**Fields**:
- `groupName` — logical group name such as `PersistenceSelection`, `AuthCookie`, or `HostedPaymentsPayFast`
- `keys` — canonical ASP.NET Core configuration keys in the group
- `consumers` — startup locations or options classes that bind the group
- `requiredMode` — `Always`, `ProviderConditional`, `FeatureConditional`, or `Optional`
- `allowedSources` — ordered source set `AppSettings`, `AwsSecretArtifact`, `Environment`
- `validationRule` — parse/completeness/business rule applied after resolution
- `sensitivity` — `Secret`, `Operational`, or `NonSecretConfig`

**Relationships**:
- Resolves through one `Configuration Resolution Stack`
- May appear in one `Rendered Aws Secret Artifact`
- May produce zero or more `Scope Validation Result` entries

**Validation Rules**:
- Keys must preserve `Environment > AwsSecretArtifact > AppSettings`.
- Validation rules must be identical regardless of which allowed source wins.
- Secret-sensitive values must never appear in diagnostics.

### 2. Excluded Setting Group

**Purpose**: A configuration group that must never be consumed from the AWS Secrets-backed app-config artifact.

**Fields**:
- `groupName` — `DynamoDbRuntime` or `AwsCredentials`
- `keys` — excluded keys in the group
- `exclusionReason` — constitution or infrastructure rationale for exclusion
- `allowedRuntimeSources` — accepted non-app-config sources such as environment variables, IAM credential chain, or deployment/bootstrap wiring
- `detectionBehavior` — `BlockArtifact`
- `operatorAction` — required remediation when detected

**Relationships**:
- May be detected in one `Rendered Aws Secret Artifact`
- Must produce one blocking `Scope Validation Result` when present

**Validation Rules**:
- Presence in the AWS app-config artifact always results in a non-compliant outcome.
- Diagnostics may identify keys/groups only; never secret values.

### 3. Rendered Aws Secret Artifact

**Purpose**: The optional JSON artifact rendered at deployment time and loaded by the app as the AWS-secret-backed configuration source.

**Fields**:
- `path` — runtime location, normally `/etc/payslip4all/app-config.secrets.json`
- `format` — flat JSON object keyed by ASP.NET Core configuration paths
- `presentKeys` — all keys materialized in the artifact
- `eligibleKeys` — subset matching the eligible catalog
- `excludedKeys` — subset matching the excluded catalog
- `status` — `Absent`, `Rendered`, `InvalidJson`, or `ScopeViolation`

**Relationships**:
- Feeds one `Configuration Resolution Stack`
- Is evaluated into one `Scope Validation Result`

**Validation Rules**:
- The artifact is optional; absence must not break non-secret deployments.
- Only flat scalar values are allowed.
- Any excluded key changes the artifact result to `ScopeViolation`.

### 4. Configuration Resolution Stack

**Purpose**: The ordered set of sources used to resolve a supported setting.

**Fields**:
- `baselineSource` — checked-in appsettings files
- `awsSecretSource` — optional rendered artifact
- `environmentSource` — process/service environment variables
- `winningSource` — source that supplies the effective value for a key
- `resolutionMode` — `NoSecretArtifact`, `SecretsOnly`, or `MixedSources`

**Relationships**:
- Resolves many `Eligible Secret-Backed Setting Group` keys
- Depends on zero or one `Rendered Aws Secret Artifact`
- Can emit many `Scope Validation Result` entries

**Validation Rules**:
- Winning source must be deterministic.
- Eligible settings must preserve the documented precedence order.
- Excluded settings must never treat the AWS app-config artifact as an allowed source.

### 5. Scope Validation Result

**Purpose**: The operator-visible result of reviewing the AWS app-config artifact and the resolved supported settings.

**Fields**:
- `status` — `Compliant`, `NonCompliant`, or `Incomplete`
- `artifactSource` — `AwsSecretArtifact`, `Environment`, or `AppSettings`
- `affectedGroup` — supported or excluded group being evaluated
- `affectedKeys` — relevant keys or key group
- `failureKind` — `ExcludedKeyPresent`, `Missing`, `Invalid`, or `Incomplete`
- `blocksStartup` — whether startup must stop
- `operatorMessage` — sanitized actionable guidance

**Relationships**:
- References one `Rendered Aws Secret Artifact`
- May reference one `Eligible Secret-Backed Setting Group` or `Excluded Setting Group`

**Validation Rules**:
- `ExcludedKeyPresent` always blocks startup.
- `operatorMessage` must never reveal secret values.
- The result must be specific enough for operators to remediate the payload.

### 6. Migration Guidance

**Purpose**: The documented transition path from the feature 014 catalog to the refined feature 015 scope.

**Fields**:
- `fromVersion` — `014-aws-secrets-config`
- `toVersion` — `015-refine-aws-secrets-scope`
- `keysToRemove` — excluded keys that must leave the app-config artifact
- `keysThatMayRemain` — eligible settings still supported from AWS Secrets
- `replacementSources` — recommended runtime destinations for excluded keys
- `verificationSteps` — operator checks confirming the migration is complete

**Relationships**:
- References both `Eligible Secret-Backed Setting Group` and `Excluded Setting Group`
- Consumes `Scope Validation Result` as proof of compliance

**Validation Rules**:
- Guidance must distinguish supported and excluded catalogs clearly.
- Migration steps must preserve supported secret-backed behavior for eligible settings.

## Catalogs

### Eligible Secret-Backed Keys

- `PERSISTENCE_PROVIDER`
- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:MySqlConnection`
- `Auth:Cookie:ExpireDays`
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

### Excluded Keys

- `DYNAMODB_REGION`
- `DYNAMODB_ENDPOINT`
- `DYNAMODB_TABLE_PREFIX`
- `DYNAMODB_ENABLE_PITR`
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`

## State Model

### Refined AWS Secret Scope Lifecycle

1. `BaselineLoaded` — checked-in appsettings are loaded.
2. `ArtifactDiscovered` — startup determines whether the rendered AWS app-config artifact exists.
3. `ArtifactParsed` — the optional JSON artifact is parsed as flat scalar keys.
4. `ScopeValidated` — supported and excluded catalogs are evaluated.
5. `EffectiveConfigurationBuilt` — eligible AWS-secret keys are inserted between appsettings and environment variables.
6. `ResolvedValuesValidated` — supported groups are validated using normal startup/feature rules.
7. `Running` — the app starts with a compliant refined scope.
8. `Blocked` — startup stops because the artifact is invalid, excluded keys are present, or required supported values are unusable.

**Transitions**:
- `BaselineLoaded -> ArtifactDiscovered`: startup checks the configured artifact path.
- `ArtifactDiscovered -> EffectiveConfigurationBuilt`: no artifact exists, so the app continues without AWS-secret overrides.
- `ArtifactDiscovered -> ArtifactParsed`: artifact exists and is read.
- `ArtifactParsed -> Blocked`: invalid JSON or non-scalar values are found.
- `ArtifactParsed -> ScopeValidated`: parsed keys are compared to supported/excluded catalogs.
- `ScopeValidated -> Blocked`: one or more excluded keys are present.
- `ScopeValidated -> EffectiveConfigurationBuilt`: only eligible keys are present.
- `EffectiveConfigurationBuilt -> ResolvedValuesValidated`: normal typed/options validation runs.
- `ResolvedValuesValidated -> Running`: resolved configuration is compliant and usable.
- `ResolvedValuesValidated -> Blocked`: supported values are missing, incomplete, or invalid after resolution.
