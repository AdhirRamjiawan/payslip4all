using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Payslip4All.Web.Tests.Pages;

public class WalletTopUpNotConfirmedTests : TestContext
{
    [Fact]
    public void NotConfirmedPage_ShowsGenericMessage_WithoutLeakingAttemptData()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/portal/wallet/top-ups/return/not-confirmed?ref=abc-123");

        var cut = RenderComponent<Payslip4All.Web.Pages.WalletTopUpNotConfirmed>();

        Assert.Contains("could not match", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("abc-123", cut.Markup);
        Assert.DoesNotContain("wallet balance", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
