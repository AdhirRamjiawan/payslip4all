using Microsoft.EntityFrameworkCore;
using Payslip4All.Infrastructure.Persistence.Repositories;

namespace Payslip4All.Infrastructure.Tests.Repositories;

public class WalletTopUpSettlementTests : RepositoryTestBase
{
    private readonly WalletTopUpAttemptRepository _repo;

    public WalletTopUpSettlementTests()
    {
        _repo = new WalletTopUpAttemptRepository(Db);
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
}
