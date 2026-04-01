using Moq;
using Payslip4All.Application.DTOs.Pricing;
using Payslip4All.Application.Interfaces;

namespace Payslip4All.Web.Tests.Pages;

public class IndexModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsConfiguredPrice()
    {
        var pricingService = new Mock<IPayslipPricingService>();
        pricingService.Setup(s => s.GetCurrentPriceAsync()).ReturnsAsync(new PayslipPricingSettingDto
        {
            PricePerPayslip = 15m,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedByUserId = "admin-1"
        });

        var model = new IndexModel(pricingService.Object);

        await model.OnGetAsync();

        Assert.True(model.IsPublicPriceAvailable);
        Assert.Equal(15m, model.CurrentPayslipPrice);
        Assert.Contains("15", model.PublicPriceSummary);
        Assert.Contains("per payslip", model.PublicPriceSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OnGetAsync_WhenPricingLoadFails_UsesFallbackMessaging()
    {
        var pricingService = new Mock<IPayslipPricingService>();
        pricingService.Setup(s => s.GetCurrentPriceAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        var model = new IndexModel(pricingService.Object);

        await model.OnGetAsync();

        Assert.False(model.IsPublicPriceAvailable);
        Assert.Null(model.CurrentPayslipPrice);
        Assert.Contains("temporarily unavailable", model.PublicPriceSummary, StringComparison.OrdinalIgnoreCase);
    }
}
