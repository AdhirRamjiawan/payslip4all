# Payslip4All - Payslip Generation System

A full-stack web application for generating and managing payslips for South African low-income employees.

## Project Overview

**Payslip4All** is a comprehensive payslip management system built with:
- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: Angular 17+ (Standalone Components)
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT-based with role-based access control

## Architecture

The application follows a layered architecture pattern:

```
Angular SPA (Standalone Components)
    ↓ (REST API over HTTPS)
ASP.NET Core Web API (Controllers, Middleware, Filters)
    ↓
Application Layer (Services, DTOs, Interfaces)
    ↓
Domain Layer (Entities, Value Objects, Enums)
    ↓
Infrastructure Layer (EF Core, Repositories, Authentication)
    ↓
SQL Server Database
```

## Backend Structure

### Project Layout
```
src/
├── Payslip4All.Api/              # API Layer - Controllers, Middleware
├── Payslip4All.Application/      # Application Layer - Services, DTOs, Validators
├── Payslip4All.Domain/           # Domain Layer - Entities, Enums
└── Payslip4All.Infrastructure/   # Infrastructure - EF Core, Repositories, Auth
```

### Key Components

#### Entities (Domain)
- **User**: Admin and CompanyOwner roles
- **Company**: Owned by CompanyOwner, contains Employees
- **Employee**: Belongs to Company, has Payslips and Loans
- **Payslip**: Monthly salary document with deductions
- **Loan**: Tracked loan deductions from payslips
- **Deduction**: Line items on payslips (UIF, Loan, Tax, Other)

#### Services (Application)
- **AuthService**: Login, token validation
- **UserService**: Admin user management
- **CompanyService**: Company CRUD with ownership verification
- **EmployeeService**: Employee management
- **PayslipService**: Payslip generation with UIF calculations (1%)
- **LoanService**: Loan tracking and deduction management

#### Repositories (Infrastructure)
- Implements repository pattern for all entities
- EF Core for database access
- Supports async/await throughout

#### API Controllers (6 Controllers, 26 Endpoints)
1. **AuthController** (1 endpoint)
   - `POST /api/auth/login` - User authentication

2. **UsersController** (6 endpoints - Admin only)
   - User management, password reset, activation/deactivation

3. **CompaniesController** (5 endpoints)
   - CRUD operations, ownership-based filtering

4. **EmployeesController** (5 endpoints)
   - Employee management under companies

5. **PayslipsController** (4 endpoints)
   - Payslip generation, retrieval, updates

6. **LoansController** (5 endpoints)
   - Loan creation, tracking, and cancellation

### Authentication & Authorization
- JWT tokens with 60-minute expiration
- Roles: Admin, CompanyOwner
- Password hashing with BCrypt
- Role-based access control on sensitive endpoints
- Ownership verification prevents unauthorized data access

## Frontend Structure

### Project Layout
```
src/frontend/Payslip4All.Web/src/app/
├── core/                         # Global services, guards, interceptors
│   ├── services/                 # AuthService, ApiService
│   ├── guards/                   # AuthGuard, RoleGuard
│   └── interceptors/             # JwtInterceptor, ErrorInterceptor
├── features/                     # Feature modules
│   ├── auth/                     # Login component
│   ├── admin/                    # Company owner management
│   ├── company/                  # Company CRUD
│   ├── employee/                 # Employee CRUD, Loan management
│   ├── payslip/                  # Payslip viewing, generation
│   └── dashboard/                # Main dashboard
├── shared/                       # Shared utilities
│   └── models/                   # API DTOs and interfaces
└── app.routes.ts                 # Lazy-loaded routing
```

### Key Features

#### Services
- **AuthService**: Token management, login/logout
- **ApiService**: Base HTTP service with automatic `/api/` prefix
- **CompanyService, EmployeeService, PayslipService, LoanService**: Feature-specific services
- **UserService**: Admin user management

#### Guards
- **AuthGuard**: Redirects unauthenticated users to login
- **RoleGuard**: Enforces role-based access control

#### Interceptors
- **JwtInterceptor**: Adds `Authorization: Bearer <token>` header
- **ErrorInterceptor**: Centralized error handling (401 logout, 403 redirect, 500 errors)

#### Components
- **LoginComponent**: User authentication with form validation
- **DashboardComponent**: Main landing page with role-based navigation
- **CompanyListComponent**: List and manage companies
- **CompanyDetailComponent**: Create/edit company
- **EmployeeListComponent**: List and manage employees
- **EmployeeDetailComponent**: Create/edit employee
- **PayslipListComponent**: View payslips for employee
- **PayslipDetailComponent**: View detailed payslip breakdown
- **CompanyOwnersComponent**: Admin interface for managing company owners

## Setup & Running

### Backend Setup

1. **Prerequisites**
   - .NET 8 SDK
   - SQL Server or SQL Server Express
   - Visual Studio or VS Code

2. **Configure Database**
   - Update connection string in `appsettings.json`:
     ```json
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=Payslip4All;..."
     }
   ```

3. **Run Migrations**
   ```bash
   cd src/Payslip4All.Api
   dotnet ef database update
   ```

4. **Start API**
   ```bash
   dotnet run
   ```
   - API runs on `https://localhost:5001`
   - Swagger UI: `https://localhost:5001/swagger`

### Frontend Setup

1. **Prerequisites**
   - Node.js 18+
   - Angular CLI 17+

2. **Install Dependencies**
   ```bash
   cd src/frontend/Payslip4All.Web
   npm install
   ```

3. **Configure Backend URL**
   - Update `core/services/api.service.ts` if backend URL differs
   - Default: `https://localhost:5001/api`

4. **Start Development Server**
   ```bash
   ng serve
   ```
   - Frontend runs on `http://localhost:4200`

## User Roles & Workflows

### Site Administrator
- Login with Admin role
- Navigate to `/admin/company-owners`
- Create, update, deactivate company owners
- Reset company owner passwords

### Company Owner
- Login with CompanyOwner role
- View and manage their companies
- Create and manage employees within their companies
- Generate payslips for employees
- View payslip details
- Manage employee loans

## Business Rules

### Payslip Generation
- Monthly payslip for each active employee
- Base salary + bonuses = gross pay
- UIF contribution: 1% of base salary (for low-income earners)
- Loan deductions: Automatically calculated from active loans
- Net pay = Gross pay - All deductions - Advanced pay

### Loan Management
- Employees can have multiple active loans
- Monthly deduction amount specified per loan
- Loan automatically marked paid-off when remaining amount reaches zero
- Loan deductions appear in payslips

### Data Isolation
- Company owners only see their own companies and employees
- Endpoint filters automatically based on current user
- Authorization checks prevent cross-company data access

## API Examples

### Login
```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "owner@example.com",
  "password": "password123"
}

Response: { token, user }
```

### Create Company
```bash
POST /api/companies
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "ACME Corp",
  "registrationNumber": "2024/001234",
  "address": "123 Main St",
  "city": "Cape Town",
  "postalCode": "8000",
  "contactEmail": "admin@acme.co.za",
  "contactPhone": "+27211234567"
}
```

### Generate Payslip
```bash
POST /api/payslips
Authorization: Bearer <token>
Content-Type: application/json

{
  "employeeId": 1,
  "year": 2024,
  "month": 3,
  "bonus": 500,
  "advancedPay": 0
}
```

## Security Features

- ✅ JWT authentication with expiration
- ✅ BCrypt password hashing
- ✅ Role-based access control
- ✅ Ownership verification for data access
- ✅ HTTPS enforced on all endpoints
- ✅ CORS configured for frontend origin
- ✅ Input validation on all endpoints
- ✅ Error messages don't leak sensitive information

## Development Notes

### Code Patterns
- **Dependency Injection**: All services injected via constructor or `inject()`
- **Async/Await**: Used throughout backend for non-blocking operations
- **DTOs**: All API communication uses DTOs, not domain entities
- **Repositories**: Abstract data access through repository interfaces
- **Guards**: Route-level authorization with CanActivateFn
- **Interceptors**: Cross-cutting concerns handled via HTTP interceptors

### Database Migrations
```bash
# Create new migration
dotnet ef migrations add MigrationName -p src/Payslip4All.Infrastructure

# Apply migrations
dotnet ef database update -p src/Payslip4All.Infrastructure
```

### Testing
- Backend: Not included in this implementation (can be added with xUnit/NUnit)
- Frontend: Not included in this implementation (can be added with Jasmine/Karma)

## Deployment Considerations

- Set strong JWT secret in production
- Use environment-specific configuration files
- Enable HTTPS with valid certificates
- Configure CORS for actual frontend domain
- Use connection pooling for database
- Implement rate limiting for API endpoints
- Add logging and monitoring
- Backup database regularly

## Future Enhancements

- [ ] PDF payslip export
- [ ] Email payslip delivery
- [ ] Scheduled payslip generation
- [ ] Advanced tax calculations
- [ ] Multi-currency support
- [ ] Payslip templates customization
- [ ] Bulk employee import
- [ ] Analytics dashboard
- [ ] Audit logging
- [ ] Two-factor authentication

## Support

For questions or issues, refer to:
- `ARCHITECTURE.md` - Technical architecture details
- `CONTRIBUTING.md` - Development guidelines
- API Swagger docs: `https://localhost:5001/swagger`

## License

This project is proprietary software for Payslip4All.

---

**Built with**: ASP.NET Core 8, Angular 17+, Entity Framework Core, SQL Server, JWT, BCrypt
# payslip4all
