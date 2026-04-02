using Microsoft.EntityFrameworkCore;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence.Repositories;

namespace Payslip4All.Infrastructure.Tests.Repositories;

public class WalletTopUpAttemptRepositoryTests : RepositoryTestBase
{
    private readonly WalletTopUpAttemptRepository _repo;

    public WalletTopUpAttemptRepositoryTests()
    {
        _repo = new WalletTopUpAttemptRepository(Db);
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
    public async Task GetByIdAsync_ReturnsNull_WhenOwnerDoesNotMatch()
    {
        var owner = SeedUser("owner-topup@test.com");
        var other = SeedUser("other-topup@test.com");
        var attempt = WalletTopUpAttempt.CreatePending(owner.Id, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));
        await _repo.AddAsync(attempt);

        var result = await _repo.GetByIdAsync(attempt.Id, other.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsNewestFirst()
    {
        var user = SeedUser("history-topup@test.com");
        var older = WalletTopUpAttempt.CreatePending(user.Id, 50m, "fake");
        older.RegisterHostedSession("session-old", "token-old", DateTimeOffset.UtcNow.AddMinutes(15));
        var newer = WalletTopUpAttempt.CreatePending(user.Id, 75m, "fake");
        newer.RegisterHostedSession("session-new", "token-new", DateTimeOffset.UtcNow.AddMinutes(15));

        await _repo.AddAsync(older);
        await Task.Delay(5);
        await _repo.AddAsync(newer);

        var result = await _repo.GetByUserIdAsync(user.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }
}
