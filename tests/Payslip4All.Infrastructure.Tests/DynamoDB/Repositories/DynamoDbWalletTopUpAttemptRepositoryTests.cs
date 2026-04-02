using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

[Collection(DynamoDbTestCollection.Name)]
[Trait("Category", "Integration")]
public class DynamoDbWalletTopUpAttemptRepositoryTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbWalletTopUpAttemptRepository _repo;

    public DynamoDbWalletTopUpAttemptRepositoryTests(DynamoDbTestFixture fixture)
    {
        _repo = new DynamoDbWalletTopUpAttemptRepository(fixture.Client);
    }

    [Fact]
    public async Task AddAsync_StoresAndReadsAttemptByOwner()
    {
        var userId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));

        await _repo.AddAsync(attempt);
        var result = await _repo.GetByIdAsync(attempt.Id, userId);

        Assert.NotNull(result);
        Assert.Equal(attempt.Id, result!.Id);
    }

    [Fact]
    public async Task GetByCorrelationTokenAsync_ReturnsAttempt()
    {
        var userId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));
        await _repo.AddAsync(attempt);

        var result = await _repo.GetByCorrelationTokenAsync("token-123");

        Assert.NotNull(result);
        Assert.Equal(attempt.Id, result!.Id);
    }
}
