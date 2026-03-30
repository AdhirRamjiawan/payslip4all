using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Infrastructure.Tests.DynamoDB;

/// <summary>
/// Unit tests for <see cref="DynamoDbUnitOfWork"/>.
/// SaveChanges is a no-op, but transactional members should fail fast.
/// </summary>
[Collection(DynamoDbTestCollection.Name)]
public class DynamoDbUnitOfWorkTests
{
    private readonly DynamoDbUnitOfWork _sut = new();

    [Fact]
    public async Task SaveChangesAsync_ReturnsZeroWithoutException()
    {
        var result = await _sut.SaveChangesAsync();
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_ReturnsZero()
    {
        using var cts = new CancellationTokenSource();
        var result = await _sut.SaveChangesAsync(cts.Token);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task BeginTransactionAsync_ThrowsNotSupportedException()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _sut.BeginTransactionAsync());
    }

    [Fact]
    public async Task CommitTransactionAsync_ThrowsNotSupportedException()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _sut.CommitTransactionAsync());
    }

    [Fact]
    public async Task RollbackTransactionAsync_ThrowsNotSupportedException()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _sut.RollbackTransactionAsync());
    }

    [Fact]
    public async Task MultipleCallsToSaveChanges_AlwaysReturnZero()
    {
        var r1 = await _sut.SaveChangesAsync();
        var r2 = await _sut.SaveChangesAsync();
        var r3 = await _sut.SaveChangesAsync();

        Assert.Equal(0, r1);
        Assert.Equal(0, r2);
        Assert.Equal(0, r3);
    }
}
