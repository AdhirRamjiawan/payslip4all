using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Company;
using Payslip4All.Application.Interfaces;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages;

public class DashboardTests : TestContext
{
    [Fact]
    public void Dashboard_ShowsLoadingSpinnerInitially()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var tcs = new TaskCompletionSource<IReadOnlyList<CompanyDto>>();
        var mockService = new Mock<ICompanyService>();
        mockService.Setup(s => s.GetCompaniesForUserAsync(It.IsAny<Guid>()))
            .Returns(tcs.Task);
        Services.AddSingleton(mockService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Dashboard>();

        Assert.Contains("spinner-border", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_ShowsCompanyCards_WhenLoaded()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var companies = new List<CompanyDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Acme Corp", Address = "123 Street", EmployeeCount = 5 }
        };
        var mockService = new Mock<ICompanyService>();
        mockService.Setup(s => s.GetCompaniesForUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(companies);
        Services.AddSingleton(mockService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Dashboard>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.Contains("Acme Corp", cut.Markup);
        Assert.Contains("5 employees", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_ShowsEmptyState_WhenNoCompanies()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var mockService = new Mock<ICompanyService>();
        mockService.Setup(s => s.GetCompaniesForUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<CompanyDto>());
        Services.AddSingleton(mockService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Dashboard>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.Contains("No companies yet", cut.Markup);
        Assert.Contains("Add Your First Company", cut.Markup);
    }
}
