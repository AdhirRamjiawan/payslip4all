# Research: AWS Secrets-Sourced Custom Configuration

## Decision 1: Limit the covered scope to repo-owned custom settings explicitly consumed by Payslip4All

- **Decision**: Cover the custom settings read by `Program.cs`, the DynamoDB startup/runtime path, and `PayFastHostedPaymentOptions`: `PERSISTENCE_PROVIDER`, `ConnectionStrings:*`, `Auth:Cookie:ExpireDays`, `DYNAMODB_*`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, and the `HostedPayments:PayFast:*` section. Exclude framework-owned sections such as `AllowedHosts`, baseline `Logging`, and `Serilog`, plus dev-only fake-payment settings that are not deployment-facing.
- **Rationale**: The feature spec scopes AWS Secrets support to repo-owned custom configuration that the app explicitly depends on. The identified keys drive startup decisions, persistence selection, hosted payment behavior, and auth cookie lifetime; untouched framework defaults do not. Excluding framework-default configuration keeps the design focused and avoids expanding the feature into generic host-configuration management.
- **Alternatives considered**:
  - **Include every key present in `appsettings*.json`**: Rejected because it would pull framework defaults into scope and violate FR-002.
  - **Limit scope to hosted-payment secrets only**: Rejected because FR-001 and FR-013 require the complete covered catalog, not just the currently obvious secret-like values.

## Decision 2: Reuse the existing CloudFormation/bootstrap Secrets Manager path instead of adding an app-level AWS Secrets SDK dependency

- **Decision**: Extend the existing AWS deployment/bootstrap flow to fetch the optional custom-config secret and render a deployment-owned intermediate configuration artifact for the app, rather than adding a new direct AWS Secrets Manager client dependency to `Payslip4All.Web`.
- **Rationale**: The repository already uses `aws secretsmanager get-secret-value` during bootstrap for TLS and hosted-payment deployment secrets. Reusing that path keeps AWS-specific secret retrieval in infrastructure concerns, avoids a new NuGet dependency that would need constitution review, and matches the user's deployment-focused feature context.
- **Alternatives considered**:
  - **Add `AWSSDK.SecretsManager` and fetch secrets directly in the app**: Rejected because it introduces a new tech-stack dependency and duplicates deployment responsibilities already handled in `infra/aws/cloudformation/`.
  - **Keep using only raw environment variables**: Rejected because it would not make AWS Secrets Manager a first-class supported source.

## Decision 3: Materialize the secret as a flat JSON object keyed by standard ASP.NET Core configuration paths

- **Decision**: Require the AWS secret payload to use flat configuration keys such as `HostedPayments:PayFast:MerchantId`, `Auth:Cookie:ExpireDays`, and `PERSISTENCE_PROVIDER`, rendered unchanged into an intermediate JSON config file.
- **Rationale**: Standard ASP.NET Core configuration paths are already the repository's effective contract, so using them directly minimizes translation logic and operator surprise. Flat keys also make partial/mixed-source resolution straightforward because the app can bind the same sections it already binds today.
- **Alternatives considered**:
  - **Nested JSON grouped by section**: Rejected because it requires additional merge/flatten logic and makes per-key precedence harder to reason about during mixed-source deployments.
  - **A bespoke mapping format (`secretName -> configKey`)**: Rejected because FR-014 calls for a documented operator mapping, not a second configuration DSL.

## Decision 4: Rebuild configuration precedence explicitly as `appsettings -> rendered AWS secrets -> environment variables`

- **Decision**: Update startup configuration composition so the optional rendered AWS secret source sits between the checked-in JSON files and normal environment variables.
- **Rationale**: FR-005 requires env vars to override AWS secrets, and AWS secrets to override checked-in appsettings. The default `CreateBuilder` ordering must therefore be made explicit so the secret-backed source is inserted at the correct priority rather than appended arbitrarily.
- **Alternatives considered**:
  - **Append the secrets source after `CreateBuilder()` without reordering**: Rejected because that would typically give secrets higher priority than environment variables.
  - **Write secret values straight into the same environment file as normal overrides**: Rejected because it would collapse AWS secrets and env vars into one source, making FR-005 and FR-012 impossible to document or diagnose clearly.

## Decision 5: Split failure handling between bootstrap secret access errors and app-level value validation

- **Decision**: Treat unreadable/missing secret references as bootstrap failures that prevent service startup, and treat malformed/incomplete/invalid covered values as app validation failures after source resolution. Preserve the current feature-aware validation boundary for hosted payments unless a failing test demonstrates startup validation is required there too.
- **Rationale**: Bootstrap already owns AWS secret retrieval, so access failures belong there. The app remains the only place that understands which resolved values are valid for each feature, so it must continue to enforce section completeness and type/format rules without logging secret contents.
- **Alternatives considered**:
  - **Move all validation into bootstrap shell scripts**: Rejected because shell scripts should not duplicate application-specific validation rules already expressed in C#.
  - **Fail the whole app for every optional section omission immediately**: Rejected because FR-004 requires backward compatibility and some sections (notably PayFast) currently validate at feature-entry time rather than unconditional startup.

## Decision 6: Keep hosted AWS deployment topology intact and add only one new app-config secret contract path

- **Decision**: Add a new optional CloudFormation/bootstrap path for custom app configuration (parameter, conditional IAM read permission, output/reference, and rendered file handling) while leaving the existing TLS certificate secret and current deployment topology intact.
- **Rationale**: The repo already has a single-host AWS deployment story with explicit secret references and outputs. Extending that story is lower risk than inventing a second deployment path, and it keeps the feature suitable for immediate downstream task generation.
- **Alternatives considered**:
  - **Replace `HostedPaymentsSecretArn` with a totally different deployment model**: Rejected because it would create unnecessary migration risk in an otherwise focused feature.
  - **Handle the feature only in docs with no deployment-asset updates**: Rejected because FR-014 requires an explicit operator mapping and the current AWS path must be able to exercise it.

## Decision 7: Drive implementation with startup/integration tests that prove secrets-only, mixed-source, and unchanged legacy deployments

- **Decision**: Plan TDD around `Payslip4All.Web.Tests` and, where useful, `Payslip4All.Infrastructure.Tests` for covered-setting catalog resolution, precedence, fallback, and safe failure diagnostics.
- **Rationale**: The constitution requires failing tests before code changes. The repo already uses xUnit and WebApplicationFactory for startup/runtime validation, so the same patterns should prove secrets-only deployments, env-var overrides, appsettings fallback, and sanitized failure behavior.
- **Alternatives considered**:
  - **Rely on manual AWS smoke testing only**: Rejected because it violates Principle I and would leave the precedence contract under-specified.
  - **Introduce a new external infrastructure test framework**: Rejected because existing .NET test patterns are sufficient for this feature.
