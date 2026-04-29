# Data Model: Local DynamoDB Development Environment

## Entity: LocalStack Image Configuration

**Purpose**: Defines the source-controlled container settings that expose a DynamoDB-compatible local service for contributors.

| Field | Type | Description | Validation |
|---|---|---|---|
| `baseImageTag` | string | Pinned LocalStack image tag used by the repository Dockerfile | Must be an explicit non-empty tag, not a floating placeholder |
| `enabledServices` | string list | AWS service set enabled inside the image | Must include DynamoDB; should not enable unrelated services without justification |
| `listenAddress` | string | Container bind address for the LocalStack gateway | Must allow host reachability for the documented local run mode |
| `listenPort` | integer | Port exposed for the local DynamoDB endpoint | Must match documented build/run instructions and test expectations |
| `defaultRegion` | string | Default AWS region used for local startup | Must align with documented local configuration |
| `eagerServiceLoading` | boolean | Whether DynamoDB is initialized during container startup | Must support predictable readiness for smoke tests |
| `persistenceMode` | boolean | Whether emulator state is persisted across restarts | Defaults to ephemeral local runs for clean validation |

## Entity: Local DynamoDB Runtime Contract

**Purpose**: Represents the environment values a contributor uses to connect the application or tests to the emulator.

| Field | Type | Description | Validation |
|---|---|---|---|
| `persistenceProvider` | string | Application persistence provider selector | Must be `dynamodb` for this workflow |
| `region` | string | Region value consumed by DynamoDB client configuration | Required and non-empty |
| `endpoint` | URI string | Local DynamoDB-compatible endpoint | Required for the local emulator path; must be a valid HTTP URL |
| `tablePrefix` | string | Prefix used for locally provisioned DynamoDB tables | Must be non-empty and safe for repeated local runs |
| `credentialMode` | enum (`explicit`, `emulator-default`) | How credentials are resolved for the runtime | Must be internally consistent with endpoint presence and app validation rules |

## Entity: Developer Verification Workflow

**Purpose**: Describes the contributor journey used to verify the local emulator is usable.

| Field | Type | Description | Validation |
|---|---|---|---|
| `buildCommand` | string | Command used to build the local image | Must resolve from repo root |
| `runCommand` | string | Command used to start the container | Must expose the documented local port |
| `readinessSignal` | string | Observable signal that the local service is usable | Must be checkable by documentation or tests |
| `smokeTestCommand` | string | Command used to validate DynamoDB-backed behavior locally | Must run against the documented endpoint contract |
| `troubleshootingGuide` | string | Pointer to contributor-facing failure guidance | Must exist in repository documentation |

## Relationships

- **LocalStack Image Configuration** produces one **Local DynamoDB Runtime Contract**.
- **Local DynamoDB Runtime Contract** is consumed by one or more **Developer Verification Workflow** executions.
- **Developer Verification Workflow** validates that the runtime contract stays compatible with existing application startup and test expectations.

## State Transitions

### Local emulator lifecycle

1. `Not Built` -> `Built`: Contributor builds the LocalStack image from the repository Dockerfile.
2. `Built` -> `Running`: Contributor starts the container with the documented port mapping.
3. `Running` -> `Reachable`: The endpoint is available and the app/tests can connect using the documented configuration.
4. `Reachable` -> `Verified`: Startup and smoke-test workflows complete successfully.
5. `Running` or `Reachable` -> `Failed`: Port conflict, misconfiguration, or container startup issues prevent successful use.
6. `Verified` -> `Stopped`: Contributor stops or removes the container, ending the ephemeral local session.
