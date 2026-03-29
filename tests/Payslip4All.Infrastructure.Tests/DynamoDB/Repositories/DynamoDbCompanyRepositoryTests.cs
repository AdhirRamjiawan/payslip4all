using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

/// <summary>
/// Integration tests for <see cref="DynamoDbCompanyRepository"/>.
/// Requires DynamoDB Local running at DYNAMODB_ENDPOINT (default: http://localhost:8000).
/// </summary>
[Trait("Category", "Integration")]
public class DynamoDbCompanyRepositoryTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbCompanyRepository _sut;

    public DynamoDbCompanyRepositoryTests(DynamoDbTestFixture fixture)
    {
        _sut = new DynamoDbCompanyRepository(fixture.Client);
    }

    private static Company MakeCompany(Guid userId, string name = "Test Co")
        => new Company { Name = name, UserId = userId };

    [Fact]
    public async Task AddAsync_PersistsCompany()
    {
        var userId = Guid.NewGuid();
        var company = MakeCompany(userId, "Acme Inc");

        await _sut.AddAsync(company);
        var retrieved = await _sut.GetByIdAsync(company.Id, userId);

        Assert.NotNull(retrieved);
        Assert.Equal(company.Id, retrieved.Id);
        Assert.Equal("Acme Inc", retrieved.Name);
        Assert.Equal(userId, retrieved.UserId);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_ReturnsOnlyCompaniesForThatUser()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        await _sut.AddAsync(MakeCompany(userId1, "U1-Company-A"));
        await _sut.AddAsync(MakeCompany(userId1, "U1-Company-B"));
        await _sut.AddAsync(MakeCompany(userId2, "U2-Company"));

        var results = await _sut.GetAllByUserIdAsync(userId1);

        Assert.All(results, c => Assert.Equal(userId1, c.UserId));
        Assert.True(results.Count >= 2);
        Assert.DoesNotContain(results, c => c.Name == "U2-Company");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIdDoesNotMatch_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var company = MakeCompany(userId);
        await _sut.AddAsync(company);

        var result = await _sut.GetByIdAsync(company.Id, Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var userId = Guid.NewGuid();
        var company = MakeCompany(userId, "Before");
        await _sut.AddAsync(company);

        company.Name = "After";
        await _sut.UpdateAsync(company);

        var updated = await _sut.GetByIdAsync(company.Id, userId);
        Assert.Equal("After", updated!.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCompany()
    {
        var userId = Guid.NewGuid();
        var company = MakeCompany(userId);
        await _sut.AddAsync(company);

        await _sut.DeleteAsync(company);

        var result = await _sut.GetByIdAsync(company.Id, userId);
        Assert.Null(result);
    }

    [Fact]
    public async Task HasEmployeesAsync_WhenNoEmployees_ReturnsFalse()
    {
        var company = MakeCompany(Guid.NewGuid());
        await _sut.AddAsync(company);

        Assert.False(await _sut.HasEmployeesAsync(company.Id));
    }

    [Fact]
    public async Task GetByIdWithEmployeesAsync_HydratesEmployeesList()
    {
        var userId = Guid.NewGuid();
        var company = MakeCompany(userId, "WithEmps");
        await _sut.AddAsync(company);

        // The employees list should be empty since none were added
        var result = await _sut.GetByIdWithEmployeesAsync(company.Id, userId);

        Assert.NotNull(result);
        Assert.NotNull(result.Employees);
        Assert.Empty(result.Employees);
    }

    [Fact]
    public async Task AddAsync_WithOptionalFields_PersistsAllFields()
    {
        var userId = Guid.NewGuid();
        var company = new Company
        {
            Name = "Full Co",
            Address = "123 Main St",
            UifNumber = "UIF-12345",
            SarsPayeNumber = "SARS-9999",
            UserId = userId,
        };

        await _sut.AddAsync(company);
        var retrieved = await _sut.GetByIdAsync(company.Id, userId);

        Assert.NotNull(retrieved);
        Assert.Equal("123 Main St", retrieved.Address);
        Assert.Equal("UIF-12345", retrieved.UifNumber);
        Assert.Equal("SARS-9999", retrieved.SarsPayeNumber);
    }
}
