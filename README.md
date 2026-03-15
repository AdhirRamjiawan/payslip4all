# Payslip4All

A web application for generating and managing employee payslips, built for South African employers. Supports multiple companies per employer, employee loan tracking, and PDF payslip generation with automatic UIF deduction calculations.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
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
- **Data isolation** — employers only ever see their own companies, employees, and payslips

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 8 / ASP.NET Core 8 |
| UI | Blazor Server (C# 12) |
| ORM | Entity Framework Core 8 |
| Database | SQLite (default) · MySQL 8+ (optional) |
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

Edit `src/Payslip4All.Web/appsettings.Development.json`:

```json
{
  "DatabaseProvider": "mysql",
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=payslip4all;User=root;Password=yourpassword;"
  }
}
```

Then run normally with `dotnet run`. Migrations are applied automatically on startup.

### First-run walkthrough

1. Navigate to `/Auth/Register` and create an employer account
2. Add a company from the dashboard
3. Add an employee to the company
4. *(Optional)* Add a loan to the employee
5. Generate a payslip and download the PDF

---

## Configuration

| Key | Default | Description |
|-----|---------|-------------|
| `DatabaseProvider` | `"sqlite"` | `"sqlite"` or `"mysql"` |
| `ConnectionStrings:DefaultConnection` | `"Data Source=payslip4all.db"` | SQLite file path |
| `ConnectionStrings:MySqlConnection` | `""` | MySQL connection string |
| `Auth:Cookie:ExpireDays` | `30` | Session lifetime in days |
| `BCrypt:WorkFactor` | `12` | Password hashing cost (10–15 recommended) |

Configuration is loaded from `appsettings.json`, overridden by `appsettings.Development.json` in development, and can be further overridden via environment variables using the standard `__` separator (e.g. `Auth__Cookie__ExpireDays=7`).

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

The test suite covers all four layers (128 tests total across Domain, Application, Infrastructure, and Web).

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

## License

QuestPDF is used under the [Community License](https://www.questpdf.com/license/community.html) (free for qualifying projects).
