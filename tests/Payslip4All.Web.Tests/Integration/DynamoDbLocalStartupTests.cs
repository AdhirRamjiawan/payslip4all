using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Web.Tests.Startup;

namespace Payslip4All.Web.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(DynamoDbStartupTestCollection.Name)]
public sealed class DynamoDbLocalStartupTests : IDisposable
{
    private readonly Dictionary<string, string?> _savedEnv = new();

    private static string GetTestEndpoint()
        => Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT")?.Trim()
           ?? "http://localhost:8000";

    private static readonly string[] TableSuffixes =
    {
        "users",
        "companies",
        "employees",
        "employee_loans",
        "payslips",
        "payslip_loan_deductions",
        "wallets",
        "wallet_activities",
        "wallet_topup_attempts",
        "payment_return_evidences",
        "outcome_normalization_decisions",
        "unmatched_payment_return_records"
    };

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
        });
    }

    [Fact]
    public async Task CreateClient_WithLocalEndpoint_ProvisionsAllPrefixedTables()
    {
        var prefix = $"webtest_{Guid.NewGuid():N}";
        var endpoint = GetTestEndpoint();
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", endpoint);
        SetEnv("DYNAMODB_TABLE_PREFIX", prefix);
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);

        await using var factory = BuildFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var dynamoDb = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();

        try
        {
            var client = Assert.IsType<AmazonDynamoDBClient>(dynamoDb);
            Assert.Equal($"{endpoint.TrimEnd('/')}/", client.Config.ServiceURL);

            foreach (var tableName in GetExpectedTableNames(prefix))
            {
                var response = await dynamoDb.DescribeTableAsync(tableName);
                Assert.Equal(TableStatus.ACTIVE, response.Table.TableStatus);
            }
        }
        finally
        {
            await DeleteProvisionedTablesAsync(dynamoDb, prefix);
        }
    }

    [Fact]
    public async Task CreateClient_WithLocalEndpoint_AllowsRepositoryCreateReadCycle()
    {
        var prefix = $"webtest_{Guid.NewGuid():N}";
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var endpoint = GetTestEndpoint();

        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", endpoint);
        SetEnv("DYNAMODB_TABLE_PREFIX", prefix);
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);

        await using var factory = BuildFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var dynamoDb = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();

        try
        {
            var user = new User
            {
                Email = email,
                PasswordHash = "hash",
            };

            await userRepository.AddAsync(user);

            var savedUser = await userRepository.GetByEmailAsync(email);

            Assert.NotNull(savedUser);
            Assert.Equal(user.Id, savedUser!.Id);
            Assert.True(await userRepository.ExistsAsync(email));
        }
        finally
        {
            await DeleteProvisionedTablesAsync(dynamoDb, prefix);
        }
    }

    [Fact]
    public async Task CreateClient_WithLocalEndpoint_ExposesPayFastNotifyRoute()
    {
        var prefix = $"webtest_{Guid.NewGuid():N}";
        var endpoint = GetTestEndpoint();
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", endpoint);
        SetEnv("DYNAMODB_TABLE_PREFIX", prefix);
        SetEnv("AWS_ACCESS_KEY_ID", null);
        SetEnv("AWS_SECRET_ACCESS_KEY", null);

        await using var factory = BuildFactory();
        using var client = factory.CreateClient();

        try
        {
            using var response = await client.PostAsync("/api/payments/payfast/notify", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["m_payment_id"] = Guid.NewGuid().ToString("N"),
                ["signature"] = "invalid"
            }));

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var dynamoDb = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
            await DeleteProvisionedTablesAsync(dynamoDb, prefix);
        }
    }

    private static IEnumerable<string> GetExpectedTableNames(string prefix)
        => TableSuffixes.Select(suffix => $"{prefix}_{suffix}");

    private static async Task DeleteProvisionedTablesAsync(IAmazonDynamoDB dynamoDb, string prefix)
    {
        foreach (var tableName in GetExpectedTableNames(prefix))
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
}
