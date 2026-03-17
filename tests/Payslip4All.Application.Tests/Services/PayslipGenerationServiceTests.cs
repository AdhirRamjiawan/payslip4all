using Moq;
using Payslip4All.Application.DTOs;
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
        _mockPayslipRepo.Setup(r => r.AddAsync(It.IsAny<Payslip>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId, UserId = userId,
            PayPeriodMonth = 1, PayPeriodYear = 2024, OverwriteExisting = false
        });

        Assert.True(result.Success);
        _mockPayslipRepo.Verify(r => r.AddAsync(It.IsAny<Payslip>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        var company = new Company { Name = "Test Co", UserId = userId };
        var employee = new Employee
        {
            FirstName = "John", LastName = "Doe", IdNumber = "123", EmployeeNumber = "E001",
            Occupation = "Dev", MonthlyGrossSalary = 10000m, CompanyId = company.Id
        };
        employee.Company = company;
        var payslip = new Payslip
        {
            EmployeeId = employee.Id, PayPeriodMonth = 1, PayPeriodYear = 2024,
            GrossEarnings = 10000m, UifDeduction = 100m, NetPay = 9900m,
            TotalDeductions = 100m, TotalLoanDeductions = 0m
        };
        payslip.Employee = employee;

        _mockPayslipRepo.Setup(r => r.GetByIdAsync(payslipId, userId)).ReturnsAsync(payslip);
        _mockPdfService.Setup(p => p.GeneratePayslip(It.IsAny<PayslipDocument>())).Returns(new byte[] { 1, 2, 3 });

        var result = await _service.GetPdfAsync(payslipId, userId);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Length);
        _mockPdfService.Verify(p => p.GeneratePayslip(It.IsAny<PayslipDocument>()), Times.Once);
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

    // ──────────────────────────────────────────────────────────────────────
    // T009 — Mapping tests for PayslipDocument fields.
    // PDF construction now lives in GetPdfAsync (on-the-fly generation).
    // ──────────────────────────────────────────────────────────────────────

    private Payslip BuildSavedPayslip(Employee employee, int month, int year)
    {
        var uif = Payslip4All.Domain.Services.PayslipCalculator.CalculateUifDeduction(employee.MonthlyGrossSalary);
        var p = new Payslip
        {
            EmployeeId = employee.Id,
            PayPeriodMonth = month, PayPeriodYear = year,
            GrossEarnings = employee.MonthlyGrossSalary,
            UifDeduction = uif, TotalLoanDeductions = 0m,
            TotalDeductions = uif, NetPay = employee.MonthlyGrossSalary - uif
        };
        p.Employee = employee;
        return p;
    }

    [Fact]
    public async Task GetPdfAsync_MapsCompanyUifNumberToPayslipDocument()
    {
        var payslipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var company = new Company { Name = "UIF Corp", UserId = userId, UifNumber = "U999888", SarsPayeNumber = "SARS-001" };
        var employee = CreateFullEmployee(company);
        var payslip = BuildSavedPayslip(employee, 1, 2025);

        _mockPayslipRepo.Setup(r => r.GetByIdAsync(payslipId, userId)).ReturnsAsync(payslip);
        PayslipDocument? capturedDoc = null;
        _mockPdfService.Setup(p => p.GeneratePayslip(It.IsAny<PayslipDocument>()))
            .Callback<PayslipDocument>(d => capturedDoc = d).Returns(new byte[] { 1 });

        await _service.GetPdfAsync(payslipId, userId);

        Assert.NotNull(capturedDoc);
        Assert.Equal("U999888", capturedDoc!.CompanyUifNumber);
    }

    [Fact]
    public async Task GetPdfAsync_MapsCompanySarsPayeNumberToPayslipDocument()
    {
        var payslipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var company = new Company { Name = "SARS Corp", UserId = userId, UifNumber = null, SarsPayeNumber = "7654321A" };
        var employee = CreateFullEmployee(company);
        var payslip = BuildSavedPayslip(employee, 1, 2025);

        _mockPayslipRepo.Setup(r => r.GetByIdAsync(payslipId, userId)).ReturnsAsync(payslip);
        PayslipDocument? capturedDoc = null;
        _mockPdfService.Setup(p => p.GeneratePayslip(It.IsAny<PayslipDocument>()))
            .Callback<PayslipDocument>(d => capturedDoc = d).Returns(new byte[] { 1 });

        await _service.GetPdfAsync(payslipId, userId);

        Assert.NotNull(capturedDoc);
        Assert.Equal("7654321A", capturedDoc!.CompanySarsPayeNumber);
    }

    [Fact]
    public async Task GetPdfAsync_MapsEmployeeIdNumberToPayslipDocument()
    {
        var payslipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var company = new Company { Name = "ID Corp", UserId = userId };
        var employee = CreateFullEmployee(company, idNumber: "9001015009087");
        var payslip = BuildSavedPayslip(employee, 1, 2025);

        _mockPayslipRepo.Setup(r => r.GetByIdAsync(payslipId, userId)).ReturnsAsync(payslip);
        PayslipDocument? capturedDoc = null;
        _mockPdfService.Setup(p => p.GeneratePayslip(It.IsAny<PayslipDocument>()))
            .Callback<PayslipDocument>(d => capturedDoc = d).Returns(new byte[] { 1 });

        await _service.GetPdfAsync(payslipId, userId);

        Assert.NotNull(capturedDoc);
        Assert.Equal("9001015009087", capturedDoc!.EmployeeIdNumber);
    }

    [Fact]
    public async Task GetPdfAsync_MapsEmployeeStartDateToPayslipDocument()
    {
        var payslipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var startDate = new DateOnly(2021, 3, 1);
        var company = new Company { Name = "Start Corp", UserId = userId };
        var employee = CreateFullEmployee(company, startDate: startDate);
        var payslip = BuildSavedPayslip(employee, 1, 2025);

        _mockPayslipRepo.Setup(r => r.GetByIdAsync(payslipId, userId)).ReturnsAsync(payslip);
        PayslipDocument? capturedDoc = null;
        _mockPdfService.Setup(p => p.GeneratePayslip(It.IsAny<PayslipDocument>()))
            .Callback<PayslipDocument>(d => capturedDoc = d).Returns(new byte[] { 1 });

        await _service.GetPdfAsync(payslipId, userId);

        Assert.NotNull(capturedDoc);
        Assert.Equal(startDate, capturedDoc!.EmployeeStartDate);
    }

    [Fact]
    public async Task GetPdfAsync_MapsEmployeeUifReferenceToPayslipDocument()
    {
        var payslipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var company = new Company { Name = "UIF Ref Corp", UserId = userId };
        var employee = CreateFullEmployee(company, uifReference: "UIF-EMP-001");
        var payslip = BuildSavedPayslip(employee, 1, 2025);

        _mockPayslipRepo.Setup(r => r.GetByIdAsync(payslipId, userId)).ReturnsAsync(payslip);
        PayslipDocument? capturedDoc = null;
        _mockPdfService.Setup(p => p.GeneratePayslip(It.IsAny<PayslipDocument>()))
            .Callback<PayslipDocument>(d => capturedDoc = d).Returns(new byte[] { 1 });

        await _service.GetPdfAsync(payslipId, userId);

        Assert.NotNull(capturedDoc);
        Assert.Equal("UIF-EMP-001", capturedDoc!.EmployeeUifReference);
    }

    [Fact]
    public async Task GetPdfAsync_PaymentDateIsLastCalendarDayOfPayPeriodMonth()
    {
        var payslipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var company = new Company { Name = "Payment Corp", UserId = userId };
        var employee = CreateFullEmployee(company);
        // January 2025 — last day is 31st
        var payslip = BuildSavedPayslip(employee, 1, 2025);

        _mockPayslipRepo.Setup(r => r.GetByIdAsync(payslipId, userId)).ReturnsAsync(payslip);
        PayslipDocument? capturedDoc = null;
        _mockPdfService.Setup(p => p.GeneratePayslip(It.IsAny<PayslipDocument>()))
            .Callback<PayslipDocument>(d => capturedDoc = d).Returns(new byte[] { 1 });

        await _service.GetPdfAsync(payslipId, userId);

        Assert.NotNull(capturedDoc);
        Assert.Equal(new DateOnly(2025, 1, 31), capturedDoc!.PaymentDate);
    }

    [Fact]
    public async Task GetPdfAsync_PaymentDateIsLastDayOfFebruary_LeapYear()
    {
        var payslipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var company = new Company { Name = "Leap Corp", UserId = userId };
        var employee = CreateFullEmployee(company);
        // February 2024 — leap year, last day is 29th
        var payslip = BuildSavedPayslip(employee, 2, 2024);

        _mockPayslipRepo.Setup(r => r.GetByIdAsync(payslipId, userId)).ReturnsAsync(payslip);
        PayslipDocument? capturedDoc = null;
        _mockPdfService.Setup(p => p.GeneratePayslip(It.IsAny<PayslipDocument>()))
            .Callback<PayslipDocument>(d => capturedDoc = d).Returns(new byte[] { 1 });

        await _service.GetPdfAsync(payslipId, userId);

        Assert.NotNull(capturedDoc);
        Assert.Equal(new DateOnly(2024, 2, 29), capturedDoc!.PaymentDate);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private static Employee CreateFullEmployee(
        Company company,
        Guid? employeeId = null,
        string idNumber = "9001015009087",
        DateOnly? startDate = null,
        string? uifReference = "UIF-EMP-001")
    {
        var employee = new Employee
        {
            FirstName = "Jane", LastName = "Doe",
            IdNumber = idNumber,
            EmployeeNumber = "EMP-001",
            Occupation = "Engineer",
            MonthlyGrossSalary = 35_000m,
            CompanyId = company.Id,
            StartDate = startDate ?? new DateOnly(2021, 3, 1),
            UifReference = uifReference
        };
        employee.Company = company;
        return employee;
    }

    private void SetupGeneratePayslipMocks(Guid employeeId, Guid userId, Employee employee)
    {
        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockPayslipRepo.Setup(r => r.AddAsync(It.IsAny<Payslip>())).Returns(Task.CompletedTask);
        _mockLoanRepo.Setup(r => r.UpdateAsync(It.IsAny<EmployeeLoan>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
    }
}
