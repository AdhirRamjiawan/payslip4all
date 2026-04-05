# payslip4all Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-05

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
- C# / .NET 8 + ASP.NET Core Blazor Server, Entity Framework Core 8, AWSSDK.DynamoDBv2, xUnit, Moq, bUnit, QuestPDF, Serilog (007-wallet-credits)
- SQLite/MySQL via EF Core migrations by default, DynamoDB via `PERSISTENCE_PROVIDER=dynamodb` exception path (007-wallet-credits)
- C# / .NET 8 + ASP.NET Core Blazor Server, Entity Framework Core 8, AWSSDK.DynamoDBv2, xUnit, Moq, bUnit, Serilog, QuestPDF (008-wallet-card-topup)
- SQLite/MySQL through EF Core migrations by default, DynamoDB through the existing `PERSISTENCE_PROVIDER=dynamodb` exception path, with new wallet top-up attempt persistence added to both paths (008-wallet-card-topup)
- C# 12 / .NET 8 (LTS) + ASP.NET Core Blazor Server, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, xUnit, Moq, bUnit, Serilog, `Microsoft.AspNetCore.Mvc.Testing` (008-wallet-card-topup)
- SQLite/MySQL through EF Core migrations by default; DynamoDB through the approved `PERSISTENCE_PROVIDER=dynamodb` exception path, with wallet top-up attempt and unmatched-return persistence added to both paths (008-wallet-card-topup)
- C# / .NET 8 + ASP.NET Core Blazor Server, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, cookie authentication, Serilog, xUnit, Moq, bUni (008-wallet-card-topup)
- SQLite/MySQL via EF Core for relational providers; AWS DynamoDB via repository implementations when `PERSISTENCE_PROVIDER=dynamodb` (008-wallet-card-topup)
- SQLite/MySQL through EF Core migrations; DynamoDB through parallel Infrastructure repositories selected by `PERSISTENCE_PROVIDER=dynamodb` (008-wallet-card-topup)
- C# / .NET 8 + ASP.NET Core Blazor Server, Entity Framework Core 8, AWSSDK.DynamoDBv2, Serilog, xUnit, Moq, bUni (008-wallet-card-topup)
- SQLite for local EF Core development, MySQL / SQL Server via EF Core configuration, DynamoDB via approved provider path (008-wallet-card-topup)
- C# 12 on .NET 8 Blazor Server + ASP.NET Core Blazor Server, Entity Framework Core 8, AWSSDK.DynamoDBv2, Serilog, xUnit, Moq, bUnit, PayFast hosted checkout via form-post + ITN validation using built-in `HttpClient` and .NET cryptography (009-payfast-card-integration)
- SQLite/MySQL through EF Core and DynamoDB through existing repository implementations; wallet top-up attempts, payment evidence, normalization decisions, unmatched return records, and wallet activity ledger entries persist audit state (009-payfast-card-integration)
- C# / .NET 8 + ASP.NET Core Blazor Server, Entity Framework Core 8, AWSSDK.DynamoDBv2, Serilog, xUnit, Moq, bUnit, built-in `HttpClient` + hashing utilities for PayFast signing/validation (009-payfast-card-integration)
- SQLite for local EF Core development, MySQL / SQL Server via EF Core provider swap, DynamoDB via approved provider path; existing wallet top-up attempts, evidence, normalization decisions, unmatched returns, wallets, and wallet activities remain the persistence anchors (009-payfast-card-integration)
- C# / .NET 8 Blazor Server + ASP.NET Core Blazor Server, Entity Framework Core 8, xUnit, Moq, bUnit, Serilog, PayFast hosted-payment integration via `IHostedPaymentProvider` (009-payfast-card-integration)
- EF Core-backed SQLite/MySQL/SQL Server path plus DynamoDB provider parity; payment/top-up audit data persisted via existing repository abstractions (009-payfast-card-integration)
- C# 12 / .NET 8 (LTS) + ASP.NET Core 8 Blazor Server, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, Serilog, xUnit, Moq, bUnit, built-in `HttpClient` plus .NET hashing/validation utilities behind `IHostedPaymentProvider` (009-payfast-card-integration)
- SQLite/MySQL/SQL Server via EF Core for relational providers; DynamoDB via `PERSISTENCE_PROVIDER=dynamodb`; persisted audit state in wallet top-up attempts, payment evidence, normalization decisions, unmatched return records, and wallet activity ledger entries (009-payfast-card-integration)
- C# 12 / .NET 8 LTS + ASP.NET Core Blazor Server, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, Serilog, xUnit, Moq, bUni (009-payfast-card-integration)
- SQLite by default, MySQL via EF Core, or DynamoDB via `PERSISTENCE_PROVIDER=dynamodb`; payment flows rely on `wallet_topup_attempts`, `payment_return_evidences`, `outcome_normalization_decisions`, `unmatched_payment_return_records`, `wallets`, and `wallet_activities` (009-payfast-card-integration)
- C# / .NET 8 + ASP.NET Core Blazor Server, Entity Framework Core 8, AWSSDK.DynamoDBv2, Serilog, xUnit, Moq, bUnit, BCrypt.Net-Next, QuestPDF (009-payfast-card-integration)
- C# 12 on .NET 8 (`net8.0`) + ASP.NET Core 8 Blazor Server, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, Serilog, `IHttpClientFactory`, PayFast hosted checkout integration (009-payfast-card-integration)
- SQLite for local relational development, MySQL/other EF Core relational providers in supported deployments, and DynamoDB when `PERSISTENCE_PROVIDER=dynamodb`; payment audit data is persisted as wallet top-up attempts, payment return evidence, normalization decisions, unmatched return records, wallets, and wallet activities (009-payfast-card-integration)
- C# 12 on .NET 8 / ASP.NET Core 8 Blazor Web App + ASP.NET Core Blazor Server, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, Serilog, xUnit, bUnit, Moq, PayFast hosted-payment integration (009-payfast-card-integration)
- SQLite/MySQL via EF Core migrations; DynamoDB via repository implementations and startup table verification when `PERSISTENCE_PROVIDER=dynamodb` (009-payfast-card-integration)

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
- 009-payfast-card-integration: Added C# 12 on .NET 8 / ASP.NET Core 8 Blazor Web App + ASP.NET Core Blazor Server, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, Serilog, xUnit, bUnit, Moq, PayFast hosted-payment integration
- 009-payfast-card-integration: Added C# 12 on .NET 8 (`net8.0`) + ASP.NET Core 8 Blazor Server, Entity Framework Core 8, `AWSSDK.DynamoDBv2`, Serilog, `IHttpClientFactory`, PayFast hosted checkout integration
- 009-payfast-card-integration: Added C# / .NET 8 + ASP.NET Core Blazor Server, Entity Framework Core 8, AWSSDK.DynamoDBv2, Serilog, xUnit, Moq, bUnit, BCrypt.Net-Next, QuestPDF


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
