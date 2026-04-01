using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Pricing;
using Payslip4All.Application.Interfaces;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages.Admin;

public class WalletPricingTests : TestContext
{
    [Fact]
    public void WalletPricing_DisplaysCurrentPrice()
    {
        SetAuthorizedAdmin();
        var pricingService = new Mock<IPayslipPricingService>();
        pricingService.Setup(s => s.GetCurrentPriceAsync()).ReturnsAsync(new PayslipPricingSettingDto
        {
            PricePerPayslip = 7m,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedByUserId = "admin-1"
        });
        Services.AddSingleton(pricingService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Admin.WalletPricing>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Current Price", cut.Markup);
            Assert.Contains("admin-1", cut.Markup);
        });
    }

    [Fact]
    public void WalletPricing_Submit_UpdatesPrice()
    {
        SetAuthorizedAdmin();
        var pricingService = new Mock<IPayslipPricingService>();
        pricingService.Setup(s => s.GetCurrentPriceAsync()).ReturnsAsync(new PayslipPricingSettingDto
        {
            PricePerPayslip = 7m,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        pricingService.Setup(s => s.UpdatePriceAsync(It.IsAny<UpdatePayslipPriceCommand>()))
            .ReturnsAsync(new PayslipPricingSettingDto
            {
                PricePerPayslip = 9m,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedByUserId = "admin-2"
            });
        Services.AddSingleton(pricingService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Admin.WalletPricing>();
        cut.WaitForAssertion(() => Assert.Contains("Update Price", cut.Markup));
        cut.Find("input").Change("9");
        cut.Find("form").Submit();

        Assert.Contains("Payslip price updated", cut.Markup);
        pricingService.Verify(s => s.UpdatePriceAsync(It.Is<UpdatePayslipPriceCommand>(c => c.PricePerPayslip == 9m)), Times.Once);
    }

    [Fact]
    public void WalletPricing_Submit_RequiresAuthenticatedAdminId()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@test.com");
        authContext.SetRoles("SiteAdministrator");

        var pricingService = new Mock<IPayslipPricingService>();
        pricingService.Setup(s => s.GetCurrentPriceAsync()).ReturnsAsync(new PayslipPricingSettingDto
        {
            PricePerPayslip = 7m,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        Services.AddSingleton(pricingService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Admin.WalletPricing>();
        cut.WaitForAssertion(() => Assert.Contains("Update Price", cut.Markup));
        cut.Find("input").Change("9");
        cut.Find("form").Submit();

        Assert.Contains("Unable to determine the administrator account", cut.Markup);
        pricingService.Verify(s => s.UpdatePriceAsync(It.IsAny<UpdatePayslipPriceCommand>()), Times.Never);
    }

    private void SetAuthorizedAdmin()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@test.com");
        authContext.SetRoles("SiteAdministrator");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
    }
}
