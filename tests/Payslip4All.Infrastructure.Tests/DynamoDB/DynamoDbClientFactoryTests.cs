using Amazon.DynamoDBv2;
using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Infrastructure.Tests.DynamoDB;

/// <summary>
/// Unit tests for <see cref="DynamoDbClientFactory"/>.
/// </summary>
public class DynamoDbClientFactoryTests : IDisposable
{
    private readonly Dictionary<string, string?> _savedEnv = new();

    private static string GetTestEndpoint()
        => Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT")?.Trim()
           ?? "http://localhost:8000";

    private static string NormalizeServiceUrl(string endpoint)
        => endpoint.EndsWith("/", StringComparison.Ordinal) ? endpoint : $"{endpoint}/";

    private void SetEnv(string key, string? value)
    {
        _savedEnv.TryAdd(key, Environment.GetEnvironmentVariable(key));
        Environment.SetEnvironmentVariable(key, value);
    }

    public void Dispose()
    {
        foreach (var (key, value) in _savedEnv)
            Environment.SetEnvironmentVariable(key, value);
    }

    [Fact]
    public void Create_WithRegionOnly_ReturnsClient()
    {
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", null);
        SetEnv("AWS_ACCESS_KEY_ID", "dummy");
        SetEnv("AWS_SECRET_ACCESS_KEY", "dummy");

        var client = DynamoDbClientFactory.Create();

        Assert.NotNull(client);
        client.Dispose();
    }

    [Fact]
    public void Create_WithEndpointOverride_SetsServiceURL()
    {
        var endpoint = GetTestEndpoint();
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", endpoint);
        SetEnv("AWS_ACCESS_KEY_ID", "dummy");
        SetEnv("AWS_SECRET_ACCESS_KEY", "dummy");

        var client = DynamoDbClientFactory.Create();

        Assert.NotNull(client);
        // The client config should have the service URL set
        var amazonClient = (AmazonDynamoDBClient)client;
        var config = amazonClient.Config;
        Assert.Equal(NormalizeServiceUrl(endpoint), config.ServiceURL);

        client.Dispose();
    }

    [Fact]
    public void Create_WithoutRegion_ThrowsInvalidOperationException()
    {
        SetEnv("DYNAMODB_REGION", null);
        SetEnv("DYNAMODB_ENDPOINT", null);

        var ex = Assert.Throws<InvalidOperationException>(() => DynamoDbClientFactory.Create());
        Assert.Contains("DYNAMODB_REGION", ex.Message);
    }

    [Fact]
    public void Create_WithWhitespaceRegion_ThrowsInvalidOperationException()
    {
        SetEnv("DYNAMODB_REGION", "   ");
        SetEnv("DYNAMODB_ENDPOINT", null);

        var ex = Assert.Throws<InvalidOperationException>(() => DynamoDbClientFactory.Create());
        Assert.Contains("DYNAMODB_REGION", ex.Message);
    }

    [Fact]
    public void Create_WithExplicitCredentials_ReturnsClient()
    {
        SetEnv("DYNAMODB_REGION", "eu-west-1");
        SetEnv("DYNAMODB_ENDPOINT", null);
        SetEnv("AWS_ACCESS_KEY_ID", "test-key");
        SetEnv("AWS_SECRET_ACCESS_KEY", "test-secret");

        var client = DynamoDbClientFactory.Create();

        Assert.NotNull(client);
        client.Dispose();
    }

    [Fact]
    public void Create_WithEndpointAndCredentials_ReturnsClientWithServiceURL()
    {
        var endpoint = GetTestEndpoint();
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", endpoint);
        SetEnv("AWS_ACCESS_KEY_ID", "dummy");
        SetEnv("AWS_SECRET_ACCESS_KEY", "dummy");

        var client = DynamoDbClientFactory.Create();

        Assert.NotNull(client);
        var amazonClient = (AmazonDynamoDBClient)client;
        Assert.Equal(NormalizeServiceUrl(endpoint), amazonClient.Config.ServiceURL);

        client.Dispose();
    }

    /// <summary>
    /// T027 — Integration test: DynamoDbClientFactory with endpoint override connects to the configured
    /// DynamoDB-compatible endpoint and provisioner successfully creates tables.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_WithEndpointOverride_ProvisionerCreatesTablesAgainstLocal()
    {
        var endpoint = GetTestEndpoint();
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", endpoint);
        SetEnv("AWS_ACCESS_KEY_ID", "dummy");
        SetEnv("AWS_SECRET_ACCESS_KEY", "dummy");

        var prefix = $"t027_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable("DYNAMODB_TABLE_PREFIX", prefix);
        _savedEnv.TryAdd("DYNAMODB_TABLE_PREFIX", null);

        var client = DynamoDbClientFactory.Create();
        var amazonClient = (AmazonDynamoDBClient)client;
        Assert.Equal(NormalizeServiceUrl(endpoint), amazonClient.Config.ServiceURL);

        var loggerMock = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<Payslip4All.Infrastructure.Persistence.DynamoDB.DynamoDbTableProvisioner>>();
        var provisioner = new Payslip4All.Infrastructure.Persistence.DynamoDB.DynamoDbTableProvisioner(client, loggerMock.Object);

        // Act — provision tables
        await provisioner.StartAsync(CancellationToken.None);

        // Assert — all 6 tables should exist
        var tableNames = new[]
        {
            $"{prefix}_users", $"{prefix}_companies", $"{prefix}_employees",
            $"{prefix}_employee_loans", $"{prefix}_payslips", $"{prefix}_payslip_loan_deductions",
        };

        foreach (var tableName in tableNames)
        {
            var desc = await amazonClient.DescribeTableAsync(tableName);
            Assert.Equal(Amazon.DynamoDBv2.TableStatus.ACTIVE, desc.Table.TableStatus);
            // Cleanup
            await amazonClient.DeleteTableAsync(tableName);
        }

        client.Dispose();
    }
}
