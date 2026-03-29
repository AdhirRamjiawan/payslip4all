using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

/// <summary>
/// Integration tests for <see cref="DynamoDbLoanRepository"/>.
/// Requires DynamoDB Local running at DYNAMODB_ENDPOINT (default: http://localhost:8000).
/// </summary>
[Trait("Category", "Integration")]
public class DynamoDbLoanRepositoryTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbTestFixture _fixture;
    private readonly DynamoDbLoanRepository _sut;

    public DynamoDbLoanRepositoryTests(DynamoDbTestFixture fixture)
    {
        _fixture = fixture;
        _sut = new DynamoDbLoanRepository(fixture.Client);
    }

    private async Task<(Company company, Employee employee)> SeedCompanyAndEmployeeAsync(Guid userId)
    {
        var company = new Company { Name = "Loan Test Corp", UserId = userId };
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
            FirstName = "Alice", LastName = "Smith",
            IdNumber = "0001010001087", EmployeeNumber = "E001",
            StartDate = new DateOnly(2023, 1, 1), Occupation = "Dev",
            MonthlyGrossSalary = 40000m, CompanyId = company.Id,
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

    private static EmployeeLoan MakeLoan(Guid employeeId)
        => new EmployeeLoan
        {
            Description = "Personal Loan",
            TotalLoanAmount = 12000.00m,
            NumberOfTerms = 12,
            MonthlyDeductionAmount = 1000.00m,
            PaymentStartDate = new DateOnly(2024, 1, 1),
            EmployeeId = employeeId,
        };

    [Fact]
    public async Task AddAsync_PersistsLoanWithDenormalisedUserId()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var loan = MakeLoan(employee.Id);

        await _sut.AddAsync(loan);
        var retrieved = await _sut.GetByIdAsync(loan.Id, userId);

        Assert.NotNull(retrieved);
        Assert.Equal(loan.Id, retrieved.Id);
        Assert.Equal(12000.00m, retrieved.TotalLoanAmount);
        Assert.Equal(1000.00m, retrieved.MonthlyDeductionAmount);
    }

    [Fact]
    public async Task GetAllByEmployeeIdAsync_FiltersOwnershipByUserId()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var loan = MakeLoan(employee.Id);
        await _sut.AddAsync(loan);

        var results = await _sut.GetAllByEmployeeIdAsync(employee.Id, userId);

        Assert.NotEmpty(results);
        Assert.All(results, l => Assert.Equal(employee.Id, l.EmployeeId));
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIdMismatches_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var loan = MakeLoan(employee.Id);
        await _sut.AddAsync(loan);

        var result = await _sut.GetByIdAsync(loan.Id, Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesLoan()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var loan = MakeLoan(employee.Id);
        await _sut.AddAsync(loan);

        await _sut.DeleteAsync(loan);

        var result = await _sut.GetByIdAsync(loan.Id, userId);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WithConcurrentModification_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var loan = MakeLoan(employee.Id);
        await _sut.AddAsync(loan);

        // Simulate concurrent modification: update the stored termsCompleted to a different value
        await _fixture.Client.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _fixture.EmployeeLoansTable,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = loan.Id.ToString() },
            },
            UpdateExpression = "SET termsCompleted = :tc",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":tc"] = new() { N = "99" },
            },
        });

        // Try to update with original termsCompleted=0 — should fail
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateAsync(loan));
    }

    [Fact]
    public async Task UpdateAsync_WhenNoConcurrencyConflict_Succeeds()
    {
        var userId = Guid.NewGuid();
        var (_, employee) = await SeedCompanyAndEmployeeAsync(userId);
        var loan = MakeLoan(employee.Id);
        await _sut.AddAsync(loan);

        // Increment within domain
        loan.IncrementTermsCompleted();
        await _sut.UpdateAsync(loan);

        var updated = await _sut.GetByIdAsync(loan.Id, userId);
        Assert.Equal(1, updated!.TermsCompleted);
    }
}
