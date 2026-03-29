# Quickstart: Running Payslip4All with DynamoDB

**Feature**: 006-dynamodb-persistence
**Audience**: Developers and deployment operators

---

## Prerequisites

- Docker Desktop (or Docker Engine) installed and running
- .NET 8 SDK
- AWS CLI (optional — only needed for inspecting real AWS DynamoDB)

---

## Local Development with DynamoDB Local

### Step 1 — Start DynamoDB Local

```bash
docker run -d \
  --name dynamodb-local \
  -p 8000:8000 \
  amazon/dynamodb-local \
  -jar DynamoDBLocal.jar -sharedDb -inMemory
```

> `-sharedDb` makes all clients share the same database file.
> `-inMemory` keeps data in RAM (reset on container restart — ideal for development).
> Remove `-inMemory` if you want data to survive container restarts.

Verify it's running:
```bash
curl http://localhost:8000
# Expected: {"__type":"com.amazon.coral.service#UnknownOperationException",...}
```

### Step 2 — Configure Environment Variables

Set the following before starting the application (or add to your IDE's launch profile):

```bash
export PERSISTENCE_PROVIDER=dynamodb
export DYNAMODB_ENDPOINT=http://localhost:8000
export DYNAMODB_REGION=us-east-1
export AWS_ACCESS_KEY_ID=dummy          # Required by SDK; any non-empty value works for local
export AWS_SECRET_ACCESS_KEY=dummy      # Required by SDK; any non-empty value works for local
export DYNAMODB_TABLE_PREFIX=payslip4all  # Optional; this is the default
```

**Visual Studio / Rider**: Add these to your `launchSettings.json` under the
`environmentVariables` section for the `Payslip4All.Web` profile.

### Step 3 — Run the Application

```bash
cd src/Payslip4All.Web
dotnet run
```

On startup, the application will:
1. Detect `PERSISTENCE_PROVIDER=dynamodb`
2. Connect to `http://localhost:8000`
3. Auto-create the 6 required DynamoDB tables (logged at Information level)
4. Start serving requests

Expected startup log entries:
```
[INF] Persistence provider: dynamodb
[INF] DynamoDB table 'payslip4all_users' created.
[INF] DynamoDB table 'payslip4all_companies' created.
[INF] DynamoDB table 'payslip4all_employees' created.
[INF] DynamoDB table 'payslip4all_employee_loans' created.
[INF] DynamoDB table 'payslip4all_payslips' created.
[INF] DynamoDB table 'payslip4all_payslip_loan_deductions' created.
```

---

## Switching Between Providers

| Provider | `PERSISTENCE_PROVIDER` | Additional required vars |
|----------|------------------------|--------------------------|
| SQLite (default) | `sqlite` or unset | None (uses `DefaultConnection` from appsettings) |
| MySQL | `mysql` | `ConnectionStrings__MySqlConnection` |
| DynamoDB (local) | `dynamodb` | `DYNAMODB_ENDPOINT`, `DYNAMODB_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` |
| DynamoDB (AWS) | `dynamodb` | `DYNAMODB_REGION`; credentials via env vars or IAM role |

---

## AWS Production Deployment

### With IAM Role (recommended for EC2 / ECS / Lambda)

Attach an IAM role to your compute resource with the following permissions:

```json
{
  "Effect": "Allow",
  "Action": [
    "dynamodb:CreateTable",
    "dynamodb:DescribeTable",
    "dynamodb:PutItem",
    "dynamodb:GetItem",
    "dynamodb:UpdateItem",
    "dynamodb:DeleteItem",
    "dynamodb:Query",
    "dynamodb:Scan"
  ],
  "Resource": "arn:aws:dynamodb:<REGION>:<ACCOUNT_ID>:table/payslip4all_*"
}
```

Set only these environment variables (no credentials needed when using an IAM role):

```bash
PERSISTENCE_PROVIDER=dynamodb
DYNAMODB_REGION=<your-region>       # e.g., af-south-1
DYNAMODB_TABLE_PREFIX=payslip4all   # optional
```

### With Explicit Credentials (CI/CD, non-IAM environments)

```bash
PERSISTENCE_PROVIDER=dynamodb
DYNAMODB_REGION=<your-region>
AWS_ACCESS_KEY_ID=<your-access-key>
AWS_SECRET_ACCESS_KEY=<your-secret-key>
```

> **Never commit credentials to source control.** Use secrets management (AWS Secrets
> Manager, environment-level secrets in your CI/CD platform, etc.).

---

## Running Integration Tests Against DynamoDB Local

The integration tests in `Payslip4All.Infrastructure.Tests/DynamoDB/` require
DynamoDB Local to be running on port 8000.

```bash
# Start DynamoDB Local (if not already running)
docker run -d --name dynamodb-local -p 8000:8000 \
  amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb -inMemory

# Run only DynamoDB integration tests
cd tests/Payslip4All.Infrastructure.Tests
dotnet test --filter "Category=DynamoDB"
```

Tests use xUnit's `IAsyncLifetime` fixture to create and destroy tables before/after
each test class. No data persists between test runs.

---

## Inspecting DynamoDB Local Tables

Use the AWS CLI with a local endpoint override:

```bash
# List all tables
aws dynamodb list-tables --endpoint-url http://localhost:8000 --region us-east-1

# Scan a table (all items)
aws dynamodb scan \
  --table-name payslip4all_users \
  --endpoint-url http://localhost:8000 \
  --region us-east-1
```

---

## Stopping DynamoDB Local

```bash
docker stop dynamodb-local && docker rm dynamodb-local
```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| `Connection refused` on startup | DynamoDB Local not running | Run `docker start dynamodb-local` |
| `ResourceNotFoundException` on first request | Tables not created | Check startup logs; ensure provisioner ran; verify `DYNAMODB_ENDPOINT` is correct |
| `UnrecognizedPersistenceProvider` error on startup | `PERSISTENCE_PROVIDER` typo | Valid values: `sqlite`, `mysql`, `dynamodb` (case-insensitive) |
| `CreateTable` permission denied (AWS) | IAM role lacks `dynamodb:CreateTable` | Add permission or pre-provision tables via IaC |
| Decimal precision issues | Monetary values stored as `N` | Check that all monetary attributes use `S` type; see data-model.md |
