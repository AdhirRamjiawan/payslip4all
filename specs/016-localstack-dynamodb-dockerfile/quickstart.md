# Quickstart: Local DynamoDB Development Environment

## 1. Build the LocalStack image

From the repository root:

```bash
docker -H ssh://adhir-server build -f infra/localstack/Dockerfile -t payslip4all-localstack .
```

This workflow is for local development and smoke testing only. The contributor-facing operator guide lives at `infra/localstack/README.md`.

## 2. Start the local DynamoDB emulator

```bash
docker -H ssh://adhir-server run --rm --name payslip4all-localstack -p 8000:8000 payslip4all-localstack
```

The container must remain running while the application or tests use the local DynamoDB endpoint on `adhir-server`.

## 3. Configure the application for the local DynamoDB path

In a second terminal:

```bash
export PERSISTENCE_PROVIDER=dynamodb
export DYNAMODB_REGION=us-east-1
export DYNAMODB_ENDPOINT=http://adhir-server:8000
export DYNAMODB_TABLE_PREFIX=payslip4all
```

If explicit AWS credentials are not set, the app should use emulator-safe dummy credentials automatically for this local endpoint.
If explicit credentials are supplied, both `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` must be present together.
If you run the application or tests from a shell on `adhir-server` itself, you can substitute `http://localhost:8000`.

## 4. Verify the emulator with the existing integration workflow

```bash
dotnet test tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj --filter Category=Integration
```

This verifies that startup can provision the expected prefixed tables and that representative repository behavior works against the local endpoint.

## 5. Run the application locally against DynamoDB

```bash
cd src/Payslip4All.Web
dotnet run
```

## 6. Stop the local environment

After the integration test command above completes, stop the running container with `Ctrl+C`, or terminate it from another terminal:

```bash
docker -H ssh://adhir-server stop payslip4all-localstack
```

## Troubleshooting

- If port `8000` is already in use, free the port or choose a different host port and update `DYNAMODB_ENDPOINT` to match.
- If you choose a different host port, keep the `docker run` port mapping and `DYNAMODB_ENDPOINT` aligned.
- If startup fails immediately, confirm `DYNAMODB_REGION` is set and that paired explicit credentials are either both set or both omitted.
- If the application cannot reach the emulator, check that the container is still running and `DYNAMODB_ENDPOINT` still points to `http://adhir-server:8000`.
- If integration tests fail after partial setup, restart the container and rerun the tests with a clean local session.
