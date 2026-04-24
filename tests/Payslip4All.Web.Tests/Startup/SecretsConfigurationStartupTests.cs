using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Payslip4All.Infrastructure.HostedPayments;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Infrastructure.Persistence.DynamoDB;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;
using System.Text.Json;

namespace Payslip4All.Web.Tests.Startup;

[Collection(DynamoDbStartupTestCollection.Name)]
public sealed class SecretsConfigurationStartupTests : IDisposable
{
    private readonly Dictionary<string, string?> _savedEnv = new();

    // ====================================================================
    // Feature 015 Tests: US1 - Keep compliant secret-backed settings
    // ====================================================================

    [Fact]
    public void EligibleSettings_ResolveSuccessfullyFromAwsSecrets()
    {
        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["PERSISTENCE_PROVIDER"] = "sqlite",
            ["Auth:Cookie:ExpireDays"] = "45",
            ["HostedPayments:PayFast:MerchantId"] = "eligible-merchant",
            ["HostedPayments:PayFast:MerchantKey"] = "eligible-key",
            ["HostedPayments:PayFast:PublicNotifyUrl"] = "https://eligible.example.com/notify",
        });

        SetEnv("PERSISTENCE_PROVIDER", null);

        using var factory = new SqliteTestFactory();
        using var scope = factory.Services.CreateScope();

        var cookieOptions = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var payfastOptions = scope.ServiceProvider.GetRequiredService<PayFastHostedPaymentOptions>();

        Assert.Equal(TimeSpan.FromDays(45), cookieOptions.ExpireTimeSpan);
        Assert.Equal("eligible-merchant", payfastOptions.MerchantId);
        Assert.Equal("eligible-key", payfastOptions.MerchantKey);
    }

    [Fact]
    public void EligibleSettings_PreservePrecedence_EnvironmentOverridesSecrets()
    {
        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["Auth:Cookie:ExpireDays"] = "60",
        });

        SetEnv("Auth__Cookie__ExpireDays", "14");

        using var factory = new SqliteTestFactory();
        using var scope = factory.Services.CreateScope();

        var options = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(TimeSpan.FromDays(14), options.ExpireTimeSpan);
    }

    [Fact]
    public void EligibleSettings_PreservePrecedence_SecretsOverrideAppSettings()
    {
        // appsettings.json has Auth:Cookie:ExpireDays: 30
        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["Auth:Cookie:ExpireDays"] = "90",
        });

        SetEnv("Auth__Cookie__ExpireDays", null);

        using var factory = new SqliteTestFactory();
        using var scope = factory.Services.CreateScope();

        var options = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(TimeSpan.FromDays(90), options.ExpireTimeSpan);
    }

    // ====================================================================
    // Feature 015 Tests: US3 - Fail safely on scope violations
    // ====================================================================

    [Fact]
    public void ExcludedDynamoDbKeys_BlockStartupWithSafeDiagnostics()
    {
        const string secretValue = "af-south-1";

        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["PERSISTENCE_PROVIDER"] = "dynamodb",
            ["DYNAMODB_REGION"] = secretValue,
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                        services.Remove(descriptor);
                });
            });

            _ = factory.Services;
        });

        Assert.Contains("excluded keys", exception.Message);
        Assert.Contains("DYNAMODB_REGION", exception.Message);
        Assert.DoesNotContain(secretValue, exception.Message);
    }

    [Fact]
    public void ExcludedAwsCredentialKeys_BlockStartupWithSafeDiagnostics()
    {
        const string secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["AWS_ACCESS_KEY_ID"] = "AKIAIOSFODNN7EXAMPLE",
            ["AWS_SECRET_ACCESS_KEY"] = secretKey,
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = new SqliteTestFactory();
            _ = factory.Services;
        });

        Assert.Contains("excluded keys", exception.Message);
        Assert.Contains("AWS_ACCESS_KEY_ID", exception.Message);
        Assert.Contains("AWS_SECRET_ACCESS_KEY", exception.Message);
        Assert.DoesNotContain(secretKey, exception.Message);
    }

    [Fact]
    public void MixedEligibleAndExcludedKeys_BlockStartup()
    {
        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["Auth:Cookie:ExpireDays"] = "30",
            ["DYNAMODB_ENDPOINT"] = "http://localhost:8000",
            ["HostedPayments:PayFast:MerchantId"] = "10047421",
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = new SqliteTestFactory();
            _ = factory.Services;
        });

        Assert.Contains("DYNAMODB_ENDPOINT", exception.Message);
        Assert.Contains("excluded keys", exception.Message);
    }

    // ====================================================================
    // Legacy Feature 014 Tests (preserved for regression)
    // ====================================================================

    [Fact]
    public void SecretsArtifact_WithDynamoDbSettingsOnly_RegistersDynamoDbServices()
    {
        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["PERSISTENCE_PROVIDER"] = "dynamodb",
        });

        SetEnv("PERSISTENCE_PROVIDER", null);
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", "http://localhost:8000");
        SetEnv("DYNAMODB_TABLE_PREFIX", "secret-prefix");
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(descriptor);
            });
        });

        using var scope = factory.Services.CreateScope();
        var client = Assert.IsType<AmazonDynamoDBClient>(scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>());

        Assert.Equal("http://localhost:8000/", client.Config.ServiceURL);
        Assert.IsType<DynamoDbUserRepository>(scope.ServiceProvider.GetRequiredService<Payslip4All.Application.Interfaces.Repositories.IUserRepository>());
    }

    [Fact]
    public void SecretsArtifact_WhenRequiredDynamoDbValueIsMissing_FailsFastWithoutLeakingSecretValue()
    {
        const string secretValue = "super-secret-access-key";

        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["PERSISTENCE_PROVIDER"] = "dynamodb",
        });

        SetEnv("PERSISTENCE_PROVIDER", null);
        SetEnv("DYNAMODB_REGION", null);
        SetEnv("AWS_ACCESS_KEY_ID", secretValue);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                        services.Remove(descriptor);
                });
            });

            _ = factory.Services;
        });

        Assert.Contains("DYNAMODB_REGION", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secretValue, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvironmentVariables_OverrideSecretsForAuthCookieExpireDays()
    {
        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["Auth:Cookie:ExpireDays"] = "21",
        });

        SetEnv("Auth__Cookie__ExpireDays", "7");

        using var factory = new SqliteTestFactory();
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(TimeSpan.FromDays(7), options.ExpireTimeSpan);
    }

    [Fact]
    public void SecretsArtifact_OverridesTrackedAppSettingsForPayFastOptions()
    {
        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["HostedPayments:PayFast:MerchantId"] = "10047421",
            ["HostedPayments:PayFast:MerchantKey"] = "merchant-key",
            ["HostedPayments:PayFast:Passphrase"] = "secret-passphrase",
            ["HostedPayments:PayFast:PublicNotifyUrl"] = "https://payments.example.com/api/payments/payfast/notify",
            ["HostedPayments:PayFast:UseSandbox"] = "true",
        });

        using var factory = new SqliteTestFactory();
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<PayFastHostedPaymentOptions>();

        Assert.Equal("10047421", options.MerchantId);
        Assert.Equal("merchant-key", options.MerchantKey);
        Assert.Equal("secret-passphrase", options.Passphrase);
        Assert.True(options.UseSandbox);
        Assert.Equal("payfast", options.ProviderKey);
        Assert.Equal("https://payments.example.com/api/payments/payfast/notify", options.PublicNotifyUrl);
    }

    [Fact]
    public void EnvironmentVariables_OverrideSecretsForPayFastOptions()
    {
        using var secrets = new SecretsArtifactScope(_savedEnv, new Dictionary<string, string?>
        {
            ["HostedPayments:PayFast:MerchantId"] = "secret-merchant",
            ["HostedPayments:PayFast:MerchantKey"] = "secret-key",
            ["HostedPayments:PayFast:PublicNotifyUrl"] = "https://secret.example.com/api/payments/payfast/notify",
        });

        SetEnv("HostedPayments__PayFast__MerchantId", "env-merchant");

        using var factory = new SqliteTestFactory();
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<PayFastHostedPaymentOptions>();

        Assert.Equal("env-merchant", options.MerchantId);
        Assert.Equal("secret-key", options.MerchantKey);
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

    private sealed class SqliteTestFactory : WebApplicationFactory<Program>, IDisposable
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"p4a_secrets_{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("PERSISTENCE_PROVIDER", "sqlite");
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PayslipDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<PayslipDbContext>(options =>
                    options.UseSqlite($"Data Source={_dbPath}"));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
    }

    private sealed class SecretsArtifactScope : IDisposable
    {
        private readonly Dictionary<string, string?> _savedEnv;
        private readonly string _path;

        public SecretsArtifactScope(Dictionary<string, string?> savedEnv, IReadOnlyDictionary<string, string?> values)
        {
            _savedEnv = savedEnv;
            _path = Path.Combine(Path.GetTempPath(), $"p4a-app-secrets-{Guid.NewGuid():N}.json");
            File.WriteAllText(_path, JsonSerializer.Serialize(values));

            _savedEnv.TryAdd("PAYSLIP4ALL_AWS_SECRETS_CONFIG_PATH", Environment.GetEnvironmentVariable("PAYSLIP4ALL_AWS_SECRETS_CONFIG_PATH"));
            Environment.SetEnvironmentVariable("PAYSLIP4ALL_AWS_SECRETS_CONFIG_PATH", _path);
        }

        public void Dispose()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
    }
}
