# Wallet UI Contract

## CompanyOwner Wallet Page

- **Route**: `/portal/wallet`
- **Authorization**: `CompanyOwner`
- **Displays**:
  - Current wallet balance
  - Current payslip price
  - Top-up amount entry
  - Recent wallet activity list
- **Interactions**:
  - Submit a positive top-up amount
  - View success or validation feedback
  - See updated balance and new activity entry after a successful top-up

## Payslip Generation Page Update

- **Route**: Existing generate payslip route under `/portal/companies/{companyId}/employees/{employeeId}/payslips/generate`
- **Authorization**: `CompanyOwner`
- **Displays**:
  - Current payslip price
  - Current available wallet balance
  - Insufficient-funds warning when balance is too low
- **Interactions**:
  - Prevent successful generation when balance is insufficient
  - Show post-generation confirmation including the charged amount when generation succeeds

## SiteAdministrator Pricing Page

- **Route**: `/admin/wallet-pricing`
- **Authorization**: `SiteAdministrator`
- **Displays**:
  - Current active payslip price
  - Last updated metadata
- **Interactions**:
  - Enter and save a new price
  - Receive validation feedback for invalid amounts
  - See the new price reflected immediately after save

## Public Landing Page Wallet Section

- **Route**: `/`
- **Authorization**: Public
- **Displays**:
  - Wallet-credit explanation for how payslip generation is paid for
  - Current public price per generated payslip in rand
  - Call-to-action links for registration and sign-in
- **Interactions**:
  - Visitor can understand the wallet model without logging in
  - Visitor can navigate to registration after reading the wallet pricing section
  - Page never displays private wallet balances, wallet activity, or owner-only controls
