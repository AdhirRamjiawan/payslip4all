using Amazon.DynamoDBv2.Model;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

[Collection(DynamoDbTestCollection.Name)]
[Trait("Category", "Integration")]
public class DynamoDbWalletTopUpSettlementTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbTestFixture _fixture;
    private readonly DynamoDbWalletTopUpAttemptRepository _repo;

    public DynamoDbWalletTopUpSettlementTests(DynamoDbTestFixture fixture)
    {
        _fixture = fixture;
        _repo = new DynamoDbWalletTopUpAttemptRepository(fixture.Client);
    }

    [Fact]
    public async Task SettleSuccessfulAsync_CreditsConfirmedAmountExactlyOnce()
    {
        var userId = Guid.NewGuid();
        var attempt = WalletTopUpAttempt.CreatePending(userId, 100m, "fake");
        attempt.RegisterHostedSession("session-123", "token-123", DateTimeOffset.UtcNow.AddMinutes(15));
        attempt.RecordValidatedSuccess(95m, "payment-123", DateTimeOffset.UtcNow);
        await _repo.AddAsync(attempt);

        var first = await _repo.SettleSuccessfulAsync(attempt);
        var second = await _repo.SettleSuccessfulAsync(attempt);

        var walletResponse = await _fixture.Client.GetItemAsync(new GetItemRequest
        {
            TableName = _fixture.WalletsTable,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = userId.ToString() }
            },
            ConsistentRead = true
        });

        Assert.True(walletResponse.IsItemSet);
        Assert.Equal("95", walletResponse.Item["currentBalance"].S);
        Assert.Equal(first.WalletActivityId, second.WalletActivityId);
        Assert.False(second.CreditedNow);
    }
}
