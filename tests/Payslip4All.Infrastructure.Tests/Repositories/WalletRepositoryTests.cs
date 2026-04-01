using Payslip4All.Infrastructure.Persistence.Repositories;

namespace Payslip4All.Infrastructure.Tests.Repositories;

public class WalletRepositoryTests : RepositoryTestBase
{
    private readonly WalletRepository _repo;

    public WalletRepositoryTests() => _repo = new WalletRepository(Db);

    [Fact]
    public async Task AddAsync_StoresWallet()
    {
        var user = SeedUser();
        var wallet = new Payslip4All.Domain.Entities.Wallet
        {
            UserId = user.Id,
            CurrentBalance = 25m,
        };

        await _repo.AddAsync(wallet);

        Assert.Equal(1, Db.Wallets.Count());
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsMatchingWallet()
    {
        var user = SeedUser();
        var wallet = SeedWallet(user.Id, 15m);

        var result = await _repo.GetByUserIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(wallet.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenWrongOwner()
    {
        var owner = SeedUser("owner@wallet.test");
        var other = SeedUser("other@wallet.test");
        var wallet = SeedWallet(owner.Id, 10m);

        var result = await _repo.GetByIdAsync(wallet.Id, other.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsNewBalance()
    {
        var user = SeedUser();
        var wallet = SeedWallet(user.Id, 5m);
        wallet.CurrentBalance = 20m;

        await _repo.UpdateAsync(wallet);

        var reloaded = await _repo.GetByUserIdAsync(user.Id);
        Assert.Equal(20m, reloaded!.CurrentBalance);
    }

    [Fact]
    public async Task AddAsync_EnforcesOneWalletPerUser()
    {
        var user = SeedUser();
        await _repo.AddAsync(new Payslip4All.Domain.Entities.Wallet { UserId = user.Id, CurrentBalance = 1m });

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _repo.AddAsync(new Payslip4All.Domain.Entities.Wallet { UserId = user.Id, CurrentBalance = 2m }));
    }
}
