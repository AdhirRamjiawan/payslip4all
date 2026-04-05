using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Infrastructure.HostedPayments;

public sealed class PayFastHostedPaymentProvider : IHostedPaymentProvider
{
    private readonly PayFastHostedPaymentOptions _options;
    private readonly PayFastSignatureVerifier _signatureVerifier;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PayFastHostedPaymentProvider> _logger;

    public PayFastHostedPaymentProvider(
        PayFastHostedPaymentOptions options,
        PayFastSignatureVerifier signatureVerifier,
        IHttpClientFactory httpClientFactory,
        ILogger<PayFastHostedPaymentProvider> logger)
    {
        _options = options;
        _signatureVerifier = signatureVerifier;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string ProviderKey => _options.ProviderKey;

    public Task<HostedPaymentSessionResult> StartHostedTopUpAsync(
        WalletTopUpAttempt attempt,
        Uri returnUrl,
        Uri? cancelUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _options.ValidateForStart();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "PayFast start validation failed. FailureCategory: {FailureCategory}", "MerchantMisconfiguration");
            throw;
        }

        return StartHostedTopUpCoreAsync(attempt, returnUrl, cancelUrl, cancellationToken);
    }

    private Task<HostedPaymentSessionResult> StartHostedTopUpCoreAsync(
        WalletTopUpAttempt attempt,
        Uri returnUrl,
        Uri? cancelUrl,
        CancellationToken cancellationToken)
    {
        var correlationToken = $"pf-{Guid.NewGuid():N}";
        var sessionReference = $"pf-session-{Guid.NewGuid():N}";
        var deadline = DateTimeOffset.UtcNow.AddMinutes(15);
        var payload = new List<KeyValuePair<string, string?>>
        {
            new("merchant_id", _options.MerchantId),
            new("merchant_key", _options.MerchantKey),
            new("return_url", AppendProvider(returnUrl.ToString(), attempt.Id)),
            new("cancel_url", AppendProvider((cancelUrl ?? returnUrl).ToString(), attempt.Id)),
            new("notify_url", _options.PublicNotifyUrl),
            new("m_payment_id", attempt.MerchantPaymentReference),
            new("amount", attempt.RequestedAmount.ToString("0.00", CultureInfo.InvariantCulture)),
            new("item_name", _options.ItemName),
            new("custom_str1", attempt.UserId.ToString()),
            new("custom_str2", correlationToken),
            new("custom_str3", sessionReference),
            new("custom_str4", attempt.CurrencyCode),
            new("payment_method", "cc")
        };

        var redirectUrl = $"{_options.GetProcessUrl()}?{_signatureVerifier.BuildQueryString(payload, _options.Passphrase)}";
        return Task.FromResult(new HostedPaymentSessionResult(
            redirectUrl,
            sessionReference,
            correlationToken,
            deadline));
    }

    public Task<HostedPaymentReturnResult> ValidateReturnAsync(
        WalletTopUpAttempt attempt,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HostedPaymentReturnResult
        {
            AttemptId = attempt.Id,
            Outcome = HostedPaymentOutcome.Unverified,
            ProviderSessionReference = payload.TryGetValue("custom_str3", out var session) ? session : null,
            ProviderPaymentReference = payload.TryGetValue("pf_payment_id", out var paymentId) ? paymentId : null,
            ValidatedAt = DateTimeOffset.UtcNow,
            FailureCode = "browser_return_informational",
            FailureMessage = "Top-up not confirmed",
            DisplayMessage = "Top-up not confirmed"
        });
    }

    public Task<HostedPaymentReturnEvidenceDto> ParseReturnEvidenceAsync(
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default)
    {
        return BuildEvidenceAsync(payload, "BrowserReturn", false, cancellationToken);
    }

    public Task<HostedPaymentReturnEvidenceDto> ParseAuthoritativeEvidenceAsync(
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default)
    {
        return BuildEvidenceAsync(payload, "PayFastNotify", true, cancellationToken);
    }

    private async Task<HostedPaymentReturnEvidenceDto> BuildEvidenceAsync(
        IReadOnlyDictionary<string, string> payload,
        string sourceChannel,
        bool authoritative,
        CancellationToken cancellationToken)
    {
        payload.TryGetValue("m_payment_id", out var merchantPaymentReference);
        payload.TryGetValue("pf_payment_id", out var providerPaymentReference);
        payload.TryGetValue("custom_str1", out var ownerUserIdValue);
        payload.TryGetValue("custom_str2", out var correlationToken);
        payload.TryGetValue("custom_str3", out var sessionReference);
        payload.TryGetValue("payment_method", out var paymentMethodCode);
        payload.TryGetValue("payment_status", out var paymentStatus);
        payload.TryGetValue("amount_gross", out var amountValue);
        payload.TryGetValue("amount", out var browserAmountValue);
        payload.TryGetValue("currency", out var currencyValue);
        payload.TryGetValue("custom_str4", out var fallbackCurrency);

        var hasAmount = decimal.TryParse(amountValue ?? browserAmountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount);
        var notificationParameterString = authoritative
            ? _signatureVerifier.BuildNotificationParameterString(payload)
            : null;
        var signatureVerified = authoritative && _signatureVerifier.VerifyNotification(payload, _options.Passphrase);
        var cardOnly = string.IsNullOrWhiteSpace(paymentMethodCode)
            || string.Equals(paymentMethodCode, "cc", StringComparison.OrdinalIgnoreCase);
        var serverConfirmed = authoritative && signatureVerified && cardOnly
            && !string.IsNullOrWhiteSpace(notificationParameterString)
            && await ConfirmWithPayFastAsync(notificationParameterString, cancellationToken);
        var trustworthyNotify = authoritative && signatureVerified && cardOnly && serverConfirmed;
        var trustLevel = trustworthyNotify
            ? PaymentReturnTrustLevel.Trustworthy
            : sourceChannel == "BrowserReturn"
                ? PaymentReturnTrustLevel.Untrusted
                : PaymentReturnTrustLevel.LowConfidence;

        return new HostedPaymentReturnEvidenceDto
        {
            Id = Guid.NewGuid(),
            ProviderKey = ProviderKey,
            SourceChannel = sourceChannel,
            ProviderSessionReference = sessionReference,
            ProviderPaymentReference = providerPaymentReference,
            MerchantPaymentReference = merchantPaymentReference,
            ReturnCorrelationToken = correlationToken,
            OwnerUserId = Guid.TryParse(ownerUserIdValue, out var ownerUserId) ? ownerUserId : null,
            CorrelationDisposition = string.IsNullOrWhiteSpace(merchantPaymentReference)
                ? PaymentReturnCorrelationDisposition.MissingData
                : PaymentReturnCorrelationDisposition.ExactMatch,
            ClaimedOutcome = NormalizeOutcome(paymentStatus),
            TrustLevel = trustLevel,
            PaymentMethodCode = string.IsNullOrWhiteSpace(paymentMethodCode) ? "cc" : paymentMethodCode,
            EnvironmentMode = _options.UseSandbox ? "sandbox" : "live",
            SignatureVerified = signatureVerified,
            SourceVerified = trustworthyNotify,
            ServerConfirmed = serverConfirmed,
            ConfirmedChargedAmount = hasAmount ? parsedAmount : null,
            ConfirmedCurrencyCode = string.IsNullOrWhiteSpace(currencyValue) ? fallbackCurrency ?? "ZAR" : currencyValue,
            EvidenceOccurredAt = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            ValidatedAt = DateTimeOffset.UtcNow,
            SafePayloadSnapshot = JsonSerializer.Serialize(
                payload.Where(kvp => !IsSensitiveKey(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)),
            ValidationMessage = !authoritative || !signatureVerified
                ? authoritative ? "PayFast signature verification failed." : "PayFast payload parsed successfully."
                : !cardOnly
                    ? "PayFast notify payload was rejected because it was not a card payment."
                    : !serverConfirmed
                        ? "PayFast server confirmation failed."
                        : "PayFast payload parsed successfully."
        };
    }

    private async Task<bool> ConfirmWithPayFastAsync(
        string parameterString,
        CancellationToken cancellationToken)
    {
        try
        {
            using var content = new StringContent(parameterString, Encoding.ASCII, "application/x-www-form-urlencoded");

            using var response = await _httpClientFactory.CreateClient(nameof(PayFastHostedPaymentProvider))
                .PostAsync(_options.GetValidationUrl(), content, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            return string.Equals(body, "VALID", StringComparison.OrdinalIgnoreCase);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PayFast server confirmation request failed. FailureCategory: {FailureCategory}", "GatewayUnavailable");
            return false;
        }
    }

    private string AppendProvider(string url, Guid attemptId)
    {
        var uri = new Uri(url);
        return QueryHelpers.AddQueryString(uri.ToString(), new Dictionary<string, string?>
        {
            ["provider"] = ProviderKey,
            ["attemptId"] = attemptId.ToString("D")
        });
    }

    private static PaymentReturnClaimedOutcome NormalizeOutcome(string? outcome)
        => (outcome ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "complete" or "completed" => PaymentReturnClaimedOutcome.Completed,
            "cancelled" or "canceled" => PaymentReturnClaimedOutcome.Cancelled,
            _ => PaymentReturnClaimedOutcome.Unknown
        };

    private static bool IsSensitiveKey(string key)
        => key.Contains("card", StringComparison.OrdinalIgnoreCase)
           || key.Contains("cvv", StringComparison.OrdinalIgnoreCase)
           || key.Contains("pan", StringComparison.OrdinalIgnoreCase)
           || key.Contains("expiry", StringComparison.OrdinalIgnoreCase)
           || key.Contains("diagnostic", StringComparison.OrdinalIgnoreCase);
}
