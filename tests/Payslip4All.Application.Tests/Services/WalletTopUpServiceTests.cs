using Moq;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.Tests.Services;

public class WalletTopUpServiceTests
{
    private readonly Mock<IWalletTopUpAttemptRepository> _attemptRepository = new();
    private readonly Mock<IHostedPaymentProvider> _provider = new();
    private readonly Mock<IWalletRepository> _walletRepository = new();

    private WalletTopUpService CreateService()
    {
        _provider.SetupGet(p => p.ProviderKey).Returns("fake");
        return new WalletTopUpService(
            _attemptRepository.Object,
            new[] { _provider.Object },
            _walletRepository.Object);
    }

    [Fact]
    public async Task StartHostedTopUpAsync_PersistsPendingAttemptBeforeRedirect()
    {
        var userId = Guid.NewGuid();
        WalletTopUpAttempt? createdAttempt = null;
        var sequence = new MockSequence();
        _attemptRepository
            .InSequence(sequence)
            .Setup(r => r.AddAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()))
            .Callback<WalletTopUpAttempt, CancellationToken>((attempt, _) => createdAttempt = attempt)
            .Returns(Task.CompletedTask);

        _provider
            .InSequence(sequence)
            .Setup(p => p.StartHostedTopUpAsync(
                It.IsAny<WalletTopUpAttempt>(),
                It.IsAny<Uri>(),
                It.IsAny<Uri?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HostedPaymentSessionResult(
                "https://example.test/hosted",
                "session-123",
                "token-123",
                DateTimeOffset.UtcNow.AddMinutes(15)));

        _attemptRepository
            .InSequence(sequence)
            .Setup(r => r.UpdateAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateService().StartHostedTopUpAsync(new StartWalletTopUpCommand
        {
            UserId = userId,
            RequestedAmount = 100m,
            ReturnUrl = "https://app.test/portal/wallet/top-ups/return"
        });

        Assert.NotNull(createdAttempt);
        Assert.Equal(WalletTopUpAttemptStatus.Pending, result.Status);
        Assert.Equal("https://example.test/hosted", result.RedirectUrl);
        Assert.Equal(createdAttempt!.Id, result.WalletTopUpAttemptId);
        _attemptRepository.Verify(r => r.AddAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
        _attemptRepository.Verify(r => r.UpdateAsync(It.Is<WalletTopUpAttempt>(a =>
            a.ProviderSessionReference == "session-123" &&
            a.ReturnCorrelationToken == "token-123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinalizeHostedReturnAsync_OnValidatedSuccess_SettlesUsingConfirmedAmount()
    {
        var userId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));
        var activityId = Guid.NewGuid();
        WalletTopUpAttempt? settledAttempt = null;

        _attemptRepository
            .Setup(r => r.GetByIdAsync(attempt.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _provider.SetupGet(p => p.ProviderKey).Returns("fake");
        _provider
            .Setup(p => p.ValidateReturnAsync(attempt, It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HostedPaymentReturnResult
            {
                AttemptId = attempt.Id,
                Outcome = HostedPaymentOutcome.Succeeded,
                ProviderSessionReference = "session-123",
                ProviderPaymentReference = "payment-123",
                ConfirmedChargedAmount = 95m,
                ValidatedAt = DateTimeOffset.UtcNow,
                DisplayMessage = "Top-up completed."
            });
        var sequence = new MockSequence();
        _attemptRepository
            .InSequence(sequence)
            .Setup(r => r.UpdateAsync(It.Is<WalletTopUpAttempt>(a =>
                a.ConfirmedChargedAmount == 95m &&
                a.ProviderPaymentReference == "payment-123" &&
                a.LastValidatedAt.HasValue), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _attemptRepository
            .InSequence(sequence)
            .Setup(r => r.SettleSuccessfulAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()))
            .Callback<WalletTopUpAttempt, CancellationToken>((value, _) =>
            {
                settledAttempt = new WalletTopUpAttempt
                {
                    UserId = value.UserId,
                    RequestedAmount = value.RequestedAmount,
                    ConfirmedChargedAmount = value.ConfirmedChargedAmount,
                    CurrencyCode = value.CurrencyCode,
                    Status = value.Status,
                    ProviderKey = value.ProviderKey,
                    ProviderSessionReference = value.ProviderSessionReference,
                    ProviderPaymentReference = value.ProviderPaymentReference,
                    ReturnCorrelationToken = value.ReturnCorrelationToken,
                    FailureCode = value.FailureCode,
                    FailureMessage = value.FailureMessage,
                    CreditedWalletActivityId = value.CreditedWalletActivityId,
                    UpdatedAt = value.UpdatedAt,
                    RedirectedAt = value.RedirectedAt,
                    LastValidatedAt = value.LastValidatedAt,
                    CompletedAt = value.CompletedAt,
                    HostedPageDeadline = value.HostedPageDeadline
                };
                typeof(WalletTopUpAttempt)
                    .GetProperty(nameof(WalletTopUpAttempt.Id))!
                    .SetValue(settledAttempt, value.Id);
            })
            .ReturnsAsync(new WalletTopUpSettlementResult
            {
                WalletId = userId,
                WalletActivityId = activityId,
                WalletBalance = 95m,
                CreditedNow = true
            });

        var result = await CreateService().FinalizeHostedReturnAsync(new FinalizeWalletTopUpReturnCommand
        {
            UserId = userId,
            WalletTopUpAttemptId = attempt.Id,
            ReturnPayload = new Dictionary<string, string>
            {
                ["outcome"] = "succeeded"
            }
        });

        Assert.Equal(WalletTopUpAttemptStatus.Completed, result.Status);
        Assert.Equal(95m, result.ConfirmedChargedAmount);
        Assert.Equal(95m, result.WalletBalance);
        Assert.Equal(activityId, result.CreditedWalletActivityId);
        Assert.NotNull(settledAttempt);
        Assert.Equal(WalletTopUpAttemptStatus.Pending, settledAttempt!.Status);
        Assert.Equal(95m, settledAttempt.ConfirmedChargedAmount);
        Assert.Equal("payment-123", settledAttempt.ProviderPaymentReference);
        _attemptRepository.Verify(r => r.UpdateAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
        _attemptRepository.Verify(r => r.SettleSuccessfulAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinalizeHostedReturnAsync_WhenReplayOccurs_ReturnsExistingCompletedResultWithoutProviderValidation()
    {
        var userId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));
        attempt.MarkCompleted(100m, "payment-123", DateTimeOffset.UtcNow, activityId);

        _attemptRepository
            .Setup(r => r.GetByIdAsync(attempt.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _walletRepository
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new Wallet { UserId = userId, CurrentBalance = 100m });

        var result = await CreateService().FinalizeHostedReturnAsync(new FinalizeWalletTopUpReturnCommand
        {
            UserId = userId,
            WalletTopUpAttemptId = attempt.Id,
            ReturnPayload = new Dictionary<string, string>()
        });

        Assert.Equal(WalletTopUpAttemptStatus.Completed, result.Status);
        Assert.Equal(activityId, result.CreditedWalletActivityId);
        Assert.Equal(100m, result.WalletBalance);
        _provider.Verify(p => p.ValidateReturnAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FinalizeHostedReturnAsync_WhenOutcomeIsCancelled_DoesNotCreditWallet()
    {
        var userId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));

        _attemptRepository
            .Setup(r => r.GetByIdAsync(attempt.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
        _provider.SetupGet(p => p.ProviderKey).Returns("fake");
        _provider
            .Setup(p => p.ValidateReturnAsync(attempt, It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HostedPaymentReturnResult
            {
                AttemptId = attempt.Id,
                Outcome = HostedPaymentOutcome.Cancelled,
                ProviderSessionReference = "session-123",
                ValidatedAt = DateTimeOffset.UtcNow,
                FailureCode = "cancelled",
                FailureMessage = "Payment was cancelled.",
                DisplayMessage = "Payment was cancelled."
            });
        _attemptRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _walletRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Wallet?)null);

        var result = await CreateService().FinalizeHostedReturnAsync(new FinalizeWalletTopUpReturnCommand
        {
            UserId = userId,
            WalletTopUpAttemptId = attempt.Id,
            ReturnPayload = new Dictionary<string, string>
            {
                ["outcome"] = "cancelled"
            }
        });

        Assert.Equal(WalletTopUpAttemptStatus.Cancelled, result.Status);
        Assert.False(result.CreditedWallet);
        Assert.Equal(0m, result.WalletBalance);
        _attemptRepository.Verify(r => r.SettleSuccessfulAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FinalizeHostedReturnAsync_WhenAttemptDoesNotBelongToUser_ThrowsInvalidOperationException()
    {
        _attemptRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletTopUpAttempt?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().FinalizeHostedReturnAsync(new FinalizeWalletTopUpReturnCommand
            {
                UserId = Guid.NewGuid(),
                WalletTopUpAttemptId = Guid.NewGuid(),
                ReturnPayload = new Dictionary<string, string>()
            }));
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsNewestFirstMappedAttempts()
    {
        var userId = Guid.NewGuid();
        var older = WalletTopUpAttempt.CreatePending(userId, 10m, "fake");
        older.RegisterHostedSession("session-old", "token-old", DateTimeOffset.UtcNow.AddMinutes(15));
        older.MarkFailed("failed", "Payment failed.", DateTimeOffset.UtcNow.AddMinutes(-5));

        var newer = WalletTopUpAttempt.CreatePending(userId, 20m, "fake");
        newer.RegisterHostedSession("session-new", "token-new", DateTimeOffset.UtcNow.AddMinutes(15));
        newer.MarkCompleted(20m, "payment-new", DateTimeOffset.UtcNow, Guid.NewGuid());

        _attemptRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WalletTopUpAttempt> { newer, older });

        var result = await CreateService().GetHistoryAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(20m, result[0].ConfirmedChargedAmount);
        Assert.Equal(WalletTopUpAttemptStatus.Failed, result[1].Status);
    }
}
