# Quickstart: Payslip Generation System (001)

**Branch**: `001-payslip-generation`  
**Date**: 2025-07-15

## Prerequisites

- .NET 8 SDK (`dotnet --version` → 8.x.x)
- Git

## Clone & Build

```bash
git clone <repo-url>
cd payslip4all
git checkout 001-payslip-generation
dotnet restore
dotnet build
```

## Run (SQLite — default)

```bash
cd src/Payslip4All.Web
dotnet run
```

Open https://localhost:5001 in your browser.

## Run (MySQL)

Edit `src/Payslip4All.Web/appsettings.Development.json`:

```json
{
  "DatabaseProvider": "mysql",
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=payslip4all;User=root;Password=yourpassword;"
  }
}
```

Then:
```bash
dotnet run
```

## Apply Migrations Manually

```bash
cd src/Payslip4All.Web
dotnet ef database update --project ../Payslip4All.Infrastructure
```

> Migrations are also applied automatically on startup via `MigrateAsync()`.

## Run Tests

```bash
dotnet test
```

Run with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Project Layout

```
src/
├── Payslip4All.Domain/          # Entities, domain services, enums (no dependencies)
├── Payslip4All.Application/     # Use cases, service interfaces, DTOs
├── Payslip4All.Infrastructure/  # EF Core, repositories, PDF, auth
└── Payslip4All.Web/             # Blazor Server pages and components

tests/
├── Payslip4All.Domain.Tests/
├── Payslip4All.Application.Tests/
├── Payslip4All.Infrastructure.Tests/
└── Payslip4All.Web.Tests/       # bUnit component tests
```

## Key Configuration Keys (`appsettings.json`)

| Key | Default | Description |
|-----|---------|-------------|
| `DatabaseProvider` | `"sqlite"` | `"sqlite"` or `"mysql"` |
| `ConnectionStrings:DefaultConnection` | `"Data Source=payslip4all.db"` | SQLite path |
| `ConnectionStrings:MySqlConnection` | `""` | MySQL connection string |
| `Auth:Cookie:ExpireDays` | `30` | Session cookie lifetime |
| `BCrypt:WorkFactor` | `12` | Password hashing cost |

## First Run Walkthrough

1. Navigate to https://localhost:5001/register
2. Create an employer account
3. Add a company from the dashboard
4. Add an employee to the company
5. (Optional) Add a loan to the employee
6. Generate and download a payslip PDF
