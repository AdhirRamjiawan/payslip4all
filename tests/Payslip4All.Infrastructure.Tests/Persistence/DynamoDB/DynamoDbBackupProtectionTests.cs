using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Infrastructure.Tests.Persistence.DynamoDB;

public class DynamoDbBackupProtectionTests
{
    [Fact]
    public async Task StartAsync_WhenHostedAwsConfigurationIsActive_EnablesPointInTimeRecoveryForAllRequiredTables()
    {
        var dynamoDb = new Mock<IAmazonDynamoDB>();
        var prefix = $"backup_{Guid.NewGuid():N}";

        dynamoDb
            .Setup(x => x.DescribeContinuousBackupsAsync(It.IsAny<DescribeContinuousBackupsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeContinuousBackupsResponse
            {
                ContinuousBackupsDescription = new ContinuousBackupsDescription
                {
                    PointInTimeRecoveryDescription = new PointInTimeRecoveryDescription
                    {
                        PointInTimeRecoveryStatus = PointInTimeRecoveryStatus.DISABLED
                    }
                }
            });

        dynamoDb
            .Setup(x => x.UpdateContinuousBackupsAsync(It.IsAny<UpdateContinuousBackupsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateContinuousBackupsResponse());

        await WithEnvironment(prefix, null, null, async () =>
        {
            var sut = CreateSut(dynamoDb.Object);
            await sut.StartAsync(CancellationToken.None);
        });

        dynamoDb.Verify(
            x => x.UpdateContinuousBackupsAsync(
                It.Is<UpdateContinuousBackupsRequest>(r =>
                    r.PointInTimeRecoverySpecification.PointInTimeRecoveryEnabled
                    && r.TableName == $"{prefix}_users"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        dynamoDb.Verify(
            x => x.UpdateContinuousBackupsAsync(
                It.IsAny<UpdateContinuousBackupsRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(13));
    }

    [Fact]
    public async Task StartAsync_WhenRunningAgainstLocalEndpoint_SkipsBackupEnablement()
    {
        var dynamoDb = new Mock<IAmazonDynamoDB>();

        await WithEnvironment("localprefix", "http://localhost:8000", null, async () =>
        {
            var sut = CreateSut(dynamoDb.Object);
            await sut.StartAsync(CancellationToken.None);
        });

        dynamoDb.Verify(
            x => x.UpdateContinuousBackupsAsync(
                It.IsAny<UpdateContinuousBackupsRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenPointInTimeRecoveryAlreadyEnabled_DoesNotReapplyProtection()
    {
        var dynamoDb = new Mock<IAmazonDynamoDB>();

        dynamoDb
            .Setup(x => x.DescribeContinuousBackupsAsync(It.IsAny<DescribeContinuousBackupsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeContinuousBackupsResponse
            {
                ContinuousBackupsDescription = new ContinuousBackupsDescription
                {
                    PointInTimeRecoveryDescription = new PointInTimeRecoveryDescription
                    {
                        PointInTimeRecoveryStatus = PointInTimeRecoveryStatus.ENABLED
                    }
                }
            });

        await WithEnvironment("enabledprefix", null, null, async () =>
        {
            var sut = CreateSut(dynamoDb.Object);
            await sut.StartAsync(CancellationToken.None);
        });

        dynamoDb.Verify(
            x => x.UpdateContinuousBackupsAsync(
                It.IsAny<UpdateContinuousBackupsRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenPointInTimeRecoveryIsDisabledByConfiguration_SkipsProtection()
    {
        var dynamoDb = new Mock<IAmazonDynamoDB>();

        await WithEnvironment("disabledprefix", null, "false", async () =>
        {
            var sut = CreateSut(dynamoDb.Object);
            await sut.StartAsync(CancellationToken.None);
        });

        dynamoDb.Verify(
            x => x.UpdateContinuousBackupsAsync(
                It.IsAny<UpdateContinuousBackupsRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void BuildRestoreTargetTableName_ReturnsDistinctNonLiveTableName()
    {
        var restorePoint = new DateTimeOffset(2026, 04, 07, 12, 30, 45, TimeSpan.Zero);

        var targetTableName = DynamoDbBackupProtectionHostedService.BuildRestoreTargetTableName(
            "payslip4all_wallets",
            restorePoint);

        Assert.Equal("payslip4all_wallets-restore-20260407123045", targetTableName);
        Assert.NotEqual("payslip4all_wallets", targetTableName);
        Assert.Contains("-restore-", targetTableName, StringComparison.Ordinal);
    }

    private static IHostedService CreateSut(IAmazonDynamoDB dynamoDb)
    {
        return new DynamoDbBackupProtectionHostedService(
            dynamoDb,
            NullLogger<DynamoDbBackupProtectionHostedService>.Instance);
    }

    private static async Task WithEnvironment(string prefix, string? endpoint, string? enablePointInTimeRecovery, Func<Task> action)
    {
        var savedPrefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX");
        var savedEndpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT");
        var savedEnablePointInTimeRecovery = Environment.GetEnvironmentVariable("DYNAMODB_ENABLE_PITR");

        Environment.SetEnvironmentVariable("DYNAMODB_TABLE_PREFIX", prefix);
        Environment.SetEnvironmentVariable("DYNAMODB_ENDPOINT", endpoint);
        Environment.SetEnvironmentVariable("DYNAMODB_ENABLE_PITR", enablePointInTimeRecovery);

        try
        {
            await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("DYNAMODB_TABLE_PREFIX", savedPrefix);
            Environment.SetEnvironmentVariable("DYNAMODB_ENDPOINT", savedEndpoint);
            Environment.SetEnvironmentVariable("DYNAMODB_ENABLE_PITR", savedEnablePointInTimeRecovery);
        }
    }
}
