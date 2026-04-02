using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System.Globalization;
using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Infrastructure.Tests.DynamoDB;

/// <summary>
/// xUnit IAsyncLifetime fixture that creates all 6 DynamoDB tables before each test class
/// and deletes them after. Uses DynamoDB Local via DYNAMODB_ENDPOINT env var.
/// Set DYNAMODB_ENDPOINT=http://localhost:8000 (or the env var) before running integration tests.
/// </summary>
public class DynamoDbTestFixture : IAsyncLifetime
{
    public IAmazonDynamoDB Client { get; private set; } = null!;
    public string Prefix { get; private set; } = null!;
    private string? _originalTablePrefix;
    private string? _originalRegion;
    private string? _originalEndpoint;
    private string? _originalAccessKey;
    private string? _originalSecretKey;

    // Table names
    public string UsersTable => $"{Prefix}_users";
    public string CompaniesTable => $"{Prefix}_companies";
    public string EmployeesTable => $"{Prefix}_employees";
    public string EmployeeLoansTable => $"{Prefix}_employee_loans";
    public string PayslipsTable => $"{Prefix}_payslips";
    public string PayslipLoanDeductionsTable => $"{Prefix}_payslip_loan_deductions";
    public string WalletsTable => $"{Prefix}_wallets";
    public string WalletActivitiesTable => $"{Prefix}_wallet_activities";
    public string WalletTopUpAttemptsTable => $"{Prefix}_wallet_topup_attempts";
    public string PayslipPricingTable => $"{Prefix}_payslip_pricing";

    public async Task InitializeAsync()
    {
        _originalTablePrefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX");
        _originalRegion = Environment.GetEnvironmentVariable("DYNAMODB_REGION");
        _originalEndpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT");
        _originalAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        _originalSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        // Use a unique prefix per test run to avoid conflicts between parallel runs
        Prefix = $"test_{Guid.NewGuid():N}";

        Environment.SetEnvironmentVariable(
            "DYNAMODB_REGION",
            Environment.GetEnvironmentVariable("DYNAMODB_REGION")?.Trim() ?? "us-east-1");
        Environment.SetEnvironmentVariable(
            "DYNAMODB_ENDPOINT",
            Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT")?.Trim() ?? "http://localhost:8000");

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")))
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "dummy");

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "dummy");

        Client = DynamoDbClientFactory.Create();

        // Set env vars so repositories can find tables
        Environment.SetEnvironmentVariable("DYNAMODB_TABLE_PREFIX", Prefix);

        await CreateAllTablesAsync();
    }

    public async Task DisposeAsync()
    {
        await DeleteAllTablesAsync();
        Client.Dispose();
        Environment.SetEnvironmentVariable("DYNAMODB_TABLE_PREFIX", _originalTablePrefix);
        Environment.SetEnvironmentVariable("DYNAMODB_REGION", _originalRegion);
        Environment.SetEnvironmentVariable("DYNAMODB_ENDPOINT", _originalEndpoint);
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", _originalAccessKey);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", _originalSecretKey);
    }

    private async Task CreateAllTablesAsync()
    {
        var tables = GetTableDefinitions();
        foreach (var request in tables)
        {
            await Client.CreateTableAsync(request);
            await WaitForActiveAsync(request.TableName);
        }
    }

    private async Task DeleteAllTablesAsync()
    {
        var tableNames = new[]
        {
            UsersTable, CompaniesTable, EmployeesTable,
            EmployeeLoansTable, PayslipsTable, PayslipLoanDeductionsTable,
            WalletsTable, WalletActivitiesTable, WalletTopUpAttemptsTable, PayslipPricingTable,
        };

        foreach (var name in tableNames)
        {
            try
            {
                await Client.DeleteTableAsync(name);
            }
            catch (ResourceNotFoundException)
            {
                // Already gone
            }
        }
    }

    private async Task WaitForActiveAsync(string tableName)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var response = await Client.DescribeTableAsync(tableName, cts.Token);
                if (response.Table.TableStatus == TableStatus.ACTIVE)
                    return;
            }
            catch (ResourceNotFoundException)
            {
                // Table creation is eventually consistent; keep polling until it becomes visible.
            }

            await Task.Delay(200, cts.Token);
        }
        throw new TimeoutException($"Table {tableName} did not become ACTIVE within 30 seconds.");
    }

    private List<CreateTableRequest> GetTableDefinitions() =>
        new List<CreateTableRequest>
        {
            new()
            {
                TableName = UsersTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "email", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new()
                {
                    new()
                    {
                        IndexName = "email-index",
                        KeySchema = new() { new() { AttributeName = "email", KeyType = KeyType.HASH } },
                        Projection = new() { ProjectionType = ProjectionType.ALL },
                    },
                },
            },
            new()
            {
                TableName = CompaniesTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "userId", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new()
                {
                    new()
                    {
                        IndexName = "userId-index",
                        KeySchema = new() { new() { AttributeName = "userId", KeyType = KeyType.HASH } },
                        Projection = new() { ProjectionType = ProjectionType.ALL },
                    },
                },
            },
            new()
            {
                TableName = EmployeesTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "companyId", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new()
                {
                    new()
                    {
                        IndexName = "companyId-index",
                        KeySchema = new() { new() { AttributeName = "companyId", KeyType = KeyType.HASH } },
                        Projection = new() { ProjectionType = ProjectionType.ALL },
                    },
                },
            },
            new()
            {
                TableName = EmployeeLoansTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "employeeId", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new()
                {
                    new()
                    {
                        IndexName = "employeeId-index",
                        KeySchema = new() { new() { AttributeName = "employeeId", KeyType = KeyType.HASH } },
                        Projection = new() { ProjectionType = ProjectionType.ALL },
                    },
                },
            },
            new()
            {
                TableName = PayslipsTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "employeeId", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "generatedAt", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new()
                {
                    new()
                    {
                        IndexName = "employeeId-index",
                        KeySchema = new()
                        {
                            new() { AttributeName = "employeeId", KeyType = KeyType.HASH },
                            new() { AttributeName = "generatedAt", KeyType = KeyType.RANGE },
                        },
                        Projection = new() { ProjectionType = ProjectionType.ALL },
                    },
                },
            },
            new()
            {
                TableName = PayslipLoanDeductionsTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "payslipId", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new()
                {
                    new()
                    {
                        IndexName = "payslipId-index",
                        KeySchema = new() { new() { AttributeName = "payslipId", KeyType = KeyType.HASH } },
                        Projection = new() { ProjectionType = ProjectionType.ALL },
                    },
                },
            },
            new()
            {
                TableName = WalletsTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                },
            },
            new()
            {
                TableName = WalletActivitiesTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "walletId", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "occurredAt", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new()
                {
                    new()
                    {
                        IndexName = "walletId-index",
                        KeySchema = new()
                        {
                            new() { AttributeName = "walletId", KeyType = KeyType.HASH },
                            new() { AttributeName = "occurredAt", KeyType = KeyType.RANGE },
                        },
                        Projection = new() { ProjectionType = ProjectionType.ALL },
                    },
                },
            },
            new()
            {
                TableName = WalletTopUpAttemptsTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "userId", AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = "createdAt", AttributeType = ScalarAttributeType.S },
                },
                GlobalSecondaryIndexes = new()
                {
                    new()
                    {
                        IndexName = "userId-createdAt-index",
                        KeySchema = new()
                        {
                            new() { AttributeName = "userId", KeyType = KeyType.HASH },
                            new() { AttributeName = "createdAt", KeyType = KeyType.RANGE },
                        },
                        Projection = new() { ProjectionType = ProjectionType.ALL },
                    },
                },
            },
            new()
            {
                TableName = PayslipPricingTable,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new()
                {
                    new() { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                },
            },
        };

    public async Task SeedWalletAsync(
        Guid walletId,
        Guid userId,
        decimal currentBalance = 0m,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        var created = createdAt ?? DateTimeOffset.UtcNow;
        var updated = updatedAt ?? created;

        await Client.PutItemAsync(new PutItemRequest
        {
            TableName = WalletsTable,
            Item = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = walletId.ToString() },
                ["userId"] = new() { S = userId.ToString() },
                ["currentBalance"] = new() { S = currentBalance.ToString("G", CultureInfo.InvariantCulture) },
                ["createdAt"] = new() { S = created.ToString("O") },
                ["updatedAt"] = new() { S = updated.ToString("O") },
            },
        });
    }

    public async Task SeedWalletActivityAsync(
        Guid activityId,
        Guid walletId,
        string activityType,
        decimal amount,
        decimal balanceAfterActivity,
        DateTimeOffset? occurredAt = null,
        string? description = null,
        string? referenceType = null,
        string? referenceId = null)
    {
        var occurred = occurredAt ?? DateTimeOffset.UtcNow;
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = activityId.ToString() },
            ["walletId"] = new() { S = walletId.ToString() },
            ["activityType"] = new() { S = activityType },
            ["amount"] = new() { S = amount.ToString("G", CultureInfo.InvariantCulture) },
            ["balanceAfterActivity"] = new() { S = balanceAfterActivity.ToString("G", CultureInfo.InvariantCulture) },
            ["occurredAt"] = new() { S = occurred.ToString("O") },
        };

        if (!string.IsNullOrWhiteSpace(description))
            item["description"] = new() { S = description };
        if (!string.IsNullOrWhiteSpace(referenceType))
            item["referenceType"] = new() { S = referenceType };
        if (!string.IsNullOrWhiteSpace(referenceId))
            item["referenceId"] = new() { S = referenceId };

        await Client.PutItemAsync(new PutItemRequest
        {
            TableName = WalletActivitiesTable,
            Item = item,
        });
    }

    public async Task SeedPayslipPricingAsync(
        Guid pricingId,
        decimal pricePerPayslip = 0m,
        string? updatedByUserId = null,
        DateTimeOffset? updatedAt = null)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = pricingId.ToString() },
            ["pricePerPayslip"] = new() { S = pricePerPayslip.ToString("G", CultureInfo.InvariantCulture) },
            ["updatedAt"] = new() { S = (updatedAt ?? DateTimeOffset.UtcNow).ToString("O") },
        };

        if (!string.IsNullOrWhiteSpace(updatedByUserId))
            item["updatedByUserId"] = new() { S = updatedByUserId };

        await Client.PutItemAsync(new PutItemRequest
        {
            TableName = PayslipPricingTable,
            Item = item,
        });
    }
}
