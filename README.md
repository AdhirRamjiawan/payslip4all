# Payslip4All

A web application for generating and managing employee payslips, built for South African employers. Supports multiple companies per employer, employee loan tracking, and PDF payslip generation with automatic UIF deduction calculations.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [AWS CloudFormation Deployment](#aws-cloudformation-deployment)
- [Payment Gateway Setup](#payment-gateway-setup)
- [Configuration](#configuration)
- [Running Tests](#running-tests)
- [CI/CD](#cicd)
- [Architecture](#architecture)

---

## Features

- **Employer accounts** — register, login, and manage your profile securely
- **Multi-company support** — create and manage multiple companies under one account
- **Employee management** — add employees with full details (ID number, occupation, salary, start date)
- **Loan tracking** — multiple concurrent loans per employee with automatic monthly deductions and completion detection
- **Payslip generation** — monthly payslips with automatic calculations:
  - Gross earnings (monthly salary)
  - UIF deduction — `MIN(monthly salary, R17 712) × 1%` (SA legal standard)
  - Loan deductions (one line per active loan)
  - Net pay
- **PDF download** — professionally formatted payslips via QuestPDF
- **Payslip history** — view all past payslips in reverse chronological order
- **Wallet credits** — each company owner has a wallet balance that is charged per successful payslip generation
- **Wallet activity history** — auditable credit and debit entries with resulting balances
- **Generic hosted payment returns** — provider returns land on `/portal/wallet/top-ups/return`, are normalized into explicit statuses (`Pending`, `Completed`, `Cancelled`, `Expired`, `Abandoned`, `NotConfirmed`), and only matched trustworthy completions credit the wallet
- **Admin pricing** — site administrators can change the per-payslip wallet charge from the web UI
- **Data isolation** — employers only ever see their own companies, employees, and payslips

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 8 / ASP.NET Core 8 |
| UI | Blazor Server (C# 12) |
| ORM | Entity Framework Core 8 |
| Database | SQLite (default) · MySQL 8+ (optional) · AWS DynamoDB (optional) |
| PDF | QuestPDF 2024.10.4 (Community license) |
| Auth | ASP.NET Core Cookie Authentication |
| Passwords | BCrypt.Net-Next (work factor 12) |
| Testing | xUnit · bUnit · Moq · WebApplicationFactory |
| CI | GitHub Actions |

---

## Project Structure

```
payslip4all/
├── src/
│   ├── Payslip4All.Domain/          # Entities, value objects, domain rules
│   ├── Payslip4All.Application/     # Use cases, service interfaces, DTOs
│   ├── Payslip4All.Infrastructure/  # EF Core, repositories, PDF service, BCrypt
│   └── Payslip4All.Web/             # Blazor Server app, Razor Pages (auth)
├── tests/
│   ├── Payslip4All.Domain.Tests/
│   ├── Payslip4All.Application.Tests/
│   ├── Payslip4All.Infrastructure.Tests/
│   └── Payslip4All.Web.Tests/
├── specs/                           # Feature specifications & design artifacts
├── Directory.Build.props            # Warnings-as-errors, LangVersion=12
└── Payslip4All.sln
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git
- *(Optional)* MySQL 8+ — only needed if switching from the default SQLite backend
- *(Optional)* Docker — only needed for local DynamoDB emulation

---

## Getting Started

### 1. Clone and build

```bash
git clone <repo-url>
cd payslip4all
dotnet restore
dotnet build
```

### 2. Run (SQLite — default)

```bash
cd src/Payslip4All.Web
dotnet run
```

Open **https://localhost:5001** in your browser. The database is created automatically on first run.

### 3. Run (MySQL — optional)

```bash
export PERSISTENCE_PROVIDER=mysql
export ConnectionStrings__MySqlConnection="Server=localhost;Database=payslip4all;User=root;Password=yourpassword;"
cd src/Payslip4All.Web
dotnet run
```

Migrations are applied automatically on startup for SQLite and MySQL.

### 4. Run (DynamoDB — optional)

#### Local DynamoDB emulator

```bash
docker run -d --name dynamodb-local -p 8000:8000 amazon/dynamodb-local \
  -jar DynamoDBLocal.jar -sharedDb -inMemory

export PERSISTENCE_PROVIDER=dynamodb
export DYNAMODB_REGION=us-east-1
export DYNAMODB_ENDPOINT=http://localhost:8000
export DYNAMODB_TABLE_PREFIX=payslip4all

cd src/Payslip4All.Web
dotnet run
```

If `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` are both unset while `DYNAMODB_ENDPOINT`
is configured, the app supplies dummy credentials automatically for local emulators.

#### Hosted AWS

```bash
export PERSISTENCE_PROVIDER=dynamodb
export DYNAMODB_REGION=af-south-1
export DYNAMODB_TABLE_PREFIX=payslip4all

cd src/Payslip4All.Web
dotnet run
```

For hosted AWS, either set both `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` explicitly or
let the AWS SDK resolve credentials from its default chain (for example IAM roles, container
credentials, instance metadata, `AWS_PROFILE`, or shared credentials files).

### 5. Deploy to AWS with CloudFormation

Payslip4All includes a repository-owned AWS deployment guide for a low-cost hosted setup using:

- one EC2 instance for the web application,
- one Application Load Balancer for public HTTPS access,
- Route 53 aliasing for `payslip.co.za`,
- DynamoDB persistence with hosted AWS point-in-time recovery backups.

Start with:

- template: `infra/aws/cloudformation/payslip4all-web.yaml`
- operator guide: `infra/aws/cloudformation/README.md`

The deployment guide keeps the operator workflow intentionally small:

1. publish the application artifact,
2. confirm ACM certificate issuance,
3. confirm Route 53 authority for `payslip.co.za`,
4. gather external secret references,
5. launch the CloudFormation stack.

Operators then verify deployment success using the documented signal set: `ApplicationUrl`, `InstanceId`, `LoadBalancerArn`, `InstanceSecurityGroupId`, `LoadBalancerSecurityGroupId`, `BackupProtectionMode`, ALB target health, and the public `/health` endpoint.

This deployment path is designed to use as much of the AWS free tier as practical, but it is not fully free-tier-only because the ALB and Route 53 are required paid services for the requested public-domain setup.

### First-run walkthrough

1. Navigate to `/Auth/Register` and create an employer account
2. Add a company from the dashboard
3. Add an employee to the company
4. *(Optional)* Add a loan to the employee
5. Generate a payslip and download the PDF
6. *(Optional)* Top up `/portal/wallet` before generating payslips and adjust `/admin/wallet-pricing` as a site administrator

### Wallet rollout and manual verification

Use this sequence after deploying wallet-credit changes or refreshing a local environment:

0. Confirm the seeded default public payslip price is **R 15.00** unless an administrator has already changed it in the target environment.
1. Sign out and open `/` to confirm the public wallet section shows only public pricing and wallet messaging.
2. Sign in as a `SiteAdministrator`, open `/admin/wallet-pricing`, set a rand price such as `15.00`, and confirm the value updates immediately.
3. Sign in as a `CompanyOwner`, open `/portal/wallet`, start a hosted card top-up, and confirm the browser redirects to the PayFast-hosted payment page without Payslip4All collecting any card details.
4. Complete a sandbox or live test payment so the browser returns through `/portal/wallet/top-ups/return`, then confirm Payslip4All redirects the owner to `/portal/wallet/top-ups/{attemptId}/return` and credits the wallet exactly once after the server-side PayFast notify callback is accepted.
5. Repeat the hosted flow with cancelled, expired, and unmatched/generic return outcomes, and confirm the wallet balance remains unchanged while wallet history shows the correct owner-scoped status or generic not-confirmed page.
6. Generate a payslip with sufficient funds and confirm the charged amount, wallet debit, and wallet activity entry all match.
7. Retry generation with insufficient funds and confirm no payslip is created and no wallet debit occurs.
8. Overwrite an existing payslip for the same employee and period, then confirm the original payslip is only removed after the replacement and wallet charge both succeed.
9. When `PERSISTENCE_PROVIDER=dynamodb`, repeat the hosted wallet top-up and payslip generation checks against a live emulator or AWS-backed environment before release.

---

## Payment Gateway Setup

Payslip4All uses **PayFast** for real wallet top-ups. The payment journey is **hosted**, **card-only**, and designed so Payslip4All never collects or stores card details directly.

### Required PayFast configuration

Set up these values before you try a wallet top-up:

| Key | Required for | What it does |
|-----|--------------|--------------|
| `HostedPayments:PayFast:ProviderKey` | All environments | Must remain `payfast` so the wallet flow resolves the PayFast provider |
| `HostedPayments:PayFast:UseSandbox` | All environments | `true` for sandbox/test setups, `false` for live merchant setups |
| `HostedPayments:PayFast:MerchantId` | Sandbox and live | Your PayFast merchant account identifier |
| `HostedPayments:PayFast:MerchantKey` | Sandbox and live | Your PayFast merchant key |
| `HostedPayments:PayFast:Passphrase` | If your PayFast account uses one | The passphrase used when PayFast signatures are created and verified |
| `HostedPayments:PayFast:PublicNotifyUrl` | All environments | Public HTTPS callback URL that PayFast can post back to for trustworthy payment confirmation |

### Where to put the values

- **Local development**: put PayFast secrets in `src/Payslip4All.Web/appsettings.Development.Private.json`. This file is already ignored by git.
- **Deployed environments**: prefer environment variables or your platform's secret store.
- **Do not commit** sandbox or live merchant credentials to `appsettings.json`, `appsettings.Development.json`, or any tracked file.

Example local sandbox configuration:

```json
{
  "HostedPayments": {
    "PayFast": {
      "ProviderKey": "payfast",
      "UseSandbox": true,
      "MerchantId": "<your-sandbox-merchant-id>",
      "MerchantKey": "<your-sandbox-merchant-key>",
      "Passphrase": "<your-sandbox-passphrase-or-empty-string>",
      "PublicNotifyUrl": "https://your-public-host.example/api/payments/payfast/notify"
    }
  }
}
```

Equivalent environment variables:

```bash
export HostedPayments__PayFast__ProviderKey=payfast
export HostedPayments__PayFast__UseSandbox=true
export HostedPayments__PayFast__MerchantId=<your-merchant-id>
export HostedPayments__PayFast__MerchantKey=<your-merchant-key>
export HostedPayments__PayFast__Passphrase=<your-passphrase-or-empty-string>
export HostedPayments__PayFast__PublicNotifyUrl=https://your-public-host.example/api/payments/payfast/notify
```

### Sandbox vs live mode

- Use **sandbox** mode for local development, demos, and pre-production verification. Set `HostedPayments:PayFast:UseSandbox` to `true`.
- Use **live** mode only with a real live PayFast merchant account. Set `HostedPayments:PayFast:UseSandbox` to `false`.
- Switching between sandbox and live is a **configuration change**, not a code change.

### Callback requirements

`HostedPayments:PayFast:PublicNotifyUrl` is required because wallet credit depends on the **server-side PayFast notify callback**, not the browser return alone.

Your notify URL must:

1. Use **HTTPS**
2. Be **publicly reachable** by PayFast
3. Point to **your Payslip4All app**, not a PayFast URL
4. End at `/api/payments/payfast/notify`

Important notes:

- `localhost` is **not valid** for `PublicNotifyUrl`
- A private LAN address or a tunnel that is offline will prevent trustworthy payment confirmation
- The browser return to `/portal/wallet/top-ups/return` is **informational only**; the wallet is credited only after the PayFast notify callback is accepted

For local development, use a public tunnel or hosted environment so PayFast can reach your app.

### Verify the setup

After you save the configuration:

1. Start the app from `src/Payslip4All.Web`
2. Sign in as a `CompanyOwner`
3. Open `/portal/wallet`
4. Enter a valid top-up amount and start the hosted payment
5. Confirm the browser leaves Payslip4All and lands on the PayFast-hosted payment page
6. Complete a sandbox or live test payment and confirm the browser returns to Payslip4All
7. Confirm the final wallet result appears under `/portal/wallet/top-ups/{attemptId}/return`
8. If the browser returns but the wallet is not credited, troubleshoot the notify callback first

### Troubleshooting

**Payment could not be started**

- Check `MerchantId` and `MerchantKey`
- Confirm `ProviderKey` is still `payfast`
- Confirm you are using the correct merchant account for the selected sandbox/live mode

**The browser returns, but the wallet is not credited**

- Check that `PublicNotifyUrl` is public HTTPS and currently reachable
- Check that the notify URL points to `/api/payments/payfast/notify`
- Check that your tunnel, reverse proxy, or deployed host is still forwarding PayFast callbacks correctly

**PayFast opens the wrong environment**

- Check `HostedPayments:PayFast:UseSandbox`
- Confirm your merchant credentials belong to the same environment you selected

**You are testing locally and PayFast cannot call back**

- Use a public HTTPS tunnel or deploy the app to a reachable environment
- Do not use `localhost` or a private-only address for `PublicNotifyUrl`

---

## Configuration

| Key | Default | Description |
|-----|---------|-------------|
| `PERSISTENCE_PROVIDER` | `"sqlite"` | `"sqlite"`, `"mysql"`, or `"dynamodb"` |
| `ConnectionStrings:DefaultConnection` | `"Data Source=payslip4all.db"` | SQLite file path |
| `ConnectionStrings:MySqlConnection` | `""` | MySQL connection string |
| `DYNAMODB_REGION` | — | Required when `PERSISTENCE_PROVIDER=dynamodb` |
| `DYNAMODB_ENDPOINT` | — | Optional endpoint override for DynamoDB Local or other emulators |
| `DYNAMODB_TABLE_PREFIX` | `"payslip4all"` | Optional prefix for the thirteen required DynamoDB tables |
| `AWS_ACCESS_KEY_ID` | — | Optional explicit DynamoDB credential; must be paired with `AWS_SECRET_ACCESS_KEY` |
| `AWS_SECRET_ACCESS_KEY` | — | Optional explicit DynamoDB credential; must be paired with `AWS_ACCESS_KEY_ID` |
| `Auth:Cookie:ExpireDays` | `30` | Session lifetime in days |
| `BCrypt:WorkFactor` | `12` | Password hashing cost (10–15 recommended) |

Configuration is loaded from `appsettings.json`, overridden by `appsettings.Development.json` in development, and can be further overridden via environment variables using the standard `__` separator (e.g. `Auth__Cookie__ExpireDays=7`).

### DynamoDB startup behavior

When `PERSISTENCE_PROVIDER=dynamodb`:

1. The application bypasses `PayslipDbContext` registration and EF Core migration startup.
2. Startup provisions these tables automatically if they are missing:
    - `{prefix}_users`
    - `{prefix}_companies`
    - `{prefix}_employees`
    - `{prefix}_employee_loans`
    - `{prefix}_payslips`
    - `{prefix}_payslip_loan_deductions`
    - `{prefix}_wallets`
    - `{prefix}_wallet_activities`
    - `{prefix}_wallet_topup_attempts`
    - `{prefix}_payment_return_evidences`
    - `{prefix}_outcome_normalization_decisions`
    - `{prefix}_unmatched_payment_return_records`
    - `{prefix}_payslip_pricing`
3. Explicit AWS credentials win when both are set.
4. If only one explicit credential variable is set, startup fails fast.
5. If `DYNAMODB_ENDPOINT` is set and explicit credentials are absent, the app uses dummy credentials for local emulators.
6. If `DYNAMODB_ENDPOINT` is absent and explicit credentials are absent, the AWS SDK default credential chain is used.

**Operational notes:**
- Cross-provider data migration is out of scope. Switching providers does not migrate existing data.
- Hosted AWS environments must allow the application identity to call `CreateTable`, `DescribeTable`, and related DynamoDB APIs during startup provisioning.

---

## Running Tests

```bash
# Run all non-DynamoDB-local tests
dotnet test --filter "Category!=Integration"

# Run with code coverage
dotnet test --filter "Category!=Integration" --collect:"XPlat Code Coverage"
```

The test suite covers all four layers (128 tests total across Domain, Application, Infrastructure, and Web).

For DynamoDB-specific work, prefer non-integration validation unless a local emulator is running:

```bash
dotnet test tests/Payslip4All.Infrastructure.Tests/Payslip4All.Infrastructure.Tests.csproj --filter "Category!=Integration"
dotnet test tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj
```

When `DYNAMODB_ENDPOINT` is configured for a local emulator, you can additionally run the DynamoDB integration repository suite.

```bash
dotnet test tests/Payslip4All.Infrastructure.Tests/Payslip4All.Infrastructure.Tests.csproj --filter "Category=Integration"
```

Coverage requirements (enforced in CI):
- **Domain layer** — ≥ 80% line coverage
- **Application layer** — ≥ 80% line coverage

---

## CI/CD

GitHub Actions workflow at `.github/workflows/ci.yml` runs on every push to `main` and on pull requests targeting `main`.

**Pipeline steps:**

1. Restore dependencies
2. Build in Release mode (`TreatWarningsAsErrors=true`)
3. Run all tests with XPlat code coverage
4. Enforce ≥ 80% coverage on Domain + Application layers
5. Upload coverage report (HTML) and test results (TRX) as artifacts

---

## Architecture

Payslip4All follows **Clean Architecture** with strict layer dependencies:

```
Web  →  Application  →  Domain
         ↑
   Infrastructure
```

- **Domain** — core business rules, no external dependencies
- **Application** — use cases and service interfaces (depends on Domain only)
- **Infrastructure** — EF Core, QuestPDF, BCrypt implementations (depends on Application + Domain)
- **Web** — Blazor Server UI and Razor Pages for auth (depends on Infrastructure)

**Auth note:** Login, Register, and Logout are implemented as Razor Pages (`.cshtml`) rather than Blazor components because Blazor Server runs over SignalR and cannot call `HttpContext.SignInAsync()` / `SignOutAsync()` during the render cycle. All other pages are Blazor components.

---

## Logging

Payslip4All uses [Serilog](https://serilog.net/) for structured file logging with global exception handling.

### Log files

Log files are written to the `logs/` directory (relative to the application's working directory) and rotate daily:

```
logs/payslip4all-20260319.log
logs/payslip4all-20260320.log
...
```

Files are retained for **31 days** by default. The `logs/` directory is created automatically on first write.

### Configuring log levels

Log levels are controlled via `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

Use `appsettings.Development.json` to increase verbosity locally (e.g., `"Default": "Debug"`) without changing production config.

### Global exception handling

All unhandled exceptions are intercepted by `GlobalExceptionMiddleware`, which:

1. Logs the exception at `Error` level with request path, HTTP method, and authenticated user ID.
2. Returns a user-friendly 500 response — stack traces are never exposed to the client.
3. For API paths (`/payslips/...`), returns `{"error":"An unexpected error occurred."}` JSON; otherwise returns a plain-text message.

---

## License

QuestPDF is used under the [Community License](https://www.questpdf.com/license/community.html) (free for qualifying projects).
