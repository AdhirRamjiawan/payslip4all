# Research: AWS Secrets Scope Refinement

## Decision 1

- **Decision**: Keep the AWS Secrets-backed app-config artifact limited to the 15 repo-owned application settings that remain constitution-compliant: `PERSISTENCE_PROVIDER`, `ConnectionStrings:DefaultConnection`, `ConnectionStrings:MySqlConnection`, `Auth:Cookie:ExpireDays`, and `HostedPayments:PayFast:*`.
- **Rationale**: These keys are the repo-owned custom settings explicitly consumed by Payslip4All startup or feature code and match the intended refinement outcome for feature 015. Preserving them keeps AWS Secrets support for eligible app-owned configuration while avoiding rollback of the supported auth-cookie and hosted-payment paths introduced in feature 014.
- **Alternatives considered**:
  - **Remove AWS Secrets support for all persistence-related keys**: Rejected because the spec narrows the scope only for DynamoDB runtime and AWS credential groups, not for relational connection strings or provider selection.
  - **Leave the full feature 014 catalog untouched**: Rejected because it preserves the constitution conflict that feature 015 exists to resolve.

## Decision 2

- **Decision**: Define a separately identifiable excluded catalog containing exactly the six DynamoDB runtime and AWS credential keys: `DYNAMODB_REGION`, `DYNAMODB_ENDPOINT`, `DYNAMODB_TABLE_PREFIX`, `DYNAMODB_ENABLE_PITR`, `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY`.
- **Rationale**: These keys describe infrastructure runtime wiring or AWS credentials rather than repo-owned custom app configuration. Keeping them outside the AWS app-config artifact aligns with the constitution’s DynamoDB provider rules, preserves the preferred IAM/default-credential-chain path, and gives operators a concrete supported-versus-excluded catalog to validate before deployment.
- **Alternatives considered**:
  - **Exclude only AWS credential keys and keep DynamoDB runtime keys in the artifact**: Rejected because the runtime keys are coupled to infrastructure/bootstrap concerns and are part of the same constitution conflict.
  - **Treat excluded keys as warnings only**: Rejected because the spec requires non-compliant payloads to be flagged and not consumed.

## Decision 3

- **Decision**: Validate the rendered AWS app-config artifact immediately after JSON parsing and before it is merged into `ConfigurationManager`, failing fast if any excluded keys are present.
- **Rationale**: Validating before merge is the clearest way to guarantee the app never consumes excluded DynamoDB or AWS credential keys from the secret-backed path. It also preserves source attribution for diagnostics, avoids silent key dropping, and keeps the failure early enough that the deployment is not treated as healthy.
- **Alternatives considered**:
  - **Silently drop excluded keys during merge**: Rejected because operators would not know their payload is non-compliant or incomplete.
  - **Defer validation to `DynamoDbConfigurationOptions.ValidateForStartup()`**: Rejected because that logic validates resolved values, not whether the secret-backed artifact itself contains forbidden keys.

## Decision 4

- **Decision**: Preserve the current precedence for eligible keys as `environment variables > rendered AWS-secret config > checked-in appsettings > code defaults`, while requiring excluded DynamoDB runtime and credential values to come from environment variables, IAM roles, or other non-app-config deployment sources.
- **Rationale**: Existing feature 014 tests already prove the precedence contract for auth-cookie and hosted-payment settings, and the spec explicitly requires that behavior to remain unchanged. Separating the excluded catalog changes scope, not source precedence for the remaining eligible settings.
- **Alternatives considered**:
  - **Promote AWS-secret values above environment variables**: Rejected because it would break the documented emergency-override path.
  - **Collapse AWS-secret values into the environment-variable layer**: Rejected because it would remove the operator-visible distinction between supported secret-backed settings and excluded runtime inputs.

## Decision 5

- **Decision**: Keep diagnostics actionable but secret-safe by naming only the offending key or excluded group and the corrective action, never the secret value or credential contents.
- **Rationale**: The spec requires operator-facing guidance without exposing sensitive values. The repository already follows this pattern in startup validation tests, so the refined scope should continue reporting only keys/groups such as `DYNAMODB_REGION` or “AWS credential settings” plus guidance to move them out of the AWS app-config artifact.
- **Alternatives considered**:
  - **Echo offending values for debugging**: Rejected because it violates the feature’s explicit secret-safety requirement.
  - **Return a generic failure with no key names**: Rejected because it would not be actionable enough for migration from feature 014.

## Decision 6

- **Decision**: Treat operator migration guidance as a first-class output by updating the feature contract and quickstart to show how feature 014 payloads are audited, which keys must move out of scope, and how supported settings remain in AWS Secrets.
- **Rationale**: Existing deployments may already use the broader feature 014 catalog. Clear migration steps reduce upgrade risk, keep supported settings intact, and satisfy the spec requirement that the supported and excluded catalogs be separately identifiable before deployment or upgrade.
- **Alternatives considered**:
  - **Rely only on runtime validation errors**: Rejected because operators need an upgrade path before rollout, not just after a failed deployment.
  - **Document only the new supported catalog**: Rejected because migrations require an explicit excluded catalog and destination guidance for those keys.

## Decision 7

- **Decision**: Implement the refinement with TDD using startup/integration/documentation tests that cover supported-scope resolution, excluded-scope rejection, precedence preservation, and secret-safe diagnostics.
- **Rationale**: The constitution requires failing tests before implementation, and the repository already has focused startup tests around the AWS secret artifact path. Extending those tests is sufficient to prove the refined scope without introducing new tooling or a new project.
- **Alternatives considered**:
  - **Rely on manual hosted AWS smoke testing only**: Rejected because it violates the TDD principle and would underspecify regression coverage.
  - **Introduce a new external test harness**: Rejected because existing xUnit/WebApplicationFactory coverage patterns are already appropriate for this configuration feature.
