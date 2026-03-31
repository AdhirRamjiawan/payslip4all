using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
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
        };
}
