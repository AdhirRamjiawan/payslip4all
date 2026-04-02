using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
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
    public void Wallet_SubmittingTopUp_RedirectsToHostedPage()
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
        cut.Find("input").Change("25");
        cut.Find("form").Submit();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.Equal("http://localhost/hosted-payments/fake", nav.Uri);
        walletTopUpService.Verify(s => s.StartHostedTopUpAsync(It.Is<StartWalletTopUpCommand>(c =>
            c.RequestedAmount == 25m && c.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Wallet_ShowsActivityList()
    {
        var userId = SetAuthorizedOwner();
        var walletService = new Mock<IWalletService>();
        var walletTopUpService = new Mock<IWalletTopUpService>();
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
        walletTopUpService.Setup(s => s.GetHistoryAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<WalletTopUpAttemptDto>());
        Services.AddSingleton(walletService.Object);
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Top up", cut.Markup);
            Assert.Contains("Credit", cut.Markup);
        });
    }

    [Fact]
    public void Wallet_DisplaysActivityTimestampsInUtc()
    {
        var userId = SetAuthorizedOwner();
        var walletService = new Mock<IWalletService>();
        var walletTopUpService = new Mock<IWalletTopUpService>();
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
                    OccurredAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.FromHours(2))
                }
            }
        });
        walletTopUpService.Setup(s => s.GetHistoryAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<WalletTopUpAttemptDto>());
        Services.AddSingleton(walletService.Object);
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("2026-04-01 10:00 UTC", cut.Markup);
        });
    }

    [Fact]
    public void Wallet_DoesNotSubmit_WhenAmountIsInvalid()
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
        Services.AddSingleton(walletService.Object);
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() => Assert.Contains("Add Wallet Credits", cut.Markup));
        cut.Find("form").Submit();

        walletTopUpService.Verify(s => s.StartHostedTopUpAsync(It.IsAny<StartWalletTopUpCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Wallet_ShowsTopUpHistory()
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
                Status = WalletTopUpAttemptStatus.Completed,
                RequestedAmount = 100m,
                ConfirmedChargedAmount = 95m,
                CreatedAt = DateTimeOffset.UtcNow
            }
        });
        Services.AddSingleton(walletService.Object);
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Wallet>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Top-up History", cut.Markup);
            Assert.Contains("Completed", cut.Markup);
            Assert.Contains("95.00", cut.Markup);
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
