using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Moq;
using Payslip4All.Domain.Enums;
using Payslip4All.Infrastructure.HostedPayments;
using System.Net;
using System.Text;

namespace Payslip4All.Infrastructure.Tests.HostedPayments;

public class PayFastHostedPaymentProviderTests
{
    private readonly PayFastHostedPaymentOptions _options = new()
    {
        MerchantId = "10000100",
        MerchantKey = "46f0cd694581a",
        PublicNotifyUrl = "https://notify.example.test/api/payments/payfast/notify"
    };

    private readonly Mock<ILogger<PayFastHostedPaymentProvider>> _logger = new();

    private PayFastHostedPaymentProvider CreateProvider(HttpMessageHandler? handler = null)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(nameof(PayFastHostedPaymentProvider)))
            .Returns(new HttpClient(handler ?? new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("VALID", Encoding.UTF8, "text/plain")
            }))));

        return new(_options, new PayFastSignatureVerifier(), httpClientFactory.Object, _logger.Object);
    }

    [Fact]
    public async Task StartHostedTopUpAsync_UsesCardOnlyAndNotifyUrl()
    {
        var attempt = Payslip4All.Domain.Entities.WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");

        var result = await CreateProvider().StartHostedTopUpAsync(
            attempt,
            new Uri("https://app.example.test/portal/wallet/top-ups/return"),
            new Uri("https://app.example.test/portal/wallet"));

        var uri = new Uri(result.RedirectUrl);
        var query = QueryHelpers.ParseQuery(uri.Query);

        Assert.Equal("cc", query["payment_method"].ToString());
        Assert.Equal(_options.PublicNotifyUrl, query["notify_url"].ToString());
        Assert.Equal(attempt.MerchantPaymentReference, query["m_payment_id"].ToString());
        Assert.Matches("^[a-f0-9]{32}$", query["signature"].ToString());
        Assert.Contains("item_name=Payslip4All+wallet+top-up", uri.Query, StringComparison.Ordinal);

        var returnUrl = new Uri(query["return_url"].ToString());
        var returnQuery = QueryHelpers.ParseQuery(returnUrl.Query);
        Assert.Equal("payfast", returnQuery["provider"].ToString());
        Assert.Equal(attempt.Id.ToString("D"), returnQuery["attemptId"].ToString());
    }

    [Fact]
    public async Task StartHostedTopUpAsync_WhenNotifyUrlIsNotPublic_Throws()
    {
        _options.PublicNotifyUrl = "http://localhost:5000/api/payments/payfast/notify";
        var attempt = Payslip4All.Domain.Entities.WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateProvider().StartHostedTopUpAsync(
            attempt,
            new Uri("https://app.example.test/portal/wallet/top-ups/return"),
            new Uri("https://app.example.test/portal/wallet")));

        Assert.Contains("notify_url", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartHostedTopUpAsync_WhenNotifyUrlPointsToPayFast_Throws()
    {
        _options.PublicNotifyUrl = "https://sandbox.payfast.co.za/eng/process";
        var attempt = Payslip4All.Domain.Entities.WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateProvider().StartHostedTopUpAsync(
            attempt,
            new Uri("https://app.example.test/portal/wallet/top-ups/return"),
            new Uri("https://app.example.test/portal/wallet")));

        Assert.Contains("not a PayFast URL", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartHostedTopUpAsync_WhenGatewayClientWouldFail_StillBuildsRedirectUrl()
    {
        var attempt = Payslip4All.Domain.Entities.WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");
        var provider = CreateProvider(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("gateway offline")));

        var result = await provider.StartHostedTopUpAsync(
            attempt,
            new Uri("https://app.example.test/portal/wallet/top-ups/return"),
            new Uri("https://app.example.test/portal/wallet"));

        Assert.StartsWith("https://", result.RedirectUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartHostedTopUpAsync_WhenMerchantConfigurationIsInvalid_LogsMerchantMisconfigurationCategory()
    {
        _options.MerchantId = string.Empty;
        var attempt = Payslip4All.Domain.Entities.WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateProvider().StartHostedTopUpAsync(
            attempt,
            new Uri("https://app.example.test/portal/wallet/top-ups/return"),
            new Uri("https://app.example.test/portal/wallet")));

        VerifyLogContains(LogLevel.Error, "MerchantMisconfiguration");
    }

    [Fact]
    public async Task ParseAuthoritativeEvidenceAsync_WithValidSignature_ReturnsTrustworthyNotifyEvidence()
    {
        var payload = PayFastTestData.CompletedNotifyPayload(Guid.NewGuid());
        payload["signature"] = new PayFastSignatureVerifier().ComputeNotificationSignature(payload, _options.Passphrase);

        var result = await CreateProvider().ParseAuthoritativeEvidenceAsync(payload);

        Assert.Equal("PayFastNotify", result.SourceChannel);
        Assert.Equal(PaymentReturnClaimedOutcome.Completed, result.ClaimedOutcome);
        Assert.Equal(PaymentReturnTrustLevel.Trustworthy, result.TrustLevel);
        Assert.True(result.SignatureVerified);
        Assert.True(result.ServerConfirmed);
    }

    [Fact]
    public async Task ParseAuthoritativeEvidenceAsync_WhenServerConfirmationFails_IsLowConfidence()
    {
        var payload = PayFastTestData.CompletedNotifyPayload(Guid.NewGuid());
        payload["signature"] = new PayFastSignatureVerifier().ComputeNotificationSignature(payload, _options.Passphrase);
        var provider = CreateProvider(new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("INVALID", Encoding.UTF8, "text/plain")
        })));

        var result = await provider.ParseAuthoritativeEvidenceAsync(payload);

        Assert.Equal(PaymentReturnTrustLevel.LowConfidence, result.TrustLevel);
        Assert.False(result.ServerConfirmed);
        Assert.Contains("server confirmation failed", result.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAuthoritativeEvidenceAsync_WithInvalidSignature_IsNotTrustworthy()
    {
        var payload = PayFastTestData.CompletedNotifyPayload(Guid.NewGuid());
        payload["signature"] = "invalid";

        var result = await CreateProvider().ParseAuthoritativeEvidenceAsync(payload);

        Assert.False(result.SignatureVerified);
        Assert.NotEqual(PaymentReturnTrustLevel.Trustworthy, result.TrustLevel);
    }

    [Fact]
    public async Task ParseAuthoritativeEvidenceAsync_StripsSensitiveFields_FromSafePayloadSnapshot()
    {
        var payload = PayFastTestData.CompletedNotifyPayload(Guid.NewGuid());
        payload["card_number"] = "4111111111111111";
        payload["cvv"] = "123";
        payload["expiry_date"] = "12/26";
        payload["raw_gateway_diagnostic"] = "secret";
        payload["signature"] = new PayFastSignatureVerifier().ComputeNotificationSignature(payload, _options.Passphrase);

        var result = await CreateProvider().ParseAuthoritativeEvidenceAsync(payload);

        Assert.DoesNotContain("card_number", result.SafePayloadSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cvv", result.SafePayloadSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expiry", result.SafePayloadSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("diagnostic", result.SafePayloadSnapshot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAuthoritativeEvidenceAsync_WithNonCardPayment_IsLowConfidence()
    {
        var payload = PayFastTestData.CompletedNotifyPayload(Guid.NewGuid());
        payload["payment_method"] = "eft";
        payload["signature"] = new PayFastSignatureVerifier().ComputeNotificationSignature(payload, _options.Passphrase);

        var result = await CreateProvider().ParseAuthoritativeEvidenceAsync(payload);

        Assert.Equal("eft", result.PaymentMethodCode);
        Assert.Equal(PaymentReturnTrustLevel.LowConfidence, result.TrustLevel);
        Assert.False(result.ServerConfirmed);
    }

    [Fact]
    public async Task StartHostedTopUpAsync_WhenSandboxConfigured_UsesSandboxProcessUrl()
    {
        _options.UseSandbox = true;
        var attempt = Payslip4All.Domain.Entities.WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");

        var result = await CreateProvider().StartHostedTopUpAsync(
            attempt,
            new Uri("https://app.example.test/portal/wallet/top-ups/return"),
            new Uri("https://app.example.test/portal/wallet"));

        Assert.StartsWith("https://sandbox.payfast.co.za/", result.RedirectUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartHostedTopUpAsync_WhenLiveModeConfigured_UsesLiveProcessUrl()
    {
        _options.UseSandbox = false;
        var attempt = Payslip4All.Domain.Entities.WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");

        var result = await CreateProvider().StartHostedTopUpAsync(
            attempt,
            new Uri("https://app.example.test/portal/wallet/top-ups/return"),
            new Uri("https://app.example.test/portal/wallet"));

        Assert.StartsWith("https://www.payfast.co.za/", result.RedirectUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAuthoritativeEvidenceAsync_WhenSandboxModeConfigured_CanStillBeTrustworthy()
    {
        _options.UseSandbox = true;
        var payload = PayFastTestData.CompletedNotifyPayload(Guid.NewGuid());
        payload["signature"] = new PayFastSignatureVerifier().ComputeNotificationSignature(payload, _options.Passphrase);

        var result = await CreateProvider().ParseAuthoritativeEvidenceAsync(payload);

        Assert.Equal("sandbox", result.EnvironmentMode);
        Assert.Equal(PaymentReturnTrustLevel.Trustworthy, result.TrustLevel);
        Assert.True(result.ServerConfirmed);
        Assert.DoesNotContain("sandbox", result.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAuthoritativeEvidenceAsync_PostsStep4ParameterString_WithoutSignature_AndWithEmptyFields()
    {
        string? postedBody = null;
        string? postedContentType = null;
        var payload = PayFastTestData.CompletedNotifyPayload(Guid.NewGuid());
        payload["custom_str5"] = string.Empty;
        payload["signature"] = new PayFastSignatureVerifier().ComputeNotificationSignature(payload, _options.Passphrase);
        var provider = CreateProvider(new StubHttpMessageHandler(async (request, _) =>
        {
            postedBody = await request.Content!.ReadAsStringAsync();
            postedContentType = request.Content.Headers.ContentType?.MediaType;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("VALID", Encoding.UTF8, "text/plain")
            };
        }));

        var result = await provider.ParseAuthoritativeEvidenceAsync(payload);

        Assert.Equal(PaymentReturnTrustLevel.Trustworthy, result.TrustLevel);
        Assert.Equal("application/x-www-form-urlencoded", postedContentType);
        Assert.DoesNotContain("signature=", postedBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("custom_str5=", postedBody, StringComparison.Ordinal);
        Assert.StartsWith("m_payment_id=", postedBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAuthoritativeEvidenceAsync_WhenStep4RequestThrows_LogsGatewayUnavailableCategory()
    {
        var payload = PayFastTestData.CompletedNotifyPayload(Guid.NewGuid());
        payload["signature"] = new PayFastSignatureVerifier().ComputeNotificationSignature(payload, _options.Passphrase);
        var provider = CreateProvider(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("gateway unavailable")));

        var result = await provider.ParseAuthoritativeEvidenceAsync(payload);

        Assert.Equal(PaymentReturnTrustLevel.LowConfidence, result.TrustLevel);
        Assert.False(result.ServerConfirmed);
        VerifyLogContains(LogLevel.Warning, "GatewayUnavailable");
    }

    private void VerifyLogContains(LogLevel level, string expectedText)
    {
        _logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(expectedText, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
