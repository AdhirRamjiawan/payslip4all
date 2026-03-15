using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Employee;
using Payslip4All.Application.DTOs.Payslip;
using Payslip4All.Application.Interfaces;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages;

public class GeneratePayslipTests : TestContext
{
    [Fact]
    public async Task GeneratePayslip_ShowsPreviewForm_Initially()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        var userId = Guid.NewGuid();
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var employeeId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var mockPayslipService = new Mock<IPayslipService>();
        Services.AddSingleton(mockPayslipService.Object);

        var mockEmployeeService = new Mock<IEmployeeService>();
        mockEmployeeService.Setup(s => s.GetEmployeeByIdAsync(employeeId, userId))
            .ReturnsAsync(new EmployeeDto
            {
                Id = employeeId, FirstName = "John", LastName = "Doe",
                IdNumber = "123", EmployeeNumber = "E001", Occupation = "Dev",
                MonthlyGrossSalary = 15000, CompanyId = companyId
            });
        Services.AddSingleton(mockEmployeeService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Payslips.GeneratePayslip>(p => p
            .Add(c => c.CompanyId, companyId)
            .Add(c => c.EmployeeId, employeeId));
        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.Contains("Preview", cut.Markup);
    }
}
