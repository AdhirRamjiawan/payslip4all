# LocalStack DynamoDB development container

Use this image for **development and smoke testing only**. It pins a DynamoDB-only LocalStack runtime to the repository's documented local endpoint contract without requiring a live AWS account.

## Build

From the repository root:

```bash
docker -H ssh://adhir-server build -f infra/localstack/Dockerfile -t payslip4all-localstack .
```

## Run

```bash
docker -H ssh://adhir-server run --rm --name payslip4all-localstack -p 8000:8000 payslip4all-localstack
```

The container must remain running while the application or tests use the emulator. These commands target the Docker daemon on `adhir-server`.

## Configure the app or tests

```bash
export PERSISTENCE_PROVIDER=dynamodb
export DYNAMODB_REGION=us-east-1
export DYNAMODB_ENDPOINT=http://adhir-server:8000
export DYNAMODB_TABLE_PREFIX=payslip4all
```

If you open a shell directly on `adhir-server` to run the application or tests there, you can keep `DYNAMODB_ENDPOINT=http://localhost:8000` instead.

If `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` are both unset, the app supplies emulator-safe dummy credentials automatically. This local workflow does not require a live AWS account. If you do set explicit credentials, provide the paired explicit credentials together.

## Verify the LocalStack workflow

Run the LocalStack-focused integration suite from the repository root:

```bash
dotnet test tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj --filter Category=Integration
```

This verifies that startup provisioning can create the prefixed DynamoDB tables and that representative local repository workflows succeed against the configured endpoint.
Run this verification while the container is still running, then stop the emulator afterward.

## Stop the emulator

Stop the foreground container with `Ctrl+C`, or from another terminal run:

```bash
docker -H ssh://adhir-server stop payslip4all-localstack
```

## Configurable values

- `DYNAMODB_ENDPOINT` defaults to `http://adhir-server:8000` in the documented remote workflow. If port `8000` is already in use on `adhir-server`, choose a different host port in `docker run` and update `DYNAMODB_ENDPOINT` to match.
- `DYNAMODB_TABLE_PREFIX` controls the table-name prefix used for local tables so contributors can avoid naming collisions.
- `DYNAMODB_REGION` should stay aligned with the documented LocalStack contract unless you intentionally need a different local region.

## Troubleshooting

- If port `8000` is already in use, free the port or choose a different host port and update `DYNAMODB_ENDPOINT` consistently.
- If startup fails because configuration validation rejects the endpoint, make sure `DYNAMODB_ENDPOINT` is an absolute `http://` or `https://` URL.
- If the app cannot reach the emulator, check that the container is still running, confirm `DYNAMODB_ENDPOINT` matches the `docker run` port mapping, and verify the LocalStack container finished starting.
- If startup fails immediately, confirm `DYNAMODB_REGION` is set and that paired explicit credentials are either both set or both omitted.
- If the integration workflow fails after a partial run, stop the container, restart it, and rerun the integration test command with a clean local session.
