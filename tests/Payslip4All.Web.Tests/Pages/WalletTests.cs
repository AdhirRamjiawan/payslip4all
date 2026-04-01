using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Domain.Enums;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages;

public class WalletTests : TestContext
{
    [Fact]
    public void Wallet_DisplaysBalanceAndPrice()
    {
        var userId = SetAuthorizedOwner();
        var walletService = new Mock<IWalletService>();
        walletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 150m,
            CurrentPayslipPrice = 5m
        });
        Services.AddSingleton(walletService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Current Balance", cut.Markup);
            Assert.Contains("Current Payslip Price", cut.Markup);
        });
    }

    [Fact]
    public void Wallet_SubmittingTopUp_ShowsUpdatedBalance()
    {
        var userId = SetAuthorizedOwner();
        var walletService = new Mock<IWalletService>();
        walletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 10m,
            CurrentPayslipPrice = 5m
        });
        walletService.Setup(s => s.TopUpAsync(It.IsAny<AddWalletCreditCommand>())).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 35m,
            CurrentPayslipPrice = 5m
        });
        Services.AddSingleton(walletService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() => Assert.Contains("Add Wallet Credits", cut.Markup));
        cut.Find("input").Change("25");
        cut.Find("form").Submit();

        Assert.Contains("Wallet topped up successfully", cut.Markup);
        walletService.Verify(s => s.TopUpAsync(It.Is<AddWalletCreditCommand>(c => c.Amount == 25m && c.UserId == userId)), Times.Once);
    }

    [Fact]
    public void Wallet_ShowsActivityList()
    {
        var userId = SetAuthorizedOwner();
        var walletService = new Mock<IWalletService>();
        walletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 20m,
            CurrentPayslipPrice = 5m,
            Activities = new List<WalletActivityDto>
            {
                new()
                {
                    ActivityType = WalletActivityType.Credit,
                    Amount = 20m,
                    BalanceAfterActivity = 20m,
                    Description = "Top up",
                    OccurredAt = DateTimeOffset.UtcNow
                }
            }
        });
        Services.AddSingleton(walletService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Top up", cut.Markup);
            Assert.Contains("Credit", cut.Markup);
        });
    }

    private Guid SetAuthorizedOwner()
    {
        var userId = Guid.NewGuid();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("owner@test.com");
        authContext.SetRoles("CompanyOwner");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        return userId;
    }
}
