# Feature Specification: Payslip Generation System

**Feature Branch**: `001-payslip-generation`  
**Created**: 2026-03-15  
**Status**: Implemented  
**Input**: User description: "Payslip4All is an application that allows employers to generate payslips for their employees. A user of the system can have one or more companies and each company can have one or more employees. An employer will generate a PDF payslip for an employee every month."

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

This feature establishes the core domain of Payslip4All and must be implemented in strict compliance with the project constitution.

**TDD Obligation**: Every functional requirement below has corresponding acceptance scenarios that MUST be written as failing tests before any implementation begins. Payslips are legal financial documents — test coverage on Domain and Application layers must not fall below 80%.

**Clean Architecture Layer Assignment**:

| Requirement Area              | Layer          |
|-------------------------------|----------------|
| Payslip calculation rules     | Domain         |
| Company/Employee/Payslip CRUD | Application    |
| PDF generation, EF Core repos | Infrastructure |
| Blazor pages & components     | Web            |

**Ownership Filtering**: All service methods that retrieve or mutate company, employee, or payslip data MUST filter by the authenticated user's ID. A Company Owner MUST never see or modify another owner's data.

**New Entities Introduced**: User, Company, Employee, Payslip, EmployeeLoan — all require EF Core migrations.

---

### User Story 1 - Employer Registration & Login (Priority: P1)

An employer visits the application for the first time, creates an account with their email and password, and then signs in. Once authenticated they land on their personal dashboard where they can see their companies.

**Why this priority**: Authentication is the foundation for every other user journey. Without a verified owner identity, ownership-filtering guarantees cannot be enforced and no payslip can be securely attributed to a company.

**Independent Test**: Can be fully tested by registering a new account, logging out, and logging back in — delivers a secure, authenticated session with no other features required.

**Acceptance Scenarios**:

1. **Given** a visitor is on the registration page, **When** they provide a unique email and a valid password and submit the form, **Then** their account is created, they are automatically signed in, and they are redirected to the dashboard.
2. **Given** a visitor attempts to register with an email already in use, **When** they submit the form, **Then** the system displays a generic error message without revealing whether the email exists.
3. **Given** a registered employer is on the login page, **When** they enter correct credentials and submit, **Then** they are authenticated and redirected to their dashboard.
4. **Given** a registered employer enters incorrect credentials, **When** they submit the login form, **Then** the system displays a generic "invalid credentials" error without indicating which field is wrong.
5. **Given** an authenticated employer, **When** they click "Sign Out", **Then** their session is terminated and they are redirected to the login page.
6. **Given** an unauthenticated visitor, **When** they attempt to navigate to any protected page, **Then** they are redirected to the login page.

---

### User Story 2 - Company Management (Priority: P2)

An authenticated employer creates and manages one or more companies within their account. Each company represents a separate business entity for which payslips will be generated.

**Why this priority**: Companies are the top-level container for employees and payslips. Without at least one company, an employer cannot add employees or generate payslips. This story unlocks all downstream functionality.

**Independent Test**: Can be fully tested by creating a company, verifying it appears on the dashboard, and confirming a second employer cannot see it — delivers multi-company isolation with no payslip generation required.

**Acceptance Scenarios**:

1. **Given** an authenticated employer on the dashboard, **When** they provide a company name (and optionally an address) and submit the "Add Company" form, **Then** the new company appears in their company list.
2. **Given** an authenticated employer with existing companies, **When** they view the dashboard, **Then** they see only their own companies, never those of other employers.
3. **Given** an authenticated employer viewing a company, **When** they update the company name and/or address and save, **Then** the updated details are reflected immediately.
4. **Given** an authenticated employer viewing a company with no employees or payslips, **When** they delete the company, **Then** the company is removed from their list.
5. **Given** an authenticated employer attempts to delete a company that has existing employees or payslips, **When** they confirm deletion, **Then** the system prevents the deletion and displays a descriptive error message.
6. **Given** an employer submits an empty company name, **When** the form is validated, **Then** the system displays a validation error and does not create the company.

---

### User Story 3 - Employee Management (Priority: P3)

An authenticated employer adds and manages employees under a specific company. Each employee record stores the personal and financial details required to calculate and generate their monthly payslip.

**Why this priority**: Employees are the direct recipients of payslips. Accurate employee records (including pay details) are a prerequisite for correct payslip generation.

**Independent Test**: Can be fully tested by adding an employee to a company, editing their details, and confirming the record persists — delivers a complete employee register with no payslip generation required.

**Acceptance Scenarios**:

1. **Given** an authenticated employer viewing a company, **When** they provide required employee details and submit the "Add Employee" form, **Then** the new employee appears in the company's employee list.
2. **Given** an authenticated employer with employees, **When** they view a company's employee list, **Then** they see only employees belonging to that company and never employees from another employer's companies.
3. **Given** an authenticated employer viewing an employee record, **When** they update employee details and save, **Then** the updated details are reflected immediately.
4. **Given** an authenticated employer viewing an employee with no associated payslips, **When** they delete the employee, **Then** the employee is removed from the list.
5. **Given** an authenticated employer attempts to delete an employee who has existing payslips, **When** they confirm deletion, **Then** the system prevents the deletion and displays a descriptive error message.
6. **Given** an employer submits an employee form with missing mandatory fields (e.g., name, monthly salary), **When** the form is validated, **Then** the system displays field-level validation errors and does not create the employee.

---

### User Story 4 - Monthly Payslip Generation & PDF Download (Priority: P4)

An authenticated employer selects an employee, chooses a payslip month, reviews the calculated payslip details, and downloads the payslip as a PDF document. The payslip serves as the employee's official record of earnings and deductions for that month.

**Why this priority**: This is the primary value-delivering outcome of the entire application. All prior stories exist to enable this one. It is placed at P4 because it depends on all earlier stories being complete.

**Independent Test**: Can be fully tested end-to-end by generating and downloading a PDF payslip for a single employee for a single month — delivers the legally required document without any additional features.

**Acceptance Scenarios**:

1. **Given** an authenticated employer viewing an employee, **When** they select a month and year and initiate payslip generation, **Then** a payslip record is created containing the employee's gross pay, deductions, and net pay for that period.
2. **Given** a payslip has been generated, **When** the employer clicks "Download PDF", **Then** a correctly formatted PDF file is produced and downloaded to their device.
3. **Given** an employer attempts to generate a payslip for a month that already has a payslip record for that employee, **When** they submit the request, **Then** the system warns them that a payslip already exists and asks for confirmation before overwriting.
4. **Given** an employer views an employee's payslip history, **When** they view the list, **Then** payslips are listed in reverse chronological order (most recent first).
5. **Given** an employer is on the payslip generation page, **When** they have not yet saved the payslip, **Then** they can preview the calculated values (gross, deductions, net) before confirming.
6. **Given** an employer attempts to generate a payslip for an employee with no salary configured, **When** they submit the request, **Then** the system displays a validation error and prevents generation.

---

### Edge Cases

- What happens when an employer has no companies yet? → The dashboard displays an empty state with a prominent call-to-action to add their first company.
- What happens when a company has no employees? → The employee list shows an empty state with a prompt to add the first employee.
- What happens when a payslip PDF generation fails mid-process? → The system displays a user-friendly error message; no partial payslip record is saved (generation is atomic).
- How does the system handle concurrent payslip generation for the same employee and month? → The system enforces a unique constraint per employee per month, returning a conflict error to the second request.
- What happens if an employer tries to access a URL belonging to another employer's company or employee? → The system returns a "not found" response (ownership filter prevents data leakage without revealing resource existence).
- What happens when a negative or zero salary is entered for an employee? → The system rejects the value with a validation error; payslip generation is blocked until a positive salary is set.
- What happens when an employee has no active loans for a given pay period? → The Loan Deduction section is omitted from the payslip; Total Deductions equals UIF Deduction only.
- What happens when a loan reaches its final term during payslip generation? → The final deduction MUST still be included on the payslip for that pay period; the system then marks the loan Status as `Completed`. No further deduction is taken for that loan in any subsequent pay period.
- If an employee has 3 Active loans during a pay period, the payslip MUST show 3 separate Loan Deduction lines, each displaying the loan Description and Monthly Deduction Amount; all 3 amounts are included in Total Deductions.
- An employer attempts to edit a loan's amount or terms after the first payslip deduction has been applied → the system rejects the edit with a clear error message; the loan record remains unchanged.
- An employer attempts to delete a loan after the first payslip deduction has been applied → the system rejects the deletion with a clear error message; the loan record is retained.
- An employer adds a new loan record but no pay period deduction has yet been processed for it → the employer MAY delete that loan before the next payslip generation runs, and the deletion succeeds with no error.

## Requirements *(mandatory)*

### Functional Requirements

**User Account Management**

- **FR-001**: System MUST allow visitors to register a new employer account using a unique email address and a password.
- **FR-002**: System MUST hash all passwords using an approved algorithm before storing them; plaintext passwords MUST never be persisted.
- **FR-003**: System MUST authenticate registered employers via their email and password and establish a session upon successful login.
- **FR-004**: System MUST display generic error messages for authentication failures without revealing whether the email exists or which field is incorrect.
- **FR-005**: System MUST terminate a user's session when they sign out and redirect them to the login page.
- **FR-006**: System MUST redirect unauthenticated visitors away from any protected page to the login page.

**Company Management**

- **FR-007**: System MUST allow an authenticated employer to create one or more companies associated with their account.
- **FR-008**: System MUST display only the authenticated employer's own companies; no employer may view or modify another employer's companies.
- **FR-009**: System MUST allow an employer to update a company's name and address.
- **FR-010**: System MUST allow an employer to delete a company only when it has no employees or payslips; deletion of a non-empty company MUST be prevented with a clear error message.
- **FR-011**: System MUST validate that a company name is non-empty before creating or updating it. Company address is optional at creation but MUST be a non-empty string if provided.

**Employee Management**

- **FR-012**: System MUST allow an authenticated employer to add one or more employees to a company they own.
- **FR-013**: System MUST display only employees belonging to the authenticated employer's own companies.
- **FR-014**: System MUST allow an employer to update an employee's personal and financial details.
- **FR-015**: System MUST allow an employer to delete an employee only when no payslips exist for that employee; deletion of an employee with payslip history MUST be prevented with a clear error message.
- **FR-016**: System MUST validate that mandatory employee fields (First Name, Last Name, ID Number, Employee Number, Start Date, Occupation, Monthly Gross Salary) are present and valid before creating or updating an employee record.
- **FR-017**: System MUST reject employee Monthly Gross Salary values that are zero or negative; the field is required and MUST be a positive decimal.

**Payslip Generation**

- **FR-018**: System MUST allow an authenticated employer to generate a payslip for any employee belonging to their companies for a specified month and year.
- **FR-019**: System MUST calculate and display the following named line items before the employer confirms generation: **Gross Earnings** (taken from Employee.Monthly Gross Salary), **UIF Deduction** (`MIN(Monthly Gross Salary, R17,712) × 1%` — South African legal standard employee contribution rate, capped at the UIF earnings ceiling), **Loan Deduction(s)** (one line per active loan for the pay period — see FR-026 through FR-028), **Total Deductions** (UIF Deduction + sum of all Loan Deductions), and **Net Pay** (Gross Earnings − Total Deductions).
- **FR-020**: System MUST persist a payslip record containing Gross Earnings, UIF Deduction, Net Pay, Employee reference, and the pay period (month and year).
- **FR-021**: System MUST enforce one payslip per employee per month; attempting to generate a duplicate MUST trigger a confirmation prompt before overwriting.
- **FR-022**: System MUST produce a downloadable PDF payslip document that includes the following named line items: **Gross Earnings**, **UIF Deduction**, one line per active **Loan Deduction** (showing the loan description and monthly deduction amount), **Total Deductions**, and **Net Pay** — plus employer company name, company address, employee full name, and pay period.
- **FR-023**: System MUST display the employer's payslip generation request as an atomic operation — either the payslip is fully saved and the PDF produced, or neither persists.
- **FR-024**: System MUST display an employee's payslip history in reverse chronological order.
- **FR-025**: System MUST prevent payslip generation for an employee whose Monthly Gross Salary is not configured (null or absent).
- **FR-026**: System MUST allow an authenticated employer to add one or more loans to any employee belonging to their companies. Each loan requires: Description (text — what the loan is for), Total Loan Amount (decimal), Number of Terms (positive integer — number of monthly instalments), Monthly Deduction Amount (decimal — fixed amount deducted per payslip), and Payment Start Date (month + year the deductions begin).
- **FR-027**: System MUST automatically include a Loan Deduction line item on the payslip for every **active** loan belonging to the employee for that pay period. A loan is **active** when: `pay period >= Payment Start Date` AND `TermsCompleted < NumberOfTerms`. There is **no limit** on the number of concurrently Active loans per employee; all Active loans MUST each appear as a separate, individually labelled Loan Deduction line item on the payslip.
- **FR-028**: System MUST validate that Monthly Deduction Amount and Total Loan Amount are positive decimals, and that Number of Terms is a positive integer, before creating or updating a loan record.
- **FR-029**: System MUST automatically transition an EmployeeLoan's Status from `Active` to `Completed` when `TermsCompleted == NumberOfTerms`. Completed loans MUST remain persisted and visible in a read-only loan history view scoped to the employee.
- **FR-030**: System MUST prevent any field on an EmployeeLoan record from being edited once `TermsCompleted > 0` (i.e., at least one payslip deduction has been applied). An attempt to edit such a loan MUST be rejected with a clear error message (e.g., "This loan cannot be edited because at least one deduction has already been applied.").
- **FR-031**: System MUST prevent deletion of an EmployeeLoan record when `TermsCompleted > 0`. An attempt to delete such a loan MUST be rejected with a clear error message (e.g., "This loan cannot be deleted because deductions have already been applied."). A loan with `TermsCompleted == 0` (not yet reached its Payment Start Date or not yet included on any payslip) MAY be deleted.
- **FR-032**: System MUST increment `TermsCompleted` atomically — as part of the same transaction that generates the payslip — each time a loan deduction is applied to a payslip. `TermsCompleted` is the sole authoritative source for determining the read-only guard (`TermsCompleted > 0`) and the completion check (`TermsCompleted == NumberOfTerms`). `TermsCompleted` defaults to `0` on loan creation and MUST NOT be directly editable by users.

### Key Entities

- **User**: Represents an employer account. Attributes: unique email address, hashed password. Owns zero or more Companies. Owns the **CompanyOwner** role. Note: the `SiteAdministrator` role constant is seeded in this feature but enforcement is deferred to feature `002-admin-portal`.
- **Company**: Represents a business entity owned by a User. Attributes: name (employer/trading name), address (street address used on payslips), owner (User reference), creation date. A Company has zero or more Employees.
- **Employee**: Represents a person employed by a Company. Attributes: First Name, Last Name, ID Number (national identity document number), Employee Number (employer-assigned identifier), Start Date (employment commencement date), Occupation (job title / role), UIF Reference (optional — Unemployment Insurance Fund registration reference), Monthly Gross Salary (decimal, required — one fixed salary figure used for every payslip generated for this employee), Company reference. An Employee has zero or more Payslips.
- **Payslip**: Represents the official earnings record for one Employee for one calendar month. Attributes: pay period (month + year), Gross Earnings (derived from Employee.Monthly Gross Salary at generation time), UIF Deduction (`MIN(Gross Earnings, R17,712) × 1%` — SA legal standard employee contribution rate, capped at the UIF earnings ceiling), Loan Deduction line items (one snapshot entry per active EmployeeLoan at generation time — captures Description and Monthly Deduction Amount), Total Deductions (UIF Deduction + sum of all Loan Deduction amounts), Net Pay (Gross Earnings − Total Deductions), Employee reference, Company reference, generation date. The PDF renders: **Gross Earnings**, one line per **Loan Deduction**, **UIF Deduction**, **Total Deductions**, **Net Pay**. Unique constraint: one Payslip per Employee per pay period.
- **EmployeeLoan**: Represents a loan repayment arrangement attached to an Employee. Attributes: Description (text — describes what the loan is for), Total Loan Amount (decimal), Number of Terms (positive integer — total number of monthly instalments), Monthly Deduction Amount (decimal — fixed amount deducted from each payslip), Payment Start Date (month + year deductions begin), Status (enum: `Active | Completed`), **TermsCompleted** (integer, default `0` — incremented atomically each time a payslip deduction is applied; not directly editable by users), Employee reference. A loan is **active** for a given pay period when: `pay period >= Payment Start Date` AND `TermsCompleted < NumberOfTerms`. Lifecycle: Status transitions automatically from `Active` → `Completed` when `TermsCompleted == NumberOfTerms`. Completed loans remain in the data store and are visible in a read-only loan history view per employee. An employee may have zero or more EmployeeLoan records; **multiple may be Active simultaneously** — there is no limit on the number of concurrently Active loans per employee. **Mutability rules**: A loan record is fully editable when `TermsCompleted == 0` (no deductions yet taken). Once `TermsCompleted > 0` the record becomes **read-only** — no fields may be changed. A loan may be **deleted** only when `TermsCompleted == 0`; deletion is permanently blocked once `TermsCompleted > 0`. `TermsCompleted` is the sole authoritative source for both the read-only guard and the completion check.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An employer can complete the full journey — register, create a company, add an employee, generate and download a payslip PDF — in under 5 minutes on a first visit.
- **SC-002**: A payslip PDF is available for download within 3 seconds of the employer confirming generation.
- **SC-003**: 100% of generated payslip PDFs contain all mandatory fields: company name, company address, employee name, pay period, Gross Earnings, UIF Deduction, Total Deductions, and Net Pay — plus one line per active Loan Deduction (if any).
- **SC-004**: An employer with 10 companies each containing 50 employees can navigate between companies and view employee lists without noticeable delay (under 2 seconds per page load).
- **SC-005**: No employer can access, view, or modify payslip data, employee records, or company records belonging to another employer — verified by targeted security test scenarios.
- **SC-006**: The system correctly prevents duplicate payslip generation (same employee, same month) in 100% of cases without data loss.
- **SC-007**: All authentication error messages are generic and do not reveal account existence information, verified through automated test scenarios.

## Assumptions

The following assumptions have been made based on industry standards and the project constitution. They should be reviewed and corrected if inaccurate:

1. **Deductions scope (initial release)**: The initial release calculates UIF deduction and supports loan-based deductions (see EmployeeLoan entity and FR-026–FR-028). Additional deduction types (tax, pension) are out of scope for this feature and will be addressed in a follow-on feature.
2. **Single currency**: All monetary values are assumed to be in a single currency (South African Rand, ZAR) for this release. Multi-currency support is out of scope.
3. **Payslip period**: A payslip period corresponds to a calendar month (e.g., June 2025). Weekly or bi-weekly pay cycles are out of scope.
4. **PDF branding**: The PDF payslip uses a standard system-provided layout. Custom branding or logo upload per company is out of scope for this feature.
5. **Employee role access**: Only the Company Owner (employer) can generate and download payslips. There is no employee self-service portal in this release.
6. **Data retention**: Payslip records are retained indefinitely unless the employer explicitly deletes them (subject to the deletion rules in FR-015/FR-010). Automated expiry is out of scope.
7. **Email notifications**: No email is sent to employees upon payslip generation. Distribution is the employer's responsibility.

## Clarifications

### Session 2026-03-14

- Q: What fields should the Company entity have? → A: Company has Name (employer/trading name), Address (street address printed on payslips), and a list of Employees. Owner reference and creation date are also retained.
- Q: What fields should the Employee entity have? → A: First Name, Last Name, ID Number, Employee Number, Start Date, Occupation, UIF Reference. (Pay Period was initially listed here but confirmed in Q2 to belong on Payslip only.)
- Q: Where does Pay Period (Month + Year) belong? → A: On Payslip only — removed from Employee entity.
- Q: How is employee salary captured for payslip calculation? → A: Monthly Gross Salary (decimal) stored on Employee record — one fixed figure per employee used for all payslips.
- Q: What line items appear on the payslip PDF? → A: Three named lines — Gross Earnings (from Employee.Monthly Gross Salary), UIF Deduction (calculated from gross), Net Pay (Gross Earnings − Total Deductions). **Correction (H4)**: The full formula is Net Pay = Gross Earnings − Total Deductions, where Total Deductions = UIF Deduction + sum of all active Loan Deduction amounts. See FR-019 for the authoritative definition.
- Q: What is the UIF deduction calculation formula? → A: UIF = MIN(Gross, R17,712) × 1% — SA legal standard employee contribution, capped at earnings ceiling.
- Q: Can payslips have additional deductions beyond UIF? → A: Yes — Loan deductions supported. Each loan has: Description, Total Loan Amount, Number of Terms, Monthly Deduction Amount, Payment Start Date. Active loans auto-appear as line items on payslip.
- Q: What happens to a loan when all terms are paid? → A: Loan status automatically changes to Completed — remains visible in read-only loan history; final deduction is still included on the payslip for that pay period.
- Q: Can an employee have multiple concurrent Active loans? → A: Yes — no limit; all Active loans appear as individual Loan Deduction line items on the payslip
- Q: Can loans be edited or deleted after deductions are applied? → A: No — loans become read-only after first deduction is applied; deletion only permitted if zero deductions have been taken
- Q: How is the count of applied deductions tracked per loan? → A: TermsCompleted integer stored on EmployeeLoan — incremented atomically on each payslip generation; authoritative source for read-only guard and completion check
