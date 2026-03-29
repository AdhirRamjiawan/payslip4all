using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

/// <summary>
/// Integration tests for <see cref="DynamoDbUserRepository"/>.
/// Requires DynamoDB Local running at DYNAMODB_ENDPOINT (default: http://localhost:8000).
/// </summary>
[Trait("Category", "Integration")]
public class DynamoDbUserRepositoryTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbUserRepository _sut;

    public DynamoDbUserRepositoryTests(DynamoDbTestFixture fixture)
    {
        _sut = new DynamoDbUserRepository(fixture.Client);
    }

    [Fact]
    public async Task AddAsync_PersistsUser_CanBeRetrievedByEmail()
    {
        var user = new User { Email = $"test_{Guid.NewGuid():N}@example.com", PasswordHash = "hash123" };

        await _sut.AddAsync(user);
        var retrieved = await _sut.GetByEmailAsync(user.Email);

        Assert.NotNull(retrieved);
        Assert.Equal(user.Id, retrieved.Id);
        Assert.Equal(user.Email.ToLower(), retrieved.Email);
        Assert.Equal(user.PasswordHash, retrieved.PasswordHash);
    }

    [Fact]
    public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        var result = await _sut.GetByEmailAsync("nobody_" + Guid.NewGuid().ToString("N") + "@example.com");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseInsensitive()
    {
        var email = $"Case_{Guid.NewGuid():N}@Example.com";
        var user = new User { Email = email.ToLower(), PasswordHash = "hash" };
        await _sut.AddAsync(user);

        var retrieved = await _sut.GetByEmailAsync(email.ToUpper());
        Assert.NotNull(retrieved);
        Assert.Equal(user.Id, retrieved.Id);
    }

    [Fact]
    public async Task ExistsAsync_WhenEmailExists_ReturnsTrue()
    {
        var email = $"exists_{Guid.NewGuid():N}@example.com";
        var user = new User { Email = email, PasswordHash = "hash" };
        await _sut.AddAsync(user);

        Assert.True(await _sut.ExistsAsync(email));
    }

    [Fact]
    public async Task ExistsAsync_WhenEmailDoesNotExist_ReturnsFalse()
    {
        var email = $"nope_{Guid.NewGuid():N}@example.com";
        Assert.False(await _sut.ExistsAsync(email));
    }
}
