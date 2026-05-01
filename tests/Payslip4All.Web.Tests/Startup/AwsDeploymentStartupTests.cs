using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Payslip4All.Application.Interfaces;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Infrastructure.Persistence.DynamoDB;
using Payslip4All.Web.Tests.Infrastructure;
using Yarp.ReverseProxy.Configuration;

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
    public void ReverseProxyMode_WhenEnabled_RegistersYarpServices_WithoutTheApplicationRuntime()
    {
        using var certificate = TestTlsCertificate.Create();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("REVERSE_PROXY_ENABLED", "true");
            builder.UseSetting("REVERSE_PROXY_PUBLIC_HOST", "payslip4all.co.za");
            builder.UseSetting("REVERSE_PROXY_UPSTREAM_BASE_URL", "http://127.0.0.1:8080");
            builder.UseSetting("Kestrel:Certificates:Default:Path", certificate.CertificatePath);
            builder.UseSetting("Kestrel:Certificates:Default:Password", certificate.Password);
        });

        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IProxyConfigProvider>());
        Assert.Null(scope.ServiceProvider.GetService<IPayslipService>());
        Assert.Null(scope.ServiceProvider.GetService<PayslipDbContext>());
    }

    [Fact]
    public void ReverseProxyMode_WhenCertificateSettingsAreMissing_FailsClosedWithExactActivationError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("REVERSE_PROXY_ENABLED", "true");
                builder.UseSetting("REVERSE_PROXY_PUBLIC_HOST", "payslip4all.co.za");
                builder.UseSetting("REVERSE_PROXY_UPSTREAM_BASE_URL", "http://127.0.0.1:8080");
            });

            using var client = factory.CreateClient();
        });

        Assert.Equal(
            "HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.",
            exception.Message);
    }

    [Fact]
    public void ReverseProxyMode_WhenCertificateMaterialIsInvalid_FailsClosedWithExactActivationError()
    {
        var invalidCertificatePath = Path.Combine(Path.GetTempPath(), $"p4a-invalid-{Guid.NewGuid():N}.pfx");
        File.WriteAllText(invalidCertificatePath, "invalid-pfx");

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development");
                    builder.UseSetting("REVERSE_PROXY_ENABLED", "true");
                    builder.UseSetting("REVERSE_PROXY_PUBLIC_HOST", "payslip4all.co.za");
                    builder.UseSetting("REVERSE_PROXY_UPSTREAM_BASE_URL", "http://127.0.0.1:8080");
                    builder.UseSetting("Kestrel:Certificates:Default:Path", invalidCertificatePath);
                    builder.UseSetting("Kestrel:Certificates:Default:Password", "not-a-real-password");
                });

                using var client = factory.CreateClient();
            });

            Assert.Equal(
                "HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.",
                exception.Message);
        }
        finally
        {
            if (File.Exists(invalidCertificatePath))
                File.Delete(invalidCertificatePath);
        }
    }

    [Fact]
    public void ReverseProxyMode_WhenConfiguredForHttpOnly_DoesNotRequireCertificateSettings()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("REVERSE_PROXY_ENABLED", "true");
            builder.UseSetting("REVERSE_PROXY_PUBLIC_HOST", "payslip4all.co.za");
            builder.UseSetting("REVERSE_PROXY_UPSTREAM_BASE_URL", "http://127.0.0.1:8080");
            builder.UseSetting("ASPNETCORE_URLS", "http://0.0.0.0:80");
        });

        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IProxyConfigProvider>());
    }

    [Fact]
    public void ReverseProxyMode_WhenUpstreamTargetIsPublic_FailsFastDuringStartup()
    {
        using var certificate = TestTlsCertificate.Create();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("REVERSE_PROXY_ENABLED", "true");
                builder.UseSetting("REVERSE_PROXY_PUBLIC_HOST", "payslip4all.co.za");
                builder.UseSetting("REVERSE_PROXY_UPSTREAM_BASE_URL", "http://8.8.8.8:8080");
                builder.UseSetting("Kestrel:Certificates:Default:Path", certificate.CertificatePath);
                builder.UseSetting("Kestrel:Certificates:Default:Password", certificate.Password);
            });

            using var client = factory.CreateClient();
        });

        Assert.Contains("internal-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AwsHostedDeployment_KeepsTheBackendOnLoopback_AndPublishesYarpOn80And443()
    {
        var solutionRoot = GetSolutionRoot();
        var bootstrap = File.ReadAllText(Path.Combine(solutionRoot, "infra", "aws", "cloudformation", "user-data", "bootstrap-payslip4all.sh"));
        var template = File.ReadAllText(Path.Combine(solutionRoot, "infra", "aws", "cloudformation", "payslip4all-web.yaml"));

        Assert.Contains("ASPNETCORE_URLS=http://127.0.0.1:8080", bootstrap, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://127.0.0.1:8080", template, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://0.0.0.0:80;https://0.0.0.0:443", bootstrap, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://0.0.0.0:80;https://0.0.0.0:443", template, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080", bootstrap, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080", template, StringComparison.Ordinal);
    }

    [Fact]
    public void AwsHostedDeployment_RendersOptionalAppConfigSecretToProtectedJsonFile()
    {
        var solutionRoot = GetSolutionRoot();
        var bootstrap = File.ReadAllText(Path.Combine(solutionRoot, "infra", "aws", "cloudformation", "user-data", "bootstrap-payslip4all.sh"));
        var template = File.ReadAllText(Path.Combine(solutionRoot, "infra", "aws", "cloudformation", "payslip4all-web.yaml"));

        Assert.Contains("APP_CONFIG_SECRET_ARN", bootstrap, StringComparison.Ordinal);
        Assert.Contains("/etc/payslip4all/app-config.secrets.json", bootstrap, StringComparison.Ordinal);
        Assert.Contains("jq -e 'if type == \"object\" then . else error(\"App config secret must be a JSON object.\") end'", bootstrap, StringComparison.Ordinal);
        Assert.Contains("chmod 600 \"$APP_CONFIG_SECRETS_FILE\"", bootstrap, StringComparison.Ordinal);
        Assert.Contains("AppConfigSecretArn", template, StringComparison.Ordinal);
        Assert.Contains("/etc/payslip4all/app-config.secrets.json", template, StringComparison.Ordinal);
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
