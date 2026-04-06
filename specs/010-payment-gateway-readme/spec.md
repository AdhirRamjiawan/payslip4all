# Feature Specification: Document Payment Gateway Setup

**Feature Branch**: `010-payment-gateway-readme`  
**Created**: 2026-04-05  
**Status**: Ready for Planning  
**Input**: User description: "update the readme with information how to setup the payment gateway"

## Architecture & TDD Alignment *(mandatory for Payslip4All)*

- **Domain**: No business rules change in this feature; the specification is limited to clear operational guidance for an already-defined wallet top-up payment flow.
- **Application**: The setup guide must explain the prerequisites needed to start and verify a hosted wallet top-up without changing payment orchestration behaviour.
- **Infrastructure**: The setup guide must cover merchant configuration, environment-specific setup, callback reachability, and safe handling of gateway credentials.
- **Web**: The setup guide must describe how a user verifies configuration through the existing wallet top-up journey and payment return flow.
- **TDD expectation**: Each requirement below must map to acceptance scenarios that can be validated by reviewing the setup guide and following it in a clean environment before implementation is considered complete.

## Assumptions

- The payment gateway referenced by this feature is the current hosted PayFast card-funded wallet top-up flow already supported by Payslip4All.
- The repository README remains the primary onboarding document for local and deployment-oriented setup guidance.
- Merchant credentials and other sensitive payment values will be supplied through private or deployment-specific configuration rather than committed sample secrets.

### User Story 1 - Configure the gateway from the setup guide (Priority: P1)

As a developer or operator, I want the repository setup guide to show exactly which payment gateway values are required and where they belong so that I can enable hosted wallet top-ups without reading source code.

**Why this priority**: This is the core value of the feature. If the guide does not make the required setup inputs clear, the payment gateway cannot be enabled reliably.

**Independent Test**: Can be fully tested by handing the guide to a teammate unfamiliar with the payment setup and confirming they can identify every required configuration value and prepare a valid payment configuration without consulting other project artifacts.

**Acceptance Scenarios**:

1. **Given** a teammate is setting up payments for the first time, **When** they read the setup guide, **Then** they can see all required gateway configuration values, what each one is used for, and the approved place to supply them.
2. **Given** a teammate has only the setup guide, **When** they follow the documented steps, **Then** they can prepare the application to launch a hosted wallet top-up without needing to inspect implementation files.

---

### User Story 2 - Understand public callback and environment requirements (Priority: P2)

As a developer or operator, I want the setup guide to explain the difference between test and live payment modes and the public callback requirement so that I avoid misconfigurations that prevent trustworthy payment confirmation.

**Why this priority**: Payment setup is unsafe or incomplete if the environment mode and callback expectations are unclear. These mistakes can block payment confirmation even when other settings appear correct.

**Independent Test**: Can be fully tested by reviewing the guide and confirming it clearly describes when to use test versus live mode, what makes a callback URL valid, and what common misconfiguration symptoms to expect.

**Acceptance Scenarios**:

1. **Given** a teammate needs to configure the gateway for a non-production environment, **When** they read the setup guide, **Then** they can determine how to use test mode safely and what must change before moving to live processing.
2. **Given** a teammate is choosing the callback address, **When** they read the setup guide, **Then** they can tell that the payment confirmation callback must be publicly reachable, secure, and point to the application's payment notification path rather than a local-only address.

---

### User Story 3 - Verify the setup end to end (Priority: P3)

As a developer or operator, I want the setup guide to include a short verification path so that I can confirm the gateway is working after configuration and recognize the most common failure states quickly.

**Why this priority**: A setup guide is incomplete if it ends at configuration entry. Teams need a reliable way to confirm that hosted checkout starts and returns are handled as expected.

**Independent Test**: Can be fully tested by following the documented verification path after configuration and confirming that the reader can launch a hosted payment, observe the expected callback behaviour, and identify the documented next step when setup fails.

**Acceptance Scenarios**:

1. **Given** the payment configuration has been supplied, **When** a teammate follows the verification steps, **Then** they can confirm that a hosted wallet top-up starts successfully and that the application has a documented way to receive payment confirmation.
2. **Given** the verification flow does not succeed, **When** the teammate checks the setup guide, **Then** they can find troubleshooting guidance for the most likely setup mistakes without exposing sensitive merchant details in the documentation.

---

### Edge Cases

- The guide omits a required merchant value and a reader believes the payment gateway is fully configured when it is not.
- The guide does not make clear that the payment confirmation callback cannot use `localhost` or another private-only address.
- A reader enables the wrong payment mode for the target environment and expects live settlement from a test setup.
- A reader stores merchant credentials in committed configuration instead of a private or deployment-specific location.
- A reader can start a hosted checkout but cannot confirm why wallet credit never completes because callback prerequisites were not explained.
- The guide describes a setup path that conflicts with the current hosted PayFast card-funded wallet top-up flow supported by the application.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system documentation MUST identify the currently supported payment gateway for wallet top-ups and make clear that this guide applies to that payment flow only.
- **FR-002**: The setup guide MUST list every required gateway configuration value needed before a hosted payment can be started, along with a plain-language explanation of each value's purpose.
- **FR-003**: The setup guide MUST explain where maintainers are expected to supply payment gateway values for local development and deployed environments without instructing them to commit live secrets to shared source control.
- **FR-004**: The setup guide MUST explain the difference between test and live payment modes and describe when each mode is appropriate.
- **FR-005**: The setup guide MUST describe the payment confirmation callback requirement, including that the callback address must be publicly reachable, secure, and targeted at the application's payment notification route.
- **FR-006**: The setup guide MUST explain how a maintainer can verify that the configured gateway starts a hosted wallet top-up successfully after setup.
- **FR-007**: The setup guide MUST include troubleshooting guidance for the most likely setup failures, including incomplete credentials, invalid callback configuration, and incorrect environment mode.
- **FR-008**: The setup guide MUST remain consistent with the existing hosted PayFast card-funded wallet top-up journey and MUST NOT describe unsupported payment methods or unsupported settlement behaviour.

### Key Entities *(include if feature involves data)*

- **Payment Gateway Setup Guide**: The repository-facing instructions that explain prerequisites, configuration inputs, environment choices, verification steps, and troubleshooting needed to enable hosted wallet top-ups.
- **Payment Configuration Values**: The merchant and environment-specific inputs a maintainer must supply so the application can start hosted payments and receive trustworthy payment confirmations.
- **Payment Confirmation Callback**: The public application address used by the gateway to report payment results back to Payslip4All so wallet top-ups can be confirmed reliably.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a clean environment, a maintainer can configure the payment gateway and reach the first hosted wallet-top-up screen in 15 minutes or less using the setup guide alone.
- **SC-002**: The setup guide contains 100% of the required payment setup inputs, callback prerequisites, and environment-selection decisions needed before a first payment verification attempt.
- **SC-003**: In review, 90% of maintainers following the guide can correctly identify how to resolve the documented common setup failures without consulting source code or private tribal knowledge.
- **SC-004**: After following the documented verification path, a maintainer can determine whether payment setup is working or misconfigured in one pass without needing undocumented investigation steps.
