# Research: Local DynamoDB Development Environment

## Decision 1: Keep a DynamoDB-only LocalStack image pinned to an explicit base version

- **Decision**: Continue using a dedicated `infra/localstack/Dockerfile` based on an explicitly pinned LocalStack image (`localstack/localstack:3.5`) and keep the container limited to DynamoDB.
- **Rationale**: The repository already uses a single-purpose LocalStack image that exposes only the DynamoDB emulator on port `8000`, which matches the existing integration-test default and reduces local startup variability. A pinned image version preserves reproducibility across contributor machines and CI-style smoke testing.
- **Alternatives considered**:
  - `latest` or floating major tags: rejected because they weaken reproducibility and can break local setup unexpectedly.
  - Multi-service LocalStack image: rejected because this feature only needs DynamoDB and extra services add noise, startup time, and maintenance burden.

## Decision 2: Preserve the existing local runtime contract around `http://localhost:8000`

- **Decision**: Keep the local emulator contract centered on `PERSISTENCE_PROVIDER=dynamodb`, `DYNAMODB_REGION=us-east-1`, `DYNAMODB_ENDPOINT=http://localhost:8000`, and a configurable `DYNAMODB_TABLE_PREFIX`.
- **Rationale**: `Program.cs`, `DynamoDbConfigurationOptions`, `DynamoDbClientFactory`, README guidance, and `DynamoDbLocalStartupTests` already align around that contract. Preserving it means contributors and tests use one documented setup instead of inventing a parallel local-only path.
- **Alternatives considered**:
  - New feature-specific configuration keys: rejected because the repo already centralizes DynamoDB configuration and validates it at startup.
  - Dynamic endpoint discovery: rejected because deterministic local documentation and test setup are more valuable than added flexibility here.

## Decision 3: Rely on emulator-friendly dummy credentials when a local endpoint is configured

- **Decision**: Keep the existing credential pattern where explicit AWS credentials are optional for local emulators and dummy credentials are supplied automatically when `DYNAMODB_ENDPOINT` is set.
- **Rationale**: `DynamoDbClientFactory` already protects the local workflow by providing syntactically valid credentials for emulator use while still allowing the AWS SDK credential chain for hosted AWS. This keeps local onboarding simple and avoids teaching contributors to manage unnecessary local secrets.
- **Alternatives considered**:
  - Requiring contributors to export dummy credentials manually: rejected because the application already handles this more safely and consistently.
  - Hardcoding credentials in docs or source: rejected for security and maintainability reasons.

## Decision 4: Keep the local emulator ephemeral and eager to start

- **Decision**: Retain the current LocalStack runtime choices of `SERVICES=dynamodb`, `GATEWAY_LISTEN=0.0.0.0:8000`, `EAGER_SERVICE_LOADING=1`, and `PERSISTENCE=0`.
- **Rationale**: These settings fit the repo's current workflow: DynamoDB-only startup, deterministic port binding, low ceremony for local smoke tests, and clean-state runs that avoid stale local data interfering with prefixed test tables. The current infrastructure tests already verify the most important parts of this contract.
- **Alternatives considered**:
  - Enabling persistence: rejected because the integration-test path benefits from ephemeral state and table cleanup.
  - Standard LocalStack edge port `4566`: rejected because the repository already documents and tests `8000`.

## Decision 5: Validate the feature through documentation and startup/integration tests, not a new abstraction layer

- **Decision**: Plan implementation around failing tests in `tests/Payslip4All.Web.Tests` plus documentation updates in `README.md` and `infra/localstack/README.md`.
- **Rationale**: The repo already has coverage for LocalStack Dockerfile configuration, startup validation, provider switching, and end-to-end DynamoDB create/read provisioning. Extending those surfaces is the shortest path that keeps the feature aligned with TDD and avoids unnecessary architectural churn.
- **Alternatives considered**:
  - Creating a new tooling project or helper CLI just to manage LocalStack: rejected because the feature scope is a source-controlled infrastructure artifact, not a new application subsystem.
  - Relying on manual verification only: rejected because the constitution requires tests to lead implementation.

## Decision 6: Treat the contributor-facing runtime contract as the feature's external interface

- **Decision**: Document the local LocalStack build/run contract in a dedicated contract artifact under `contracts/`, covering build command, run command, exposed port, required app environment variables, readiness expectation, and troubleshooting signals.
- **Rationale**: Even though this feature is infrastructure-focused, contributors interact with it through a stable interface: a Docker image plus runtime settings. Capturing that contract explicitly makes planning and later task generation clearer.
- **Alternatives considered**:
  - No contract artifact: rejected because the planning workflow expects contracts where a user- or system-facing interface exists.
  - Embedding all interface rules only in README prose: rejected because it weakens the separation between design contract and how-to guidance.
