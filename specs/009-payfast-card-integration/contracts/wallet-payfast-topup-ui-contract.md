# Wallet PayFast Top-Up UI Contract

## Purpose

Define the owner-facing Blazor contract for wallet top-up initiation, return routing, history freshness, and owner-safe result messaging.

## Route: `/portal/wallet`

- **Access**: `[Authorize(Roles = "CompanyOwner")]`
- **Capabilities**:
  - show current wallet balance and top-up history,
  - accept a wallet top-up amount,
  - start the hosted PayFast flow.
- **Rules**:
  - Form validation must reject values below R50 and above R1000 with user-friendly copy.
  - UI copy must state that card details are collected on an external hosted page.
  - Starting a top-up must not expose another owner’s data and must redirect only after a pending attempt exists.
  - Card-only behavior must be clear in the top-up UX.
  - Start failures caused by gateway or merchant configuration problems must show user-friendly copy without raw gateway diagnostics.
  - Owner wallet balance and top-up history must reflect authoritative successful settlement within 1 minute.
  - When overdue unresolved attempts exist, the page may invoke read-through reconciliation before rendering history.
  - This page requires bUnit coverage for amount validation, card-only messaging, start-failure copy, and owner-scoped history rendering.

## Route: `/portal/wallet/top-ups/return`

- **Access**: `[Authorize(Roles = "CompanyOwner")]`
- **Purpose**: Generic browser return landing route that passes untrusted query values to the Application layer for safe processing.
- **Rules**:
  - This route must never credit the wallet by itself.
  - If an exact owner-safe match exists, redirect to the matched attempt result route.
  - If no safe exact match exists, redirect to `/portal/wallet/top-ups/return/not-confirmed`.
  - Browser-return success without callback-backed settlement must still remain non-crediting.
  - This page requires bUnit coverage for informational-only routing behavior.

## Route: `/portal/wallet/top-ups/{attemptId}/return`

- **Access**: `[Authorize(Roles = "CompanyOwner")]`
- **Purpose**: Owner-scoped result page for a specific attempt.
- **Displayed fields**:
  - requested amount,
  - confirmed amount when available,
  - current status,
  - wallet-credit created / not-created message,
  - owner-safe display message.
- **Rules**:
  - Only the owning Company Owner may see this page.
  - Refreshing this page is read-only and idempotent apart from allowed read-through reconciliation of overdue attempts.
  - Browser-return success without authoritative server confirmation must not render as settled success.
  - Negative statuses shown here are limited to `Cancelled`, `Expired`, `Abandoned`, and `NotConfirmed`.
  - `Expired` and `Abandoned` must be displayed as distinct states.
  - Late authoritative notify success or cancellation may supersede `Expired`, `Abandoned`, or `NotConfirmed` for the same owner-safe attempt.
  - This page requires bUnit coverage for owner-scoped result rendering across completed, cancelled, expired, abandoned, and not-confirmed flows.

## Route: `/portal/wallet/top-ups/return/not-confirmed`

- **Access**: `[Authorize(Roles = "CompanyOwner")]`
- **Purpose**: Generic owner-safe outcome page.
- **Displayed copy**:
  - primary outcome: `Top-up not confirmed`
  - optional generic support guidance
- **Rules**:
  - The same outcome/copy family must be used for unmatched, foreign-owner, duplicate-finalized, and otherwise unsafe-to-disclose cases.
  - The page must not reveal whether any specific attempt exists or already completed.
  - The page must not show raw gateway diagnostics.
  - Callback-delivery failure and other non-trustworthy confirmation failures must also collapse to this safe outcome family unless a matched attempt page is already owner-safe.
  - This page requires bUnit coverage proving unified `Top-up not confirmed` messaging.

## Route: `/portal/admin/wallet-topups/review`

- **Access**: `[Authorize(Roles = "SiteAdministrator")]`
- **Purpose**: Internal review route for conflicting evidence, unmatched returns, and Payment Confirmation Record traceability.
- **Displayed fields**:
  - attempt identifier
  - owner/user identifier
  - requested amount
  - confirmed amount when available
  - authoritative status
  - Payment Confirmation Record identifier
  - normalization decision summary/reason
  - wallet activity identifier when a credit exists
  - safe correlation references (`m_payment_id`, `pf_payment_id`)
- **Rules**:
  - Non-admin users must be denied.
  - Only minimum non-sensitive evidence may be shown.
  - Raw gateway diagnostics, PAN, CVV, expiry, and secrets must never render.
  - This page is read-only and must not embed business-settlement logic in Razor.
  - This page requires bUnit coverage for authorization, minimum-evidence rendering, and privacy-safe display behavior.

## Security and UX rules

- Razor components must never collect, display, or log card details.
- All business decisions come from injected services, not inline Razor logic.
- All wallet top-up pages must load owner-scoped data only.
- Async operations must show loading and error states consistent with existing wallet UX.
- Owner-facing pages may show only non-sensitive PayFast evidence summaries.
- Internal review pages may show only admin-safe non-sensitive audit summaries.
