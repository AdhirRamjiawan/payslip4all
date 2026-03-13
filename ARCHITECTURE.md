# ARCHITECTURE.md - Payslip4All Blazor Server Edition

## Overview

This repository contains a full-stack web application built with:

- **Backend & Frontend**: ASP.NET Core 8 Blazor Server
- **Authentication**: Cookie-based with custom AuthenticationStateProvider
- **Data Storage**: SQLite database accessed via Entity Framework Core
- **Architecture**: Layered architecture with separation of concerns

The application is a **unified single-tier solution** where Blazor Server handles all UI rendering on the server while communicating with the client via WebSocket.

High-level structure:

```
Blazor Server (net8.0)
  ├── Razor Components (Pages, Shared)
  ├── Application Services (Authentication, Company, Employee)
  └── Infrastructure Layer
       ├── DbContext (Entity Framework Core)
       └── SQLite Database
```

---

## Architecture Layers

### Web/Presentation Layer (Payslip4All.Web)

Responsible for UI rendering and user interaction.

Contains:
- **Pages/**: Routable Razor components (@page directive)
- **Shared/**: Layout components (MainLayout.razor, NavMenu.razor, etc.)
- **Services/**: BlazorAuthenticationStateProvider for authentication state
- **App.razor**: Root component with cascading parameters

Key features:
- Server-side Razor components with event binding
- Built-in cascading authentication state
- Bootstrap CSS framework for styling
- Two-way data binding with @bind directive

### Application/Infrastructure Service Layer

Manages business logic and data access.

Contains:
- **Services/** in Infrastructure project:
  - `IAuthenticationService`: User registration and login
  - `ICompanyService`: Company CRUD operations  
  - `IEmployeeService`: Employee CRUD operations
- Each service implements repository pattern with DbContext

Services are:
- Registered in DI container as Scoped
- Injected into Razor components via @inject directive
- Database-agnostic (LINQ to Entities)

### Domain Layer (Payslip4All.Domain)

Core domain model and business rules.

Contains:
- **Entities/**:
  - `User`: Application users with credentials
  - `Company`: Company records with registration details
  - `Employee`: Employee records with contact and employment info
  - `EmployeeStatus`: Enum for employee states

Characteristics:
- No dependencies on infrastructure or web frameworks
- Pure C# classes with navigation properties
- Validation rules and constraints defined via EF Core

### Infrastructure Layer (Payslip4All.Infrastructure)

Data persistence and external dependencies.

Contains:
- **Persistence/PayslipDbContext**: EF Core DbContext for SQLite
- **Services/**: Application service implementations
- **Migrations/**: (Future) EF Core database migrations

Characteristics:
- Depends on Domain and Application layers
- Implements repository interfaces
- Configures EF Core entity relationships
- Manages database initialization

---

## Authentication Architecture

### Authentication Flow

1. User navigates to `/login` or `/register`
2. Register page: Creates new User entity with hashed password
3. Login page: Validates credentials, retrieves User from database
4. `BlazorAuthenticationStateProvider.LoginAsync()` called with authenticated User
5. Provider creates claims and signs in with ASP.NET Core Cookie authentication
6. `AuthenticationState` notified, UI updates based on authenticated user

### Authentication Components

**BlazorAuthenticationStateProvider** (src/Payslip4All.Web/Services/):
- Extends `AuthenticationStateProvider`
- Manages login/logout operations
- Tracks authenticated user claims
- Notifies Blazor of authentication state changes

**Login & Register Pages** (src/Payslip4All.Web/Pages/):
- Form-based user input
- Call `IAuthenticationService` for validation
- Redirect to dashboard on success
- Display errors on failure

### Security

- Passwords hashed with SHA256
- Cookie-based session management (30-day expiry)
- Claims-based authorization with @attribute [Authorize]
- HTTPS enforced in production

---

## Data Access

### Entity Framework Core with SQLite

**Connection String**:
```
Data Source=payslip4all.db
```

**DbContext** (Payslip4All.Infrastructure/Persistence/PayslipDbContext.cs):
- Manages all entity relationships
- Configures foreign keys and cascading deletes
- Initialized on application startup

**Key Relationships**:
- User has many Companies (1:N)
- Company has many Employees (1:N)
- Cascading delete: Company deletion removes related Employees

### LINQ Queries in Services

All data access through Entity Framework Core LINQ:

```csharp
// Example from CompanyService
var companies = await _dbContext.Companies
    .Where(c => c.UserId == userId && c.IsActive)
    .OrderByDescending(c => c.CreatedAt)
    .ToListAsync();
```

### Database Migrations

Future migrations (when schema changes):
```bash
dotnet ef migrations add MigrationName -p src/Payslip4All.Infrastructure
dotnet ef database update -p src/Payslip4All.Infrastructure
```

---

## Dependency Injection

Services registered in Program.cs:

```csharp
builder.Services.AddScoped<PayslipDbContext>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<BlazorAuthenticationStateProvider>();
```

**Scoped lifetime**: New instance per HTTP request (ideal for DbContext)

Injected into Razor components:
```razor
@inject ICompanyService CompanyService
@inject IAuthenticationService AuthService
```

---

## Razor Components

### Page Components (@page directive)

Located in `src/Payslip4All.Web/Pages/`:
- `/login` - User login form
- `/register` - User registration form
- `/` - Dashboard (future)
- `/companies` - Company list and CRUD (future)
- `/employees` - Employee management (future)

### Shared Layout Components

Located in `src/Payslip4All.Web/Shared/`:
- `MainLayout.razor` - Root layout with sidebar
- `NavMenu.razor` - Navigation menu
- `SurveyPrompt.razor` - Placeholder component

### Component Features

- **Two-way binding**: `@bind="Property"`
- **Event handlers**: `@onclick`, `@onsubmit`
- **Cascading parameters**: `[CascadingParameter]`
- **Dependency injection**: `@inject IService`
- **Authorization**: `@attribute [Authorize]`
- **Conditional rendering**: `@if`, `@foreach`

---

## Project Structure

```
Payslip4All/
├── src/
│   ├── Payslip4All.Domain/                (net8.0)
│   │   └── Entities/
│   │       ├── User.cs
│   │       ├── Company.cs
│   │       └── Employee.cs
│   │
│   ├── Payslip4All.Application/           (net8.0, future use)
│   │   └── (Placeholder for shared logic)
│   │
│   ├── Payslip4All.Infrastructure/        (net8.0)
│   │   ├── Persistence/
│   │   │   └── PayslipDbContext.cs
│   │   └── Services/
│   │       ├── AuthenticationService.cs
│   │       ├── CompanyService.cs
│   │       └── EmployeeService.cs
│   │
│   └── Payslip4All.Web/                   (net8.0 Blazor Server)
│       ├── Components/
│       ├── Pages/
│       │   ├── Login.razor
│       │   ├── Register.razor
│       │   └── Index.razor
│       ├── Shared/
│       │   ├── MainLayout.razor
│       │   └── NavMenu.razor
│       ├── Services/
│       │   └── BlazorAuthenticationStateProvider.cs
│       ├── Program.cs
│       ├── App.razor
│       ├── appsettings.json
│       └── wwwroot/
│           ├── css/
│           └── js/
│
├── payslip4all.db                         (SQLite database)
├── Payslip4All.sln
├── global.json
└── ARCHITECTURE.md
```

---

## Why Blazor Server Instead of Angular + API?

### Benefits of Unified Architecture

| Aspect | Blazor Server | Angular + API |
|--------|---------------|---------------|
| **Language** | C# throughout | TypeScript + C# |
| **Build Complexity** | Single dotnet build | npm + dotnet |
| **Startup Scripts** | 1 command | 2+ commands |
| **Code Reuse** | Domain entities directly in components | API DTOs + frontend models |
| **Deployment** | Single deployable project | Frontend SPA + API server |
| **Learning Curve** | Single language/framework | Multiple paradigms |
| **Real-time** | Built-in WebSocket | Requires SignalR library |
| **Bundle Size** | ~5MB app | Angular SPA |

### Trade-offs

**Advantages**:
- Simpler architecture
- Faster development
- No API contract management
- Single technology stack
- Easier deployment

**Limitations**:
- Server-side rendering (not suitable for very high concurrency)
- Less client-side flexibility
- Blazor Server requires persistent WebSocket connections

---

## Configuration Files

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=payslip4all.db"
  },
  "Logging": { ... },
  "AllowedHosts": "*"
}
```

### Program.cs

Configures:
- Razor Pages and Blazor services
- Entity Framework Core with SQLite
- Authentication and authorization
- Dependency injection
- Database initialization

---

## Coding Guidelines

### Backend (C#)

- Prefer async/await for I/O operations
- Use DTOs/ViewModels for complex data transfer
- Keep controllers (API endpoints) thin
- Business logic in services
- Domain rules in entities

### Frontend (Razor)

- Components remain presentation-focused
- Inject services for data access
- Use @bind for two-way binding
- Implement loading states
- Show validation errors to user

### Database

- Use EF Core for data access
- No raw SQL (use LINQ)
- Define relationships in DbContext.OnModelCreating
- Use migrations for schema changes

---

## Deployment

### Local Development

```bash
cd src/Payslip4All.Web
dotnet run
```

Runs on: `https://localhost:7035` (or next available port)

### Production

```bash
dotnet publish -c Release
```

Deploy the `bin/Release/net8.0/publish` folder to hosting provider.

---

## Future Enhancements

- [ ] Payslip generation module
- [ ] Employee search and filtering
- [ ] Bulk employee import (CSV)
- [ ] Attendance tracking
- [ ] Role-based authorization (Admin, Manager, Employee)
- [ ] Email notifications
- [ ] Audit logging
- [ ] API export (JSON/CSV)

---

## Migrations to SQL Server (if needed)

If scaling to SQL Server in future:

1. Install NuGet package: `Microsoft.EntityFrameworkCore.SqlServer`
2. Update Program.cs connection string and provider:
   ```csharp
   options.UseSqlServer(connectionString)
   ```
3. Update database migrations
4. No code changes needed (EF Core abstraction)

---

## Support & Documentation

- **EF Core**: https://learn.microsoft.com/en-us/ef/core/
- **Blazor**: https://learn.microsoft.com/en-us/aspnet/core/blazor/
- **ASP.NET Core**: https://learn.microsoft.com/en-us/aspnet/core/

---

**Last Updated**: March 13, 2026  
**Architecture**: Blazor Server (Unified Single-Tier)  
**Status**: Phase 3 Complete - Authentication & Infrastructure
