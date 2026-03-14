# Payslip4All Web Application

## Problem statement
A system is needed to generate payslips on a monthly basis for South African employees. These employees are low income earners that would not quality for income tax, only for UIF (Unemployment Insurance Fund. See more info here [UIF contributions](https://www.labour.gov.za/DocumentCenter/Acts/UIF/Amended%20Act%20-%20Unemployment%20Insurance%20Contributions.pdf)). These employees may be able to earn extra bonuses, request for advanced pay or loans that they will have deducted from their monthly pay until the loan amount is paid in full. Payslips must generated and exported as PDFs.

## User Roles
- Site administrator: Not related to any end user. Performs site administration tasks.
- Company Owner: This user can register one or more companies with each company having one or more employees.

## Use Cases
### Site Administrator
- Manage all company owners
- Make company owner active/inactive
- Reset passwords for company user
- View Dashboard of summarized information

### Company Owner
- CRUD functionality for one or more Companies
- CRUD functionality for one or more Employees associated with a company
- Manually generate a payslip for a given month, defaulting to the current month.
- Ability to schedule payslip generation and be emailed to Company owner


## Business Constraints
### Company Owner
- The company owner user should only be able to view the company they've registered, not any any other company and not any employee details in another company.
