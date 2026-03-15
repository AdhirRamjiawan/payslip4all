using Moq;
using Payslip4All.Application.DTOs.Company;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Tests.Services;

public class CompanyServiceTests
{
    private readonly Mock<ICompanyRepository> _mockRepo;
    private readonly CompanyService _service;

    public CompanyServiceTests()
    {
        _mockRepo = new Mock<ICompanyRepository>();
        _service = new CompanyService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetCompaniesForUserAsync_ReturnsOnlyUserCompanies()
    {
        var userId = Guid.NewGuid();
        var companies = new List<Company>
        {
            new() { Name = "Test Co", UserId = userId, Employees = new List<Employee>() }
        };
        _mockRepo.Setup(r => r.GetAllByUserIdAsync(userId)).ReturnsAsync(companies);

        var result = await _service.GetCompaniesForUserAsync(userId);

        Assert.Single(result);
        Assert.Equal("Test Co", result[0].Name);
        Assert.Equal(userId, result[0].UserId);
    }

    [Fact]
    public async Task GetCompaniesForUserAsync_ReturnsEmployeeCount()
    {
        var userId = Guid.NewGuid();
        var company = new Company { Name = "Test Co", UserId = userId };
        company.Employees.Add(new Employee { FirstName = "A", LastName = "B", IdNumber = "1", EmployeeNumber = "E1", Occupation = "Dev", CompanyId = company.Id });
        company.Employees.Add(new Employee { FirstName = "C", LastName = "D", IdNumber = "2", EmployeeNumber = "E2", Occupation = "Dev", CompanyId = company.Id });
        _mockRepo.Setup(r => r.GetAllByUserIdAsync(userId)).ReturnsAsync(new List<Company> { company });

        var result = await _service.GetCompaniesForUserAsync(userId);

        Assert.Single(result);
        Assert.Equal(2, result[0].EmployeeCount);
    }

    [Fact]
    public async Task CreateCompanyAsync_WithValidName_PersistsAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

        var result = await _service.CreateCompanyAsync(new CreateCompanyCommand
        {
            Name = "New Company",
            Address = "123 Street",
            UserId = userId
        });

        Assert.Equal("New Company", result.Name);
        Assert.Equal("123 Street", result.Address);
        Assert.Equal(userId, result.UserId);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Company>()), Times.Once);
    }

    [Fact]
    public async Task CreateCompanyAsync_WithEmptyName_ThrowsArgumentException()
    {
        var cmd = new CreateCompanyCommand { Name = "", UserId = Guid.NewGuid() };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateCompanyAsync(cmd));
    }

    [Fact]
    public async Task UpdateCompanyAsync_WithWrongUserId_ReturnsNull()
    {
        var wrongUserId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), wrongUserId)).ReturnsAsync((Company?)null);

        var result = await _service.UpdateCompanyAsync(new UpdateCompanyCommand
        {
            Id = Guid.NewGuid(),
            Name = "Updated",
            UserId = wrongUserId
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteCompanyAsync_WithEmployeesPresent_ReturnsFalse()
    {
        var companyId = Guid.NewGuid();
        _mockRepo.Setup(r => r.HasEmployeesAsync(companyId)).ReturnsAsync(true);

        var result = await _service.DeleteCompanyAsync(companyId, Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteCompanyAsync_WithNoEmployees_ReturnsTrue()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var company = new Company { Name = "Co", UserId = userId };
        _mockRepo.Setup(r => r.HasEmployeesAsync(companyId)).ReturnsAsync(false);
        _mockRepo.Setup(r => r.GetByIdAsync(companyId, userId)).ReturnsAsync(company);
        _mockRepo.Setup(r => r.DeleteAsync(company)).Returns(Task.CompletedTask);

        var result = await _service.DeleteCompanyAsync(companyId, userId);

        Assert.True(result);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_ExistingCompany_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        var company = new Company { Name = "Test Co", UserId = userId };
        _mockRepo.Setup(r => r.GetByIdAsync(company.Id, userId)).ReturnsAsync(company);

        var result = await _service.GetCompanyByIdAsync(company.Id, userId);

        Assert.NotNull(result);
        Assert.Equal("Test Co", result!.Name);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_NotFound_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), userId)).ReturnsAsync((Company?)null);

        var result = await _service.GetCompanyByIdAsync(Guid.NewGuid(), userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCompanyAsync_ExistingCompany_UpdatesAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var company = new Company { Name = "Old Name", UserId = userId };
        _mockRepo.Setup(r => r.GetByIdAsync(company.Id, userId)).ReturnsAsync(company);
        _mockRepo.Setup(r => r.UpdateAsync(company)).Returns(Task.CompletedTask);

        var result = await _service.UpdateCompanyAsync(new UpdateCompanyCommand
        {
            Id = company.Id, Name = "New Name", Address = "123 St", UserId = userId
        });

        Assert.NotNull(result);
        Assert.Equal("New Name", result!.Name);
    }
}
