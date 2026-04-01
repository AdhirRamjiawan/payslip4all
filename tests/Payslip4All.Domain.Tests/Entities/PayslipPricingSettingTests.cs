using Payslip4All.Domain.Entities;

namespace Payslip4All.Domain.Tests.Entities;

public class PayslipPricingSettingTests
{
    [Fact]
    public void PayslipPricingSetting_EnsureValid_RejectsNegativePrice()
    {
        var setting = new PayslipPricingSetting
        {
            PricePerPayslip = -1m,
        };

        Assert.Throws<ArgumentException>(() => setting.EnsureValid());
    }

    [Fact]
    public void PayslipPricingSetting_DefaultPrice_IsConfigurableConstant()
    {
        Assert.Equal(0m, PayslipPricingSetting.DefaultPricePerPayslip);
    }
}
