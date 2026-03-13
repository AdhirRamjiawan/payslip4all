# ARCHITECTURE.md

## Overview

This repository contains a full-stack web application consisting of:

-   Backend: ASP.NET Core Web API (.NET 8)
-   Frontend: Angular
-   Authentication: Username/password with JWT tokens
-   Communication: REST API over HTTPS
-   Data Storage: SQLite database accessed via Entity Framework Core

The architecture follows a layered approach to ensure separation of
concerns and maintainability.

High-level structure:

Angular SPA\
→ REST API (HTTPS)\
→ ASP.NET Core Web API\
→ Application Layer\
→ Domain Layer\
→ Infrastructure Layer\
→ SQLite Database

Copilot should follow the patterns described in this document when
generating new code.

------------------------------------------------------------------------

# Backend Architecture (ASP.NET Core)

## Project Structure

/src /Api Controllers Middleware Filters Program.cs

/Application Services DTOs Interfaces Validators

/Domain Entities ValueObjects Enums

/Infrastructure Persistence Repositories Authentication ExternalServices

## Layer Responsibilities

### API Layer

Responsible for HTTP concerns.

Contains:

-   Controllers
-   Request/response mapping
-   Authorization attributes
-   Model binding

Controllers must remain thin and delegate work to application services.

### Application Layer

Contains business logic.

Includes:

-   Application services
-   DTOs
-   Validation
-   Interfaces for repositories

Services orchestrate use cases but should not contain database access
logic.

### Domain Layer

Contains the core domain model.

Includes:

-   Entities
-   Value objects
-   Enums
-   Domain rules

This layer must not depend on infrastructure or ASP.NET.

### Infrastructure Layer

Implements external dependencies.

Includes:

-   EF Core DbContext
-   Repository implementations
-   Authentication services
-   External integrations

Infrastructure depends on the domain and application layers.

------------------------------------------------------------------------

# Authentication Architecture

Authentication uses JWT tokens.

Flow:

User submits username + password\
→ POST /api/auth/login\
→ Server validates credentials\
→ JWT token issued\
→ Angular stores token\
→ Token attached to Authorization header

Example header:

Authorization: Bearer `<jwt-token>`{=html}

JWT tokens should contain:

-   userId
-   username
-   roles
-   expiration

Passwords must be stored using secure hashing such as BCrypt or ASP.NET
Identity password hasher.

Never store plaintext passwords.

------------------------------------------------------------------------

# API Design Guidelines

Endpoints follow REST conventions.

Example structure:

/api/auth/login\
/api/users\
/api/users/{{id}}\
/api/products

Controllers should:

-   return ActionResult`<T>`{=html}
-   use DTOs instead of domain entities
-   validate input using model validation

Example:

\[HttpPost("login")\] public async
Task\<ActionResult`<AuthResponseDto>`{=html}\> Login(LoginRequestDto
request)

------------------------------------------------------------------------

# Dependency Injection

ASP.NET Core dependency injection must be used.

Services should be registered in Program.cs.

Example:

services.AddScoped\<IUserService, UserService\>();
services.AddScoped\<IUserRepository, UserRepository\>();

Controllers must depend on interfaces, not concrete implementations.

------------------------------------------------------------------------

# Data Access

Use Entity Framework Core.

Guidelines:

-   DbContext lives in Infrastructure
-   Entities live in Domain
-   Repositories live in Infrastructure
-   Application layer depends on repository interfaces

Example repository interface:

public interface IUserRepository { Task\<User?\>
GetByUsernameAsync(string username); Task`<User>`{=html} AddAsync(User
user); }

------------------------------------------------------------------------

# Angular Architecture

The Angular application is a Single Page Application (SPA).

Structure:

/src/app /core services interceptors guards

    /features
        auth
        users
        dashboard

    /shared
        components
        models
        utilities

### Core Module

Contains global services such as:

-   AuthService
-   HTTP interceptors
-   Route guards

### Feature Modules

Each feature contains:

-   components
-   services
-   routing
-   models

Example:

features/auth\
login.component\
auth.service.ts\
auth.models.ts

------------------------------------------------------------------------

# Angular Authentication Flow

Authentication uses JWT.

Flow:

User logs in\
→ AuthService calls /api/auth/login\
→ Token stored in memory or localStorage\
→ HTTP interceptor attaches token to requests

Interceptor adds:

Authorization: Bearer `<token>`{=html}

Protected routes use an AuthGuard.

------------------------------------------------------------------------

# HTTP Communication

Angular services should call backend endpoints via HttpClient.

Example:

login(request: LoginRequest): Observable`<AuthResponse>`{=html} { return
this.http.post`<AuthResponse>`{=html}("/api/auth/login", request); }

All API calls should be centralized in services.

Components must not call HTTP directly.

------------------------------------------------------------------------

# Error Handling

Backend:

-   Use middleware for global exception handling
-   Return consistent error responses

Example format:

{ "message": "Invalid credentials", "status": 401 }

Frontend:

-   Use an HTTP interceptor to handle errors
-   Display user-friendly messages

------------------------------------------------------------------------

# Security Guidelines

Always follow these rules:

-   Never expose internal entities directly through the API
-   Always validate input
-   Use HTTPS only
-   Use password hashing
-   Use JWT expiration
-   Validate tokens on every request

------------------------------------------------------------------------

# Coding Guidelines

Backend:

-   Prefer async/await
-   Use DTOs for API communication
-   Keep controllers thin
-   Business logic belongs in services

Frontend:

-   Components should remain presentation-focused
-   Services handle data access
-   Use strong typing with interfaces

------------------------------------------------------------------------

# Copilot Instructions

When generating code:

1.  Follow the layered architecture defined here.
2.  Place business logic in Application services.
3.  Do not place database logic in controllers.
4.  Use DTOs between API and frontend.
5.  Use JWT authentication patterns described above.
6.  Use dependency injection for services.
7.  Follow Angular feature module structure.

------------------------------------------------------------------------

# Database Architecture (SQLite)

## Overview

The application uses **SQLite** as the database engine, providing a lightweight,
file-based relational database that requires no server installation.

## Database File

- **Location**: `payslip4all.db` (in application root directory)
- **Type**: File-based SQLite database
- **Creation**: Automatically created on first run via Entity Framework Core migrations

## Connection String

```
Data Source=payslip4all.db
```

## Why SQLite?

### Advantages
- ✅ **Zero Configuration**: No server setup required
- ✅ **Portability**: Single file database, easy to backup and distribute
- ✅ **Development**: Perfect for local development without external dependencies
- ✅ **Deployment**: Can be included in deployment package
- ✅ **Cross-Platform**: Works on Windows, Linux, macOS
- ✅ **Performance**: Sufficient for small to medium organizations
- ✅ **Reliability**: ACID-compliant transactions

### Limitations
- ⚠️ **Concurrency**: Limited for high-concurrency scenarios (designed for moderate concurrent access)
- ⚠️ **Scalability**: Not suitable for extremely large datasets (typically >10GB)
- ⚠️ **User Management**: No built-in user authentication (handled by application)

## When to Consider SQL Server

If the application scales to require:
- Multiple concurrent users (>100 simultaneous)
- Distributed database replicas
- Complex administration and monitoring
- Datasets exceeding 10GB
- High-availability requirements

Migration to SQL Server is straightforward as Entity Framework Core abstracts the database provider.

## Entity Framework Core Configuration

### Database Provider
```csharp
options.UseSqlite(connectionString)
```

### Data Types
SQLite supports standard data types:
- INTEGER (int, long)
- REAL (decimal, float, double)
- TEXT (string)
- BLOB (byte arrays)

### Constraints
- Foreign Keys: Supported (enabled by default)
- Unique Indexes: Supported
- Check Constraints: Supported
- Default Values: Supported

## Migrations

### Create Migration
```bash
dotnet ef migrations add MigrationName
```

### Apply Migration
```bash
dotnet ef database update
```

### View Migrations
```bash
dotnet ef migrations list
```

The SQLite database is automatically created on first migration application.

## Backup & Restore

### Backup
Simply copy the `payslip4all.db` file to a backup location.

```bash
cp payslip4all.db payslip4all.backup.db
```

### Restore
Replace the current database file with the backup.

```bash
cp payslip4all.backup.db payslip4all.db
```

## File Size Management

SQLite includes built-in transaction management. Database files may grow with operations
and can be optimized using:

```sql
VACUUM;
```

This rebuilds the database and reclaims unused space. EF Core does not expose this directly,
so it would need custom migration or direct SQL execution.

------------------------------------------------------------------------
