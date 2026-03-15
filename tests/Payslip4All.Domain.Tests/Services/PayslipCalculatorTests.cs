using Payslip4All.Domain.Services;

namespace Payslip4All.Domain.Tests.Services;

public class PayslipCalculatorTests
{
    [Fact]
    public void CalculateUifDeduction_GrossAboveCeiling_ReturnsCeilingRate()
        => Assert.Equal(177.12m, PayslipCalculator.CalculateUifDeduction(25000m));

    [Fact]
    public void CalculateUifDeduction_GrossAtCeiling_ReturnsCeilingRate()
        => Assert.Equal(177.12m, PayslipCalculator.CalculateUifDeduction(17712m));

    [Fact]
    public void CalculateUifDeduction_GrossBelowCeiling_ReturnsOnePercent()
        => Assert.Equal(100.00m, PayslipCalculator.CalculateUifDeduction(10000m));

    [Fact]
    public void CalculateUifDeduction_ZeroGross_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => PayslipCalculator.CalculateUifDeduction(0m));

    [Fact]
    public void CalculateUifDeduction_NegativeGross_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => PayslipCalculator.CalculateUifDeduction(-1m));

    [Fact]
    public void CalculateNetPay_SubtractsAllDeductions()
    {
        var netPay = PayslipCalculator.CalculateNetPay(25000m, 177.12m, new[] { 500m });
        Assert.Equal(24322.88m, netPay);
    }

    [Fact]
    public void CalculateTotalDeductions_SumsUifAndLoans()
    {
        var total = PayslipCalculator.CalculateTotalDeductions(177.12m, new[] { 500m, 300m });
        Assert.Equal(977.12m, total);
    }
}
