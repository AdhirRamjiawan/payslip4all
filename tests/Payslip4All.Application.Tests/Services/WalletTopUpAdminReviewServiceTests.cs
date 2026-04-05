using Moq;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.Tests.Services;

public class WalletTopUpAdminReviewServiceTests
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
        _provider.SetupGet(p => p.ProviderKey).Returns("payfast");
        _timeProvider.SetupGet(t => t.UtcNow).Returns(new DateTimeOffset(2026, 4, 5, 12, 0, 0, TimeSpan.Zero));
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
    public async Task GetAdminReviewAsync_WhenRequesterIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateService().GetAdminReviewAsync(new SiteAdministratorPaymentReviewQueryDto()));
    }

    [Fact]
    public async Task GetAdminReviewAsync_WhenPaymentConfirmationRecordMatchesAttempt_ReturnsPrivacyMinimizedReviewRow()
    {
        var userId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "payfast");
        attempt.RecordValidatedSuccess(95m, "pf-123", DateTimeOffset.UtcNow);
        attempt.Status = WalletTopUpAttemptStatus.Completed;
        attempt.CreditedWalletActivityId = Guid.NewGuid();

        var evidence = new PaymentReturnEvidence
        {
            ProviderKey = "payfast",
            SourceChannel = "PayFastNotify",
            MatchedAttemptId = attempt.Id,
            MerchantPaymentReference = attempt.MerchantPaymentReference,
            ProviderPaymentReference = "pf-123",
            SignatureVerified = true,
            SourceVerified = true,
            ServerConfirmed = true,
            SafePayloadSnapshot = "{\"safe\":true}"
        };

        var decision = new OutcomeNormalizationDecision
        {
            AttemptId = attempt.Id,
            PaymentReturnEvidenceId = evidence.Id,
            DecisionType = "EvidenceEvaluation",
            DecisionReasonCode = "confirmed",
            DecisionSummary = "Payment confirmation record accepted."
        };

        _evidenceRepository.Setup(r => r.GetByIdAsync(evidence.Id, It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        _attemptRepository.Setup(r => r.GetForAdminReviewAsync(attempt.Id, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { attempt });
        _evidenceRepository.Setup(r => r.GetByAttemptIdAsync(attempt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { evidence });
        _decisionRepository.Setup(r => r.GetByAttemptIdAsync(attempt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { decision });

        var result = await CreateService().GetAdminReviewAsync(new SiteAdministratorPaymentReviewQueryDto
        {
            RequestingUserIsSiteAdministrator = true,
            PaymentConfirmationRecordId = evidence.Id
        });

        var row = Assert.Single(result);
        Assert.Equal(attempt.Id, row.WalletTopUpAttemptId);
        Assert.Equal(userId, row.OwnerUserId);
        Assert.Equal(evidence.Id, row.PaymentConfirmationRecordId);
        Assert.Equal("pf-123", row.ProviderPaymentReference);
        Assert.Equal("confirmed", row.DecisionReasonCode);
        Assert.Null(row.SafePayloadSnapshot);
    }

    [Fact]
    public async Task GetAdminReviewAsync_WhenUnmatchedOnly_ReturnsSafeUnmatchedRows()
    {
        var evidence = new PaymentReturnEvidence
        {
            ProviderKey = "payfast",
            SourceChannel = "BrowserReturn",
            MerchantPaymentReference = "merchant-404",
            ProviderPaymentReference = "pf-404",
            SafePayloadSnapshot = "{\"status\":\"pending\"}"
        };
        var unmatched = new UnmatchedPaymentReturnRecord
        {
            PrimaryEvidenceId = evidence.Id,
            ProviderKey = "payfast",
            CorrelationDisposition = "NoMatch",
            DisplayMessage = "Top-up not confirmed",
            SafePayloadSnapshot = "{\"status\":\"pending\"}"
        };
        var decision = new OutcomeNormalizationDecision
        {
            UnmatchedPaymentReturnRecordId = unmatched.Id,
            DecisionType = "UnmatchedReturn",
            DecisionReasonCode = "no_match",
            DecisionSummary = "Review unmatched return safely."
        };

        _unmatchedRepository.Setup(r => r.GetForAdminReviewAsync(null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { unmatched });
        _evidenceRepository.Setup(r => r.GetByIdAsync(evidence.Id, It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        _decisionRepository.Setup(r => r.GetByUnmatchedRecordIdAsync(unmatched.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { decision });

        var result = await CreateService().GetAdminReviewAsync(new SiteAdministratorPaymentReviewQueryDto
        {
            RequestingUserIsSiteAdministrator = true,
            UnmatchedOnly = true
        });

        var row = Assert.Single(result);
        Assert.True(row.IsUnmatchedReturn);
        Assert.Equal(unmatched.Id, row.UnmatchedPaymentReturnRecordId);
        Assert.Equal("NoMatch", row.CorrelationDisposition);
        Assert.Equal("{\"status\":\"pending\"}", row.SafePayloadSnapshot);
        Assert.Null(row.PaymentConfirmationRecordId);
    }

    [Fact]
    public async Task GetAdminReviewAsync_WhenConflictsOnly_FiltersNonConflictingRows()
    {
        var attempt = WalletTopUpAttempt.CreatePending(Guid.NewGuid(), 100m, "payfast");
        var decision = new OutcomeNormalizationDecision
        {
            AttemptId = attempt.Id,
            DecisionType = "EvidenceEvaluation",
            DecisionReasonCode = "conflict",
            DecisionSummary = "Conflicting late evidence retained for audit.",
            ConflictWithAcceptedFinalOutcome = true
        };

        _attemptRepository.Setup(r => r.GetForAdminReviewAsync(null, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { attempt });
        _evidenceRepository.Setup(r => r.GetByAttemptIdAsync(attempt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<PaymentReturnEvidence>());
        _decisionRepository.Setup(r => r.GetByAttemptIdAsync(attempt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { decision });

        var result = await CreateService().GetAdminReviewAsync(new SiteAdministratorPaymentReviewQueryDto
        {
            RequestingUserIsSiteAdministrator = true,
            ConflictsOnly = true
        });

        var row = Assert.Single(result);
        Assert.True(row.ConflictWithAcceptedFinalOutcome);
        Assert.Equal("conflict", row.DecisionReasonCode);
    }
}
