using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Domain.Enums;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages.Admin;

public class WalletTopUpReviewTests : TestContext
{
    [Fact]
    public void WalletTopUpReview_DisplaysPrivacyMinimizedAdminRows()
    {
        SetAuthorizedAdmin();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletTopUpService
            .Setup(s => s.GetAdminReviewAsync(It.IsAny<SiteAdministratorPaymentReviewQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SiteAdministratorPaymentReviewDto
                {
                    WalletTopUpAttemptId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    OwnerUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Status = WalletTopUpAttemptStatus.Completed,
                    PaymentConfirmationRecordId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    DecisionType = "EvidenceEvaluation",
                    DecisionReasonCode = "confirmed",
                    DecisionSummary = "Payment confirmation record accepted.",
                    MerchantPaymentReference = "merchant-123",
                    ProviderPaymentReference = "pf-123",
                    WalletActivityId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    CreditedWallet = true,
                    SafePayloadSnapshot = "PAN=4111"
                }
            });
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Admin.WalletTopUpReview>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Wallet Top-Up Review", cut.Markup);
            Assert.Contains("Payment confirmation record accepted.", cut.Markup);
            Assert.Contains("merchant-123", cut.Markup);
            Assert.Contains("pf-123", cut.Markup);
            Assert.Contains("Wallet activity", cut.Markup);
            Assert.DoesNotContain("PAN=4111", cut.Markup);
        });
    }

    [Fact]
    public void WalletTopUpReview_NonAdminUser_IsDeniedWithoutCallingService()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("owner@test.com");
        authContext.SetRoles("CompanyOwner");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var walletTopUpService = new Mock<IWalletTopUpService>();
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Admin.WalletTopUpReview>();

        cut.WaitForAssertion(() => Assert.Contains("Access denied.", cut.Markup));
        walletTopUpService.Verify(s => s.GetAdminReviewAsync(It.IsAny<SiteAdministratorPaymentReviewQueryDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void WalletTopUpReview_RendersConflictAndUnmatchedRowsSafely()
    {
        SetAuthorizedAdmin();
        var walletTopUpService = new Mock<IWalletTopUpService>();
        walletTopUpService
            .Setup(s => s.GetAdminReviewAsync(It.IsAny<SiteAdministratorPaymentReviewQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SiteAdministratorPaymentReviewDto
                {
                    WalletTopUpAttemptId = Guid.NewGuid(),
                    Status = WalletTopUpAttemptStatus.Completed,
                    ConflictWithAcceptedFinalOutcome = true,
                    DecisionSummary = "Conflicting late evidence retained.",
                    MerchantPaymentReference = "merchant-conflict",
                    ProviderPaymentReference = "pf-conflict"
                },
                new SiteAdministratorPaymentReviewDto
                {
                    IsUnmatchedReturn = true,
                    UnmatchedPaymentReturnRecordId = Guid.NewGuid(),
                    DecisionSummary = "Top-up not confirmed",
                    CorrelationDisposition = "NoMatch",
                    SafePayloadSnapshot = "{\"safe\":true}"
                }
            });
        Services.AddSingleton(walletTopUpService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Admin.WalletTopUpReview>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Conflict retained for internal review.", cut.Markup);
            Assert.Contains("Unmatched", cut.Markup);
            Assert.Contains("{\"safe\":true}", cut.Markup);
            Assert.DoesNotContain("CVV", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    private void SetAuthorizedAdmin()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@test.com");
        authContext.SetRoles("SiteAdministrator");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
    }
}
