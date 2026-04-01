# Research: Customer Wallet Credits

## Decision 1: Use one wallet per company owner account

- **Decision**: Model a single wallet for each authenticated `CompanyOwner`, shared across that owner's companies and payslip generation actions.
- **Rationale**: The feature specification assumes one wallet per customer account, and the current ownership model already filters company, employee, loan, and payslip data by `UserId`. A per-owner wallet avoids duplicate balances across companies while aligning with the existing authentication and authorization seams.
- **Alternatives considered**:
  - **One wallet per company**: Rejected because it complicates balance visibility and requires the owner to manage multiple balances for one login.
  - **One wallet per employee**: Rejected because pricing is tied to payslip generation, not employee-level funding.

## Decision 2: Persist a wallet ledger, not just a balance

- **Decision**: Store both the current wallet balance and a time-ordered wallet activity ledger containing credits, debits, reason, reference, and resulting balance.
- **Rationale**: The feature requires customers to see wallet history, and payslip pricing is financially sensitive. A ledger provides an auditable trail for top-ups, payslip charges, and future reconciliations while keeping the current balance cheap to read.
- **Alternatives considered**:
  - **Balance-only model**: Rejected because it cannot explain how a balance changed or satisfy the wallet activity requirement.
  - **Recomputing balance from activity on every read**: Rejected because the existing app favors straightforward read models and this would add avoidable latency and complexity.

## Decision 3: Represent money in rand using decimal values with two-decimal validation

- **Decision**: Store wallet balances, top-up amounts, and the configurable payslip price as decimal monetary values validated to non-negative two-decimal rand amounts.
- **Rationale**: The existing domain already uses `decimal` for salary and deduction amounts, so this keeps monetary handling consistent across payroll calculations and wallet charging.
- **Alternatives considered**:
  - **Integer cents**: Rejected because it diverges from the codebase's current monetary conventions.
  - **Floating point values**: Rejected because payroll and wallet charges require predictable financial precision.

## Decision 4: Charge the wallet only after successful payslip persistence

- **Decision**: The application service will verify sufficient balance before generation, create the payslip, and then record the wallet debit within the same relational unit of work where available. The DynamoDB path will preserve the same observable rule by applying the debit only after payslip creation succeeds.
- **Rationale**: The specification requires no wallet deduction on failed generation. The existing `PayslipGenerationService` already owns duplicate checks, loan updates, payslip persistence, and save orchestration, making it the correct application seam for the additional wallet debit rule.
- **Alternatives considered**:
  - **Debit before payslip generation**: Rejected because failures after the debit would violate the spec and require compensation logic.
  - **Separate asynchronous charging**: Rejected because customers must see the charge outcome immediately when generation completes.

## Decision 5: Add site-wide pricing configuration as a dedicated settings record

- **Decision**: Store the per-payslip rand charge in a dedicated pricing/settings record managed through a SiteAdministrator-only workflow.
- **Rationale**: The feature requires a configurable amount per generated payslip. A dedicated settings record keeps pricing independent from company data and allows the same value to be read consistently by CompanyOwner pages and the payslip generation workflow.
- **Alternatives considered**:
  - **Hardcoded configuration in appsettings or environment variables**: Rejected because the spec requires an authorized administrator to configure the price.
  - **Per-company pricing**: Rejected because the feature describes one configurable payslip price, not customer-specific pricing tiers.

## Decision 6: Keep top-ups internal to the application and defer external payment gateway integration

- **Decision**: Design top-ups as application-managed credit additions initiated from the website, without introducing an external payment provider in this feature.
- **Rationale**: The feature description asks for wallet credits on the website but does not name a payment gateway, compliance flow, or reconciliation provider. The safest bounded default is to implement wallet funding as an internal credit workflow and leave third-party payment integration to a later feature once gateway, settlement, and fraud requirements are defined.
- **Alternatives considered**:
  - **Immediate gateway integration**: Rejected because no provider, settlement flow, or compliance constraints are specified.
  - **Admin-only manual funding**: Rejected because the feature explicitly says customers should have and use a wallet on the website.

## Decision 7: Maintain repository parity across EF Core and DynamoDB

- **Decision**: Add application repository interfaces for wallets, wallet activity, and pricing, then implement both EF Core and DynamoDB repository sets in `Payslip4All.Infrastructure`.
- **Rationale**: The constitution requires unchanged layer boundaries and, when DynamoDB is active, a complete repository implementation for each application interface. The recently added DynamoDB provider already follows this pattern.
- **Alternatives considered**:
  - **Feature available only for relational providers**: Rejected because the current repository architecture and constitution expect provider parity.
  - **Embedding wallet logic in Web or DbContext directly**: Rejected because it violates Clean Architecture and repository rules.

## Decision 8: Surface wallet pricing on the public landing page without exposing private wallet data

- **Decision**: Extend the public home page to explain the wallet-credit model and show the current configured per-payslip rand price, while keeping all customer balances and wallet activity behind authenticated CompanyOwner pages.
- **Rationale**: The updated feature scope includes the public landing page. Pricing transparency helps visitors understand the wallet model before registration, but the page must remain public-safe and show only site-wide pricing information.
- **Alternatives considered**:
  - **Keep wallet messaging only inside authenticated pages**: Rejected because it does not satisfy the new home-page requirement.
  - **Show sample or hardcoded pricing on the landing page**: Rejected because it can drift from the configured payslip price and undermine trust.
