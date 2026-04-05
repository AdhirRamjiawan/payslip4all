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
    public async Task GetByIdAsync_WithDifferentOwner_ReturnsNull()
    {
        var owner = SeedUser("owner-filter@test.com");
        var otherOwnerId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(owner.Id, 100m, "fake");
        await _repo.AddAsync(attempt);

        var result = await _repo.GetByIdAsync(attempt.Id, otherOwnerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyOwnerAttemptsInNewestFirstOrder()
    {
        var owner = SeedUser("history-owner@test.com");
        var otherOwner = SeedUser("history-other@test.com");
        var first = WalletTopUpAttempt.CreatePending(owner.Id, 60m, "payfast");
        var second = WalletTopUpAttempt.CreatePending(owner.Id, 80m, "payfast");
        var foreign = WalletTopUpAttempt.CreatePending(otherOwner.Id, 90m, "payfast");

        await _repo.AddAsync(first);
        await Task.Delay(5);
        await _repo.AddAsync(second);
        await _repo.AddAsync(foreign);

        var attempts = await _repo.GetByUserIdAsync(owner.Id);

        Assert.Equal(2, attempts.Count);
        Assert.Equal(second.Id, attempts[0].Id);
        Assert.DoesNotContain(attempts, x => x.UserId != owner.Id);
    }

    [Fact]
    public async Task GetDueForReconciliationAsync_ReturnsOnlyDueAttempts()
    {
        var user = SeedUser("expiry-topup@test.com");
        var expiredPending = WalletTopUpAttempt.CreatePending(user.Id, 50m, "fake");
        expiredPending.NextReconciliationDueAt = expiredPending.CreatedAt;
        var expiredUnverified = WalletTopUpAttempt.CreatePending(user.Id, 75m, "fake");
        expiredUnverified.MarkUnverified("low_confidence_return", "Manual review required.", DateTimeOffset.UtcNow);
        expiredUnverified.NextReconciliationDueAt = expiredUnverified.CreatedAt;
        var activePending = WalletTopUpAttempt.CreatePending(user.Id, 55m, "fake");
        activePending.NextReconciliationDueAt = DateTimeOffset.UtcNow.AddHours(2);

        await _repo.AddAsync(expiredPending);
        await _repo.AddAsync(expiredUnverified);
        await _repo.AddAsync(activePending);

        var result = await _repo.GetDueForReconciliationAsync(DateTimeOffset.UtcNow.AddMinutes(1));

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
