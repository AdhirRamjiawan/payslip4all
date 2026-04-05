using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Infrastructure.HostedPayments;

namespace Payslip4All.Infrastructure.Tests.HostedPayments;

public class FakeHostedPaymentProviderTests
{
    private readonly FakeHostedPaymentProvider _provider = new(new FakeHostedPaymentOptions());

    [Fact]
    public async Task StartHostedTopUpAsync_ReturnsSimulatorRedirect_WithProviderMetadata()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        var result = await _provider.StartHostedTopUpAsync(
            attempt,
            new Uri("https://app.test/portal/wallet/top-ups/return"),
            new Uri("https://app.test/portal/wallet"));

        Assert.Contains("/hosted-payments/fake", result.RedirectUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("provider=fake", result.RedirectUrl, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(result.ProviderSessionReference));
        Assert.False(string.IsNullOrWhiteSpace(result.ReturnCorrelationToken));
    }

    [Fact]
    public async Task ParseReturnEvidenceAsync_WhenAmountMissingForSuccess_ReturnsLowConfidenceEvidence()
    {
        var result = await _provider.ParseReturnEvidenceAsync(new Dictionary<string, string>
        {
            ["session"] = "session-123",
            ["token"] = "token-123",
            ["outcome"] = "succeeded"
        });

        Assert.Equal(PaymentReturnCorrelationDisposition.ExactMatch, result.CorrelationDisposition);
        Assert.Equal(PaymentReturnClaimedOutcome.Completed, result.ClaimedOutcome);
        Assert.Equal(PaymentReturnTrustLevel.LowConfidence, result.TrustLevel);
    }

    [Fact]
    public async Task ParseReturnEvidenceAsync_WhenBrowserReturnMatches_DoesNotPromoteEvidenceToVerified()
    {
        var result = await _provider.ParseReturnEvidenceAsync(new Dictionary<string, string>
        {
            ["session"] = "session-123",
            ["token"] = "token-123",
            ["outcome"] = "succeeded",
            ["amount"] = "100.00"
        });

        Assert.Equal(PaymentReturnCorrelationDisposition.ExactMatch, result.CorrelationDisposition);
        Assert.Equal(PaymentReturnClaimedOutcome.Completed, result.ClaimedOutcome);
        Assert.Equal(PaymentReturnTrustLevel.LowConfidence, result.TrustLevel);
        Assert.False(result.SignatureVerified);
        Assert.False(result.SourceVerified);
        Assert.False(result.ServerConfirmed);
    }

    [Fact]
    public async Task ValidateReturnAsync_WhenCorrelationDoesNotMatch_ReturnsUnverified()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));

        var result = await _provider.ValidateReturnAsync(
            attempt,
            new Dictionary<string, string>
            {
                ["session"] = "session-123",
                ["token"] = "other-token",
                ["outcome"] = "succeeded",
                ["amount"] = "100.00"
            });

        Assert.Equal(HostedPaymentOutcome.Unverified, result.Outcome);
        Assert.Null(result.ConfirmedChargedAmount);
    }
}
