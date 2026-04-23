using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

public sealed class DynamoDbBackupProtectionHostedService : IHostedService
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ILogger<DynamoDbBackupProtectionHostedService> _logger;
    private readonly DynamoDbTableNameProvider _tableNames;
    private readonly string? _endpoint;
    private readonly bool _enablePointInTimeRecovery;

    public DynamoDbBackupProtectionHostedService(
        IAmazonDynamoDB dynamoDb,
        ILogger<DynamoDbBackupProtectionHostedService> logger,
        DynamoDbConfigurationOptions options,
        DynamoDbTableNameProvider tableNames)
    {
        _dynamoDb = dynamoDb;
        _logger = logger;
        _tableNames = tableNames;
        _endpoint = options.Endpoint;
        _enablePointInTimeRecovery = options.EnablePointInTimeRecovery;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_enablePointInTimeRecovery)
        {
            _logger.LogInformation("DynamoDB point-in-time recovery protection is disabled by configuration.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_endpoint))
        {
            _logger.LogInformation(
                "Skipping DynamoDB point-in-time recovery protection because DYNAMODB_ENDPOINT is configured for a local or custom endpoint.");
            return;
        }

        foreach (var tableName in _tableNames.GetRequiredTableNames())
        {
            DescribeContinuousBackupsResponse describeResponse;
            try
            {
                describeResponse = await _dynamoDb.DescribeContinuousBackupsAsync(
                    new DescribeContinuousBackupsRequest
                    {
                        TableName = tableName
                    },
                    cancellationToken);
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogWarning(
                    "Skipping point-in-time recovery enablement because DynamoDB table '{TableName}' does not exist yet.",
                    tableName);
                continue;
            }

            var currentStatus = describeResponse
                .ContinuousBackupsDescription?
                .PointInTimeRecoveryDescription?
                .PointInTimeRecoveryStatus;

            if (currentStatus == PointInTimeRecoveryStatus.ENABLED)
            {
                _logger.LogInformation(
                    "DynamoDB point-in-time recovery is already enabled for table '{TableName}'.",
                    tableName);
                continue;
            }

            await _dynamoDb.UpdateContinuousBackupsAsync(
                new UpdateContinuousBackupsRequest
                {
                    TableName = tableName,
                    PointInTimeRecoverySpecification = new PointInTimeRecoverySpecification
                    {
                        PointInTimeRecoveryEnabled = true
                    }
                },
                cancellationToken);

            _logger.LogInformation(
                "Enabled DynamoDB point-in-time recovery for table '{TableName}'.",
                tableName);
        }
    }

    public static string BuildRestoreTargetTableName(string liveTableName, DateTimeOffset restorePointUtc)
    {
        if (string.IsNullOrWhiteSpace(liveTableName))
            throw new ArgumentException("A live table name is required.", nameof(liveTableName));

        return $"{liveTableName}-restore-{restorePointUtc:yyyyMMddHHmmss}";
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
