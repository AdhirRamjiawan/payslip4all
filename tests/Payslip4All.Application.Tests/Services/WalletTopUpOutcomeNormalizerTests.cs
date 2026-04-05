using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.Tests.Services;

public class WalletTopUpOutcomeNormalizerTests
{
    [Fact]
    public async Task NormalizeAsync_WhenSandboxNotifyArrives_AcceptsAuthoritativeCompletion()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");
        var evidence = new PaymentReturnEvidence
        {
            SourceChannel = "PayFastNotify",
            CorrelationDisposition = PaymentReturnCorrelationDisposition.ExactMatch,
            ClaimedOutcome = PaymentReturnClaimedOutcome.Completed,
            TrustLevel = PaymentReturnTrustLevel.Trustworthy,
            SignatureVerified = true,
            SourceVerified = true,
            ServerConfirmed = true,
            PaymentMethodCode = "cc",
            EnvironmentMode = "sandbox",
            ConfirmedCurrencyCode = attempt.CurrencyCode,
            ConfirmedChargedAmount = 100m
        };

        var result = await new WalletTopUpOutcomeNormalizer().NormalizeAsync(evidence, attempt);

        Assert.True(result.IsAuthoritative);
        Assert.Equal("trustworthy_completed", result.ReasonCode);
        Assert.Equal("CreditCreated", result.WalletEffect);
        Assert.Equal(PaymentReturnClaimedOutcome.Completed, result.NormalizedOutcome);
    }
}
