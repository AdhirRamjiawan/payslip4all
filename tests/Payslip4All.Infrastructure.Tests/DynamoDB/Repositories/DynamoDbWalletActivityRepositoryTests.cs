using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

[Collection(DynamoDbTestCollection.Name)]
[Trait("Category", "Integration")]
public class DynamoDbWalletActivityRepositoryTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbTestFixture _fixture;
    private readonly DynamoDbWalletActivityRepository _repo;

    public DynamoDbWalletActivityRepositoryTests(DynamoDbTestFixture fixture)
    {
        _fixture = fixture;
        _repo = new DynamoDbWalletActivityRepository(fixture.Client);
    }

    [Fact]
    public async Task AddAsync_StoresActivity()
    {
        var walletId = Guid.NewGuid();
        await _fixture.SeedWalletAsync(walletId, Guid.NewGuid(), 10m);

        var activity = new WalletActivity
        {
            WalletId = walletId,
            ActivityType = WalletActivityType.Credit,
            Amount = 15m,
            BalanceAfterActivity = 25m,
            Description = "Top up",
        };

        await _repo.AddAsync(activity);
        var result = await _repo.GetByWalletIdAsync(walletId);

        Assert.Single(result);
        Assert.Equal(15m, result[0].Amount);
    }

    [Fact]
    public async Task GetByWalletIdAsync_ReturnsNewestFirst()
    {
        var walletId = Guid.NewGuid();
        await _fixture.SeedWalletAsync(walletId, Guid.NewGuid(), 50m);
        await _fixture.SeedWalletActivityAsync(Guid.NewGuid(), walletId, "Credit", 10m, 10m, DateTimeOffset.UtcNow.AddMinutes(-5), "Older");
        await _fixture.SeedWalletActivityAsync(Guid.NewGuid(), walletId, "Debit", 5m, 5m, DateTimeOffset.UtcNow, "Newer");

        var result = await _repo.GetByWalletIdAsync(walletId);

        Assert.Equal("Newer", result[0].Description);
        Assert.Equal("Older", result[1].Description);
    }

    [Fact]
    public async Task GetByWalletIdAsync_PaginatesAcrossAllResults()
    {
        var walletId = Guid.NewGuid();
        await _fixture.SeedWalletAsync(walletId, Guid.NewGuid(), 200m);

        for (var i = 0; i < 75; i++)
        {
            await _fixture.SeedWalletActivityAsync(
                Guid.NewGuid(),
                walletId,
                "Credit",
                1m,
                1m + i,
                DateTimeOffset.UtcNow.AddMinutes(-i),
                $"Activity {i}");
        }

        var result = await _repo.GetByWalletIdAsync(walletId);

        Assert.Equal(75, result.Count);
    }
}
