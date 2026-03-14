using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Company;
using Payslip4All.Application.DTOs.Employee;
using Payslip4All.Application.Interfaces;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages;

public class CompanyDetailTests : TestContext
{
    [Fact]
    public async Task CompanyDetail_ShowsCompanyName_WhenLoaded()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        var userId = Guid.NewGuid();
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var companyId = Guid.NewGuid();
        var mockCompanyService = new Mock<ICompanyService>();
        mockCompanyService.Setup(s => s.GetCompanyByIdAsync(companyId, userId))
            .ReturnsAsync(new CompanyDto { Id = companyId, Name = "Test Company", UserId = userId });
        Services.AddSingleton(mockCompanyService.Object);

        var mockEmployeeService = new Mock<IEmployeeService>();
        mockEmployeeService.Setup(s => s.GetEmployeesForCompanyAsync(companyId, userId))
            .ReturnsAsync(new List<EmployeeDto>());
        Services.AddSingleton(mockEmployeeService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Companies.CompanyDetail>(
            p => p.Add(c => c.CompanyId, companyId));
        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.Contains("Test Company", cut.Markup);
    }

    [Fact]
    public void CompanyDetail_ShowsLoadingSpinner_Initially()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var tcs = new TaskCompletionSource<CompanyDto?>();
        var mockCompanyService = new Mock<ICompanyService>();
        mockCompanyService.Setup(s => s.GetCompanyByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(tcs.Task);
        Services.AddSingleton(mockCompanyService.Object);

        var mockEmployeeService = new Mock<IEmployeeService>();
        Services.AddSingleton(mockEmployeeService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Companies.CompanyDetail>(
            p => p.Add(c => c.CompanyId, Guid.NewGuid()));

        Assert.Contains("spinner-border", cut.Markup);
    }
}
