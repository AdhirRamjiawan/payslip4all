using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Payslip4All.Web.Tests.Startup;

[Collection(DynamoDbStartupTestCollection.Name)]
public sealed class DynamoDbConfigurationValidationTests : IDisposable
{
    private readonly Dictionary<string, string?> _savedEnv = new();

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

    private static WebApplicationFactory<Program> BuildFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("PERSISTENCE_PROVIDER", "dynamodb");
            builder.ConfigureServices(services =>
            {
                var provisionerDescriptor = services.SingleOrDefault(
                    d => d.ImplementationType == typeof(Payslip4All.Infrastructure.Persistence.DynamoDB.DynamoDbTableProvisioner));
                if (provisionerDescriptor != null)
                    services.Remove(provisionerDescriptor);
            });
        });
    }

    [Fact]
    public void DynamoDbProvider_WithWhitespaceRegion_ThrowsInvalidOperationException()
    {
        SetEnv("DYNAMODB_REGION", "   ");
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);
        SetEnv("DYNAMODB_ENDPOINT", null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = BuildFactory();
            _ = factory.Services;
        });

        Assert.Contains("DYNAMODB_REGION", ex.Message);
    }

    [Fact]
    public void DynamoDbProvider_WithOnlyAccessKey_ThrowsInvalidOperationException()
    {
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("AWS_ACCESS_KEY_ID", "explicit-key");
        SetEnv("AWS_SECRET_ACCESS_KEY", null);
        SetEnv("DYNAMODB_ENDPOINT", null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = BuildFactory();
            _ = factory.Services;
        });

        Assert.Contains("AWS_ACCESS_KEY_ID", ex.Message);
        Assert.Contains("AWS_SECRET_ACCESS_KEY", ex.Message);
    }

    [Fact]
    public void DynamoDbProvider_WithOnlySecretKey_ThrowsInvalidOperationException()
    {
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", "explicit-secret");
        SetEnv("DYNAMODB_ENDPOINT", null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = BuildFactory();
            _ = factory.Services;
        });

        Assert.Contains("AWS_ACCESS_KEY_ID", ex.Message);
        Assert.Contains("AWS_SECRET_ACCESS_KEY", ex.Message);
    }

    [Fact]
    public void DynamoDbProvider_WithEndpointAndPrefix_RegistersClientWithoutExplicitCredentials()
    {
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", "http://localhost:8000");
        SetEnv("DYNAMODB_TABLE_PREFIX", "spec-prefix");
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);

        using var factory = BuildFactory();
        using var scope = factory.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();

        Assert.NotNull(client);
        Assert.Equal("http://localhost:8000/", ((AmazonDynamoDBClient)client).Config.ServiceURL);
    }

    [Fact]
    public void DynamoDbProvider_WithoutEndpointOrExplicitCredentials_RegistersClientForSdkCredentialChain()
    {
        SetEnv("DYNAMODB_REGION", "af-south-1");
        SetEnv("DYNAMODB_ENDPOINT", null);
        SetEnv("DYNAMODB_TABLE_PREFIX", "hosted-prefix");
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);

        using var factory = BuildFactory();
        using var scope = factory.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();

        Assert.NotNull(client);
        Assert.Null(((AmazonDynamoDBClient)client).Config.ServiceURL);
    }
}
