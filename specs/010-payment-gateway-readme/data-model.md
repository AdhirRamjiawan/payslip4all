# Data Model: Document Payment Gateway Setup

## Overview

This feature does not introduce runtime persistence. Its data model is the conceptual structure the README must communicate so a maintainer can configure and verify the existing PayFast gateway without source-code discovery.

## Entities

### 1. Payment Gateway Setup Guide

**Purpose**: The top-level README section that explains how to configure and validate the real hosted payment gateway.

**Fields**:
- `gatewayName` — supported gateway name presented to maintainers (`PayFast`)
- `supportedFlow` — scope statement for the existing card-only hosted wallet top-up journey
- `audience` — intended readers (`developer`, `operator`)
- `secretHandlingRule` — rule that secrets stay in private or deployment-specific configuration
- `callbackRule` — summary of the public HTTPS notify requirement
- `verificationSummary` — short path for proving setup works
- `troubleshootingSummary` — index of common failures and what to check first

**Relationships**:
- Has many `Payment Configuration Value`
- Has one `Payment Confirmation Callback Requirement`
- Has many `Verification Scenario`
- Has many `Troubleshooting Case`

**Validation Rules**:
- Must identify one supported real payment gateway only.
- Must not include committed live secrets or unsupported payment methods.
- Must remain consistent with the current hosted card-payment flow.

### 2. Payment Configuration Value

**Purpose**: A concrete value a maintainer must understand or supply for payment setup.

**Fields**:
- `name` — configuration key name
- `purpose` — what the value controls
- `requiredFor` — where it is required (`local sandbox`, `live environment`, `all setups`)
- `sourceLocation` — where readers should supply it (`private local settings`, `environment variables`, `deployment secrets`)
- `sensitive` — whether the value must be treated as secret
- `examplePolicy` — whether example values may be shown directly, partially masked, or described only

**Known Values**:
- `HostedPayments:PayFast:ProviderKey`
- `HostedPayments:PayFast:UseSandbox`
- `HostedPayments:PayFast:MerchantId`
- `HostedPayments:PayFast:MerchantKey`
- `HostedPayments:PayFast:Passphrase`
- `HostedPayments:PayFast:PublicNotifyUrl`

**Validation Rules**:
- Sensitive values must not be shown as real committed secrets.
- Each value must have a plain-language explanation.
- Conditional values must say when they are required.

### 3. Payment Confirmation Callback Requirement

**Purpose**: The setup rule for trustworthy server-side payment confirmation.

**Fields**:
- `route` — application callback path for notify handling
- `publiclyReachable` — whether the callback must be reachable from outside the local machine
- `schemeRequirement` — secure transport requirement
- `disallowedHosts` — known invalid callback host patterns
- `businessReason` — why the callback matters to successful verification

**Validation Rules**:
- Route must point to the application notify path.
- Address must be public HTTPS.
- `localhost` and gateway-owned callback URLs are invalid examples.

### 4. Verification Scenario

**Purpose**: A manual scenario proving the documented setup is usable.

**Fields**:
- `name` — short scenario title
- `prerequisites` — what must already be configured
- `action` — what the maintainer does
- `expectedOutcome` — what indicates success
- `failureSignal` — what indicates misconfiguration

**Validation Rules**:
- Must stay technology-light and user-action oriented.
- Must cover at least configuration completeness, hosted checkout start, and callback-readiness understanding.

### 5. Troubleshooting Case

**Purpose**: A common setup problem and the first checks a maintainer should perform.

**Fields**:
- `symptom` — what the maintainer sees
- `likelyCause` — most probable configuration issue
- `readerAction` — what the README should tell them to verify next
- `safeOutcome` — what the app does when the issue exists

**Validation Rules**:
- Must focus on common setup issues, not deep implementation internals.
- Must not expose secrets or raw gateway diagnostics.

## State Model

### Payment Gateway Setup Status

1. `Documented` — the maintainer has identified every required setup value from the README.
2. `Configured` — the values have been supplied in the correct private or deployment-specific locations.
3. `Reachable` — the public HTTPS notify callback requirement has been satisfied.
4. `Verified` — the maintainer can start a hosted wallet top-up and understand how trustworthy confirmation is received.

**Transitions**:
- `Documented -> Configured`: all required configuration values are supplied safely.
- `Configured -> Reachable`: the notify callback becomes public and valid.
- `Reachable -> Verified`: the hosted wallet top-up can be launched and the verification path succeeds.
- Any state can regress if credentials are missing, mode is wrong, or the callback becomes invalid.
