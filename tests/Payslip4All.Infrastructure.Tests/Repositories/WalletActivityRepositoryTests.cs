using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Infrastructure.Persistence.Repositories;

namespace Payslip4All.Infrastructure.Tests.Repositories;

public class WalletActivityRepositoryTests : RepositoryTestBase
{
    private readonly WalletActivityRepository _repo;

    public WalletActivityRepositoryTests() => _repo = new WalletActivityRepository(Db);

    [Fact]
    public async Task AddAsync_StoresWalletActivity()
    {
        var user = SeedUser();
        var wallet = SeedWallet(user.Id, 0m);
        var activity = new WalletActivity
        {
            WalletId = wallet.Id,
            ActivityType = WalletActivityType.Credit,
            Amount = 50m,
            BalanceAfterActivity = 50m,
            Description = "Top up",
        };

        await _repo.AddAsync(activity);

        Assert.Equal(1, Db.WalletActivities.Count());
    }

    [Fact]
    public async Task GetByWalletIdAsync_ReturnsNewestFirst()
    {
        var user = SeedUser();
        var wallet = SeedWallet(user.Id, 100m);
        var older = SeedWalletActivity(wallet.Id, WalletActivityType.Credit, 50m, 50m, "Older");
        typeof(WalletActivity).GetProperty(nameof(WalletActivity.OccurredAt))!
            .SetValue(older, DateTimeOffset.UtcNow.AddMinutes(-10));
        Db.SaveChanges();

        var newer = SeedWalletActivity(wallet.Id, WalletActivityType.Debit, 10m, 40m, "Newer");
        typeof(WalletActivity).GetProperty(nameof(WalletActivity.OccurredAt))!
            .SetValue(newer, DateTimeOffset.UtcNow);
        Db.SaveChanges();

        var results = await _repo.GetByWalletIdAsync(wallet.Id);

        Assert.Equal(newer.Id, results[0].Id);
        Assert.Equal(older.Id, results[1].Id);
    }
}
