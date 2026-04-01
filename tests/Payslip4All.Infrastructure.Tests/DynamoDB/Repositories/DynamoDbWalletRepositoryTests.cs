using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

[Collection(DynamoDbTestCollection.Name)]
[Trait("Category", "Integration")]
public class DynamoDbWalletRepositoryTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbTestFixture _fixture;
    private readonly DynamoDbWalletRepository _repo;

    public DynamoDbWalletRepositoryTests(DynamoDbTestFixture fixture)
    {
        _fixture = fixture;
        _repo = new DynamoDbWalletRepository(fixture.Client);
    }

    [Fact]
    public async Task AddAsync_StoresAndReadsWalletByUserId()
    {
        var userId = Guid.NewGuid();
        var wallet = new Wallet { UserId = userId, CurrentBalance = 20m };

        await _repo.AddAsync(wallet);
        var result = await _repo.GetByUserIdAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(20m, result!.CurrentBalance);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenOwnerMismatches()
    {
        var userId = Guid.NewGuid();
        var wallet = new Wallet { UserId = userId, CurrentBalance = 20m };
        await _repo.AddAsync(wallet);

        var result = await _repo.GetByIdAsync(wallet.Id, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_UsesCanonicalUserBackedWalletId()
    {
        var userId = Guid.NewGuid();
        var wallet = new Wallet { UserId = userId, CurrentBalance = 20m };

        await _repo.AddAsync(wallet);

        Assert.Equal(userId, wallet.Id);
    }

    [Fact]
    public async Task AddAsync_WithExistingWalletForUser_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();

        await _repo.AddAsync(new Wallet { UserId = userId, CurrentBalance = 20m });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repo.AddAsync(new Wallet { UserId = userId, CurrentBalance = 25m }));
    }
}
