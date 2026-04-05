using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Domain.Tests.Entities;

public class WalletTopUpAttemptTests
{
    [Fact]
    public void CreatePending_WithValidInput_SetsPendingDefaults()
    {
        var userId = Guid.NewGuid();

        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");

        Assert.Equal(userId, attempt.UserId);
        Assert.Equal(100m, attempt.RequestedAmount);
        Assert.Equal("ZAR", attempt.CurrencyCode);
        Assert.Equal("fake", attempt.ProviderKey);
        Assert.Equal(WalletTopUpAttemptStatus.Pending, attempt.Status);
        Assert.Null(attempt.ConfirmedChargedAmount);
        Assert.Null(attempt.CreditedWalletActivityId);
        Assert.Equal(attempt.CreatedAt.AddHours(1), attempt.AbandonAfterUtc);
    }

    [Fact]
    public void CreatePending_WithNonPositiveAmount_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => WalletTopUpAttempt.CreatePending(userId, 0m, "fake"));
    }

    [Fact]
    public void MarkCompleted_SetsConfirmedAmountSettlementAndTimestamps()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        var validatedAt = DateTimeOffset.UtcNow;
        var activityId = Guid.NewGuid();

        attempt.RegisterHostedSession("session-123", "token-123", validatedAt.AddMinutes(15));
        attempt.MarkCompleted(95m, "payment-123", validatedAt, activityId);

        Assert.Equal(WalletTopUpAttemptStatus.Completed, attempt.Status);
        Assert.Equal(95m, attempt.ConfirmedChargedAmount);
        Assert.Equal("payment-123", attempt.ProviderPaymentReference);
        Assert.Equal(activityId, attempt.CreditedWalletActivityId);
        Assert.Equal(validatedAt, attempt.LastValidatedAt);
        Assert.Equal(validatedAt, attempt.CompletedAt);
        Assert.Equal(validatedAt, attempt.AuthoritativeOutcomeAcceptedAt);
    }

    [Fact]
    public void MarkNotConfirmed_SetsExplicitStatusAndOutcomeFields()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        var now = DateTimeOffset.UtcNow;

        attempt.MarkNotConfirmed("not_confirmed", "Top-up not confirmed", now);

        Assert.Equal(WalletTopUpAttemptStatus.NotConfirmed, attempt.Status);
        Assert.Equal("not_confirmed", attempt.OutcomeReasonCode);
        Assert.Equal("Top-up not confirmed", attempt.OutcomeMessage);
        Assert.Equal(now, attempt.LastEvaluatedAt);
    }

    [Fact]
    public void MarkAbandoned_SetsExplicitStatus()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        var now = DateTimeOffset.UtcNow;

        attempt.MarkAbandoned(now);

        Assert.Equal(WalletTopUpAttemptStatus.Abandoned, attempt.Status);
        Assert.Equal("abandoned", attempt.OutcomeReasonCode);
    }

    [Fact]
    public void AcceptTrustworthyEvidence_SetsAuthoritativeFinalOutcome()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        var now = DateTimeOffset.UtcNow;
        var evidenceId = Guid.NewGuid();

        attempt.AcceptTrustworthyEvidence(evidenceId, PaymentReturnClaimedOutcome.Cancelled, null, "payment-123", now, null);

        Assert.Equal(WalletTopUpAttemptStatus.Cancelled, attempt.Status);
        Assert.Equal(evidenceId, attempt.AuthoritativeEvidenceId);
        Assert.Equal(now, attempt.AuthoritativeOutcomeAcceptedAt);
        Assert.Equal("payment-123", attempt.ProviderPaymentReference);
    }

    [Fact]
    public void FinalState_CannotTransitionToAnotherFinalState()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        var now = DateTimeOffset.UtcNow;

        attempt.RegisterHostedSession("session-123", "token-123", now.AddMinutes(15));
        attempt.MarkCancelled("cancelled", "Payment was cancelled.", now);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            attempt.MarkExpired("expired", "Payment expired.", now.AddMinutes(1)));

        Assert.Contains("final state", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Entity_DoesNotExposeCardFields()
    {
        var propertyNames = typeof(WalletTopUpAttempt)
            .GetProperties()
            .Select(p => p.Name.ToLowerInvariant())
            .ToList();

        Assert.DoesNotContain(propertyNames, name => name == "cardnumber" || name == "carddetails");
        Assert.DoesNotContain(propertyNames, name => name.Contains("cvv"));
        Assert.DoesNotContain(propertyNames, name => name.Contains("pan"));
        Assert.DoesNotContain(propertyNames, name => name == "expiry" || name == "expirydate");
    }
}
