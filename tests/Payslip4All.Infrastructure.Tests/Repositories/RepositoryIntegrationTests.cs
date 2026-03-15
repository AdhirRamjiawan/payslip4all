using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Infrastructure.Persistence.Repositories;

namespace Payslip4All.Infrastructure.Tests.Repositories;

/// <summary>
/// T070 — EF Core integration tests using SQLite in-memory database.
/// Verifies CRUD, ownership filters, unique constraints, and cascade behaviours
/// across all five repositories as specified in data-model.md.
/// </summary>

// ──────────────────────────────────────────────────────────────────────────────
// Shared fixture: opens one SQLite in-memory connection per test class instance,
// creates schema via EnsureCreated(), and tears it all down on dispose.
// ──────────────────────────────────────────────────────────────────────────────

public abstract class RepositoryTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly PayslipDbContext Db;

    protected RepositoryTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PayslipDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new PayslipDbContext(options);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }

    // ── Seed helpers ──────────────────────────────────────────────────────────

    protected User SeedUser(string email = "owner@example.com")
    {
        var user = new User { Email = email, PasswordHash = "hash" };
        Db.Users.Add(user);
        Db.SaveChanges();
        return user;
    }

    protected Company SeedCompany(Guid userId, string name = "Test Co")
    {
        var c = new Company { Name = name, UserId = userId };
        Db.Companies.Add(c);
        Db.SaveChanges();
        return c;
    }

    protected Employee SeedEmployee(Guid companyId, string empNum = "E001")
    {
        var e = new Employee
        {
            FirstName     = "Jane",
            LastName      = "Doe",
            IdNumber      = "ID123",
            EmployeeNumber= empNum,
            Occupation    = "Dev",
            StartDate     = new DateOnly(2022, 1, 1),
            MonthlyGrossSalary = 20000m,
            CompanyId     = companyId
        };
        Db.Employees.Add(e);
        Db.SaveChanges();
        return e;
    }

    protected EmployeeLoan SeedLoan(Guid employeeId, int terms = 6,
        int startMonth = 1, int startYear = 2024)
    {
        var loan = new EmployeeLoan
        {
            Description           = "Laptop",
            TotalLoanAmount       = 6000m,
            NumberOfTerms         = terms,
            MonthlyDeductionAmount= 1000m,
            PaymentStartDate      = new DateOnly(startYear, startMonth, 1),
            EmployeeId            = employeeId
        };
        Db.EmployeeLoans.Add(loan);
        Db.SaveChanges();
        return loan;
    }

    protected Payslip SeedPayslip(Guid employeeId, Guid companyId,
        int month = 1, int year = 2024)
    {
        var p = new Payslip
        {
            EmployeeId         = employeeId,
            PayPeriodMonth     = month,
            PayPeriodYear      = year,
            GrossEarnings      = 20000m,
            UifDeduction       = 177.12m,
            TotalLoanDeductions= 0m,
            TotalDeductions    = 177.12m,
            NetPay             = 19822.88m
        };
        Db.Payslips.Add(p);
        Db.SaveChanges();
        return p;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// UserRepository tests
// ═══════════════════════════════════════════════════════════════════════════════
public class UserRepositoryTests : RepositoryTestBase
{
    private readonly UserRepository _repo;

    public UserRepositoryTests() => _repo = new UserRepository(Db);

    [Fact]
    public async Task AddAsync_StoresUser()
    {
        var user = new User { Email = "a@b.com", PasswordHash = "hash" };
        await _repo.AddAsync(user);
        Assert.Equal(1, Db.Users.Count());
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenExists()
    {
        SeedUser("test@x.com");
        var found = await _repo.GetByEmailAsync("test@x.com");
        Assert.NotNull(found);
        Assert.Equal("test@x.com", found!.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenNotFound()
    {
        var found = await _repo.GetByEmailAsync("missing@x.com");
        Assert.Null(found);
    }

    [Fact]
    public async Task GetByEmailAsync_NormalisesEmailToLowercase()
    {
        SeedUser("mixed@x.com");
        // Repository should normalise — seeder stores lowercase already
        var found = await _repo.GetByEmailAsync("MIXED@X.COM");
        Assert.NotNull(found);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenEmailInUse()
    {
        SeedUser("exists@x.com");
        Assert.True(await _repo.ExistsAsync("exists@x.com"));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenEmailNotInUse()
    {
        Assert.False(await _repo.ExistsAsync("nope@x.com"));
    }

    [Fact]
    public async Task Email_UniqueConstraint_ThrowsOnDuplicate()
    {
        await _repo.AddAsync(new User { Email = "dup@x.com", PasswordHash = "h" });
        await Assert.ThrowsAnyAsync<Exception>(
            () => _repo.AddAsync(new User { Email = "dup@x.com", PasswordHash = "h2" }));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CompanyRepository tests
// ═══════════════════════════════════════════════════════════════════════════════
public class CompanyRepositoryTests : RepositoryTestBase
{
    private readonly CompanyRepository _repo;

    public CompanyRepositoryTests() => _repo = new CompanyRepository(Db);

    [Fact]
    public async Task AddAsync_StoresCompany()
    {
        var user = SeedUser();
        var c = new Company { Name = "Acme", UserId = user.Id };
        await _repo.AddAsync(c);
        Assert.Equal(1, Db.Companies.Count());
    }

    [Fact]
    public async Task GetAllByUserIdAsync_ReturnsOnlyCallerCompanies()
    {
        var u1 = SeedUser("a@x.com");
        var u2 = SeedUser("b@x.com");
        SeedCompany(u1.Id, "CompanyA");
        SeedCompany(u2.Id, "CompanyB");

        var results = await _repo.GetAllByUserIdAsync(u1.Id);

        Assert.Single(results);
        Assert.Equal("CompanyA", results[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenWrongOwner()
    {
        var u1 = SeedUser("owner@x.com");
        var u2 = SeedUser("other@x.com");
        var c  = SeedCompany(u1.Id);

        var result = await _repo.GetByIdAsync(c.Id, u2.Id);

        Assert.Null(result);  // ownership filter — never 403, always 404
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        c.Name   = "Updated Name";
        await _repo.UpdateAsync(c);
        var reloaded = await _repo.GetByIdAsync(c.Id, user.Id);
        Assert.Equal("Updated Name", reloaded!.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCompany()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        await _repo.DeleteAsync(c);
        Assert.Equal(0, Db.Companies.Count());
    }

    [Fact]
    public async Task HasEmployeesAsync_ReturnsTrue_WhenEmployeesExist()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        SeedEmployee(c.Id);

        Assert.True(await _repo.HasEmployeesAsync(c.Id));
    }

    [Fact]
    public async Task HasEmployeesAsync_ReturnsFalse_WhenNoEmployees()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        Assert.False(await _repo.HasEmployeesAsync(c.Id));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// EmployeeRepository tests
// ═══════════════════════════════════════════════════════════════════════════════
public class EmployeeRepositoryTests : RepositoryTestBase
{
    private readonly EmployeeRepository _repo;

    public EmployeeRepositoryTests() => _repo = new EmployeeRepository(Db);

    [Fact]
    public async Task AddAsync_StoresEmployee()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = new Employee
        {
            FirstName = "John", LastName = "Smith", IdNumber = "ID001",
            EmployeeNumber = "E001", Occupation = "Dev",
            StartDate = new DateOnly(2021, 1, 1),
            MonthlyGrossSalary = 15000m, CompanyId = c.Id
        };
        await _repo.AddAsync(e);
        Assert.Equal(1, Db.Employees.Count());
    }

    [Fact]
    public async Task GetAllByCompanyIdAsync_FiltersOwnership()
    {
        var u1 = SeedUser("e1@x.com");
        var u2 = SeedUser("e2@x.com");
        var c1 = SeedCompany(u1.Id);
        var c2 = SeedCompany(u2.Id);
        SeedEmployee(c1.Id, "E001");
        SeedEmployee(c2.Id, "E002");

        var results = await _repo.GetAllByCompanyIdAsync(c1.Id, u1.Id);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetAllByCompanyIdAsync_ReturnsEmpty_WhenWrongOwner()
    {
        var u1 = SeedUser("owner@x.com");
        var u2 = SeedUser("other@x.com");
        var c1 = SeedCompany(u1.Id);
        SeedEmployee(c1.Id);

        var results = await _repo.GetAllByCompanyIdAsync(c1.Id, u2.Id);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenWrongOwner()
    {
        var u1 = SeedUser("o@x.com");
        var u2 = SeedUser("t@x.com");
        var c  = SeedCompany(u1.Id);
        var e  = SeedEmployee(c.Id);

        var result = await _repo.GetByIdAsync(e.Id, u2.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task HasPayslipsAsync_ReturnsTrue_WhenPayslipsExist()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = SeedEmployee(c.Id);
        SeedPayslip(e.Id, c.Id);

        Assert.True(await _repo.HasPayslipsAsync(e.Id));
    }

    [Fact]
    public async Task EmployeeNumber_UniqueWithinCompany_ThrowsOnDuplicate()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        SeedEmployee(c.Id, "E001");

        await Assert.ThrowsAnyAsync<Exception>(
            () => _repo.AddAsync(new Employee
            {
                FirstName = "Dup", LastName = "Dup", IdNumber = "DUPID",
                EmployeeNumber = "E001",  // same number
                Occupation = "Dev",
                StartDate = new DateOnly(2022, 1, 1),
                MonthlyGrossSalary = 10000m,
                CompanyId = c.Id
            }));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// LoanRepository tests
// ═══════════════════════════════════════════════════════════════════════════════
public class LoanRepositoryTests : RepositoryTestBase
{
    private readonly LoanRepository _repo;

    public LoanRepositoryTests() => _repo = new LoanRepository(Db);

    [Fact]
    public async Task AddAsync_StoresLoan()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = SeedEmployee(c.Id);
        var loan = new EmployeeLoan
        {
            Description = "Car", TotalLoanAmount = 12000m, NumberOfTerms = 12,
            MonthlyDeductionAmount = 1000m, PaymentStartDate = new DateOnly(2024, 1, 1),
            EmployeeId = e.Id
        };
        await _repo.AddAsync(loan);
        Assert.Equal(1, Db.EmployeeLoans.Count());
    }

    [Fact]
    public async Task GetAllByEmployeeIdAsync_FiltersOwnership()
    {
        var u1 = SeedUser("l1@x.com");
        var u2 = SeedUser("l2@x.com");
        var c1 = SeedCompany(u1.Id);
        var c2 = SeedCompany(u2.Id);
        var e1 = SeedEmployee(c1.Id);
        var e2 = SeedEmployee(c2.Id);
        SeedLoan(e1.Id);
        SeedLoan(e2.Id);

        var result = await _repo.GetAllByEmployeeIdAsync(e1.Id, u1.Id);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenWrongOwner()
    {
        var u1 = SeedUser("a@x.com");
        var u2 = SeedUser("b@x.com");
        var c  = SeedCompany(u1.Id);
        var e  = SeedEmployee(c.Id);
        var l  = SeedLoan(e.Id);

        var result = await _repo.GetByIdAsync(l.Id, u2.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesLoan()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = SeedEmployee(c.Id);
        var l    = SeedLoan(e.Id);
        await _repo.DeleteAsync(l);
        Assert.Equal(0, Db.EmployeeLoans.Count());
    }

    [Fact]
    public async Task UpdateAsync_PersistsDescriptionChange()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = SeedEmployee(c.Id);
        var l    = SeedLoan(e.Id);
        l.Description = "Updated desc";
        await _repo.UpdateAsync(l);
        var reloaded = await _repo.GetByIdAsync(l.Id, user.Id);
        Assert.Equal("Updated desc", reloaded!.Description);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PayslipRepository tests
// ═══════════════════════════════════════════════════════════════════════════════
public class PayslipRepositoryTests : RepositoryTestBase
{
    private readonly PayslipRepository _repo;

    public PayslipRepositoryTests() => _repo = new PayslipRepository(Db);

    [Fact]
    public async Task AddAsync_StoresPayslip()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = SeedEmployee(c.Id);
        var p    = BuildPayslip(e.Id, c.Id);
        await _repo.AddAsync(p);
        Assert.Equal(1, Db.Payslips.Count());
    }

    [Fact]
    public async Task GetAllByEmployeeIdAsync_OrdersByYearDescThenMonthDesc()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = SeedEmployee(c.Id);
        SeedPayslip(e.Id, c.Id, month: 3, year: 2024);
        SeedPayslip(e.Id, c.Id, month: 1, year: 2025);
        SeedPayslip(e.Id, c.Id, month: 2, year: 2024);

        var list = await _repo.GetAllByEmployeeIdAsync(e.Id, user.Id);

        Assert.Equal(2025, list[0].PayPeriodYear);
        Assert.Equal(3, list[1].PayPeriodMonth);  // 2024-03 before 2024-02
    }

    [Fact]
    public async Task GetAllByEmployeeIdAsync_FiltersOwnership()
    {
        var u1 = SeedUser("p1@x.com");
        var u2 = SeedUser("p2@x.com");
        var c1 = SeedCompany(u1.Id);
        var c2 = SeedCompany(u2.Id);
        var e1 = SeedEmployee(c1.Id);
        var e2 = SeedEmployee(c2.Id);
        SeedPayslip(e1.Id, c1.Id);
        SeedPayslip(e2.Id, c2.Id);

        var list = await _repo.GetAllByEmployeeIdAsync(e1.Id, u2.Id);  // wrong owner

        Assert.Empty(list);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenDuplicatePeriod()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = SeedEmployee(c.Id);
        SeedPayslip(e.Id, c.Id, month: 5, year: 2024);

        Assert.True(await _repo.ExistsAsync(e.Id, month: 5, year: 2024));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenNoDuplicate()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = SeedEmployee(c.Id);

        Assert.False(await _repo.ExistsAsync(e.Id, month: 5, year: 2024));
    }

    [Fact]
    public async Task UniqueConstraint_ThrowsOnDuplicatePeriod()
    {
        var user = SeedUser();
        var c    = SeedCompany(user.Id);
        var e    = SeedEmployee(c.Id);
        SeedPayslip(e.Id, c.Id, month: 1, year: 2024);

        await Assert.ThrowsAnyAsync<Exception>(
            () => _repo.AddAsync(BuildPayslip(e.Id, c.Id, month: 1, year: 2024)));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenWrongOwner()
    {
        var u1 = SeedUser("r1@x.com");
        var u2 = SeedUser("r2@x.com");
        var c  = SeedCompany(u1.Id);
        var e  = SeedEmployee(c.Id);
        var p  = SeedPayslip(e.Id, c.Id);

        var result = await _repo.GetByIdAsync(p.Id, u2.Id);

        Assert.Null(result);
    }

    private static Payslip BuildPayslip(Guid employeeId, Guid companyId,
        int month = 1, int year = 2024) => new()
    {
        EmployeeId          = employeeId,
        PayPeriodMonth      = month,
        PayPeriodYear       = year,
        GrossEarnings       = 20000m,
        UifDeduction        = 177.12m,
        TotalLoanDeductions = 0m,
        TotalDeductions     = 177.12m,
        NetPay              = 19822.88m
    };
}
