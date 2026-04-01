# Quickstart: Customer Wallet Credits

## Prerequisites

- Existing CompanyOwner account able to access `/portal`
- Existing SiteAdministrator account able to access admin pages
- At least one company owner with a company, employee, and payslip generation access
- Application running with either relational persistence or DynamoDB configured

## Scenario 0: Review wallet messaging on the public landing page

1. Sign out of the application.
2. Navigate to `/`.
3. Confirm the landing page explains that payslips are generated using wallet credits.
4. Confirm the page shows the current public rand price per payslip.
5. Confirm the page does not reveal any customer-specific wallet balance or wallet activity.

## Scenario 1: Configure payslip price

1. Sign in as a `SiteAdministrator`.
2. Navigate to `/admin/wallet-pricing`.
3. Set the payslip charge to a rand amount such as `15.00`.
4. Save and confirm the updated price is shown immediately.
5. Confirm the update is rejected if the administrator session does not have a valid user identifier.
6. Confirm the navigation menu exposes **Wallet Pricing** only for the `SiteAdministrator` role.

## Scenario 2: Top up a wallet

1. Sign in as a `CompanyOwner`.
2. Navigate to `/portal/wallet`.
3. Confirm the current price per payslip is visible.
4. Enter a valid positive rand amount such as `100.00`.
5. Submit the top-up and confirm:
    - the wallet balance increases,
    - a credit activity entry appears,
    - the resulting balance is shown in the activity history.
6. Confirm the dashboard wallet summary reflects the new balance and current price.
7. If you are validating DynamoDB parity, repeat the first successful top-up and confirm the wallet can be read back immediately.

## Scenario 3: Generate a payslip with sufficient funds

1. Navigate to an employee's generate payslip page.
2. Confirm the page shows the current wallet balance and the current payslip price.
3. Generate a payslip for a valid pay period.
4. Confirm:
    - the payslip is created successfully,
    - the success message shows the charged amount,
    - the wallet balance decreases by the configured price,
    - a debit activity entry appears in `/portal/wallet`.
5. If overwriting an existing payslip for the same period, confirm the existing payslip is not removed unless the replacement generation and wallet charge both succeed.

## Scenario 4: Block generation with insufficient funds

1. Reduce the wallet balance so it is lower than the configured price.
2. Attempt to generate another payslip.
3. Confirm:
   - the action is blocked,
   - no new payslip is created,
   - the wallet balance remains unchanged,
   - the user sees an insufficient-funds message.
4. If wallet details fail to load, confirm the page blocks preview/generation and shows a generic wallet-unavailable message instead of raw exception text.
