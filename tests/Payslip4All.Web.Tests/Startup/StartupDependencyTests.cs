using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Infrastructure.Persistence;
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
        builder.UseSetting("DatabaseProvider", "sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
