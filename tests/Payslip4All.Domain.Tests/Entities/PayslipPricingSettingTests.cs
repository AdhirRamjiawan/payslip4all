using Payslip4All.Domain.Entities;

namespace Payslip4All.Domain.Tests.Entities;

public class PayslipPricingSettingTests
{
    [Fact]
    public void PayslipPricingSetting_EnsureValid_RejectsNegativePrice()
    {
        var setting = new PayslipPricingSetting();
        typeof(PayslipPricingSetting).GetProperty(nameof(PayslipPricingSetting.PricePerPayslip))!
            .SetValue(setting, -1m);

        Assert.Throws<ArgumentException>(() => setting.EnsureValid());
    }

    [Fact]
    public void PayslipPricingSetting_DefaultPrice_IsConfigurableConstant()
    {
        Assert.Equal(15m, PayslipPricingSetting.DefaultPricePerPayslip);
    }

    [Fact]
    public void PayslipPricingSetting_UpdatePrice_RejectsMoreThanTwoDecimalPlaces()
    {
        var setting = new PayslipPricingSetting();

        Assert.Throws<ArgumentException>(() => setting.UpdatePrice(12.345m, "admin"));
    }

    [Fact]
    public void PayslipPricingSetting_UsesCanonicalDefaultId()
    {
        var setting = new PayslipPricingSetting();

        Assert.Equal(PayslipPricingSetting.DefaultId, setting.Id);
    }
}
