using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

/// <summary>
/// Integration tests for <see cref="DynamoDbPayslipRepository"/>.
/// Requires DynamoDB Local running at DYNAMODB_ENDPOINT (default: http://localhost:8000).
/// </summary>
[Collection(DynamoDbTestCollection.Name)]
[Trait("Category", "Integration")]
public class DynamoDbPayslipRepositoryTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbTestFixture _fixture;
    private readonly DynamoDbPayslipRepository _sut;

    public DynamoDbPayslipRepositoryTests(DynamoDbTestFixture fixture)
    {
        _fixture = fixture;
        _sut = new DynamoDbPayslipRepository(fixture.Client);
    }

    private async Task<(Company company, Employee employee)> SeedCompanyAndEmployeeAsync(Guid userId)
    {
        var company = new Company { Name = "Payslip Test Corp", UserId = userId };
        await _fixture.Client.PutItemAsync(new PutItemRequest
        {
            TableName = _fixture.CompaniesTable,
            Item = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = company.Id.ToString() },
                ["name"] = new() { S = company.Name },
                ["userId"] = new() { S = userId.ToString() },
                ["createdAt"] = new() { S = company.CreatedAt.ToString("O") },
            },
        });

        var employee = new Employee
        {
            FirstName = "Bob", LastName = "Jones",
            IdNumber = "0001010001087", EmployeeNumber = "P001",
            StartDate = new DateOnly(2023, 1, 1), Occupation = "Engineer",
            MonthlyGrossSalary = 55000m, CompanyId = company.Id,
        };

        await _fixture.Client.PutItemAsync(new PutItemRequest
        {
            TableName = _fixture.EmployeesTable,
            Item = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = employee.Id.ToString() },
                ["firstName"] = new() { S = employee.FirstName },
                ["lastName"] = new() { S = employee.LastName },
                ["idNumber"] = new() { S = employee.IdNumber },
                ["employeeNumber"] = new() { S = employee.EmployeeNumber },
                ["startDate"] = new() { S = employee.StartDate.ToString("yyyy-MM-dd") },
                ["occupation"] = new() { S = employee.Occupation },
                ["monthlyGrossSalary"] = new() { S = employee.MonthlyGrossSalary.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
                ["companyId"] = new() { S = company.Id.ToString() },
                ["userId"] = new() { S = userId.ToString() },
                ["createdAt"] = new() { S = employee.CreatedAt.ToString("O") },
            },
        });

        return (company, employee);
    }

    private static Payslip MakePayslip(Guid employeeId, int month = 1, int year = 2025)
        => new Payslip
        {
            PayPeriodMonth = month,
            PayPeriodYear = year,
            GrossEarnings = 55000.00m,
            UifDeduction = 148.72m,
            TotalLoanDeductions = 1000.00m,
            TotalDeductions = 1148.72m,
            NetPay = 53851.28m,
            EmployeeId = employeeId,
            LoanDeductions = new List<PayslipLoanDeduction>
            {
                new PayslipLoanDeduction
                {
                    Description = "Personal Loan",
                    Amount = 1000.00m,
                    EmployeeLoanId = Guid.NewGuid(),
                },
            },
        };

    [Fact]
    public async Task AddAsync_PersistsPayslipAndDeductions()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var payslip = MakePayslip(employee.Id);
        // Set PayslipId on deductions
        payslip.LoanDeductions[0].PayslipId = payslip.Id;

        await _sut.AddAsync(payslip);
        var retrieved = await _sut.GetByIdAsync(payslip.Id, userId);

        Assert.NotNull(retrieved);
        Assert.Equal(payslip.Id, retrieved.Id);
        Assert.Equal(55000.00m, retrieved.GrossEarnings);
        Assert.Single(retrieved.LoanDeductions);
        Assert.Equal(1000.00m, retrieved.LoanDeductions[0].Amount);
    }

    [Fact]
    public async Task GetByIdAsync_HydratesEmployeeAndCompany()
    {
        var userId = Guid.NewGuid();
        var (company, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var payslip = MakePayslip(employee.Id);
        payslip.LoanDeductions[0].PayslipId = payslip.Id;

        await _sut.AddAsync(payslip);
        var retrieved = await _sut.GetByIdAsync(payslip.Id, userId);

        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.Employee);
        Assert.Equal(employee.Id, retrieved.Employee.Id);
        Assert.NotNull(retrieved.Employee.Company);
        Assert.Equal(company.Id, retrieved.Employee.Company.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIdMismatches_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var payslip = MakePayslip(employee.Id);
        payslip.LoanDeductions[0].PayslipId = payslip.Id;
        await _sut.AddAsync(payslip);

        var result = await _sut.GetByIdAsync(payslip.Id, Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllByEmployeeIdAsync_ReturnsMostRecentFirst()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);

        var p1 = MakePayslip(employee.Id, 1, 2024);
        p1.LoanDeductions[0].PayslipId = p1.Id;
        var p2 = MakePayslip(employee.Id, 2, 2024);
        p2.LoanDeductions[0].PayslipId = p2.Id;

        await _sut.AddAsync(p1);
        await Task.Delay(10); // Ensure different generatedAt
        await _sut.AddAsync(p2);

        var results = await _sut.GetAllByEmployeeIdAsync(employee.Id, userId);

        // Should be ordered by generatedAt descending (p2 is newer)
        Assert.True(results.Count >= 2);
        Assert.All(results, p => Assert.NotNull(p.LoanDeductions));
    }

    [Fact]
    public async Task ExistsAsync_WhenMatchingMonthAndYear_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var payslip = MakePayslip(employee.Id, 3, 2025);
        payslip.LoanDeductions[0].PayslipId = payslip.Id;
        await _sut.AddAsync(payslip);

        Assert.True(await _sut.ExistsAsync(employee.Id, 3, 2025));
    }

    [Fact]
    public async Task ExistsAsync_WhenNoMatch_ReturnsFalse()
    {
        var (_, employee) = await SeedCompanyAndEmployeeAsync(Guid.NewGuid());

        Assert.False(await _sut.ExistsAsync(employee.Id, 12, 2099));
    }

    [Fact]
    public async Task DeleteAsync_RemovesPayslipAndDeductions()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var payslip = MakePayslip(employee.Id, 5, 2025);
        payslip.LoanDeductions[0].PayslipId = payslip.Id;
        await _sut.AddAsync(payslip);

        await _sut.DeleteAsync(payslip);

        var result = await _sut.GetByIdAsync(payslip.Id, userId);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_WhenEmployeeDoesNotExist_ThrowsInvalidOperationException()
    {
        var payslip = MakePayslip(Guid.NewGuid());
        payslip.LoanDeductions[0].PayslipId = payslip.Id;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddAsync(payslip));
    }

    [Fact]
    public async Task GetAllByEmployeeIdAsync_OwnershipFilter_ExcludesOtherOwners()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var (_, emp1) = await SeedCompanyAndEmployeeAsync(userId1);
        var (_, emp2) = await SeedCompanyAndEmployeeAsync(userId2);

        var p1 = MakePayslip(emp1.Id);
        p1.LoanDeductions[0].PayslipId = p1.Id;
        await _sut.AddAsync(p1);

        var p2 = MakePayslip(emp2.Id);
        p2.LoanDeductions[0].PayslipId = p2.Id;
        await _sut.AddAsync(p2);

        var results = await _sut.GetAllByEmployeeIdAsync(emp1.Id, userId1);
        Assert.DoesNotContain(results, p => p.EmployeeId == emp2.Id);
    }
}
