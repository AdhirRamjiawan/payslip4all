using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Domain.Enums;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages;

public class WalletTopUpReturnTests : TestContext
{
    [Fact]
    public void ReturnPage_ShowsSuccessfulSettlementState()
    {
        var userId = SetAuthorizedOwner();
        var attemptId = Guid.NewGuid();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletTopUpService
            .Setup(s => s.GetAttemptResultAsync(attemptId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinalizedWalletTopUpResultDto
            {
                WalletTopUpAttemptId = attemptId,
                Status = WalletTopUpAttemptStatus.Completed,
                RequestedAmount = 100m,
                ConfirmedChargedAmount = 95m,
                WalletBalance = 95m,
                CreditedWallet = true,
                DisplayMessage = "Wallet credited successfully."
            });

        Services.AddSingleton(walletTopUpService.Object);
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"http://localhost/portal/wallet/top-ups/{attemptId}/return");

        var cut = RenderComponent<Payslip4All.Web.Pages.WalletTopUpReturn>(parameters => parameters
            .Add(p => p.AttemptId, attemptId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Wallet credited successfully.", cut.Markup);
            Assert.Contains("R 95.00", cut.Markup);
        });
    }

    [Theory]
    [InlineData(WalletTopUpAttemptStatus.Pending, "Payment is still pending.")]
    [InlineData(WalletTopUpAttemptStatus.Unverified, "Manual review required.")]
    [InlineData(WalletTopUpAttemptStatus.Abandoned, "abandoned")]
    public void ReturnPage_ShowsExplicitStates(WalletTopUpAttemptStatus status, string message)
    {
        var userId = SetAuthorizedOwner();
        var attemptId = Guid.NewGuid();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletTopUpService
            .Setup(s => s.GetAttemptResultAsync(attemptId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinalizedWalletTopUpResultDto
            {
                WalletTopUpAttemptId = attemptId,
                Status = status,
                RequestedAmount = 100m,
                WalletBalance = 0m,
                CreditedWallet = false,
                DisplayMessage = message
            });

        Services.AddSingleton(walletTopUpService.Object);
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"http://localhost/portal/wallet/top-ups/{attemptId}/return");

        var cut = RenderComponent<Payslip4All.Web.Pages.WalletTopUpReturn>(parameters => parameters
            .Add(p => p.AttemptId, attemptId));

        cut.WaitForAssertion(() => Assert.Contains(message, cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReturnPage_ShowsSanitizedAccessDeniedMessage()
    {
        var userId = SetAuthorizedOwner();
        var attemptId = Guid.NewGuid();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletTopUpService
            .Setup(s => s.GetAttemptResultAsync(attemptId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinalizedWalletTopUpResultDto?)null);

        Services.AddSingleton(walletTopUpService.Object);
        var cut = RenderComponent<Payslip4All.Web.Pages.WalletTopUpReturn>(parameters => parameters
            .Add(p => p.AttemptId, attemptId));

        cut.WaitForAssertion(() => Assert.Contains("could not be found", cut.Markup, StringComparison.OrdinalIgnoreCase));
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
