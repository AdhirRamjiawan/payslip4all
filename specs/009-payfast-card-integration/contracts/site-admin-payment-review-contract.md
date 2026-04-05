# Site Administrator Payment Review Contract

## Purpose

Define the internal review surface for PayFast wallet top-up troubleshooting and audit follow-up. This surface exists only for `SiteAdministrator` users and must expose only the minimum non-sensitive evidence needed to reconcile or investigate payment behavior.

## Route

- **Primary route**: `/portal/admin/wallet-topups/review`
- **Access**: `[Authorize(Roles = "SiteAdministrator")]`
- **Prohibited access**: `CompanyOwner` users and unauthenticated users must be denied

## Supported review queries

- Attempt identifier lookup
- Payment Confirmation Record identifier lookup
- Date/time range
- Outcome filter (`Completed`, `Cancelled`, `Expired`, `Abandoned`, `NotConfirmed`)
- Conflict filter (`ConflictWithAcceptedFinalOutcome = true`)
- Unmatched-return filter

## Review payload

The internal review surface may show only:

- attempt identifier
- owner/user identifier
- requested amount
- confirmed charged amount when available
- authoritative outcome
- wallet-credit indicator and wallet activity identifier
- Payment Confirmation Record identifier
- evidence source channel
- normalization decision type / reason code / summary
- timestamps needed for reconciliation and troubleshooting
- safe correlation references such as `m_payment_id` and `pf_payment_id`

The internal review surface must not show:

- PAN
- CVV
- card expiry
- raw PayFast diagnostic payloads
- merchant secrets, passphrases, or credentials
- verbose gateway error internals that are unnecessary for reconciliation

## Behavioral rules

- Review pages are read-only.
- Internal review must not mutate wallet balances, attempt states, or evidence records.
- The UI must distinguish owner-visible safe outcomes from internal-only audit detail.
- Conflicting late evidence after a completed or cancelled authoritative outcome must remain reviewable without reversing wallet movement from this feature.
- Unmatched returns must remain privacy-safe even in admin review by showing only the safe evidence snapshot and correlation disposition required for troubleshooting.

## Component-test expectations

- bUnit coverage must verify:
  - only `SiteAdministrator` users may render the review surface,
  - non-admin users are denied,
  - only minimum non-sensitive evidence fields are rendered,
  - conflicting-evidence and unmatched-return rows render safely,
  - no owner-facing messaging or settlement logic is embedded in the Razor page.

## Application/service expectations

- Any application query backing this surface must require an admin-only authorization boundary.
- Returned DTOs must be composed from safe audit fields only.
- The query path must work against relational and DynamoDB persistence implementations with the same filtering semantics.
