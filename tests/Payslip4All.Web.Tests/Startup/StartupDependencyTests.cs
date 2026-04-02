using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Infrastructure.HostedPayments;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Infrastructure.Persistence.DynamoDB;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;
using System.IO;

namespace Payslip4All.Web.Tests.Startup;

/// <summary>
/// T073 — WebApplicationFactory DI startup integration test.
/// Verifies that the Payslip4All Web application starts without DI errors,
/// and that all critical services (repositories, application services, DB context)
/// can be resolved from the DI container.
///
/// Uses an in-memory SQLite database to avoid touching any real database.
/// </summary>
public class StartupDependencyTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public StartupDependencyTests(TestWebApplicationFactory factory)
        => _factory = factory;

    [Fact]
    public void IPayslipService_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<IPayslipService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void ICompanyService_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<ICompanyService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void IEmployeeService_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<IEmployeeService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void ILoanService_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<ILoanService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void IAuthenticationService_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<IAuthenticationService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void IPasswordHasher_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<IPasswordHasher>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void ICompanyRepository_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetService<ICompanyRepository>();
        Assert.NotNull(repo);
    }

    [Fact]
    public void IEmployeeRepository_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetService<IEmployeeRepository>();
        Assert.NotNull(repo);
    }

    [Fact]
    public void IPayslipRepository_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetService<IPayslipRepository>();
        Assert.NotNull(repo);
    }

    [Fact]
    public void PayslipDbContext_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetService<PayslipDbContext>();
        Assert.NotNull(db);
    }

    [Fact]
    public void IUnitOfWork_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
        Assert.NotNull(uow);
    }

    [Fact]
    public void IWalletService_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<IWalletService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void IPayslipPricingService_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<IPayslipPricingService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void IWalletTopUpService_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<IWalletTopUpService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void IWalletRepository_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetService<IWalletRepository>();
        Assert.NotNull(repo);
    }

    [Fact]
    public void IPayslipPricingRepository_IsRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetService<IPayslipPricingRepository>();
        Assert.NotNull(repo);
    }

    [Fact]
    public void HostedPaymentServices_AreRegistered_AndResolvable()
    {
        using var scope = _factory.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IHostedPaymentProvider>());
        Assert.NotNull(scope.ServiceProvider.GetService<HostedPaymentProviderFactory>());
        Assert.NotNull(scope.ServiceProvider.GetService<IWalletTopUpAttemptRepository>());
    }

    [Fact]
    public void LegacyDatabaseProviderSetting_DoesNotOverrideSqliteDefault()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"p4a_legacy_{Guid.NewGuid():N}.db");

        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("DatabaseProvider", "mysql");
                builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<PayslipDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<PayslipDbContext>(options =>
                        options.UseSqlite($"Data Source={dbPath}"));
                });
            });

            using var scope = factory.Services.CreateScope();
            Assert.NotNull(scope.ServiceProvider.GetService<PayslipDbContext>());
            Assert.NotNull(scope.ServiceProvider.GetService<IUserRepository>());
            Assert.IsNotType<Payslip4All.Infrastructure.Persistence.DynamoDB.DynamoDbUnitOfWork>(
                scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public void MySqlProvider_WithWhitespace_RemainsOnRelationalRegistrations()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"p4a_mysql_{Guid.NewGuid():N}.db");

        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("PERSISTENCE_PROVIDER", " MySQL ");
                builder.UseSetting(
                    "ConnectionStrings:MySqlConnection",
                    "Server=localhost;Database=payslip4all;User=root;Password=placeholder;");

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<PayslipDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<PayslipDbContext>(options =>
                        options.UseSqlite($"Data Source={dbPath}"));
                });
            });

            using var scope = factory.Services.CreateScope();

            Assert.NotNull(scope.ServiceProvider.GetService<PayslipDbContext>());
            Assert.IsNotType<DynamoDbUserRepository>(
                scope.ServiceProvider.GetRequiredService<IUserRepository>());
            Assert.IsNotType<DynamoDbUnitOfWork>(
                scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}

/// <summary>
/// Custom WebApplicationFactory that swaps the real DB for a temp SQLite file
/// so no external database is required during CI.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"p4a_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(
        Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove any existing DbContext options registration.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PayslipDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Replace with a temp SQLite file so migrations run cleanly in CI.
            services.AddDbContext<PayslipDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));
        });

        builder.UseSetting("PERSISTENCE_PROVIDER", "sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
