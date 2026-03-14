using Moq;
using Payslip4All.Application.DTOs.Employee;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Tests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _mockRepo;
    private readonly EmployeeService _service;

    public EmployeeServiceTests()
    {
        _mockRepo = new Mock<IEmployeeRepository>();
        _service = new EmployeeService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetEmployeesForCompanyAsync_ReturnsEmployees()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employees = new List<Employee>
        {
            new() { FirstName = "John", LastName = "Doe", IdNumber = "1234567890", EmployeeNumber = "E001", Occupation = "Dev", MonthlyGrossSalary = 10000, CompanyId = companyId }
        };
        _mockRepo.Setup(r => r.GetAllByCompanyIdAsync(companyId, userId)).ReturnsAsync(employees);

        var result = await _service.GetEmployeesForCompanyAsync(companyId, userId);

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithValidData_PersistsAndReturnsDto()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Employee>())).Returns(Task.CompletedTask);

        var result = await _service.CreateEmployeeAsync(new CreateEmployeeCommand
        {
            FirstName = "Jane",
            LastName = "Smith",
            IdNumber = "9876543210",
            EmployeeNumber = "E002",
            StartDate = new DateOnly(2024, 1, 1),
            Occupation = "Developer",
            MonthlyGrossSalary = 25000,
            CompanyId = Guid.NewGuid()
        });

        Assert.Equal("Jane", result.FirstName);
        Assert.Equal(25000, result.MonthlyGrossSalary);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Employee>()), Times.Once);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithZeroSalary_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateEmployeeAsync(new CreateEmployeeCommand
        {
            FirstName = "Jane",
            LastName = "Smith",
            IdNumber = "123",
            EmployeeNumber = "E003",
            StartDate = new DateOnly(2024, 1, 1),
            Occupation = "Dev",
            MonthlyGrossSalary = 0,
            CompanyId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithEmptyFirstName_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateEmployeeAsync(new CreateEmployeeCommand
        {
            FirstName = "",
            LastName = "Smith",
            IdNumber = "123",
            EmployeeNumber = "E004",
            StartDate = new DateOnly(2024, 1, 1),
            Occupation = "Dev",
            MonthlyGrossSalary = 10000,
            CompanyId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task UpdateEmployeeAsync_WithWrongUserId_ReturnsNull()
    {
        var wrongUserId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), wrongUserId)).ReturnsAsync((Employee?)null);

        var result = await _service.UpdateEmployeeAsync(new UpdateEmployeeCommand
        {
            Id = Guid.NewGuid(),
            UserId = wrongUserId,
            FirstName = "Jane",
            LastName = "Smith",
            IdNumber = "123",
            EmployeeNumber = "E001",
            StartDate = new DateOnly(2024, 1, 1),
            Occupation = "Dev",
            MonthlyGrossSalary = 10000
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteEmployeeAsync_WithPayslips_ReturnsFalse()
    {
        var employeeId = Guid.NewGuid();
        _mockRepo.Setup(r => r.HasPayslipsAsync(employeeId)).ReturnsAsync(true);

        var result = await _service.DeleteEmployeeAsync(employeeId, Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteEmployeeAsync_WithNoPayslips_ReturnsTrue()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employee = new Employee { FirstName = "A", LastName = "B", IdNumber = "1", EmployeeNumber = "E1", Occupation = "Dev", CompanyId = Guid.NewGuid() };
        _mockRepo.Setup(r => r.HasPayslipsAsync(employeeId)).ReturnsAsync(false);
        _mockRepo.Setup(r => r.GetByIdAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockRepo.Setup(r => r.DeleteAsync(employee)).Returns(Task.CompletedTask);

        var result = await _service.DeleteEmployeeAsync(employeeId, userId);

        Assert.True(result);
    }
}
