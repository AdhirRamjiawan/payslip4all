using Payslip4All.Domain.Services;

namespace Payslip4All.Domain.Tests.Services;

public class WalletCalculatorTests
{
    [Fact]
    public void CalculateBalanceAfterCredit_AddsAmount()
    {
        var result = WalletCalculator.CalculateBalanceAfterCredit(10m, 5m);

        Assert.Equal(15m, result);
    }

    [Fact]
    public void CalculateBalanceAfterDebit_ThrowsWhenBalanceWouldBecomeNegative()
    {
        Assert.Throws<InvalidOperationException>(() => WalletCalculator.CalculateBalanceAfterDebit(5m, 10m));
    }

    [Fact]
    public void CanDebit_ReturnsTrue_WhenBalanceIsSufficient()
    {
        Assert.True(WalletCalculator.CanDebit(10m, 5m));
    }
}
