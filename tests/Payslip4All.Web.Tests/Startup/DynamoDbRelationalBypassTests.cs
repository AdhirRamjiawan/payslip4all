using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Infrastructure.Persistence.DynamoDB;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Web.Tests.Startup;

[Collection(DynamoDbStartupTestCollection.Name)]
public sealed class DynamoDbRelationalBypassTests : IDisposable
{
    private readonly Dictionary<string, string?> _savedEnv = new();

    public DynamoDbRelationalBypassTests()
    {
        SetEnv("DYNAMODB_REGION", "us-east-1");
        SetEnv("DYNAMODB_ENDPOINT", "http://localhost:8000");
        SetEnv("AWS_ACCESS_KEY_ID", "dummy");
        SetEnv("AWS_SECRET_ACCESS_KEY", "dummy");
        SetEnv("DYNAMODB_TABLE_PREFIX", $"bypass_{Guid.NewGuid():N}");
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

    private static WebApplicationFactory<Program> BuildFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("PERSISTENCE_PROVIDER", "dynamodb");
            builder.ConfigureServices(services =>
            {
                var provisionerDescriptor = services.SingleOrDefault(
                    d => d.ImplementationType == typeof(DynamoDbTableProvisioner));
                if (provisionerDescriptor != null)
                    services.Remove(provisionerDescriptor);
            });
        });
    }

    [Fact]
    public void DynamoDbProvider_DoesNotRegisterPayslipDbContext()
    {
        using var factory = BuildFactory();
        using var scope = factory.Services.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<PayslipDbContext>());
        Assert.Null(scope.ServiceProvider.GetService<Microsoft.EntityFrameworkCore.DbContextOptions<PayslipDbContext>>());
    }

    [Fact]
    public void DynamoDbProvider_RegistersDynamoDbRepositoriesAndUnitOfWork()
    {
        using var factory = BuildFactory();
        using var scope = factory.Services.CreateScope();

        Assert.IsType<DynamoDbUserRepository>(scope.ServiceProvider.GetRequiredService<IUserRepository>());
        Assert.IsType<DynamoDbCompanyRepository>(scope.ServiceProvider.GetRequiredService<ICompanyRepository>());
        Assert.IsType<DynamoDbEmployeeRepository>(scope.ServiceProvider.GetRequiredService<IEmployeeRepository>());
        Assert.IsType<DynamoDbLoanRepository>(scope.ServiceProvider.GetRequiredService<ILoanRepository>());
        Assert.IsType<DynamoDbPayslipRepository>(scope.ServiceProvider.GetRequiredService<IPayslipRepository>());
        Assert.IsType<DynamoDbUnitOfWork>(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    [Fact]
    public void DynamoDbProvider_StartsWithoutRelationalStartupDependencies()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(client);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPayslipService>());
    }
}
