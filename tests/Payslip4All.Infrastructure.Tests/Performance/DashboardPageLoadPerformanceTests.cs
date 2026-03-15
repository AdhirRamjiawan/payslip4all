using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Infrastructure.Persistence.Repositories;
using System.Diagnostics;

namespace Payslip4All.Infrastructure.Tests.Performance;

/// <summary>
/// T066 — Dashboard and company-detail service-layer performance test (SC-004).
///
/// Seeds a SQLite in-memory database with 10 companies × 50 employees (500 total)
/// all owned by a single user and then measures:
///
///   • Dashboard query  — <see cref="CompanyService.GetCompaniesForUserAsync"/>
///     (equivalent to the data fetch that backs the "/" dashboard page).
///   • Company-detail query — <see cref="CompanyService.GetCompanyByIdAsync"/>
///     (equivalent to the data fetch that backs the "/companies/{id}" page).
///
/// Both are asserted to complete in under 2 000 ms (median across 5 runs)
/// per the SC-004 SLO defined in the feature specification.
///
/// The service layer is the primary latency contributor for Blazor Server pages;
/// testing the service directly with a realistic dataset provides a meaningful
/// and deterministic signal without the overhead of HTTP infrastructure.
/// </summary>
public class DashboardPageLoadPerformanceTests : IDisposable
{
    // ── Test configuration ────────────────────────────────────────────────────
    private const int Companies = 10;
    private const int EmployeesPerCompany = 50;
    private const int Runs = 5;
    private const int MedianThresholdMs = 2_000;

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly SqliteConnection _connection;
    private readonly PayslipDbContext _db;
    private readonly CompanyService _companyService;
    private readonly Guid _userId;
    private readonly Guid _firstCompanyId;

    // ── Setup ─────────────────────────────────────────────────────────────────
    public DashboardPageLoadPerformanceTests()
    {
        // Open a single SQLite in-memory connection for the lifetime of this
        // test class instance (shared connection keeps the schema alive).
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PayslipDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PayslipDbContext(options);
        _db.Database.EnsureCreated();

        // Seed user
        var user = new User { Email = "perf-test@example.com", PasswordHash = "hash" };
        _db.Users.Add(user);
        _db.SaveChanges();
        _userId = user.Id;

        // Seed 10 companies × 50 employees
        var firstCompanyId = Guid.Empty;
        for (int c = 0; c < Companies; c++)
        {
            var company = new Company
            {
                Name = $"Company {c + 1:D2}",
                Address = $"{c + 1} Performance Ave, Johannesburg",
                UserId = _userId
            };

            for (int e = 0; e < EmployeesPerCompany; e++)
            {
                company.Employees.Add(new Employee
                {
                    FirstName = $"Employee{e + 1:D3}",
                    LastName = $"LastName{c + 1:D2}",
                    IdNumber = $"{c:D4}{e:D6}00000",
                    EmployeeNumber = $"E{c + 1:D2}{e + 1:D3}",
                    StartDate = new DateOnly(2020, 1, 1),
                    Occupation = "General Worker",
                    MonthlyGrossSalary = 15_000m + (e * 100m),
                    CompanyId = company.Id
                });
            }

            _db.Companies.Add(company);

            if (c == 0)
                firstCompanyId = company.Id;
        }

        _db.SaveChanges();
        _firstCompanyId = firstCompanyId;

        // Wire up real repository + service (no mocks — we want real query paths)
        var companyRepo = new CompanyRepository(_db);
        _companyService = new CompanyService(companyRepo);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DashboardQuery_GetCompaniesForUser_MedianUnder2000ms()
    {
        var elapsed = new long[Runs];

        // Warm-up
        _ = await _companyService.GetCompaniesForUserAsync(_userId);

        for (int i = 0; i < Runs; i++)
        {
            var sw = Stopwatch.StartNew();
            var companies = await _companyService.GetCompaniesForUserAsync(_userId);
            sw.Stop();
            elapsed[i] = sw.ElapsedMilliseconds;

            // Sanity: all 10 companies should be returned
            Assert.Equal(Companies, companies.Count);
        }

        Array.Sort(elapsed);
        long median = elapsed[Runs / 2];

        Assert.True(
            median < MedianThresholdMs,
            $"Dashboard query median elapsed time was {median} ms, exceeding the {MedianThresholdMs} ms SLO (SC-004). " +
            $"All run times: [{string.Join(", ", elapsed.Select(e => $"{e} ms"))}]. " +
            $"Seeded {Companies} companies × {EmployeesPerCompany} employees = {Companies * EmployeesPerCompany} total employees.");
    }

    [Fact]
    public async Task CompanyDetailQuery_GetCompanyById_MedianUnder2000ms()
    {
        var elapsed = new long[Runs];

        // Warm-up
        _ = await _companyService.GetCompanyByIdAsync(_firstCompanyId, _userId);

        for (int i = 0; i < Runs; i++)
        {
            var sw = Stopwatch.StartNew();
            var company = await _companyService.GetCompanyByIdAsync(_firstCompanyId, _userId);
            sw.Stop();
            elapsed[i] = sw.ElapsedMilliseconds;

            // Sanity: company should exist
            Assert.NotNull(company);
            Assert.Equal(_firstCompanyId, company.Id);
        }

        Array.Sort(elapsed);
        long median = elapsed[Runs / 2];

        Assert.True(
            median < MedianThresholdMs,
            $"Company-detail query median elapsed time was {median} ms, exceeding the {MedianThresholdMs} ms SLO (SC-004). " +
            $"All run times: [{string.Join(", ", elapsed.Select(e => $"{e} ms"))}]. " +
            $"Seeded {Companies} companies × {EmployeesPerCompany} employees = {Companies * EmployeesPerCompany} total employees.");
    }

    // ── Teardown ──────────────────────────────────────────────────────────────
    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
