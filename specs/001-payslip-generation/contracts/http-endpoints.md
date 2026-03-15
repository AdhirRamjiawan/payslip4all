# HTTP Endpoints: Payslip Generation System (001)

**Phase**: 1 — Design  
**Date**: 2026-03-15  
**Project Type**: Blazor Server Web Application

This document defines the non-Blazor HTTP endpoints exposed by `Payslip4All.Web`. Blazor Server
pages are handled via SignalR circuits (not traditional HTTP request-response); see
`contracts/ui-contracts.md` for those surface contracts. This file covers only the raw HTTP
endpoints registered via Minimal API / Razor Pages.

---

## Endpoint Summary

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/payslips/{payslipId}/download` | ✅ `CompanyOwner` role | Download a payslip as a PDF file |
| `GET` | `/Auth/Login` | Anonymous | Render the login Razor Page |
| `POST` | `/Auth/Login` | Anonymous | Submit login credentials, establish cookie session |
| `GET` | `/Auth/Register` | Anonymous | Render the registration Razor Page |
| `POST` | `/Auth/Register` | Anonymous | Submit registration form, create account + establish session |
| `GET` | `/Auth/Logout` | Authenticated | Sign out and clear the auth cookie |

---

## Detailed Contracts

### `GET /payslips/{payslipId}/download`

**Purpose**: Download a specific payslip as a PDF file.

**Registered in**: `Program.cs` as a Minimal API endpoint.

**Authorization**:
```csharp
[Authorize(Roles = "CompanyOwner")]
```

**Request**:

| Part | Type | Required | Notes |
|------|------|----------|-------|
| `payslipId` | `Guid` (route) | ✅ | The ID of the payslip to download |

**Response**:

| Scenario | HTTP Status | Body | Notes |
|----------|-------------|------|-------|
| PDF found and user owns it | `200 OK` | `application/pdf` binary | `Content-Disposition: attachment; filename="payslip-{id}.pdf"` |
| Payslip not found **or** user doesn't own it | `404 Not Found` | Empty | Ownership failure intentionally returns 404 (not 403) to prevent data existence leakage |
| Unauthenticated request | `401 Unauthorized` | Redirect to `/Auth/Login` | Standard cookie auth redirect |
| Invalid `payslipId` format | `400 Bad Request` | — | Route constraint `{payslipId:guid}` rejects non-Guid values |

**Implementation** (`Program.cs`):
```csharp
app.MapGet("/payslips/{payslipId:guid}/download",
    [Authorize(Roles = "CompanyOwner")]
    async (Guid payslipId, IPayslipService svc, HttpContext ctx) =>
    {
        var userIdStr = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

        var pdf = await svc.GetPdfAsync(payslipId, userId);
        if (pdf == null) return Results.NotFound();

        return Results.File(pdf, "application/pdf", $"payslip-{payslipId}.pdf");
    });
```

**Ownership filter**: `IPayslipService.GetPdfAsync(payslipId, userId)` queries
`PayslipRepository` with a join through `Employee → Company → UserId == userId`. If the
payslip does not belong to the authenticated user, `null` is returned and the endpoint
responds with `404`.

---

### `POST /Auth/Login`

**Purpose**: Authenticate an employer and establish a cookie session.

**Handled by**: `Pages/Auth/Login.cshtml.cs` (`LoginModel.OnPostAsync`)

**Authorization**: `[AllowAnonymous]`

**Request Form Fields**:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `Email` | `string` | ✅ | Normalised to lowercase before lookup |
| `Password` | `string` | ✅ | Compared against BCrypt hash |
| `ReturnUrl` | `string` | ❌ | Query param; used for post-login redirect; sanitised via `LocalRedirect` |

**Response**:

| Scenario | Result |
|----------|--------|
| Valid credentials | Cookie issued; redirect to `ReturnUrl ?? "/"` |
| Invalid credentials | Re-render page with generic error: "Invalid email or password." |

**Claims issued on success**:
```
ClaimTypes.NameIdentifier → User.Id (Guid string)
ClaimTypes.Email          → User.Email
ClaimTypes.Name           → User.Email
ClaimTypes.Role           → "CompanyOwner"
```

**Security notes**:
- Error message MUST be generic — never reveals whether the email exists (FR-004)
- `LocalRedirect` prevents open-redirect attacks on `ReturnUrl`

---

### `POST /Auth/Register`

**Purpose**: Create a new employer account and establish a cookie session.

**Handled by**: `Pages/Auth/Register.cshtml.cs` (`RegisterModel.OnPostAsync`)

**Authorization**: `[AllowAnonymous]`

**Request Form Fields**:

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `Email` | `string` | ✅ | Valid email; normalised to lowercase |
| `Password` | `string` | ✅ | Min 8 chars (application-level) |
| `ConfirmPassword` | `string` | ✅ | Must match `Password` |

**Response**:

| Scenario | Result |
|----------|--------|
| Unique email + valid password | Account created; cookie issued; redirect to `/` |
| Duplicate email | Re-render page with generic error (MUST NOT reveal email exists — FR-002) |
| Password mismatch | Re-render page with "Passwords do not match." |

**Notes**:
- Password is hashed via `IPasswordHasher` (BCrypt work factor 12) before storage
- Plain-text password is NEVER persisted (FR-002)

---

### `GET /Auth/Logout`

**Purpose**: Sign out the current employer and invalidate the auth cookie.

**Handled by**: `Pages/Auth/Logout.cshtml.cs` (`LogoutModel.OnGetAsync`)

**Authorization**: Authenticated users only (redirect to `/Auth/Login` if not authenticated)

**Response**: Signs out via `HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)`; redirects to `/Auth/Login` (FR-005).

---

## Razor Page Routes (Read-Only HTTP GET)

The following routes return full Razor Page HTML (or Blazor circuit HTML). They are not API
endpoints but are included for completeness.

| Route | Handler | Auth | Notes |
|-------|---------|------|-------|
| `GET /Auth/Login` | `Login.cshtml` | Anonymous | Render login form |
| `GET /Auth/Register` | `Register.cshtml` | Anonymous | Render registration form |
| `GET /` | `Dashboard.razor` (via Blazor) | `CompanyOwner` | Employer dashboard |
| `GET /companies/create` | `CreateCompany.razor` | `CompanyOwner` | Create company form |
| `GET /companies/{id}` | `CompanyDetail.razor` | `CompanyOwner` | Company + employee list |
| `GET /companies/{id}/edit` | `EditCompany.razor` | `CompanyOwner` | Edit company form |
| `GET /companies/{id}/employees/create` | `CreateEmployee.razor` | `CompanyOwner` | Add employee form |
| `GET /companies/{id}/employees/{empId}` | `EmployeeDetail.razor` | `CompanyOwner` | Employee detail + loans + payslips |
| `GET /companies/{id}/employees/{empId}/edit` | `EditEmployee.razor` | `CompanyOwner` | Edit employee form |
| `GET /companies/{id}/employees/{empId}/loans/create` | `CreateLoan.razor` | `CompanyOwner` | Add loan form |
| `GET /companies/{id}/employees/{empId}/loans/{loanId}/edit` | `EditLoan.razor` | `CompanyOwner` | Edit loan form (blocked if TermsCompleted > 0) |
| `GET /companies/{id}/employees/{empId}/payslips/generate` | `GeneratePayslip.razor` | `CompanyOwner` | Payslip generation + preview |

All Blazor routes enforce `@attribute [Authorize(Roles = "CompanyOwner")]`. Unauthenticated
requests are redirected to `/Auth/Login` by the cookie auth middleware.
