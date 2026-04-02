using System.Globalization;
using System.Text.Json;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Infrastructure.HostedPayments;

public class FakeHostedPaymentProvider : IHostedPaymentProvider
{
    private readonly FakeHostedPaymentOptions _options;

    public FakeHostedPaymentProvider(FakeHostedPaymentOptions options)
    {
        _options = options;
    }

    public string ProviderKey => _options.ProviderKey;

    public Task<HostedPaymentSessionResult> StartHostedTopUpAsync(
        WalletTopUpAttempt attempt,
        Uri returnUrl,
        Uri? cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var sessionReference = $"session-{Guid.NewGuid():N}";
        var correlationToken = $"token-{Guid.NewGuid():N}";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var baseUri = new Uri(returnUrl.GetLeftPart(UriPartial.Authority));
        var simulatorUri = new Uri(baseUri,
            $"{_options.HostedPagePath}?attemptId={attempt.Id:D}" +
            $"&amount={attempt.RequestedAmount.ToString("0.00", CultureInfo.InvariantCulture)}" +
            "&currency=ZAR" +
            $"&provider={Uri.EscapeDataString(ProviderKey)}" +
            $"&session={Uri.EscapeDataString(sessionReference)}" +
            $"&token={Uri.EscapeDataString(correlationToken)}" +
            $"&returnUrl={Uri.EscapeDataString(returnUrl.ToString())}" +
            $"&cancelUrl={Uri.EscapeDataString((cancelUrl ?? new Uri(baseUri, "/portal/wallet")).ToString())}");

        return Task.FromResult(new HostedPaymentSessionResult(
            simulatorUri.ToString(),
            sessionReference,
            correlationToken,
            expiresAt));
    }

    public Task<HostedPaymentReturnResult> ValidateReturnAsync(
        WalletTopUpAttempt attempt,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default)
    {
        payload.TryGetValue("session", out var session);
        payload.TryGetValue("token", out var token);
        payload.TryGetValue("outcome", out var outcomeValue);
        payload.TryGetValue("paymentReference", out var paymentReference);
        payload.TryGetValue("amount", out var amountValue);

        if (!string.Equals(session, attempt.ProviderSessionReference, StringComparison.Ordinal) ||
            !string.Equals(token, attempt.ReturnCorrelationToken, StringComparison.Ordinal))
        {
            return Task.FromResult(new HostedPaymentReturnResult
            {
                AttemptId = attempt.Id,
                Outcome = HostedPaymentOutcome.Unverified,
                ProviderSessionReference = session,
                ValidatedAt = DateTimeOffset.UtcNow,
                FailureCode = "correlation_mismatch",
                FailureMessage = "We could not verify this payment return.",
                DisplayMessage = "We could not verify this payment yet. Your wallet has not been credited."
            });
        }

        var normalizedOutcome = NormalizeOutcome(outcomeValue);
        var result = new HostedPaymentReturnResult
        {
            AttemptId = attempt.Id,
            ProviderSessionReference = session,
            ProviderPaymentReference = string.IsNullOrWhiteSpace(paymentReference) ? null : paymentReference.Trim(),
            ValidatedAt = DateTimeOffset.UtcNow
        };

        switch (normalizedOutcome)
        {
            case PaymentReturnClaimedOutcome.Completed:
                if (!decimal.TryParse(amountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var confirmedAmount) || confirmedAmount <= 0m)
                {
                    result.Outcome = HostedPaymentOutcome.Unverified;
                    result.FailureCode = "invalid_amount";
                    result.FailureMessage = "The payment amount could not be verified.";
                    result.DisplayMessage = "We could not verify the charged amount yet. Your wallet has not been credited.";
                    return Task.FromResult(result);
                }

                result.Outcome = HostedPaymentOutcome.Succeeded;
                result.ConfirmedChargedAmount = confirmedAmount;
                result.DisplayMessage = "Wallet credited successfully.";
                return Task.FromResult(result);

            case PaymentReturnClaimedOutcome.Cancelled:
                result.Outcome = HostedPaymentOutcome.Cancelled;
                result.FailureCode = "cancelled";
                result.FailureMessage = "Payment was cancelled.";
                result.DisplayMessage = "Payment was cancelled. Your wallet was not credited.";
                return Task.FromResult(result);

            case PaymentReturnClaimedOutcome.Expired:
                result.Outcome = HostedPaymentOutcome.Expired;
                result.FailureCode = "expired";
                result.FailureMessage = "Payment expired.";
                result.DisplayMessage = "Payment expired. Your wallet was not credited.";
                return Task.FromResult(result);

            default:
                result.Outcome = HostedPaymentOutcome.Pending;
                result.FailureCode = "pending";
                result.FailureMessage = "Payment is still pending.";
                result.DisplayMessage = "Payment is still pending. Your wallet has not been credited yet.";
                return Task.FromResult(result);
        }
    }

    public Task<HostedPaymentReturnEvidenceDto> ParseReturnEvidenceAsync(
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default)
    {
        payload.TryGetValue("session", out var session);
        payload.TryGetValue("token", out var token);
        payload.TryGetValue("outcome", out var outcomeValue);
        payload.TryGetValue("paymentReference", out var paymentReference);
        payload.TryGetValue("amount", out var amountValue);

        var outcome = NormalizeOutcome(outcomeValue);
        var hasAmount = decimal.TryParse(amountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) && parsedAmount > 0m;
        var hasSession = !string.IsNullOrWhiteSpace(session);
        var hasToken = !string.IsNullOrWhiteSpace(token);
        var disposition = !hasToken
            ? PaymentReturnCorrelationDisposition.MissingData
            : hasSession
                ? PaymentReturnCorrelationDisposition.ExactMatch
                : PaymentReturnCorrelationDisposition.NoMatch;
        var trustLevel = disposition == PaymentReturnCorrelationDisposition.ExactMatch
            ? outcome == PaymentReturnClaimedOutcome.Completed && !hasAmount
                ? PaymentReturnTrustLevel.LowConfidence
                : PaymentReturnTrustLevel.Trustworthy
            : PaymentReturnTrustLevel.Untrusted;

        return Task.FromResult(new HostedPaymentReturnEvidenceDto
        {
            Id = Guid.NewGuid(),
            ProviderKey = ProviderKey,
            SourceChannel = "BrowserReturn",
            ProviderSessionReference = session,
            ProviderPaymentReference = string.IsNullOrWhiteSpace(paymentReference) ? null : paymentReference.Trim(),
            ReturnCorrelationToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim(),
            CorrelationDisposition = disposition,
            ClaimedOutcome = outcome == PaymentReturnClaimedOutcome.Unknown ? null : outcome,
            TrustLevel = trustLevel,
            ConfirmedChargedAmount = hasAmount ? parsedAmount : null,
            EvidenceOccurredAt = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            ValidatedAt = DateTimeOffset.UtcNow,
            SafePayloadSnapshot = JsonSerializer.Serialize(payload),
            ValidationMessage = trustLevel switch
            {
                PaymentReturnTrustLevel.Trustworthy => "Hosted return parsed successfully.",
                PaymentReturnTrustLevel.LowConfidence => "Hosted return matched but charged amount requires verification.",
                _ => "Hosted return could not be trusted."
            }
        });
    }

    private static PaymentReturnClaimedOutcome NormalizeOutcome(string? outcomeValue)
        => (outcomeValue ?? "pending").Trim().ToLowerInvariant() switch
        {
            "succeeded" or "success" or "completed" => PaymentReturnClaimedOutcome.Completed,
            "cancelled" or "canceled" => PaymentReturnClaimedOutcome.Cancelled,
            "expired" => PaymentReturnClaimedOutcome.Expired,
            _ => PaymentReturnClaimedOutcome.Unknown
        };
}
