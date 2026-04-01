# Feature Specification: Customer Wallet Credits

**Feature Branch**: `007-wallet-credits`  
**Created**: 2026-04-01  
**Status**: Draft  
**Input**: User description: "i want to create a new feature where customers have a wallet on the website. the wallet will be used as credits for generating payslips. each payslip generated will cost a configurable amount in rands"

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

- **Domain**: Define wallet balance rules, top-up behavior, debit behavior, and prevention of payslip generation when funds are insufficient.
- **Application**: Define use cases for viewing wallet information, adding wallet credits, reading the current payslip price, generating payslips with wallet charging, and reviewing wallet activity.
- **Infrastructure**: Persist wallet balances, wallet activity, and the configurable payslip generation price.
- **Web**: Provide customer-facing wallet views and generation flows that clearly show available balance, price per payslip, and the outcome of each charge.
- **Ownership filtering**: Each company owner can view and use only the wallet and wallet activity associated with their own account and companies.
- **TDD expectation**: Every functional requirement below includes acceptance scenarios that can be turned into tests before implementation begins.

### User Story 1 - Generate payslips using wallet credits (Priority: P1)

As a company owner, I want payslip generation to deduct funds from my wallet automatically so that I can only generate payslips when enough credit is available.

**Why this priority**: This is the core business outcome of the feature. Without wallet charging during payslip generation, the wallet has no practical value.

**Independent Test**: Can be fully tested by giving a customer wallet funds, generating payslips, and verifying that successful generation deducts the correct amount while blocked generation leaves the balance unchanged.

**Acceptance Scenarios**:

1. **Given** a company owner has enough wallet balance to cover the current payslip price, **When** they generate a payslip, **Then** the payslip is created and the wallet balance decreases by the configured amount in rands.
2. **Given** a company owner does not have enough wallet balance to cover the current payslip price, **When** they attempt to generate a payslip, **Then** the payslip is not created and the owner is shown that additional funds are required.
3. **Given** the configured payslip price changes after earlier payslips were generated, **When** a company owner generates a new payslip, **Then** only the new generation uses the latest configured price and the charged amount is recorded with that transaction.

---

### User Story 2 - Add funds to the wallet (Priority: P2)

As a company owner, I want to add funds to my wallet so that I can continue generating payslips without interruption.

**Why this priority**: Customers must be able to replenish balance themselves or the primary charging flow will eventually stop delivering value.

**Independent Test**: Can be fully tested by adding funds to a wallet and verifying that the displayed balance increases and the added amount appears in wallet activity.

**Acceptance Scenarios**:

1. **Given** a company owner is viewing their wallet, **When** they add funds successfully, **Then** the wallet balance increases by the added amount in rands.
2. **Given** a company owner submits an invalid top-up amount, **When** the request is processed, **Then** the wallet balance remains unchanged and the owner is shown a validation message.
3. **Given** a company owner adds funds more than once, **When** they return to the wallet page, **Then** the current balance reflects the cumulative successful top-ups.

---

### User Story 3 - See wallet balance, pricing, and activity (Priority: P3)

As a company owner, I want to see my balance, the current price per payslip, and recent wallet activity so that I can understand my spending and decide when to add more credit.

**Why this priority**: Transparency reduces failed generation attempts and gives customers confidence in how wallet funds are being used.

**Independent Test**: Can be fully tested by viewing the wallet page after top-ups and payslip generations and verifying that the balance, current price, and activity entries are accurate for that owner only.

**Acceptance Scenarios**:

1. **Given** a company owner has wallet activity, **When** they open the wallet page, **Then** they can see the current wallet balance and the current price charged per payslip in rands.
2. **Given** a company owner has successful top-ups and payslip charges, **When** they review wallet activity, **Then** each entry shows whether it was a credit or debit, the amount, and the resulting balance after the event.
3. **Given** a company owner has no wallet activity yet, **When** they open the wallet page, **Then** they still see a zero balance and the current payslip price.

---

### User Story 4 - Understand wallet pricing from the public landing page (Priority: P4)

As a visitor, I want the public home page to explain how wallet credits work so that I understand the pricing model before I register.

**Why this priority**: This supports conversion and pricing transparency, but it is lower priority than the core wallet flows that authenticated customers need to use the feature.

**Independent Test**: Can be fully tested by visiting the public landing page while signed out and verifying that wallet credits and per-payslip pricing are explained clearly without exposing private account data.

**Acceptance Scenarios**:

1. **Given** a visitor opens the public landing page, **When** the page loads, **Then** they can see that payslips are generated using wallet credits.
2. **Given** the current payslip price is configured, **When** a visitor reads the wallet section on the landing page, **Then** they can see the current rand amount charged per payslip.
3. **Given** a visitor is not signed in, **When** they view the landing page, **Then** they see only public pricing and feature information and no customer-specific wallet balances or activity.

---

### Edge Cases

- What happens when a customer attempts to generate multiple payslips in quick succession and the remaining balance can cover only some of them?
- How does the system handle a failed or cancelled wallet top-up so that no balance is added accidentally?
- What happens when the wallet balance is exactly equal to the current configured payslip price?
- How does the system prevent one company owner from viewing or spending another owner's wallet balance?
- What happens when the landing page cannot load the current public payslip price?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide each company owner with a wallet balance denominated in South African rand.
- **FR-002**: The system MUST allow a company owner to view their current wallet balance and the current configured price charged for generating one payslip.
- **FR-003**: The system MUST allow a company owner to add funds to their wallet using valid positive rand amounts.
- **FR-004**: The system MUST record each successful wallet top-up as wallet activity that includes the amount added, the type of activity, and the resulting balance.
- **FR-005**: The system MUST require sufficient wallet balance before allowing a payslip to be generated.
- **FR-006**: The system MUST deduct the current configured payslip price from the owner's wallet each time a payslip is generated successfully.
- **FR-007**: The system MUST prevent any wallet deduction when payslip generation does not complete successfully.
- **FR-008**: The system MUST record each payslip generation charge as wallet activity that includes the charged amount, the type of activity, and the resulting balance.
- **FR-009**: The system MUST allow authorized administrators to configure the rand amount charged per generated payslip.
- **FR-010**: The system MUST apply the latest configured payslip price to new payslip generations without changing previously recorded wallet activity.
- **FR-011**: The system MUST show a clear insufficient-funds message when a company owner attempts to generate a payslip without enough wallet balance.
- **FR-012**: The system MUST ensure that a company owner can view and act on only their own wallet balance and wallet activity.
- **FR-013**: The system MUST preserve a time-ordered history of wallet credits and debits for customer review.
- **FR-014**: The system MUST present wallet credits as a public feature on the home page for signed-out visitors.
- **FR-015**: The system MUST show the current public rand price per payslip on the home page without requiring sign-in.
- **FR-016**: The system MUST ensure the home page displays only public wallet messaging and never customer-specific wallet balances or wallet activity.

### Key Entities *(include if feature involves data)*

- **Wallet**: Represents the available rand-denominated credit balance for a company owner.
- **Wallet Activity**: Represents a single credit or debit event against a wallet, including its amount, reason, timestamp, and resulting balance.
- **Payslip Price Setting**: Represents the current rand amount charged for generating one payslip.
- **Payslip Generation Charge**: Represents the wallet debit linked to a successfully generated payslip.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of successfully generated payslips result in a wallet debit equal to the configured payslip price at the time of generation.
- **SC-002**: 100% of payslip generation attempts made with insufficient wallet balance are blocked before a payslip is created.
- **SC-003**: 95% of company owners can confirm their current wallet balance and current payslip price within 30 seconds of opening the wallet area.
- **SC-004**: 95% of successful wallet top-ups are reflected in the visible wallet balance and activity history within 10 seconds of completion.
- **SC-005**: 100% of wallet activity entries displayed to a company owner belong only to that owner's account.
- **SC-006**: 95% of visitors can identify from the public landing page that payslip generation uses wallet credits and see the current per-payslip rand price within 30 seconds.

## Assumptions

- A single wallet is maintained per company owner account for website usage.
- The configured payslip price is a single current price in rand applied to each newly generated payslip.
- Previously recorded wallet activity keeps the historical charged amount even if the configured price changes later.
- Wallet balances cannot go below zero.
- The public landing page may show the current configured price, but it never shows account-specific wallet data.
