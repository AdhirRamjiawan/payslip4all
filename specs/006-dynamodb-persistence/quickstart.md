# Quickstart: Running Payslip4All with DynamoDB

**Feature**: 006-dynamodb-persistence  
**Audience**: Developers, CI engineers, and deployment operators

---

## Prerequisites

- .NET 8 SDK
- Docker Desktop / Docker Engine for local DynamoDB emulation
- Optional: AWS CLI for inspecting tables

---

## 1. Local Development with DynamoDB Local

### Step 1 — Start DynamoDB Local

```bash
docker run -d \
  --name dynamodb-local \
  -p 8000:8000 \
  amazon/dynamodb-local \
  -jar DynamoDBLocal.jar -sharedDb -inMemory
```

Verify the emulator is reachable:

```bash
curl http://localhost:8000
```

### Step 2 — Set Environment Variables

```bash
export PERSISTENCE_PROVIDER=dynamodb
export DYNAMODB_REGION=us-east-1
export DYNAMODB_ENDPOINT=http://localhost:8000
export DYNAMODB_TABLE_PREFIX=payslip4all
```

**Credential behavior for local emulators**:
- if you set both `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`, the app uses them,
- if you do not set them and `DYNAMODB_ENDPOINT` is present, the app supplies dummy
  credentials automatically for DynamoDB Local.

### Step 3 — Run the Web App

```bash
cd src/Payslip4All.Web
dotnet run
```

Expected startup behavior:

1. `Program.cs` selects the `dynamodb` provider,
2. DynamoDB services are registered instead of EF Core repositories,
3. relational migration startup is skipped,
4. missing DynamoDB tables are auto-created and logged,
5. the app starts serving requests.

---

## 2. Switching Between Providers

| Provider | `PERSISTENCE_PROVIDER` | Additional settings |
|---|---|---|
| SQLite | unset or `sqlite` | none |
| MySQL | `mysql` | `ConnectionStrings__MySqlConnection` |
| DynamoDB Local | `dynamodb` | `DYNAMODB_REGION`, `DYNAMODB_ENDPOINT` |
| DynamoDB on AWS | `dynamodb` | `DYNAMODB_REGION` plus either explicit credentials or a resolvable AWS SDK credential source |

Provider matching is case-insensitive and trimmed, so ` DynamoDB ` is treated the same as
`dynamodb`.

---

## 3. AWS Authentication Rules

When `PERSISTENCE_PROVIDER=dynamodb`, credentials resolve in this order:

1. **Explicit environment-variable credentials**  
   If both `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` are present, they are used.

2. **Dummy credentials for local emulators**  
   If `DYNAMODB_ENDPOINT` is present and explicit credentials are absent, the app supplies
   placeholder credentials for DynamoDB Local compatibility.

3. **AWS SDK default credential chain / hosted identity**  
   If explicit credentials are absent and `DYNAMODB_ENDPOINT` is not set, the SDK resolves
   credentials from its normal sources (for example IAM roles, container credentials,
   instance metadata, `AWS_PROFILE`, or shared credentials files).

---

## 4. Example AWS Deployment Configurations

### Option A — Hosted AWS with IAM Role or Other SDK-Resolved Identity

```bash
export PERSISTENCE_PROVIDER=dynamodb
export DYNAMODB_REGION=af-south-1
export DYNAMODB_TABLE_PREFIX=payslip4all
```

Use this mode when the hosting environment already exposes valid AWS credentials through
the normal SDK chain, such as an IAM role on EC2/ECS/EKS.

### Option B — Explicit Access Key Credentials

```bash
export PERSISTENCE_PROVIDER=dynamodb
export DYNAMODB_REGION=af-south-1
export AWS_ACCESS_KEY_ID=your-access-key
export AWS_SECRET_ACCESS_KEY=your-secret-key
export DYNAMODB_TABLE_PREFIX=payslip4all
```

Use this mode only when an environment cannot provide hosted identity. Keep credentials in
secret storage, never in source control.

---

## 5. Verifying Startup Provisioning

With DynamoDB selected, startup should create or confirm the following tables:

- `{prefix}_users`
- `{prefix}_companies`
- `{prefix}_employees`
- `{prefix}_employee_loans`
- `{prefix}_payslips`
- `{prefix}_payslip_loan_deductions`

Look for log messages indicating each table was created or already existed.

If you have the AWS CLI installed, you can also verify the prefixed tables directly against the
local emulator:

```bash
aws dynamodb list-tables \
  --endpoint-url http://localhost:8000 \
  --region us-east-1
```

You should see the six `{prefix}_...` tables listed for the configured `DYNAMODB_TABLE_PREFIX`.

---

## 6. Validate a Create-Read Cycle

After the app starts successfully, confirm the local emulator path works end-to-end:

1. Register or log in as a Company Owner.
2. Create a company.
3. Add an employee to that company.
4. Generate a payslip for the employee.
5. Refresh the relevant page and confirm the company, employee, and payslip still load correctly.

This verifies that startup provisioning completed and that the DynamoDB repositories can persist
and read back business data without a live AWS account.

---

## 7. Running Relevant Tests

### Web startup/provider-switching tests

```bash
cd tests/Payslip4All.Web.Tests
dotnet test --filter DynamoDbProviderSwitchingTests
```

### Infrastructure DynamoDB tests

```bash
cd tests/Payslip4All.Infrastructure.Tests
dotnet test --filter DynamoDb
```

These tests are intended to run against DynamoDB Local rather than a live AWS account.

---

## 8. Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| Startup fails saying `DYNAMODB_REGION` is required | Missing region | Set `DYNAMODB_REGION` |
| Startup fails saying provider is unknown | Invalid `PERSISTENCE_PROVIDER` value | Use `sqlite`, `mysql`, or `dynamodb` |
| Startup fails because only one AWS credential variable is set | Partial explicit credential pair | Set both `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`, or unset both |
| Local DynamoDB connection fails | Emulator not running or wrong endpoint | Start DynamoDB Local and verify `DYNAMODB_ENDPOINT` |
| AWS deployment cannot create tables | Role/key lacks `CreateTable` or related permissions | Add required DynamoDB permissions |
