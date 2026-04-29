# Contract: Local DynamoDB Runtime

## Purpose

Define the contributor-facing interface for the repository's LocalStack-backed DynamoDB development environment.

## Artifact Contract

| Item | Contract |
|---|---|
| Dockerfile path | `infra/localstack/Dockerfile` |
| Image purpose | Provide a DynamoDB-compatible local service for development and integration testing |
| Base image | Explicit LocalStack version tag |
| Enabled service set | DynamoDB only |
| Exposed container port | `8000` |
| Host endpoint | `http://adhir-server:8000` |

## Build and Run Contract

### Build

```bash
docker -H ssh://adhir-server build -f infra/localstack/Dockerfile -t payslip4all-localstack .
```

### Run

```bash
docker -H ssh://adhir-server run --rm --name payslip4all-localstack -p 8000:8000 payslip4all-localstack
```

## Application Runtime Contract

To consume the local emulator, contributors must supply the standard DynamoDB runtime settings:

```bash
export PERSISTENCE_PROVIDER=dynamodb
export DYNAMODB_REGION=us-east-1
export DYNAMODB_ENDPOINT=http://adhir-server:8000
export DYNAMODB_TABLE_PREFIX=payslip4all
```

### Credential expectations

- If `DYNAMODB_ENDPOINT` is set and explicit AWS credentials are not provided, the application must be able to use emulator-safe dummy credentials automatically.
- If explicit credentials are supplied, both access key and secret key must be present together.
- A live AWS account must not be required for this local workflow.

## Expected Behavior

- Starting the container exposes a DynamoDB-compatible endpoint on `http://adhir-server:8000`.
- Starting the app with the runtime contract above uses the DynamoDB provider path instead of relational providers.
- Existing startup provisioning creates or verifies required prefixed tables for local use.
- Integration and smoke-test workflows can run against the emulator without source-code changes.

## Verification Contract

The feature is considered integrated when contributors can use the runtime contract above and a representative validation command succeeds, such as:

```bash
dotnet test tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj --filter Category=Integration
```

That verification command must run before the LocalStack container is stopped.

## Failure Signals

| Failure | Expected Guidance |
|---|---|
| Port `8000` already in use | Documentation must explain how to free the port or remap/update the local endpoint consistently |
| Container starts but app cannot connect | Documentation must direct contributors to check `DYNAMODB_ENDPOINT`, region, and container status |
| `PERSISTENCE_PROVIDER=dynamodb` but region missing | Startup validation must fail with a clear configuration error |
| One explicit AWS credential is missing | Startup validation must fail with a clear paired-credential error |
