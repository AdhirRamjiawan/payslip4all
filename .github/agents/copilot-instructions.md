# payslip4all Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-03-29

## Active Technologies
- C# 12 / .NET 8 (LTS) + Blazor Server (ASP.NET Core 8), Entity Framework Core 8, QuestPDF 2024.10.4, BCrypt.Net-Next 4.0.3, Pomelo.EntityFrameworkCore.MySql 8.0.2 (001-payslip-generation)
- SQLite (development default, `Data Source=payslip4all.db`); MySQL/MariaDB or SQLite (production — provider swap via `DatabaseProvider` config key, zero code changes) (001-payslip-generation)
- C# 12 / .NET 8 LTS + Blazor Server (ASP.NET Core 8), Entity Framework Core 8, (001-payslip-generation)
- SQLite (development default); MySQL via Pomelo (production) — provider switched (001-payslip-generation)
- C# 12 / .NET 8 (LTS) + ASP.NET Core 8 Blazor Server, Entity Framework Core 8, BCrypt.Net-Next (v4), QuestPDF (community licence), Pomelo.EntityFrameworkCore.MySql (001-payslip-generation)
- SQLite (local development default); MySQL via `Pomelo.EntityFrameworkCore.MySql` in production — provider selected at startup via `appsettings.json` key `"DatabaseProvider"` (001-payslip-generation)
- C# 12 / .NET 8 (LTS) + QuestPDF 2024.10.4 (already in `Payslip4All.Infrastructure.csproj`); no new NuGet packages required (001-payslip-generation)
- SQLite (dev) / MySQL (prod) via EF Core — no schema changes for the PDF layout itself; optional `Company` fields (`UifNumber`, `SarsPayeNumber`) require a new EF Core migration (001-payslip-generation)
- C# 12 / .NET 8 (LTS) + `Serilog.AspNetCore` 8.x (includes `Serilog.Sinks.File`, `Serilog.Settings.Configuration`, enrichers) (002-serilog-logging)
- File system — rolling daily log files at `logs/payslip4all-[date].log` (002-serilog-logging)
- C# 12 / .NET 8 + ASP.NET Core 8 (`Microsoft.NET.Sdk.Web`), Blazor Server, xUnit, bUni (003-wwwroot-static-files-fix)
- N/A (no database changes) (003-wwwroot-static-files-fix)
- C# 12 / .NET 8 + MSBuild (in-SDK), ASP.NET Core 8, xUni (004-wwwroot-hosting-manifest-fix)
- C# 12 / .NET 8 + Serilog.AspNetCore 10.x (already installed) (005-file-logging-published-fix)
- C# / .NET 8 (LTS) + `AWSSDK.DynamoDBv2` (approved in constitution amendment v1.3.0); (006-dynamodb-persistence)
- DynamoDB (multi-table design; 6 tables); SQLite and MySQL paths unchanged (006-dynamodb-persistence)
- C# 12 / .NET 8 (LTS) + `AWSSDK.DynamoDBv2`, Entity Framework Core 8 (retained for SQLite/MySQL), Serilog, xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing` (006-dynamodb-persistence)
- SQLite (default), MySQL, or AWS DynamoDB selected by `PERSISTENCE_PROVIDER`; DynamoDB uses six auto-provisioned tables with optional `DYNAMODB_TABLE_PREFIX` (006-dynamodb-persistence)

- C# 12 / .NET 8 (LTS) + ASP.NET Core Blazor Server, Entity Framework Core 8, QuestPDF, BCrypt.Net-Next, Pomelo.EntityFrameworkCore.MySql, Microsoft.EntityFrameworkCore.Sqlite, bUnit, xUnit, Moq (001-payslip-generation)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for C# 12 / .NET 8 (LTS)

## Code Style

C# 12 / .NET 8 (LTS): Follow standard conventions

## Recent Changes
- 006-dynamodb-persistence: Added C# 12 / .NET 8 (LTS) + `AWSSDK.DynamoDBv2`, Entity Framework Core 8 (retained for SQLite/MySQL), Serilog, xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing`
- 006-dynamodb-persistence: Added C# / .NET 8 (LTS) + `AWSSDK.DynamoDBv2` (approved in constitution amendment v1.3.0);
- 005-file-logging-published-fix: Added C# 12 / .NET 8 + Serilog.AspNetCore 10.x (already installed)


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
