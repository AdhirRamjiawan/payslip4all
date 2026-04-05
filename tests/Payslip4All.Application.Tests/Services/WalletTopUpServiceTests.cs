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
    private readonly Mock<IPaymentReturnEvidenceRepository> _evidenceRepository = new();
    private readonly Mock<IOutcomeNormalizationDecisionRepository> _decisionRepository = new();
    private readonly Mock<IUnmatchedPaymentReturnRecordRepository> _unmatchedRepository = new();
    private readonly Mock<IWalletTopUpOutcomeNormalizer> _normalizer = new();
    private readonly Mock<IWalletTopUpAbandonmentService> _abandonmentService = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();

    private WalletTopUpService CreateService()
    {
        _provider.SetupGet(p => p.ProviderKey).Returns("fake");
        _timeProvider.SetupGet(t => t.UtcNow).Returns(new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero));
        return new WalletTopUpService(
            _attemptRepository.Object,
            new[] { _provider.Object },
            _walletRepository.Object,
            _evidenceRepository.Object,
            _decisionRepository.Object,
            _unmatchedRepository.Object,
            _normalizer.Object,
            _abandonmentService.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task StartHostedTopUpAsync_PersistsPendingAttemptBeforeRedirect_AndUsesGenericReturnRoute()
    {
        var userId = Guid.NewGuid();
        WalletTopUpAttempt? createdAttempt = null;
        Uri? actualReturnUrl = null;
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
            .Callback<WalletTopUpAttempt, Uri, Uri?, CancellationToken>((_, returnUrl, _, _) => actualReturnUrl = returnUrl)
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
        Assert.Equal(result.HostedPageDeadline!.Value.AddMinutes(1), createdAttempt.AbandonAfterUtc);
        Assert.Equal("https://app.test/portal/wallet/top-ups/return", actualReturnUrl?.ToString());
    }

    [Fact]
    public async Task ProcessGenericReturnAsync_WhenBrowserReturnMatches_KeepsAttemptPendingWithoutSettlement()
    {
        var userId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));
        var evidenceDto = new HostedPaymentReturnEvidenceDto
        {
            Id = Guid.NewGuid(),
            ProviderKey = "fake",
            ProviderSessionReference = "session-123",
            ReturnCorrelationToken = "token-123",
            ClaimedOutcome = PaymentReturnClaimedOutcome.Completed,
            ConfirmedChargedAmount = 95m,
            ReceivedAt = DateTimeOffset.UtcNow,
            ValidatedAt = DateTimeOffset.UtcNow,
            TrustLevel = PaymentReturnTrustLevel.Trustworthy,
            CorrelationDisposition = PaymentReturnCorrelationDisposition.ExactMatch
        };
        var resolution = new HostedPaymentReturnResolutionDto
        {
            EvidenceId = evidenceDto.Id,
            MatchedAttemptId = attempt.Id,
            CorrelationDisposition = PaymentReturnCorrelationDisposition.ExactMatch,
            NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown,
            TrustLevel = PaymentReturnTrustLevel.Untrusted,
            IsAuthoritative = false,
            ReasonCode = "browser_return_informational",
            ResolutionSummary = "Top-up not confirmed",
            ConfirmedChargedAmount = 95m,
            WalletEffect = "NoCredit"
        };

        _provider.Setup(p => p.ParseReturnEvidenceAsync(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidenceDto);
        _attemptRepository.Setup(r => r.GetByCorrelationTokenAsync("token-123", It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        _normalizer.Setup(n => n.NormalizeAsync(It.IsAny<PaymentReturnEvidence>(), attempt, It.IsAny<CancellationToken>())).ReturnsAsync(resolution);
        _attemptRepository.Setup(r => r.SettleSuccessfulAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>())).ReturnsAsync(new WalletTopUpSettlementResult
        {
            WalletId = userId,
            WalletActivityId = Guid.NewGuid(),
            WalletBalance = 95m,
            CreditedNow = true
        });

        var result = await CreateService().ProcessGenericReturnAsync(userId, new Dictionary<string, string>
        {
            ["provider"] = "fake",
            ["session"] = "session-123",
            ["token"] = "token-123",
            ["outcome"] = "succeeded",
            ["amount"] = "95.00"
        });

        Assert.True(result.IsMatched);
        Assert.Equal(attempt.Id, result.MatchedAttemptId);
        _evidenceRepository.Verify(r => r.AddAsync(It.Is<PaymentReturnEvidence>(e => e.MatchedAttemptId == attempt.Id), It.IsAny<CancellationToken>()), Times.Once);
        _attemptRepository.Verify(r => r.SettleSuccessfulAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        _attemptRepository.Verify(r => r.UpdateAsync(It.Is<WalletTopUpAttempt>(a =>
            a.Status == WalletTopUpAttemptStatus.Pending
            && a.OutcomeReasonCode == "browser_return_informational"
            && a.OutcomeMessage == "Payment is still pending. Your wallet has not been credited yet."), It.IsAny<CancellationToken>()), Times.Once);
        _decisionRepository.Verify(r => r.AddAsync(It.Is<OutcomeNormalizationDecision>(d => d.AttemptId == attempt.Id && d.WalletEffect == "NoCredit"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessGenericReturnAsync_WhenNoAttemptMatches_PersistsUnmatchedRecord()
    {
        var evidenceDto = new HostedPaymentReturnEvidenceDto
        {
            Id = Guid.NewGuid(),
            ProviderKey = "fake",
            ReturnCorrelationToken = "token-404",
            ReceivedAt = DateTimeOffset.UtcNow,
            ValidatedAt = DateTimeOffset.UtcNow,
            TrustLevel = PaymentReturnTrustLevel.Untrusted,
            CorrelationDisposition = PaymentReturnCorrelationDisposition.NoMatch
        };
        var resolution = new HostedPaymentReturnResolutionDto
        {
            EvidenceId = evidenceDto.Id,
            CorrelationDisposition = PaymentReturnCorrelationDisposition.NoMatch,
            TrustLevel = PaymentReturnTrustLevel.Untrusted,
            ReasonCode = "unmatched",
            ResolutionSummary = "Return evidence could not be matched to a single hosted top-up attempt."
        };

        _provider.Setup(p => p.ParseReturnEvidenceAsync(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidenceDto);
        _attemptRepository.Setup(r => r.GetByCorrelationTokenAsync("token-404", It.IsAny<CancellationToken>())).ReturnsAsync((WalletTopUpAttempt?)null);
        _normalizer.Setup(n => n.NormalizeAsync(It.IsAny<PaymentReturnEvidence>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(resolution);

        var result = await CreateService().ProcessGenericReturnAsync(Guid.NewGuid(), new Dictionary<string, string>
        {
            ["provider"] = "fake",
            ["token"] = "token-404"
        });

        Assert.False(result.IsMatched);
        Assert.Equal("not_confirmed", result.GenericResultCode);
        _unmatchedRepository.Verify(r => r.AddAsync(It.IsAny<UnmatchedPaymentReturnRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessGenericReturnAsync_WhenCorrelationMatchesDifferentOwner_TreatsReturnAsUnmatched()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));

        var evidenceDto = new HostedPaymentReturnEvidenceDto
        {
            Id = Guid.NewGuid(),
            ProviderKey = "fake",
            ProviderSessionReference = "session-123",
            ReturnCorrelationToken = "token-123",
            ClaimedOutcome = PaymentReturnClaimedOutcome.Completed,
            ConfirmedChargedAmount = 95m,
            ReceivedAt = DateTimeOffset.UtcNow,
            ValidatedAt = DateTimeOffset.UtcNow,
            TrustLevel = PaymentReturnTrustLevel.Trustworthy,
            CorrelationDisposition = PaymentReturnCorrelationDisposition.ExactMatch
        };
        var resolution = new HostedPaymentReturnResolutionDto
        {
            EvidenceId = evidenceDto.Id,
            CorrelationDisposition = PaymentReturnCorrelationDisposition.NoMatch,
            TrustLevel = PaymentReturnTrustLevel.Untrusted,
            ReasonCode = "unmatched",
            ResolutionSummary = "Return evidence could not be matched to a single hosted top-up attempt."
        };

        _provider.Setup(p => p.ParseReturnEvidenceAsync(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidenceDto);
        _attemptRepository.Setup(r => r.GetByCorrelationTokenAsync("token-123", It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        _normalizer.Setup(n => n.NormalizeAsync(It.IsAny<PaymentReturnEvidence>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(resolution);

        var result = await CreateService().ProcessGenericReturnAsync(Guid.NewGuid(), new Dictionary<string, string>
        {
            ["provider"] = "fake",
            ["session"] = "session-123",
            ["token"] = "token-123",
            ["outcome"] = "succeeded",
            ["amount"] = "95.00"
        });

        Assert.False(result.IsMatched);
        _attemptRepository.Verify(r => r.SettleSuccessfulAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        _unmatchedRepository.Verify(r => r.AddAsync(It.IsAny<UnmatchedPaymentReturnRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAttemptResultAsync_ReturnsOwnerScopedAttemptResult()
    {
        var userId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");
        attempt.MarkNotConfirmed("not_confirmed", "Top-up not confirmed", DateTimeOffset.UtcNow);

        _attemptRepository.Setup(r => r.GetByIdAsync(attempt.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        _walletRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new Wallet { UserId = userId, CurrentBalance = 0m });

        var result = await CreateService().GetAttemptResultAsync(attempt.Id, userId);

        Assert.NotNull(result);
        Assert.Equal(WalletTopUpAttemptStatus.NotConfirmed, result!.Status);
        Assert.Equal("Top-up not confirmed", result.OutcomeMessage);
    }

    [Fact]
    public async Task AbandonExpiredAttemptsAsync_DelegatesToAbandonmentService()
    {
        await CreateService().AbandonExpiredAttemptsAsync();
        _abandonmentService.Verify(s => s.AbandonExpiredAttemptsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(InvalidOperationException))]
    public async Task StartHostedTopUpAsync_WhenProviderStartFails_ReturnsSameOwnerSafeMessage(Type exceptionType)
    {
        var userId = Guid.NewGuid();
        Exception exception = exceptionType == typeof(HttpRequestException)
            ? new HttpRequestException("gateway down")
            : new InvalidOperationException("merchant invalid");

        _provider
            .Setup(p => p.StartHostedTopUpAsync(
                It.IsAny<WalletTopUpAttempt>(),
                It.IsAny<Uri>(),
                It.IsAny<Uri?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().StartHostedTopUpAsync(new StartWalletTopUpCommand
        {
            UserId = userId,
            RequestedAmount = 100m,
            ReturnUrl = "https://app.test/portal/wallet/top-ups/return"
        }));

        Assert.Equal("Payment could not be started", ex.Message);
        _attemptRepository.Verify(r => r.UpdateAsync(
            It.Is<WalletTopUpAttempt>(a =>
                a.UserId == userId
                && a.Status == WalletTopUpAttemptStatus.NotConfirmed
                && a.OutcomeMessage == "Top-up not confirmed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAuthoritativeCallbackAsync_WhenEvidenceConflictsWithAcceptedFinalOutcome_PersistsConflictDecisionWithoutChangingWallet()
    {
        var userId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");
        attempt.Status = WalletTopUpAttemptStatus.Completed;
        attempt.ConfirmedChargedAmount = 95m;
        attempt.ProviderPaymentReference = "payment-123";
        attempt.CreditedWalletActivityId = Guid.NewGuid();
        attempt.AuthoritativeEvidenceId = Guid.NewGuid();
        attempt.CompletedAt = DateTimeOffset.UtcNow;
        attempt.AuthoritativeOutcomeAcceptedAt = DateTimeOffset.UtcNow;
        attempt.NextReconciliationDueAt = null;

        var parsedEvidence = new HostedPaymentReturnEvidenceDto
        {
            Id = Guid.NewGuid(),
            ProviderKey = "fake",
            SourceChannel = "PayFastNotify",
            MerchantPaymentReference = attempt.MerchantPaymentReference,
            ClaimedOutcome = PaymentReturnClaimedOutcome.Cancelled,
            TrustLevel = PaymentReturnTrustLevel.Trustworthy,
            SignatureVerified = true,
            SourceVerified = true,
            ServerConfirmed = true,
            ConfirmedChargedAmount = 95m,
            ConfirmedCurrencyCode = "ZAR",
            ValidatedAt = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        _provider
            .Setup(p => p.ParseAuthoritativeEvidenceAsync(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedEvidence);
        _attemptRepository
            .Setup(r => r.GetByMerchantPaymentReferenceAsync(attempt.MerchantPaymentReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { attempt });
        _normalizer
            .Setup(n => n.NormalizeAsync(It.IsAny<PaymentReturnEvidence>(), It.Is<WalletTopUpAttempt?>(a => a != null && a.Id == attempt.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HostedPaymentReturnResolutionDto
            {
                EvidenceId = parsedEvidence.Id,
                MatchedAttemptId = attempt.Id,
                CorrelationDisposition = PaymentReturnCorrelationDisposition.DuplicateFinalized,
                TriggerSource = "PayFastNotify",
                ConflictWithAcceptedFinal = true,
                ReasonCode = "duplicate_finalized",
                ResolutionSummary = "Top-up not confirmed",
                WalletEffect = "NoCredit"
            });

        await CreateService().ProcessAuthoritativeCallbackAsync("fake", new Dictionary<string, string>
        {
            ["m_payment_id"] = attempt.MerchantPaymentReference
        });

        _attemptRepository.Verify(r => r.UpdateAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        _attemptRepository.Verify(r => r.SettleSuccessfulAsync(It.IsAny<WalletTopUpAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        _decisionRepository.Verify(r => r.AddAsync(
            It.Is<OutcomeNormalizationDecision>(d =>
                d.AttemptId == attempt.Id
                && d.ConflictWithAcceptedFinalOutcome
                && d.DecisionReasonCode == "duplicate_finalized"
                && d.WalletEffect == "NoCredit"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
