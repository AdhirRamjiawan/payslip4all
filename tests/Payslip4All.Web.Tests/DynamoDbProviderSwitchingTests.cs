using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Infrastructure.HostedPayments;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Web.Tests;

/// <summary>
/// T010 — Integration tests verifying PERSISTENCE_PROVIDER configuration drives correct DI registration.
/// Uses builder.UseSetting() so tests don't pollute process-wide environment variables.
/// </summary>
[Collection(Payslip4All.Web.Tests.Startup.DynamoDbStartupTestCollection.Name)]
public class DynamoDbProviderSwitchingTests
{
    private static string GetTestEndpoint()
        => Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT")?.Trim()
           ?? "http://localhost:8000";

    private static WebApplicationFactory<Program> CreateSqliteFactory(string? provider = "sqlite")
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            if (provider != null)
                builder.UseSetting("PERSISTENCE_PROVIDER", provider);
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<PayslipDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"p4a_sw_{Guid.NewGuid():N}.db");
                services.AddDbContext<PayslipDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));
            });
        });
    }

    [Fact]
    public void UnsetProvider_DefaultsToSqlite_DbContextRegistered()
    {
        var savedProvider = Environment.GetEnvironmentVariable("PERSISTENCE_PROVIDER");
        Environment.SetEnvironmentVariable("PERSISTENCE_PROVIDER", null);

        try
        {
            using var factory = CreateSqliteFactory(provider: null);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<PayslipDbContext>();
            Assert.NotNull(db);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PERSISTENCE_PROVIDER", savedProvider);
        }
    }

    [Fact]
    public void SqliteProvider_RegistersEfCoreRepositories()
    {
        using var factory = CreateSqliteFactory("sqlite");
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetService<IUserRepository>();
        Assert.NotNull(repo);
        Assert.IsNotType<DynamoDbUserRepository>(repo);
        Assert.IsNotType<DynamoDbWalletRepository>(scope.ServiceProvider.GetRequiredService<IWalletRepository>());
        Assert.IsNotType<DynamoDbWalletTopUpAttemptRepository>(scope.ServiceProvider.GetRequiredService<IWalletTopUpAttemptRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHostedPaymentProvider>());
    }

    [Fact]
    public void DynamoDbProvider_WithValidRegion_RegistersDynamoDbServices()
    {
        var endpoint = GetTestEndpoint();
        // Set env vars needed by DynamoDbClientFactory (called at DI registration time)
        var savedKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var savedSecret = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        var savedEndpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT");
        var savedRegion = Environment.GetEnvironmentVariable("DYNAMODB_REGION");
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "dummy");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "dummy");
        Environment.SetEnvironmentVariable("DYNAMODB_REGION", "us-east-1");
        Environment.SetEnvironmentVariable("DYNAMODB_ENDPOINT", endpoint);

        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("PERSISTENCE_PROVIDER", "dynamodb");
                builder.ConfigureServices(services =>
                {
                    // Remove DynamoDbTableProvisioner to avoid connecting to real DynamoDB in unit tests
                    var descriptor = services.SingleOrDefault(
                        d => d.ImplementationType?.Name == "DynamoDbTableProvisioner");
                    if (descriptor != null)
                        services.Remove(descriptor);
                });
            });

            using var scope = factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetService<IUserRepository>();
            Assert.NotNull(repo);
            Assert.IsType<DynamoDbUserRepository>(repo);
            Assert.IsType<DynamoDbWalletRepository>(scope.ServiceProvider.GetRequiredService<IWalletRepository>());
            Assert.IsType<DynamoDbPayslipPricingRepository>(scope.ServiceProvider.GetRequiredService<IPayslipPricingRepository>());
            Assert.IsType<DynamoDbWalletTopUpAttemptRepository>(scope.ServiceProvider.GetRequiredService<IWalletTopUpAttemptRepository>());
            Assert.IsType<DynamoDbPaymentReturnEvidenceRepository>(scope.ServiceProvider.GetRequiredService<IPaymentReturnEvidenceRepository>());
            Assert.IsType<DynamoDbOutcomeNormalizationDecisionRepository>(scope.ServiceProvider.GetRequiredService<IOutcomeNormalizationDecisionRepository>());
            Assert.IsType<DynamoDbUnmatchedPaymentReturnRecordRepository>(scope.ServiceProvider.GetRequiredService<IUnmatchedPaymentReturnRecordRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITimeProvider>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHostedPaymentProvider>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<HostedPaymentProviderFactory>());

            var client = scope.ServiceProvider.GetService<IAmazonDynamoDB>();
            Assert.NotNull(client);

            var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
            Assert.NotNull(uow);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", savedKey);
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", savedSecret);
            Environment.SetEnvironmentVariable("DYNAMODB_REGION", savedRegion);
            Environment.SetEnvironmentVariable("DYNAMODB_ENDPOINT", savedEndpoint);
        }
    }

    [Fact]
    public void UnknownProvider_ThrowsInvalidOperationException_WithValidValues()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            {
                b.UseEnvironment("Development");
                b.UseSetting("PERSISTENCE_PROVIDER", "oracle");
            });
            _ = factory.Services;
        });

        Assert.Contains("sqlite", ex.Message);
        Assert.Contains("mysql", ex.Message);
        Assert.Contains("dynamodb", ex.Message);
    }

    [Fact]
    public void DynamoDbProvider_WithoutRegion_ThrowsInvalidOperationException()
    {
        // Ensure DYNAMODB_REGION is not set
        var savedRegion = Environment.GetEnvironmentVariable("DYNAMODB_REGION");
        Environment.SetEnvironmentVariable("DYNAMODB_REGION", null);

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
                {
                    b.UseEnvironment("Development");
                    b.UseSetting("PERSISTENCE_PROVIDER", "dynamodb");
                });
                _ = factory.Services;
            });

            Assert.Contains("DYNAMODB_REGION", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DYNAMODB_REGION", savedRegion);
        }
    }
}
