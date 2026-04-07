using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

/// <summary>
/// Hosted service that ensures all required DynamoDB tables exist at startup.
/// Creates missing tables with PAY_PER_REQUEST billing and waits for ACTIVE status.
/// </summary>
public sealed class DynamoDbTableProvisioner : IHostedService
{
    private static readonly TimeSpan DefaultActivationTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(500);

    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ILogger<DynamoDbTableProvisioner> _logger;
    private readonly string _prefix;
    private readonly TimeSpan _activationTimeout;
    private readonly TimeSpan _pollInterval;

    public DynamoDbTableProvisioner(
        IAmazonDynamoDB dynamoDb,
        ILogger<DynamoDbTableProvisioner> logger,
        TimeSpan? activationTimeout = null,
        TimeSpan? pollInterval = null)
    {
        _dynamoDb = dynamoDb;
        _logger = logger;
        _prefix = GetCurrentTablePrefix();
        _activationTimeout = activationTimeout ?? DefaultActivationTimeout;
        _pollInterval = pollInterval ?? DefaultPollInterval;
    }

    public static string GetCurrentTablePrefix()
    {
        return Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim()
               ?? "payslip4all";
    }

    public static IReadOnlyList<string> GetRequiredTableNames(string prefix)
    {
        return new[]
        {
            $"{prefix}_users",
            $"{prefix}_companies",
            $"{prefix}_employees",
            $"{prefix}_employee_loans",
            $"{prefix}_payslips",
            $"{prefix}_payslip_loan_deductions",
            $"{prefix}_wallets",
            $"{prefix}_wallet_activities",
            $"{prefix}_payslip_pricing",
            $"{prefix}_payment_return_evidences",
            $"{prefix}_outcome_normalization_decisions",
            $"{prefix}_unmatched_payment_return_records",
            $"{prefix}_wallet_topup_attempts"
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var tables = GetTableDefinitions();
        foreach (var table in tables)
        {
            await EnsureTableExistsAsync(table, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureTableExistsAsync(CreateTableRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _dynamoDb.DescribeTableAsync(request.TableName, cancellationToken);
            _logger.LogInformation(
                "DynamoDB table '{TableName}' already exists with status {Status}.",
                request.TableName,
                response.Table.TableStatus);

            if (response.Table.TableStatus != TableStatus.ACTIVE)
                await WaitForTableActiveAsync(request.TableName, cancellationToken);
        }
        catch (ResourceNotFoundException)
        {
            try
            {
                await _dynamoDb.CreateTableAsync(request, cancellationToken);
                _logger.LogInformation("DynamoDB table '{TableName}' created.", request.TableName);
            }
            catch (ResourceInUseException)
            {
                _logger.LogInformation(
                    "DynamoDB table '{TableName}' is already being created by another instance.",
                    request.TableName);
            }

            await WaitForTableActiveAsync(request.TableName, cancellationToken);
        }
    }

    private async Task WaitForTableActiveAsync(string tableName, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + _activationTimeout;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var response = await _dynamoDb.DescribeTableAsync(tableName, cancellationToken);
                if (response.Table.TableStatus == TableStatus.ACTIVE)
                    return;
            }
            catch (ResourceNotFoundException)
            {
                // Table creation is eventually consistent; keep polling until it becomes visible.
            }

            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException(
                    $"Timed out waiting for DynamoDB table '{tableName}' to become ACTIVE.");

            await Task.Delay(_pollInterval, cancellationToken);
        }
    }

    private List<CreateTableRequest> GetTableDefinitions()
    {
        return new List<CreateTableRequest>
        {
            // payslip4all_users — PK: id (S), GSI: email-index on email
            new CreateTableRequest
            {
                TableName = $"{_prefix}_users",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "email", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "email-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "email", KeyType = KeyType.HASH },
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    },
                },
            },

            // payslip4all_companies — PK: id (S), GSI: userId-index on userId
            new CreateTableRequest
            {
                TableName = $"{_prefix}_companies",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "userId", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "userId-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "userId", KeyType = KeyType.HASH },
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    },
                },
            },

            // payslip4all_employees — PK: id (S), GSI: companyId-index on companyId
            new CreateTableRequest
            {
                TableName = $"{_prefix}_employees",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "companyId", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "companyId-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "companyId", KeyType = KeyType.HASH },
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    },
                },
            },

            // payslip4all_employee_loans — PK: id (S), GSI: employeeId-index on employeeId
            new CreateTableRequest
            {
                TableName = $"{_prefix}_employee_loans",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "employeeId", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "employeeId-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "employeeId", KeyType = KeyType.HASH },
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    },
                },
            },

            // payslip4all_payslips — PK: id (S), GSI: employeeId-index on employeeId + SK generatedAt
            new CreateTableRequest
            {
                TableName = $"{_prefix}_payslips",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "employeeId", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "generatedAt", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "employeeId-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "employeeId", KeyType = KeyType.HASH },
                            new KeySchemaElement { AttributeName = "generatedAt", KeyType = KeyType.RANGE },
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    },
                },
            },

            // payslip4all_payslip_loan_deductions — PK: id (S), GSI: payslipId-index on payslipId
            new CreateTableRequest
            {
                TableName = $"{_prefix}_payslip_loan_deductions",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "payslipId", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "payslipId-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "payslipId", KeyType = KeyType.HASH },
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    },
                },
            },

            // payslip4all_wallets — PK: id (S), where id is the canonical userId-backed wallet identifier
            new CreateTableRequest
            {
                TableName = $"{_prefix}_wallets",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                },
            },

            // payslip4all_wallet_activities — PK: id (S), GSI: walletId-index on walletId + occurredAt
            new CreateTableRequest
            {
                TableName = $"{_prefix}_wallet_activities",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "walletId", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "occurredAt", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new()
                    {
                        IndexName = "walletId-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new() { AttributeName = "walletId", KeyType = KeyType.HASH },
                            new() { AttributeName = "occurredAt", KeyType = KeyType.RANGE },
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    },
                },
            },

            // payslip4all_payslip_pricing — PK: id (S)
            new CreateTableRequest
            {
                TableName = $"{_prefix}_payslip_pricing",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                },
            },

            // payslip4all_payment_return_evidences — PK: id (S)
            new CreateTableRequest
            {
                TableName = $"{_prefix}_payment_return_evidences",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                },
            },

            // payslip4all_outcome_normalization_decisions — PK: id (S)
            new CreateTableRequest
            {
                TableName = $"{_prefix}_outcome_normalization_decisions",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                },
            },

            // payslip4all_unmatched_payment_return_records — PK: id (S)
            new CreateTableRequest
            {
                TableName = $"{_prefix}_unmatched_payment_return_records",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                },
            },
            // payslip4all_wallet_topup_attempts — PK: id (S), GSI: userId-createdAt-index on userId + createdAt
            new CreateTableRequest
            {
                TableName = $"{_prefix}_wallet_topup_attempts",
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = "id", KeyType = KeyType.HASH },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "userId", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "createdAt", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new()
                    {
                        IndexName = "userId-createdAt-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new() { AttributeName = "userId", KeyType = KeyType.HASH },
                            new() { AttributeName = "createdAt", KeyType = KeyType.RANGE },
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    },
                },
            },
        };
    }
}
