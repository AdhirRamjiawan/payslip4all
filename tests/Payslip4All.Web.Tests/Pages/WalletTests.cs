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
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 150m,
            CurrentPayslipPrice = 5m
        });
        walletTopUpService.Setup(s => s.GetHistoryAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<WalletTopUpAttemptDto>());
        Services.AddSingleton(walletService.Object);
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Current Balance", cut.Markup);
            Assert.Contains("Current Payslip Price", cut.Markup);
        });
    }

    [Fact]
    public void Wallet_SubmittingTopUp_RedirectsToHostedPage_UsingGenericReturnRoute()
    {
        var userId = SetAuthorizedOwner();
        var walletService = new Mock<IWalletService>();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 10m,
            CurrentPayslipPrice = 5m
        });
        walletTopUpService.Setup(s => s.GetHistoryAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<WalletTopUpAttemptDto>());
        walletTopUpService.Setup(s => s.StartHostedTopUpAsync(It.IsAny<StartWalletTopUpCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new StartWalletTopUpResultDto
        {
            WalletTopUpAttemptId = Guid.NewGuid(),
            RedirectUrl = "http://localhost/hosted-payments/fake",
            Status = WalletTopUpAttemptStatus.Pending
        });
        Services.AddSingleton(walletService.Object);
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() => Assert.Contains("Add Wallet Credits", cut.Markup));
        cut.Find("input").Change("50");
        cut.Find("form").Submit();

        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.Equal("http://localhost/hosted-payments/fake", nav.Uri);
        walletTopUpService.Verify(s => s.StartHostedTopUpAsync(It.Is<StartWalletTopUpCommand>(c =>
            c.RequestedAmount == 50m && c.UserId == userId && c.ProviderKey == "payfast" && c.ReturnUrl.EndsWith("/portal/wallet/top-ups/return")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Wallet_DoesNotRenderCardFields_AndShowsExplicitStatuses()
    {
        var userId = SetAuthorizedOwner();
        var walletService = new Mock<IWalletService>();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 20m,
            CurrentPayslipPrice = 5m
        });
        walletTopUpService.Setup(s => s.GetHistoryAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new WalletTopUpAttemptDto
            {
                Id = Guid.NewGuid(),
                Status = WalletTopUpAttemptStatus.Abandoned,
                RequestedAmount = 100m,
                CreatedAt = DateTimeOffset.UtcNow,
                AbandonAfterUtc = DateTimeOffset.UtcNow.AddHours(1)
            },
            new WalletTopUpAttemptDto
            {
                Id = Guid.NewGuid(),
                Status = WalletTopUpAttemptStatus.NotConfirmed,
                RequestedAmount = 50m,
                CreatedAt = DateTimeOffset.UtcNow,
                AbandonAfterUtc = DateTimeOffset.UtcNow.AddHours(1)
            }
        });
        Services.AddSingleton(walletService.Object);
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Abandoned", cut.Markup);
            Assert.Contains("NotConfirmed", cut.Markup);
            Assert.DoesNotContain("card number", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("cvv", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Wallet_ShowsBalanceAndCompletedHistoryWithinFreshnessWindow()
    {
        var userId = SetAuthorizedOwner();
        var acceptedAt = DateTimeOffset.UtcNow.AddSeconds(-30);
        var walletService = new Mock<IWalletService>();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 95m,
            CurrentPayslipPrice = 5m
        });
        walletTopUpService.Setup(s => s.GetHistoryAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new WalletTopUpAttemptDto
            {
                Id = Guid.NewGuid(),
                Status = WalletTopUpAttemptStatus.Completed,
                RequestedAmount = 100m,
                ConfirmedChargedAmount = 95m,
                CreatedAt = acceptedAt.AddSeconds(-10),
                UpdatedAt = acceptedAt,
                AuthoritativeOutcomeAcceptedAt = acceptedAt,
                AbandonAfterUtc = acceptedAt.AddMinutes(15)
            }
        });
        Services.AddSingleton(walletService.Object);
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() =>
        {
            Assert.True(DateTimeOffset.UtcNow - acceptedAt <= TimeSpan.FromMinutes(1));
            Assert.Contains("R 95.00", cut.Markup);
            Assert.Contains("Completed", cut.Markup);
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
