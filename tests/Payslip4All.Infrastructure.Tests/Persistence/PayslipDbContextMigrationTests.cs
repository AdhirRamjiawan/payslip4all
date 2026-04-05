using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Payslip4All.Infrastructure.Persistence;

namespace Payslip4All.Infrastructure.Tests.Persistence;

public class PayslipDbContextMigrationTests
{
    [Fact]
    public async Task MigrationsIncludePayFastAuditParityMigration()
    {
        var options = new DbContextOptionsBuilder<PayslipDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new PayslipDbContext(options);
        var migrations = db.Database.GetService<IMigrationsAssembly>().Migrations.Keys;

        Assert.Contains(migrations, m => m.Contains("AddPayFastCardIntegrationAuditParity", StringComparison.Ordinal));
    }
}
