# Quickstart: Document Payment Gateway Setup

## Purpose

Validate that the README update gives a first-time maintainer enough information to configure and verify the existing PayFast payment gateway without reading source code.

## Prerequisites

- Payslip4All checked out locally
- .NET 8 SDK installed
- Access to PayFast sandbox or live merchant credentials outside shared source control
- A public HTTPS address that can forward PayFast notify requests to the local or deployed app
- A test `CompanyOwner` account in Payslip4All

## Scenario 1: Identify required setup values from the README alone

1. Open `README.md`.
2. Locate the payment gateway setup section.
3. Confirm the guide clearly identifies:
   - the supported gateway,
   - every required PayFast configuration key,
   - where to place secrets safely,
   - the public notify callback requirement,
   - the difference between sandbox and live mode.

## Scenario 2: Configure a local sandbox setup

1. Follow the README instructions to place sandbox credentials in private local configuration.
2. Supply a public HTTPS notify URL for `/api/payments/payfast/notify`.
3. Start the web app.
4. Confirm the instructions are sufficient to reach a running configuration without checking source files.

## Scenario 3: Verify hosted checkout start

1. Sign in as a `CompanyOwner`.
2. Open `/portal/wallet`.
3. Start a wallet top-up using the README verification flow.
4. Confirm:
   - the app redirects to the hosted PayFast payment page,
   - Payslip4All does not ask for card details directly,
   - the README explains that browser return alone does not prove wallet credit.

## Scenario 4: Validate callback-readiness understanding

1. Read the README callback guidance.
2. Confirm it is clear that:
   - the notify callback must be public HTTPS,
   - `localhost` is not valid for gateway callbacks,
   - the notify route is the trustworthy confirmation path.

## Scenario 5: Use troubleshooting guidance

1. Break one setup prerequisite at a time:
   - remove a merchant credential,
   - switch to the wrong environment mode,
   - set the notify URL to a private or invalid value.
2. Confirm the README tells the maintainer what to check first for each case.

## Validation Outcome

The feature is ready for implementation when the README contract can be satisfied by the planned documentation changes and the five scenarios above can be completed using the guide alone.
