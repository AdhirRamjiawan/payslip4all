# Contract: README Payment Gateway Setup

## Purpose

Define the minimum content contract for the README changes required to document Payslip4All's real payment gateway setup.

## Audience

- Developers configuring the app locally
- Operators configuring deployed environments

## Scope

This contract covers README guidance for the existing PayFast-hosted wallet top-up flow only. It does not define new runtime APIs or payment behavior.

## Required README Sections

### 1. Payment Gateway Overview

The README MUST:
- identify PayFast as the documented real payment gateway,
- state that the flow is the hosted PayFast card-funded wallet top-up journey,
- avoid implying support for other payment gateways or other payment methods.

### 2. Required Configuration Values

The README MUST include a configuration table or equivalent structured list covering:

| Key | Must explain |
|-----|--------------|
| `HostedPayments:PayFast:ProviderKey` | Expected provider key and why it identifies the gateway |
| `HostedPayments:PayFast:UseSandbox` | How test and live mode are selected |
| `HostedPayments:PayFast:MerchantId` | Merchant account identifier requirement |
| `HostedPayments:PayFast:MerchantKey` | Merchant credential requirement |
| `HostedPayments:PayFast:Passphrase` | Optional/required passphrase guidance aligned with merchant setup |
| `HostedPayments:PayFast:PublicNotifyUrl` | Public HTTPS callback requirement and why it matters |

### 3. Secret Handling Rules

The README MUST:
- tell readers not to commit live or sandbox merchant secrets to shared source control,
- direct readers to private local settings, environment variables, or deployment secrets,
- MUST NOT embed real reusable secrets in examples and MUST use placeholder values instead.

### 4. Environment Guidance

The README MUST:
- explain when to use sandbox mode,
- explain when to use live mode,
- make clear that changing environments is a configuration choice rather than a code change.

### 5. Callback Guidance

The README MUST:
- document both the Payslip4All notify callback route path (`/api/payments/payfast/notify`) and how maintainers set the full `HostedPayments:PayFast:PublicNotifyUrl` value (for example, `https://your-domain.example/api/payments/payfast/notify`),
- state that the callback must be publicly reachable over HTTPS,
- state that `localhost` is invalid for gateway callbacks,
- avoid treating the browser return flow as the trustworthy confirmation mechanism.

### 6. Verification Flow

The README MUST include a short walkthrough that lets a maintainer:
1. supply the required values,
2. start the application,
3. open the wallet top-up flow,
4. confirm that checkout redirects to the hosted payment page,
5. understand that trustworthy confirmation depends on the notify callback.

### 7. Troubleshooting

The README MUST include first-pass troubleshooting guidance for:
- missing or incomplete merchant credentials,
- invalid, private, or non-HTTPS notify callback configuration,
- incorrect sandbox/live mode selection.

## Acceptance Conditions

The contract is satisfied when:

1. A first-time maintainer can identify all required payment setup values from the README alone.
2. The README gives a safe, non-secret-bearing example path for local and deployed configuration.
3. The README explains why hosted checkout may start yet trustworthy confirmation still fail.
4. The README stays consistent with the current hosted PayFast card-funded wallet top-up flow and does not over-promise unsupported behavior.
