using Moq;
using Payslip4All.Application.DTOs;
using Payslip4All.Application.DTOs.Payslip;
using Payslip4All.Application.DTOs.Pricing;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Tests.Services;

public class PayslipGenerationServiceTests
{
    private readonly Mock<IPayslipRepository> _mockPayslipRepo = new();
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo = new();
    private readonly Mock<ILoanRepository> _mockLoanRepo = new();
    private readonly Mock<IPdfGenerationService> _mockPdfService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IWalletService> _mockWalletService = new();
    private readonly Mock<IPayslipPricingService> _mockPricingService = new();
    private readonly PayslipGenerationService _service;

    public PayslipGenerationServiceTests()
    {
        _service = new PayslipGenerationService(
            _mockPayslipRepo.Object,
            _mockEmployeeRepo.Object,
            _mockLoanRepo.Object,
            _mockPdfService.Object,
            _mockUnitOfWork.Object,
            _mockWalletService.Object,
            _mockPricingService.Object);

        _mockPricingService.Setup(s => s.GetCurrentPriceAsync())
            .ReturnsAsync(new PayslipPricingSettingDto { PricePerPayslip = 5m });
        _mockWalletService.Setup(s => s.GetWalletAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => new WalletDto { UserId = userId, CurrentBalance = 100m });
        _mockWalletService.Setup(s => s.TryDebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
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
        Assert.Equal(10000m, result.PayslipDto!.GrossEarnings);
        Assert.Equal(100m, result.PayslipDto.UifDeduction);
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
    public async Task GeneratePayslipAsync_ValidEmployee_ReturnsSuccessAndDebitsWallet()
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
        Assert.Equal(5m, result.ChargedAmount);
        _mockPayslipRepo.Verify(r => r.AddAsync(It.IsAny<Payslip>()), Times.Once);
        _mockWalletService.Verify(s => s.TryDebitAsync(userId, 5m, It.IsAny<string>(), "Payslip", It.IsAny<string>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GeneratePayslipAsync_WhenWalletHasInsufficientFunds_ReturnsFailureWithoutSaving()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employee = new Employee
        {
            FirstName = "John", LastName = "Doe", IdNumber = "123", EmployeeNumber = "E001",
            Occupation = "Dev", MonthlyGrossSalary = 10000, CompanyId = Guid.NewGuid()
        };

        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, 1, 2024)).ReturnsAsync(false);
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockPricingService.Setup(s => s.GetCurrentPriceAsync())
            .ReturnsAsync(new PayslipPricingSettingDto { PricePerPayslip = 25m });
        _mockWalletService.Setup(s => s.GetWalletAsync(userId))
            .ReturnsAsync(new WalletDto { UserId = userId, CurrentBalance = 10m });

        var result = await _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId, UserId = userId,
            PayPeriodMonth = 1, PayPeriodYear = 2024
        });

        Assert.False(result.Success);
        Assert.True(result.InsufficientFunds);
        _mockPayslipRepo.Verify(r => r.AddAsync(It.IsAny<Payslip>()), Times.Never);
        _mockWalletService.Verify(s => s.TryDebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task GeneratePayslipAsync_WhenSaveFails_DoesNotDebitWallet()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employee = new Employee
        {
            FirstName = "John", LastName = "Doe", IdNumber = "123", EmployeeNumber = "E001",
            Occupation = "Dev", MonthlyGrossSalary = 10000, CompanyId = Guid.NewGuid()
        };

        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, 1, 2024)).ReturnsAsync(false);
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockPayslipRepo.Setup(r => r.AddAsync(It.IsAny<Payslip>())).ThrowsAsync(new InvalidOperationException("save failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId, UserId = userId,
            PayPeriodMonth = 1, PayPeriodYear = 2024
        }));

        _mockWalletService.Verify(s => s.TryDebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task GeneratePayslipAsync_WithZeroPrice_SucceedsWithoutDebitingWallet()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employee = new Employee
        {
            FirstName = "John", LastName = "Doe", IdNumber = "123", EmployeeNumber = "E001",
            Occupation = "Dev", MonthlyGrossSalary = 10000, CompanyId = Guid.NewGuid()
        };

        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, 1, 2024)).ReturnsAsync(false);
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockPricingService.Setup(s => s.GetCurrentPriceAsync())
            .ReturnsAsync(new PayslipPricingSettingDto { PricePerPayslip = 0m });
        _mockWalletService.Setup(s => s.GetWalletAsync(userId))
            .ReturnsAsync(new WalletDto { UserId = userId, CurrentBalance = 0m });

        var result = await _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId,
            UserId = userId,
            PayPeriodMonth = 1,
            PayPeriodYear = 2024
        });

        Assert.True(result.Success);
        Assert.Equal(0m, result.ChargedAmount);
        _mockWalletService.Verify(s => s.TryDebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task GeneratePayslipAsync_WhenDebitFails_RollsBackTransactionAndReturnsFailure()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employee = new Employee
        {
            FirstName = "John", LastName = "Doe", IdNumber = "123", EmployeeNumber = "E001",
            Occupation = "Dev", MonthlyGrossSalary = 10000, CompanyId = Guid.NewGuid()
        };

        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, 1, 2024)).ReturnsAsync(false);
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockWalletService.Setup(s => s.TryDebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(false);

        var result = await _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId,
            UserId = userId,
            PayPeriodMonth = 1,
            PayPeriodYear = 2024
        });

        Assert.False(result.Success);
        Assert.True(result.InsufficientFunds);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task GeneratePayslipAsync_WhenOverwriteChargeFailsWithoutTransactions_DoesNotDeleteExistingPayslip()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingPayslip = new Payslip
        {
            EmployeeId = employeeId,
            PayPeriodMonth = 1,
            PayPeriodYear = 2024,
            GrossEarnings = 9000m,
            UifDeduction = 90m,
            TotalLoanDeductions = 0m,
            TotalDeductions = 90m,
            NetPay = 8910m,
            ChargedAmount = 5m,
        };
        var employee = new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            IdNumber = "123",
            EmployeeNumber = "E001",
            Occupation = "Dev",
            MonthlyGrossSalary = 10000m,
            CompanyId = Guid.NewGuid(),
        };

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ThrowsAsync(new NotSupportedException("No transactions"));
        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, 1, 2024)).ReturnsAsync(true);
        _mockPayslipRepo.Setup(r => r.GetAllByEmployeeIdAsync(employeeId, userId))
            .ReturnsAsync(new List<Payslip> { existingPayslip });
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockPayslipRepo.Setup(r => r.AddAsync(It.IsAny<Payslip>())).Returns(Task.CompletedTask);
        _mockPayslipRepo.Setup(r => r.DeleteAsync(It.Is<Payslip>(p => p.Id != existingPayslip.Id))).Returns(Task.CompletedTask);
        _mockWalletService.Setup(s => s.TryDebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(false);

        var result = await _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId,
            UserId = userId,
            PayPeriodMonth = 1,
            PayPeriodYear = 2024,
            OverwriteExisting = true,
        });

        Assert.False(result.Success);
        Assert.True(result.InsufficientFunds);
        _mockPayslipRepo.Verify(r => r.DeleteAsync(It.Is<Payslip>(p => p.Id == existingPayslip.Id)), Times.Never);
    }

    [Fact]
    public async Task GeneratePayslipAsync_WhenOverwritingWithoutTransactions_DeletesExistingAfterDebitSucceeds()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingPayslip = new Payslip
        {
            EmployeeId = employeeId,
            PayPeriodMonth = 1,
            PayPeriodYear = 2024,
            GrossEarnings = 10000m,
            UifDeduction = 100m,
            TotalLoanDeductions = 0m,
            TotalDeductions = 100m,
            NetPay = 9900m,
            ChargedAmount = 5m,
        };
        var employee = new Employee
        {
            FirstName = "John", LastName = "Doe", IdNumber = "123", EmployeeNumber = "E001",
            Occupation = "Dev", MonthlyGrossSalary = 10000, CompanyId = Guid.NewGuid()
        };

        var sequence = new MockSequence();
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ThrowsAsync(new NotSupportedException("No transactions"));
        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, 1, 2024)).ReturnsAsync(true);
        _mockPayslipRepo.Setup(r => r.GetAllByEmployeeIdAsync(employeeId, userId)).ReturnsAsync(new List<Payslip> { existingPayslip });
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockPayslipRepo.InSequence(sequence).Setup(r => r.AddAsync(It.IsAny<Payslip>())).Returns(Task.CompletedTask);
        _mockWalletService.InSequence(sequence)
            .Setup(s => s.TryDebitAsync(userId, 5m, It.IsAny<string>(), "Payslip", It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockPayslipRepo.InSequence(sequence).Setup(r => r.DeleteAsync(existingPayslip)).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId,
            UserId = userId,
            PayPeriodMonth = 1,
            PayPeriodYear = 2024,
            OverwriteExisting = true,
        });

        Assert.True(result.Success);
        _mockPayslipRepo.Verify(r => r.DeleteAsync(existingPayslip), Times.Once);
    }

    [Fact]
    public async Task GeneratePayslipAsync_WhenTransactionsAreUnavailableDueToInvalidOperation_FallsBackToCompensationPath()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employee = new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            IdNumber = "123",
            EmployeeNumber = "E001",
            Occupation = "Dev",
            MonthlyGrossSalary = 10000m,
            CompanyId = Guid.NewGuid(),
        };

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ThrowsAsync(new InvalidOperationException("No transactions"));
        _mockPayslipRepo.Setup(r => r.ExistsAsync(employeeId, 1, 2024)).ReturnsAsync(false);
        _mockEmployeeRepo.Setup(r => r.GetByIdWithLoansAsync(employeeId, userId)).ReturnsAsync(employee);
        _mockPayslipRepo.Setup(r => r.AddAsync(It.IsAny<Payslip>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await _service.GeneratePayslipAsync(new GeneratePayslipCommand
        {
            EmployeeId = employeeId,
            UserId = userId,
            PayPeriodMonth = 1,
            PayPeriodYear = 2024,
        });

        Assert.True(result.Success);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Never);
        _mockWalletService.Verify(s => s.TryDebitAsync(userId, 5m, It.IsAny<string>(), "Payslip", It.IsAny<string>()), Times.Once);
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
            TotalDeductions = 100m, TotalLoanDeductions = 0m, ChargedAmount = 5m
        };
        _mockPayslipRepo.Setup(r => r.GetAllByEmployeeIdAsync(employeeId, userId))
            .ReturnsAsync(new List<Payslip> { payslip });

        var result = await _service.GetPayslipsForEmployeeAsync(employeeId, userId);

        Assert.Single(result);
        Assert.Equal(5m, result[0].ChargedAmount);
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
}
