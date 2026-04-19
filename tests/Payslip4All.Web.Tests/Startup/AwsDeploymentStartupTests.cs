using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Web.Tests.Startup;

[Collection(DynamoDbStartupTestCollection.Name)]
public sealed class AwsDeploymentStartupTests : IDisposable
{
    private readonly Dictionary<string, string?> _savedEnv = new();

    [Fact]
    public async Task HealthEndpoint_IsPubliclyExposed_AndReturnsHealthyPayload()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"Healthy\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public void ReverseProxyStartup_TrustsForwardedHost_Proto_And_ClientIpHeaders()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        Assert.Equal(
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
            options.ForwardedHeaders);
        Assert.Empty(options.KnownNetworks);
        Assert.Empty(options.KnownProxies);
    }

    [Fact]
    public void AwsHostedDeployment_KeepsTheUpstreamOnLoopbackOnly()
    {
        var solutionRoot = GetSolutionRoot();
        var bootstrap = File.ReadAllText(Path.Combine(solutionRoot, "infra", "aws", "cloudformation", "user-data", "bootstrap-payslip4all.sh"));
        var template = File.ReadAllText(Path.Combine(solutionRoot, "infra", "aws", "cloudformation", "payslip4all-web.yaml"));

        Assert.Contains("ASPNETCORE_URLS=http://127.0.0.1:8080", bootstrap, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://127.0.0.1:8080", template, StringComparison.Ordinal);
        Assert.DoesNotContain("ASPNETCORE_URLS=http://0.0.0.0:80", bootstrap, StringComparison.Ordinal);
        Assert.DoesNotContain("ASPNETCORE_URLS=http://0.0.0.0:80", template, StringComparison.Ordinal);
    }

    [Fact]
    public void DynamoDbProvider_WhenHostedAwsConfigured_RegistersBackupProtectionHostedService_AndTableProvisioner()
    {
        SetEnv("DYNAMODB_REGION", "af-south-1");
        SetEnv("DYNAMODB_TABLE_PREFIX", "payslip4all");
        SetEnv("DYNAMODB_ENDPOINT", null);
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("PERSISTENCE_PROVIDER", "dynamodb");
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(descriptor);
            });
        });

        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DynamoDbBackupProtectionHostedService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DynamoDbTableProvisioner>());
    }

    [Fact]
    public void DynamoDbProvider_WhenRegionIsMissing_FailsFastDuringStartup()
    {
        SetEnv("DYNAMODB_REGION", null);
        SetEnv("DYNAMODB_TABLE_PREFIX", "payslip4all");
        SetEnv("DYNAMODB_ENDPOINT", null);
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("PERSISTENCE_PROVIDER", "dynamodb");
            });

            using var client = factory.CreateClient();
        });

        Assert.Contains("DYNAMODB_REGION", exception.Message, StringComparison.Ordinal);
    }

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

    private static string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir is not null && !File.Exists(Path.Combine(currentDir, "Payslip4All.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        return currentDir ?? throw new InvalidOperationException("Could not find solution root.");
    }
}
