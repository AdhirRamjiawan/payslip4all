using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Domain.Tests.Entities;

public class EmployeeLoanTests
{
    private EmployeeLoan CreateActiveLoan(int numberOfTerms = 3) => new()
    {
        Description = "Test Loan",
        TotalLoanAmount = 3000m,
        NumberOfTerms = numberOfTerms,
        MonthlyDeductionAmount = 1000m,
        PaymentStartDate = new DateOnly(2024, 1, 1),
        EmployeeId = Guid.NewGuid()
    };

    [Fact]
    public void IncrementTermsCompleted_BelowFinalTerm_KeepsStatusActive()
    {
        var loan = CreateActiveLoan(3);
        loan.IncrementTermsCompleted();
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(1, loan.TermsCompleted);
    }

    [Fact]
    public void IncrementTermsCompleted_AtFinalTerm_TransitionsToCompleted()
    {
        var loan = CreateActiveLoan(1);
        loan.IncrementTermsCompleted();
        Assert.Equal(LoanStatus.Completed, loan.Status);
    }

    [Fact]
    public void IncrementTermsCompleted_AlreadyCompleted_ThrowsInvalidOperationException()
    {
        var loan = CreateActiveLoan(1);
        loan.IncrementTermsCompleted();
        Assert.Throws<InvalidOperationException>(() => loan.IncrementTermsCompleted());
    }

    [Fact]
    public void IsActiveForPeriod_ActiveLoanWithValidPeriod_ReturnsTrue()
    {
        var loan = CreateActiveLoan(3);
        Assert.True(loan.IsActiveForPeriod(1, 2024));
    }

    [Fact]
    public void IsActiveForPeriod_CompletedLoan_ReturnsFalse()
    {
        var loan = CreateActiveLoan(1);
        loan.IncrementTermsCompleted();
        Assert.False(loan.IsActiveForPeriod(1, 2024));
    }

    [Fact]
    public void IsActiveForPeriod_PeriodBeforeStartDate_ReturnsFalse()
    {
        var loan = CreateActiveLoan(3);
        Assert.False(loan.IsActiveForPeriod(12, 2023)); // Before 2024-01
    }

    [Fact]
    public void RestoreTermsCompleted_WhenValueExceedsTermCount_ThrowsArgumentOutOfRangeException()
    {
        var loan = CreateActiveLoan(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => loan.RestoreTermsCompleted(4));
    }

    [Fact]
    public void RestoreTermsCompleted_WhenValueIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var loan = CreateActiveLoan(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => loan.RestoreTermsCompleted(-1));
    }
}
