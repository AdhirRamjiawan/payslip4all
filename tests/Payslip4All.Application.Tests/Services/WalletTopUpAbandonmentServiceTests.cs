using Moq;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.Tests.Services;

public class WalletTopUpAbandonmentServiceTests
{
    private readonly Mock<IWalletTopUpAttemptRepository> _attemptRepository = new();
    private readonly Mock<IOutcomeNormalizationDecisionRepository> _decisionRepository = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();

    private WalletTopUpAbandonmentService CreateService(DateTimeOffset now)
    {
        _timeProvider.SetupGet(x => x.UtcNow).Returns(now);
        return new WalletTopUpAbandonmentService(
            _attemptRepository.Object,
            _decisionRepository.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task ReconcileAttemptIfDueAsync_WhenPendingAtDeadline_MarksExpired()
    {
        var now = new DateTimeOffset(2026, 4, 5, 12, 0, 0, TimeSpan.Zero);
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");
        attempt.NextReconciliationDueAt = now;

        var sut = CreateService(now);

        var reconciled = await sut.ReconcileAttemptIfDueAsync(attempt, "ScheduledSweep");

        Assert.True(reconciled);
        Assert.Equal(WalletTopUpAttemptStatus.Expired, attempt.Status);
        Assert.Equal(now.AddMinutes(1), attempt.NextReconciliationDueAt);
        _attemptRepository.Verify(r => r.UpdateAsync(attempt, It.IsAny<CancellationToken>()), Times.Once);
        _decisionRepository.Verify(r => r.AddAsync(
            It.Is<OutcomeNormalizationDecision>(d =>
                d.TriggerSource == "ScheduledSweep"
                && d.AuthoritativeOutcomeAfter == WalletTopUpAttemptStatus.Expired.ToString()
                && d.DecisionReasonCode == "expired"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReconcileAttemptIfDueAsync_WhenExpiredOnFollowUp_MarksAbandoned()
    {
        var now = new DateTimeOffset(2026, 4, 5, 12, 1, 0, TimeSpan.Zero);
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");
        attempt.MarkExpired("expired", "Top-up not confirmed", now.AddMinutes(-1), now);

        var sut = CreateService(now);

        var reconciled = await sut.ReconcileAttemptIfDueAsync(attempt, "ReadThroughHistory");

        Assert.True(reconciled);
        Assert.Equal(WalletTopUpAttemptStatus.Abandoned, attempt.Status);
        Assert.Null(attempt.NextReconciliationDueAt);
        _decisionRepository.Verify(r => r.AddAsync(
            It.Is<OutcomeNormalizationDecision>(d =>
                d.TriggerSource == "ReadThroughHistory"
                && d.AuthoritativeOutcomeBefore == WalletTopUpAttemptStatus.Expired.ToString()
                && d.AuthoritativeOutcomeAfter == WalletTopUpAttemptStatus.Abandoned.ToString()
                && d.DecisionReasonCode == "abandoned"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReconcileAttemptIfDueAsync_WhenAttemptIsNotDue_DoesNothing()
    {
        var now = new DateTimeOffset(2026, 4, 5, 12, 0, 0, TimeSpan.Zero);
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");
        attempt.NextReconciliationDueAt = now.AddMinutes(5);

        var sut = CreateService(now);

        var reconciled = await sut.ReconcileAttemptIfDueAsync(attempt, "ReadThroughResult");

        Assert.False(reconciled);
        Assert.Equal(WalletTopUpAttemptStatus.Pending, attempt.Status);
        _attemptRepository.Verify(r => r.UpdateAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        _decisionRepository.Verify(r => r.AddAsync(It.IsAny<OutcomeNormalizationDecision>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
