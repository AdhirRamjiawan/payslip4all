using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.HostedPayments;

namespace Payslip4All.Infrastructure.Tests.HostedPayments;

public class FakeHostedPaymentProviderTests
{
    private readonly FakeHostedPaymentProvider _provider = new(new FakeHostedPaymentOptions());

    [Fact]
    public async Task StartHostedTopUpAsync_ReturnsSimulatorRedirect()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        var result = await _provider.StartHostedTopUpAsync(
            attempt,
            new Uri("https://app.test/portal/wallet/top-ups/123/return"),
            new Uri("https://app.test/portal/wallet"));

        Assert.Contains("/hosted-payments/fake", result.RedirectUrl, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(result.ProviderSessionReference));
        Assert.False(string.IsNullOrWhiteSpace(result.ReturnCorrelationToken));
    }

    [Fact]
    public async Task ValidateReturnAsync_WhenSuccessfulWithDifferentAmount_ReturnsSucceededOutcome()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));

        var result = await _provider.ValidateReturnAsync(
            attempt,
            new Dictionary<string, string>
            {
                ["session"] = "session-123",
                ["token"] = "token-123",
                ["outcome"] = "succeeded",
                ["amount"] = "95.00",
                ["paymentReference"] = "payment-123"
            });

        Assert.Equal(HostedPaymentOutcome.Succeeded, result.Outcome);
        Assert.Equal(95m, result.ConfirmedChargedAmount);
        Assert.Equal("payment-123", result.ProviderPaymentReference);
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
