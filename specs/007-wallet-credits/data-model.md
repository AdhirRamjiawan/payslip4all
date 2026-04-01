# Data Model: Customer Wallet Credits

## Wallet

- **Purpose**: Stores the current rand-denominated credit balance for one company owner account.
- **Primary fields**:
  - `Id`
  - `UserId`
  - `CurrentBalance`
  - `CreatedAt`
  - `UpdatedAt`
- **Relationships**:
  - One `Wallet` belongs to one authenticated owner account.
  - One `Wallet` has many `WalletActivity` records.
- **Validation rules**:
  - `UserId` is required.
  - `CurrentBalance` must be zero or greater.
  - Only one wallet may exist per owner account.
- **State transitions**:
  - `Active` from creation onward.
  - Balance increases after successful credit activity.
  - Balance decreases after successful payslip charge activity.

## WalletActivity

- **Purpose**: Stores an auditable ledger entry for every wallet credit or debit.
- **Primary fields**:
  - `Id`
  - `WalletId`
  - `ActivityType` (`Credit` or `Debit`)
  - `Amount`
  - `ReferenceType`
  - `ReferenceId`
  - `Description`
  - `BalanceAfterActivity`
  - `OccurredAt`
- **Relationships**:
  - Many `WalletActivity` records belong to one `Wallet`.
  - A debit entry may reference a generated `Payslip`.
- **Validation rules**:
  - `Amount` must be greater than zero.
  - `BalanceAfterActivity` must never be negative.
  - `OccurredAt` is required for ordering.
- **State transitions**:
  - Created when a top-up succeeds.
  - Created when a payslip charge succeeds.
  - Never edited after creation except by explicit corrective workflow in a future feature.

## PayslipPricingSetting

- **Purpose**: Stores the current configurable rand amount charged for one generated payslip.
- **Primary fields**:
  - `Id`
  - `PricePerPayslip`
  - `UpdatedByUserId`
  - `UpdatedAt`
- **Relationships**:
  - Independent site-wide settings record.
  - Read by wallet pages and the payslip generation service.
- **Validation rules**:
  - `PricePerPayslip` must be zero or greater.
  - Only one current active pricing record should be used by the application.
- **State transitions**:
  - Seeded with an initial default value.
  - Replaced or updated when a SiteAdministrator changes pricing.

## Derived Rules

- A payslip may be generated only when `Wallet.CurrentBalance >= PayslipPricingSetting.PricePerPayslip`.
- A successful top-up creates one `WalletActivity` credit entry and updates `Wallet.CurrentBalance`.
- A successful payslip generation creates one `WalletActivity` debit entry and updates `Wallet.CurrentBalance`.
- Failed or cancelled generation creates no debit entry and does not change `Wallet.CurrentBalance`.
- Wallet reads and writes must always be filtered by the authenticated owner `UserId`.
