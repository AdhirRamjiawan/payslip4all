using Moq;
using Payslip4All.Application.DTOs.Loan;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.Tests.Services;

public class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _mockRepo;
    private readonly LoanService _service;

    public LoanServiceTests()
    {
        _mockRepo = new Mock<ILoanRepository>();
        _service = new LoanService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetLoansForEmployeeAsync_ReturnsLoans()
    {
        var employeeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var loans = new List<EmployeeLoan>
        {
            new() { Description = "Car Loan", TotalLoanAmount = 10000, NumberOfTerms = 10, MonthlyDeductionAmount = 1000, PaymentStartDate = new DateOnly(2024, 1, 1), EmployeeId = employeeId }
        };
        _mockRepo.Setup(r => r.GetAllByEmployeeIdAsync(employeeId, userId)).ReturnsAsync(loans);

        var result = await _service.GetLoansForEmployeeAsync(employeeId, userId);

        Assert.Single(result);
        Assert.Equal("Car Loan", result[0].Description);
    }

    [Fact]
    public async Task CreateLoanAsync_WithValidData_PersistsAndReturnsDto()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<EmployeeLoan>())).Returns(Task.CompletedTask);

        var result = await _service.CreateLoanAsync(new CreateLoanCommand
        {
            Description = "House Loan",
            TotalLoanAmount = 50000,
            NumberOfTerms = 24,
            MonthlyDeductionAmount = 2083.33m,
            PaymentStartDate = new DateOnly(2024, 3, 1),
            EmployeeId = Guid.NewGuid()
        });

        Assert.Equal("House Loan", result.Description);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<EmployeeLoan>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLoanAsync_WithCompletedLoan_ReturnsNull()
    {
        var loanId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var loan = new EmployeeLoan { Description = "Loan", TotalLoanAmount = 1000, NumberOfTerms = 1, MonthlyDeductionAmount = 1000, PaymentStartDate = new DateOnly(2024, 1, 1), EmployeeId = Guid.NewGuid() };
        loan.IncrementTermsCompleted(); // completes it
        _mockRepo.Setup(r => r.GetByIdAsync(loanId, userId)).ReturnsAsync(loan);

        var result = await _service.UpdateLoanAsync(new UpdateLoanCommand
        {
            LoanId = loanId,
            UserId = userId,
            Description = "Updated",
            TotalLoanAmount = 1000,
            NumberOfTerms = 1,
            MonthlyDeductionAmount = 1000,
            PaymentStartDate = new DateOnly(2024, 1, 1)
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteLoanAsync_WithCompletedLoan_ReturnsFalse()
    {
        var loanId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var loan = new EmployeeLoan { Description = "Loan", TotalLoanAmount = 1000, NumberOfTerms = 1, MonthlyDeductionAmount = 1000, PaymentStartDate = new DateOnly(2024, 1, 1), EmployeeId = Guid.NewGuid() };
        loan.IncrementTermsCompleted();
        _mockRepo.Setup(r => r.GetByIdAsync(loanId, userId)).ReturnsAsync(loan);

        var result = await _service.DeleteLoanAsync(loanId, userId);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteLoanAsync_WithActiveLoan_ReturnsTrue()
    {
        var loanId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var loan = new EmployeeLoan { Description = "Loan", TotalLoanAmount = 3000, NumberOfTerms = 3, MonthlyDeductionAmount = 1000, PaymentStartDate = new DateOnly(2024, 1, 1), EmployeeId = Guid.NewGuid() };
        _mockRepo.Setup(r => r.GetByIdAsync(loanId, userId)).ReturnsAsync(loan);
        _mockRepo.Setup(r => r.DeleteAsync(loan)).Returns(Task.CompletedTask);

        var result = await _service.DeleteLoanAsync(loanId, userId);

        Assert.True(result);
    }
}
