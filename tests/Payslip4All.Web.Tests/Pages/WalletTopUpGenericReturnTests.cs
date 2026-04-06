using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages;

public class WalletTopUpGenericReturnTests : TestContext
{
    [Fact]
    public void GenericReturnPage_WhenAttemptIdIsPresent_FallsBackToAttemptResultRoute()
    {
        var userId = SetAuthorizedOwner();
        var attemptId = Guid.NewGuid();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletTopUpService
            .Setup(s => s.ProcessGenericReturnAsync(userId, It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenericHostedReturnResultDto
            {
                IsMatched = false,
                DisplayMessage = "Top-up not confirmed",
                UnmatchedRecordId = Guid.NewGuid()
            });

        Services.AddSingleton(walletTopUpService.Object);
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"http://localhost/portal/wallet/top-ups/return?provider=payfast&attemptId={attemptId:D}");

        RenderComponent<Payslip4All.Web.Pages.WalletTopUpGenericReturn>();

        Assert.Equal($"http://localhost/portal/wallet/top-ups/{attemptId:D}/return", nav.Uri);
    }

    [Fact]
    public void GenericReturnPage_UsesPayFastProviderQuery_AndStaysGeneric_WhenAttemptIdIsMissing()
    {
        var userId = SetAuthorizedOwner();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletTopUpService
            .Setup(s => s.ProcessGenericReturnAsync(userId, It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenericHostedReturnResultDto
            {
                IsMatched = false,
                DisplayMessage = "Top-up not confirmed",
                UnmatchedRecordId = Guid.NewGuid()
            });

        Services.AddSingleton(walletTopUpService.Object);
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/portal/wallet/top-ups/return?provider=payfast");

        var cut = RenderComponent<Payslip4All.Web.Pages.WalletTopUpGenericReturn>();

        Assert.StartsWith("http://localhost/portal/wallet/top-ups/return/not-confirmed?ref=", nav.Uri, StringComparison.Ordinal);
        Assert.Contains("Top-up not confirmed", cut.Markup);
        walletTopUpService.Verify(s => s.ProcessGenericReturnAsync(
            userId,
            It.Is<IReadOnlyDictionary<string, string>>(payload => payload.ContainsKey("provider") && payload["provider"] == "payfast"),
            It.IsAny<CancellationToken>()), Times.Once);
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
