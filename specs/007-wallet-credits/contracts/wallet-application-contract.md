# Wallet Application Contract

## Application Services

### Wallet read contract

- **Actor**: `CompanyOwner`
- **Input**: Authenticated `UserId`
- **Output**:
  - Current wallet balance in rand
  - Current payslip price in rand
  - Recent wallet activity entries ordered newest first
- **Rules**:
  - Returns only the authenticated owner's wallet data.
  - Returns zero balance with empty activity when the wallet has not been used yet.

### Wallet top-up contract

- **Actor**: `CompanyOwner`
- **Input**:
  - Authenticated `UserId`
  - Positive rand amount
- **Output**:
  - Success/failure indicator
  - Updated balance
  - Created wallet activity entry
- **Rules**:
  - Rejects zero or negative amounts.
  - Creates exactly one credit activity on success.
  - Leaves balance unchanged on failure.

### Payslip generation pricing contract

- **Actor**: `CompanyOwner`
- **Input**:
  - Existing payslip generation command
  - Authenticated `UserId`
- **Output**:
  - Existing payslip generation result
  - Wallet debit applied only when generation succeeds
- **Rules**:
  - Blocks generation when balance is lower than the current configured price.
  - Records one debit activity per successful generated payslip.
  - Uses the latest configured price for new generations only.

### Pricing administration contract

- **Actor**: `SiteAdministrator`
- **Input**:
  - Authenticated administrator identity
  - New price per payslip in rand
- **Output**:
  - Updated active price
  - Audit metadata for who changed the price and when
- **Rules**:
  - Rejects negative prices.
  - Applies updated pricing to future payslip generations immediately after save.
  - Does not mutate historical wallet activity.
