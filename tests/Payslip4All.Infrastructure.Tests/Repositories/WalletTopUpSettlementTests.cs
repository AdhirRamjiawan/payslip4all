using Microsoft.EntityFrameworkCore;
using Payslip4All.Infrastructure.Persistence.Repositories;

namespace Payslip4All.Infrastructure.Tests.Repositories;

public class WalletTopUpSettlementTests : RepositoryTestBase
{
    private readonly WalletTopUpAttemptRepository _repo;
    private readonly OutcomeNormalizationDecisionRepository _decisionRepo;

    public WalletTopUpSettlementTests()
    {
        _repo = new WalletTopUpAttemptRepository(Db);
        _decisionRepo = new OutcomeNormalizationDecisionRepository(Db);
    }

    [Fact]
    public async Task SettleSuccessfulAsync_CreditsConfirmedAmountExactlyOnce()
    {
        var user = SeedUser("settlement@test.com");
        var attempt = Domain.Entities.WalletTopUpAttempt.CreatePending(user.Id, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));
        attempt.RecordValidatedSuccess(95m, "payment-123", DateTimeOffset.UtcNow);
        await _repo.AddAsync(attempt);

        var first = await _repo.SettleSuccessfulAsync(attempt);
        var second = await _repo.SettleSuccessfulAsync(attempt);

        var wallet = await Db.Wallets.SingleAsync(w => w.UserId == user.Id);
        var activities = await Db.WalletActivities.Where(a => a.WalletId == wallet.Id).ToListAsync();
        var persistedAttempt = await Db.WalletTopUpAttempts.SingleAsync(a => a.Id == attempt.Id);

        Assert.Equal(95m, wallet.CurrentBalance);
        Assert.Single(activities);
        Assert.Equal(first.WalletActivityId, second.WalletActivityId);
        Assert.False(second.CreditedNow);
        Assert.Equal(first.WalletActivityId, persistedAttempt.CreditedWalletActivityId);
    }

    [Fact]
    public async Task SettleSuccessfulAsync_PersistsTraceableWalletCreditLinks()
    {
        var user = SeedUser("traceability@test.com");
        var evidence = new Domain.Entities.PaymentReturnEvidence
        {
            ProviderKey = "payfast",
            SourceChannel = "PayFastNotify",
            SignatureVerified = true,
            SourceVerified = true,
            ServerConfirmed = true,
            ValidatedAt = DateTimeOffset.UtcNow
        };
        Db.PaymentReturnEvidences.Add(evidence);
        await Db.SaveChangesAsync();

        var attempt = Domain.Entities.WalletTopUpAttempt.CreatePending(user.Id, 100m, "payfast");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));
        attempt.AuthoritativeEvidenceId = evidence.Id;
        attempt.RecordValidatedSuccess(95m, "payment-123", DateTimeOffset.UtcNow);
        await _repo.AddAsync(attempt);

        var settlement = await _repo.SettleSuccessfulAsync(attempt);

        var activity = await Db.WalletActivities.SingleAsync(a => a.Id == settlement.WalletActivityId);
        var persistedAttempt = await Db.WalletTopUpAttempts.SingleAsync(a => a.Id == attempt.Id);

        Assert.Equal(attempt.Id.ToString(), activity.ReferenceId);
        Assert.Equal(evidence.Id, activity.PaymentReturnEvidenceId);
        Assert.Equal(evidence.Id, persistedAttempt.AuthoritativeEvidenceId);
    }

    [Fact]
    public async Task ConflictNormalizationDecision_RoundTripsConflictFlagForInternalReview()
    {
        var user = SeedUser("conflict@test.com");
        var attempt = Domain.Entities.WalletTopUpAttempt.CreatePending(user.Id, 100m, "payfast");
        await _repo.AddAsync(attempt);

        var decision = new Domain.Entities.OutcomeNormalizationDecision
        {
            AttemptId = attempt.Id,
            DecisionType = "EvidenceEvaluation",
            TriggerSource = "PayFastNotify",
            AppliedPrecedence = "DuplicateFinalized",
            NormalizedOutcome = "Unknown",
            DecisionReasonCode = "duplicate_finalized",
            DecisionSummary = "Top-up not confirmed",
            ConflictWithAcceptedFinalOutcome = true,
            WalletEffect = "NoCredit"
        };

        await _decisionRepo.AddAsync(decision);
        var persisted = await _decisionRepo.GetByAttemptIdAsync(attempt.Id);

        Assert.Single(persisted);
        Assert.True(persisted[0].ConflictWithAcceptedFinalOutcome);
    }
}
