using Moq;
using Payslip4All.Application.DTOs.Payslip;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Tests.Services;

public class PayslipGenerationServiceTests
{
    private readonly Mock<IPayslipRepository> _mockPayslipRepo;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo;
    private readonly Mock<ILoanRepository> _mockLoanRepo;
    private readonly Mock<IPdfGenerationService> _mockPdfService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly PayslipGenerationService _service;

    public PayslipGenerationServiceTests()
    {
        _mockPayslipRepo = new Mock<IPayslipRepository>();
        _mockEmployeeRepo = new Mock<IEmployeeRepository>();
        _mockLoanRepo = new Mock<ILoanRepository>();
        _mockPdfService = new Mock<IPdfGenerationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new PayslipGenerationService(
            _mockPayslipRepo.Object,
            _mockEmployeeRepo.Object,
            _mockLoanRepo.Object,
            _mockPdfService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task PreviewPayslipAsync_ValidEmployee_ReturnsSuccessWithCalculations()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employee = new Employee
        {
            FirstName = "John", LastName = "Doe", IdNumber = "123", EmployeeNumber = "E001",
            Occupation = "Dev", MonthlyGrossSalary = 10000, CompanyId = Guid.NewGuid()
        };
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);

        var result = await _service.PreviewPayslipAsync(new PreviewPayslipQuery
        {
            EmployeeId = employeeId, UserId = userId, PayPeriodMonth = 1, PayPeriodYear = 2024
        });

        Assert.True(result.Success);
        Assert.NotNull(result.PayslipDto);
        Assert.Equal(10000m, result.PayslipDto.GrossEarnings);
        Assert.Equal(100m, result.PayslipDto.UifDeduction); // 1% of 10000
        Assert.Equal(9900m, result.PayslipDto.NetPay);
    }

    [Fact]
    public async Task PreviewPayslipAsync_EmployeeNotFound_ReturnsFailure()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Employee?)null);

        var result = await _service.PreviewPayslipAsync(new PreviewPayslipQuery
        {
            EmployeeId = Guid.NewGuid(), UserId = Guid.NewGuid(), PayPeriodMonth = 1, PayPeriodYear = 2024
        });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task GeneratePayslipAsync_DuplicateWithoutOverwrite_ReturnsIsDuplicate()
    {
        var employeeId = Guid.NewGuid();
        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, 1, 2024)).ReturnsAsync(true);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var result = await _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId, UserId = Guid.NewGuid(),
            PayPeriodMonth = 1, PayPeriodYear = 2024, OverwriteExisting = false
        });

        Assert.False(result.Success);
        Assert.True(result.IsDuplicate);
    }

    [Fact]
    public async Task GeneratePayslipAsync_ValidEmployee_ReturnsSuccessAndSaves()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var company = new Company { Name = "Test Co", UserId = userId };
        var employee = new Employee
        {
            FirstName = "John", LastName = "Doe", IdNumber = "123", EmployeeNumber = "E001",
            Occupation = "Dev", MonthlyGrossSalary = 10000, CompanyId = company.Id
        };
        employee.Company = company;

        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, 1, 2024)).ReturnsAsync(false);
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockPdfService.Setup(p => p.GeneratePayslip(It.IsAny<PayslipDocument>())).Returns(new byte[] { 1, 2, 3 });
        _mockPayslipRepo.Setup(r => r.AddAsync(It.IsAny<Payslip>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

        var result = await _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId, UserId = userId,
            PayPeriodMonth = 1, PayPeriodYear = 2024, OverwriteExisting = false
        });

        Assert.True(result.Success);
        _mockPayslipRepo.Verify(r => r.AddAsync(It.IsAny<Payslip>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPayslipsForEmployeeAsync_ReturnsPayslips()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var payslip = new Payslip
        {
            EmployeeId = employeeId, PayPeriodMonth = 1, PayPeriodYear = 2024,
            GrossEarnings = 10000m, UifDeduction = 100m, NetPay = 9900m,
            TotalDeductions = 100m, TotalLoanDeductions = 0m
        };
        _mockPayslipRepo.Setup(r => r.GetAllByEmployeeIdAsync(employeeId, userId))
            .ReturnsAsync(new List<Payslip> { payslip });

        var result = await _service.GetPayslipsForEmployeeAsync(employeeId, userId);

        Assert.Single(result);
        Assert.Equal(1, result[0].PayPeriodMonth);
    }

    [Fact]
    public async Task GetPdfAsync_ExistingPayslip_ReturnsPdfBytes()
    {
        var payslipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var payslip = new Payslip
        {
            EmployeeId = Guid.NewGuid(), PdfContent = new byte[] { 1, 2, 3 }
        };
        _mockPayslipRepo.Setup(r => r.GetByIdAsync(payslipId, userId)).ReturnsAsync(payslip);

        var result = await _service.GetPdfAsync(payslipId, userId);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Length);
    }

    [Fact]
    public async Task GetPdfAsync_NotFound_ReturnsNull()
    {
        _mockPayslipRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Payslip?)null);

        var result = await _service.GetPdfAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task PreviewPayslipAsync_ZeroSalaryEmployee_ReturnsError()
    {
        var employee = new Employee
        {
            FirstName = "Jane", LastName = "Doe", IdNumber = "456", EmployeeNumber = "E002",
            Occupation = "Intern", MonthlyGrossSalary = 0m, CompanyId = Guid.NewGuid()
        };
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        var result = await _service.PreviewPayslipAsync(new PreviewPayslipQuery
        {
            EmployeeId = employee.Id, UserId = Guid.NewGuid(), PayPeriodMonth = 1, PayPeriodYear = 2024
        });

        Assert.False(result.Success);
    }
}
