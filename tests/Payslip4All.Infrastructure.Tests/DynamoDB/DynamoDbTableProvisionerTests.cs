using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Logging;
using Moq;
using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Infrastructure.Tests.DynamoDB;

/// <summary>
/// Integration tests for <see cref="DynamoDbTableProvisioner"/>.
/// Requires DynamoDB Local running at DYNAMODB_ENDPOINT (default: http://localhost:8000).
/// </summary>
[Collection(DynamoDbTestCollection.Name)]
[Trait("Category", "Integration")]
public class DynamoDbTableProvisionerTests : IAsyncLifetime
{
    private DynamoDbTestFixture _fixture = null!;
    private string _prefix = null!;

    public async Task InitializeAsync()
    {
        _fixture = new DynamoDbTestFixture();
        // We'll use a separate prefix for provisioner tests so they don't conflict
        await _fixture.InitializeAsync();
        // Use a different prefix for provisioner so tables don't already exist
        _prefix = $"prov_{Guid.NewGuid():N}";
    }

    public async Task DisposeAsync()
    {
        // Clean up provisioner tables
        var tableNames = new[]
        {
            $"{_prefix}_users", $"{_prefix}_companies", $"{_prefix}_employees",
            $"{_prefix}_employee_loans", $"{_prefix}_payslips", $"{_prefix}_payslip_loan_deductions",
        };
        foreach (var name in tableNames)
        {
            try { await _fixture.Client.DeleteTableAsync(name); }
            catch (ResourceNotFoundException) { }
        }

        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task StartAsync_WhenNoTablesExist_CreatesAll6Tables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DYNAMODB_TABLE_PREFIX", _prefix);
        var logger = new Mock<ILogger<DynamoDbTableProvisioner>>();
        var provisioner = new DynamoDbTableProvisioner(_fixture.Client, logger.Object);

        // Act
        await provisioner.StartAsync(CancellationToken.None);

        // Assert — all 6 tables should exist and be ACTIVE
        var tableNames = new[]
        {
            $"{_prefix}_users", $"{_prefix}_companies", $"{_prefix}_employees",
            $"{_prefix}_employee_loans", $"{_prefix}_payslips", $"{_prefix}_payslip_loan_deductions",
        };

        foreach (var name in tableNames)
        {
            var desc = await _fixture.Client.DescribeTableAsync(name);
            Assert.Equal(TableStatus.ACTIVE, desc.Table.TableStatus);
        }
    }

    [Fact]
    public async Task StartAsync_WhenTablesAlreadyExist_SkipsCreation()
    {
        // Arrange — create tables first
        Environment.SetEnvironmentVariable("DYNAMODB_TABLE_PREFIX", _prefix);
        var logger = new Mock<ILogger<DynamoDbTableProvisioner>>();
        var provisioner = new DynamoDbTableProvisioner(_fixture.Client, logger.Object);
        await provisioner.StartAsync(CancellationToken.None);

        // Reset mock to track second call
        logger.Reset();

        // Act — run provisioner again
        await provisioner.StartAsync(CancellationToken.None);

        // Assert — should complete without exception (skip path)
        // Verifying via logger calls: should log "already exists" messages
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already exists")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(6));
    }
}
