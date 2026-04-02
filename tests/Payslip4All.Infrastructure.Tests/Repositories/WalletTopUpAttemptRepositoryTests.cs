using Microsoft.EntityFrameworkCore;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Infrastructure.Persistence.Repositories;

namespace Payslip4All.Infrastructure.Tests.Repositories;

public class WalletTopUpAttemptRepositoryTests : RepositoryTestBase
{
    private readonly WalletTopUpAttemptRepository _repo;
    private readonly PaymentReturnEvidenceRepository _evidenceRepo;
    private readonly OutcomeNormalizationDecisionRepository _decisionRepo;
    private readonly UnmatchedPaymentReturnRecordRepository _unmatchedRepo;

    public WalletTopUpAttemptRepositoryTests()
    {
        _repo = new WalletTopUpAttemptRepository(Db);
        _evidenceRepo = new PaymentReturnEvidenceRepository(Db);
        _decisionRepo = new OutcomeNormalizationDecisionRepository(Db);
        _unmatchedRepo = new UnmatchedPaymentReturnRecordRepository(Db);
    }

    [Fact]
    public async Task AddAsync_StoresAndReadsAttemptByOwner()
    {
        var user = SeedUser();
        var attempt = WalletTopUpAttempt.CreatePending(user.Id, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));

        await _repo.AddAsync(attempt);
        var result = await _repo.GetByIdAsync(attempt.Id, user.Id);

        Assert.NotNull(result);
        Assert.Equal(attempt.Id, result!.Id);
    }

    [Fact]
    public async Task GetByCorrelationTokenAsync_FindsAttempt()
    {
        var user = SeedUser("owner-topup@test.com");
        var attempt = WalletTopUpAttempt.CreatePending(user.Id, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));
        await _repo.AddAsync(attempt);

        var result = await _repo.GetByCorrelationTokenAsync("token-123");

        Assert.NotNull(result);
        Assert.Equal(attempt.Id, result!.Id);
    }

    [Fact]
    public async Task GetPendingOrUnverifiedExpiredAsync_ReturnsOnlyCutoffAttempts()
    {
        var user = SeedUser("expiry-topup@test.com");
        var expiredPending = WalletTopUpAttempt.CreatePending(user.Id, 50m, "fake");
        expiredPending.AbandonAfterUtc = expiredPending.CreatedAt;
        var expiredUnverified = WalletTopUpAttempt.CreatePending(user.Id, 75m, "fake");
        expiredUnverified.MarkUnverified("low_confidence_return", "Manual review required.", DateTimeOffset.UtcNow);
        expiredUnverified.AbandonAfterUtc = expiredUnverified.CreatedAt;
        var activePending = WalletTopUpAttempt.CreatePending(user.Id, 30m, "fake");
        activePending.AbandonAfterUtc = DateTimeOffset.UtcNow.AddHours(2);

        await _repo.AddAsync(expiredPending);
        await _repo.AddAsync(expiredUnverified);
        await _repo.AddAsync(activePending);

        var result = await _repo.GetPendingOrUnverifiedExpiredAsync(DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, a => a.Id == activePending.Id);
    }

    [Fact]
    public async Task AuditRepositories_StoreEvidenceDecisionAndUnmatchedRecord()
    {
        var evidence = new PaymentReturnEvidence
        {
            ProviderKey = "fake",
            CorrelationDisposition = PaymentReturnCorrelationDisposition.NoMatch,
            TrustLevel = PaymentReturnTrustLevel.Untrusted,
            ValidatedAt = DateTimeOffset.UtcNow
        };
        var attemptId = Guid.NewGuid();
        var decision = new OutcomeNormalizationDecision
        {
            AttemptId = attemptId,
            DecisionType = "UnmatchedReturn",
            AppliedPrecedence = "NoMatch",
            NormalizedOutcome = "Unknown",
            DecisionReasonCode = "unmatched",
            DecisionSummary = "Return evidence could not be matched.",
            WalletEffect = "NoCredit"
        };
        var unmatched = new UnmatchedPaymentReturnRecord
        {
            PrimaryEvidenceId = evidence.Id,
            ProviderKey = "fake",
            CorrelationDisposition = "NoMatch"
        };

        await _evidenceRepo.AddAsync(evidence);
        await _decisionRepo.AddAsync(decision);
        await _unmatchedRepo.AddAsync(unmatched);

        Assert.NotNull(await _evidenceRepo.GetByIdAsync(evidence.Id));
        Assert.Single(await _decisionRepo.GetByAttemptIdAsync(attemptId));
        Assert.NotNull(await _unmatchedRepo.GetByIdAsync(unmatched.Id));
    }
}
