using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Web.Tests.Integration;

internal sealed class LocalDynamoDbTestHarness : IDisposable
{
    private readonly Dictionary<string, string?> _savedEnv = new();

    public LocalDynamoDbTestHarness(string? tablePrefix = null, string? endpoint = null)
    {
        TablePrefix = tablePrefix ?? $"webtest_{Guid.NewGuid():N}";
        Endpoint = endpoint?.Trim() ?? Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT")?.Trim() ?? "http://adhir-server:8000";

        ConfigureEnvironment();
    }

    public string Endpoint { get; }
    public string TablePrefix { get; }
    public string NormalizedServiceUrl => Endpoint.EndsWith("/", StringComparison.Ordinal) ? Endpoint : $"{Endpoint}/";

    public IReadOnlyList<string> ExpectedTableNames =>
        new DynamoDbTableNameProvider(new DynamoDbConfigurationOptions { TablePrefix = TablePrefix }).GetRequiredTableNames();

    public WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("PERSISTENCE_PROVIDER", "dynamodb");
        });
    }

    public async Task DeleteProvisionedTablesAsync(IAmazonDynamoDB dynamoDb)
    {
        foreach (var tableName in ExpectedTableNames)
        {
            try
            {
                await dynamoDb.DeleteTableAsync(tableName);
            }
            catch (ResourceNotFoundException)
            {
                // Table was never created or was already cleaned up.
            }
        }
    }

    public void Dispose()
    {
        foreach (var (key, value) in _savedEnv)
            Environment.SetEnvironmentVariable(key, value);
    }

    private void ConfigureEnvironment()
    {
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", Endpoint);
        SetEnv("DYNAMODB_TABLE_PREFIX", TablePrefix);
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);
    }

    private void SetEnv(string key, string? value)
    {
        _savedEnv.TryAdd(key, Environment.GetEnvironmentVariable(key));
        Environment.SetEnvironmentVariable(key, value);
    }
}
