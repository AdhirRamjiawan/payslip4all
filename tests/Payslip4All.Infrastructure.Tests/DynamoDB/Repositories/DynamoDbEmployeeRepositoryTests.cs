using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

/// <summary>
/// Integration tests for <see cref="DynamoDbEmployeeRepository"/>.
/// Requires DynamoDB Local running at DYNAMODB_ENDPOINT (default: http://localhost:8000).
/// </summary>
[Collection(DynamoDbTestCollection.Name)]
[Trait("Category", "Integration")]
public class DynamoDbEmployeeRepositoryTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbTestFixture _fixture;
    private readonly DynamoDbEmployeeRepository _sut;

    public DynamoDbEmployeeRepositoryTests(DynamoDbTestFixture fixture)
    {
        _fixture = fixture;
        _sut = new DynamoDbEmployeeRepository(fixture.Client);
    }

    /// <summary>Seeds a company item directly in DynamoDB for tests.</summary>
    private async Task<Company> SeedCompanyAsync(Guid userId)
    {
        var company = new Company { Name = "Test Corp", UserId = userId };
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
        return company;
    }

    private static Employee MakeEmployee(Guid companyId, string number = "E001")
        => new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            IdNumber = "0001010001087",
            EmployeeNumber = number,
            StartDate = new DateOnly(2023, 1, 1),
            Occupation = "Developer",
            MonthlyGrossSalary = 50000.00m,
            CompanyId = companyId,
        };

    [Fact]
    public async Task AddAsync_PersistsEmployeeWithDenormalisedUserId()
    {
        var userId = Guid.NewGuid();
        var company = await SeedCompanyAsync(userId);
        var employee = MakeEmployee(company.Id);

        await _sut.AddAsync(employee);
        var retrieved = await _sut.GetByIdAsync(employee.Id, userId);

        Assert.NotNull(retrieved);
        Assert.Equal(employee.Id, retrieved.Id);
        Assert.Equal(employee.FirstName, retrieved.FirstName);
        Assert.Equal(50000.00m, retrieved.MonthlyGrossSalary);
    }

    [Fact]
    public async Task GetAllByCompanyIdAsync_FiltersOwnershipByUserId()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var company1 = await SeedCompanyAsync(userId1);
        var company2 = await SeedCompanyAsync(userId2);

        var emp1 = MakeEmployee(company1.Id, "E-U1-001");
        var emp2 = MakeEmployee(company1.Id, "E-U1-002");
        var emp3 = MakeEmployee(company2.Id, "E-U2-001");

        // Temporarily switch to company2 to write emp3 with right userId
        // We'll directly insert emp3 with userId2
        await _sut.AddAsync(emp1);
        await _sut.AddAsync(emp2);
        await _sut.AddAsync(emp3); // This will have userId2 because company2 has userId2

        var results = await _sut.GetAllByCompanyIdAsync(company1.Id, userId1);

        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, e => e.Id == emp3.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIdMismatches_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var company = await SeedCompanyAsync(userId);
        var employee = MakeEmployee(company.Id);
        await _sut.AddAsync(employee);

        var result = await _sut.GetByIdAsync(employee.Id, Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdWithLoansAsync_HydratesLoansAndCompany()
    {
        var userId = Guid.NewGuid();
        var company = await SeedCompanyAsync(userId);
        var employee = MakeEmployee(company.Id);
        await _sut.AddAsync(employee);

        var result = await _sut.GetByIdWithLoansAsync(employee.Id, userId);

        Assert.NotNull(result);
        Assert.NotNull(result.Company);
        Assert.Equal(company.Id, result.Company.Id);
        Assert.NotNull(result.Loans);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var userId = Guid.NewGuid();
        var company = await SeedCompanyAsync(userId);
        var employee = MakeEmployee(company.Id);
        await _sut.AddAsync(employee);

        employee.FirstName = "Jane";
        employee.MonthlyGrossSalary = 60000.00m;
        await _sut.UpdateAsync(employee);

        var updated = await _sut.GetByIdAsync(employee.Id, userId);
        Assert.Equal("Jane", updated!.FirstName);
        Assert.Equal(60000.00m, updated.MonthlyGrossSalary);
    }

    [Fact]
    public async Task AddAsync_WhenCompanyDoesNotExist_ThrowsInvalidOperationException()
    {
        var employee = MakeEmployee(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddAsync(employee));
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeDoesNotExist_ThrowsInvalidOperationException()
    {
        var employee = MakeEmployee(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateAsync(employee));
    }

    [Fact]
    public async Task DeleteAsync_RemovesEmployee()
    {
        var userId = Guid.NewGuid();
        var company = await SeedCompanyAsync(userId);
        var employee = MakeEmployee(company.Id);
        await _sut.AddAsync(employee);

        await _sut.DeleteAsync(employee);

        var result = await _sut.GetByIdAsync(employee.Id, userId);
        Assert.Null(result);
    }

    [Fact]
    public async Task HasPayslipsAsync_WhenNoPayslips_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var company = await SeedCompanyAsync(userId);
        var employee = MakeEmployee(company.Id);
        await _sut.AddAsync(employee);

        Assert.False(await _sut.HasPayslipsAsync(employee.Id));
    }
}
