# DynamoDB Payment Storage Contract

## Purpose

Define the DynamoDB-specific startup and persistence contract that keeps the PayFast design compliant with Constitution Principle V when `PERSISTENCE_PROVIDER=dynamodb`.

## Required tables

Startup must verify or create the following tables before the app serves wallet top-up traffic:

- `{prefix}_wallet_topup_attempts`
- `{prefix}_payment_return_evidences`
- `{prefix}_outcome_normalization_decisions`
- `{prefix}_unmatched_payment_return_records`
- `{prefix}_wallets`
- `{prefix}_wallet_activities`

## Startup behavior

- The DynamoDB provider path must register a hosted startup provisioner.
- For each required table, startup must:
  - check whether the table already exists,
  - create it automatically if missing,
  - wait until the table is active before continuing,
  - log whether the table was created or already confirmed.
- Application startup must fail fast if a required table cannot be created or verified.

## Logging contract

- Logs must distinguish at least these cases:
  - table already exists / confirmed,
  - table creation started or completed,
  - waiting for table activation,
  - startup failure because a required table could not be verified.
- Logs must use table names only; they must not include sensitive payment payload data.

## Behavior parity

- DynamoDB repositories must persist the same safe fields, owner filtering, reconciliation transitions, and exactly-once settlement semantics as the relational repositories.
- DynamoDB repositories must support the same Payment Confirmation Record linkage and the same late authoritative upgrade rules for `Expired`, `Abandoned`, and `NotConfirmed` attempts as the relational repositories.
- Payment history and balance visibility rules from SC-002 apply equally to the DynamoDB path.
- Unmatched return handling and generic owner-safe messaging rules apply equally to the DynamoDB path.
- Admin-only internal review queries must return the same safe evidence fields as the relational path.

## Test expectations

- Infrastructure tests must cover:
  - missing payment tables are auto-created,
  - existing payment tables are logged as confirmed rather than recreated,
  - startup waits for active status,
  - startup fails when required tables cannot be verified,
  - repository behavior for payment attempts, evidence, normalization decisions, unmatched records, and wallet credits matches the relational path.
