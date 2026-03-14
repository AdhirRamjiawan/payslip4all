using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Employee;
using Payslip4All.Application.DTOs.Loan;
using Payslip4All.Application.Interfaces;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages;

public class EmployeeDetailTests : TestContext
{
    [Fact]
    public async Task EmployeeDetail_ShowsEmployeeName_WhenLoaded()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        var userId = Guid.NewGuid();
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var employeeId = Guid.NewGuid();
        var mockEmployeeService = new Mock<IEmployeeService>();
        mockEmployeeService.Setup(s => s.GetEmployeeByIdAsync(employeeId, userId))
            .ReturnsAsync(new EmployeeDto
            {
                Id = employeeId, FirstName = "Jane", LastName = "Smith",
                IdNumber = "123", EmployeeNumber = "E001", Occupation = "Developer",
                MonthlyGrossSalary = 25000, CompanyId = Guid.NewGuid()
            });
        Services.AddSingleton(mockEmployeeService.Object);

        var mockLoanService = new Mock<ILoanService>();
        mockLoanService.Setup(s => s.GetLoansForEmployeeAsync(employeeId, userId))
            .ReturnsAsync(new List<LoanDto>());
        Services.AddSingleton(mockLoanService.Object);

        var mockPayslipService = new Mock<IPayslipService>();
        mockPayslipService.Setup(s => s.GetPayslipsForEmployeeAsync(employeeId, userId))
            .ReturnsAsync(new List<Payslip4All.Application.DTOs.Payslip.PayslipDto>());
        Services.AddSingleton(mockPayslipService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Employees.EmployeeDetail>(
            p => p.Add(c => c.EmployeeId, employeeId));
        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.Contains("Jane", cut.Markup);
        Assert.Contains("Smith", cut.Markup);
    }

    [Fact]
    public void EmployeeDetail_ShowsLoadingSpinner_Initially()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var tcs = new TaskCompletionSource<EmployeeDto?>();
        var mockEmployeeService = new Mock<IEmployeeService>();
        mockEmployeeService.Setup(s => s.GetEmployeeByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(tcs.Task);
        Services.AddSingleton(mockEmployeeService.Object);

        var mockLoanService = new Mock<ILoanService>();
        Services.AddSingleton(mockLoanService.Object);

        var mockPayslipService = new Mock<IPayslipService>();
        Services.AddSingleton(mockPayslipService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Employees.EmployeeDetail>(
            p => p.Add(c => c.EmployeeId, Guid.NewGuid()));

        Assert.Contains("spinner-border", cut.Markup);
    }
}
