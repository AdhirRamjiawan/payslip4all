# SC-004 UAT Sign-off Pack

Use this template to record 20 sample PayFast wallet top-up attempts for release-candidate UAT.

| WalletTopUpAttempt.Id | Test date | Owner / tester | Outcome | Manual operator action required? (Yes/No) | Notes |
|---|---|---|---|---|---|
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |

- [ ] At least 18 of 20 attempts (≥ 90%) required no manual operator action to start, confirm, or reconcile payment.

## SC-002 freshness evidence

Capture one row per successful authoritative callback included in the release-candidate sample.

| WalletTopUpAttempt.Id | Callback received at | Balance visible at | History visible at | Within 1 minute for both? (Yes/No) | Notes |
|---|---|---|---|---|---|
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |

- [ ] At least 95% of sampled successful callbacks made both balance and history visible within 1 minute of callback receipt.

## SC-005 / FR-024 traceability and conflicting-evidence review

Record release-window checks proving traceability and audit-only treatment of post-final conflicting evidence.

| WalletTopUpAttempt.Id | WalletActivity.ReferenceId | Payment Confirmation Record Id | ServerConfirmed / SignatureVerified | Conflicting late evidence reviewed? (Yes/No) | Notes |
|---|---|---|---|---|---|
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |

- [ ] Every successful wallet credit in the release window is traceable from wallet activity to attempt to Payment Confirmation Record.
- [ ] Any late or conflicting evidence after a final outcome was retained for admin review and did not create a duplicate wallet credit.

## FR-026 operational evidence

Capture one example of each start-failure class during release-candidate verification.

| Failure class | Evidence source | Owner-safe message confirmed? (Yes/No) | Operator-visible classification confirmed? (Yes/No) | Notes |
|---|---|---|---|---|
| Gateway unavailable |  |  |  |  |
| Merchant misconfiguration |  |  |  |  |

- [ ] Gateway-unavailability and merchant-misconfiguration start failures were both captured with owner-safe messaging and operator-visible distinction.

## Admin review privacy evidence

| Scenario | SiteAdministrator view verified? (Yes/No) | Non-admin denied? (Yes/No) | Sensitive fields absent? (Yes/No) | Notes |
|---|---|---|---|---|
| Successful credit review |  |  |  |  |
| Conflicting late-evidence review |  |  |  |  |
| Unmatched return review |  |  |  |  |

- [ ] Admin review remained SiteAdministrator-only, read-only, and privacy-minimized for all sampled scenarios.

**Final reviewer approval**

- Reviewer name:
- Reviewer signature / approval note:
- Review date:
