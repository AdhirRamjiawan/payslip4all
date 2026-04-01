using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Employee;
using Payslip4All.Application.DTOs.Payslip;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using System.Text.RegularExpressions;
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
        var mockWalletService = new Mock<IWalletService>();
        mockWalletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 50m,
            CurrentPayslipPrice = 5m
        });
        Services.AddSingleton(mockWalletService.Object);

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
        Assert.Contains("Wallet Balance", cut.Markup);
        Assert.Contains("Current Price", cut.Markup);
    }

    [Fact]
    public async Task GeneratePayslip_ShowsInsufficientFundsWarning_WhenBalanceBelowPrice()
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

        var mockWalletService = new Mock<IWalletService>();
        mockWalletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 2m,
            CurrentPayslipPrice = 5m
        });
        Services.AddSingleton(mockWalletService.Object);

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

        Assert.Contains("too low", cut.Markup);
    }

    [Fact]
    public async Task GeneratePayslip_ShowsChargedAmountAfterSuccessfulGeneration()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        var userId = Guid.NewGuid();
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var employeeId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var mockPayslipService = new Mock<IPayslipService>();
        mockPayslipService.Setup(s => s.PreviewPayslipAsync(It.IsAny<PreviewPayslipQuery>()))
            .ReturnsAsync(new PayslipResult
            {
                Success = true,
                PayslipDto = new PayslipDto
                {
                    EmployeeId = employeeId,
                    GrossEarnings = 15000m,
                    UifDeduction = 150m,
                    TotalDeductions = 150m,
                    NetPay = 14850m
                }
            });
        mockPayslipService.Setup(s => s.GeneratePayslipAsync(It.IsAny<GeneratePayslipCommand>()))
            .ReturnsAsync(new PayslipResult
            {
                Success = true,
                ChargedAmount = 5m
            });
        Services.AddSingleton(mockPayslipService.Object);

        var mockWalletService = new Mock<IWalletService>();
        mockWalletService.SetupSequence(s => s.GetWalletAsync(userId))
            .ReturnsAsync(new WalletDto
            {
                UserId = userId,
                CurrentBalance = 50m,
                CurrentPayslipPrice = 5m
            })
            .ReturnsAsync(new WalletDto
            {
                UserId = userId,
                CurrentBalance = 45m,
                CurrentPayslipPrice = 5m
            });
        Services.AddSingleton(mockWalletService.Object);

        var mockEmployeeService = new Mock<IEmployeeService>();
        mockEmployeeService.Setup(s => s.GetEmployeeByIdAsync(employeeId, userId))
            .ReturnsAsync(new EmployeeDto
            {
                Id = employeeId,
                FirstName = "John",
                LastName = "Doe",
                IdNumber = "123",
                EmployeeNumber = "E001",
                Occupation = "Dev",
                MonthlyGrossSalary = 15000,
                CompanyId = companyId
            });
        Services.AddSingleton(mockEmployeeService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Payslips.GeneratePayslip>(p => p
            .Add(c => c.CompanyId, companyId)
            .Add(c => c.EmployeeId, employeeId));
        await cut.InvokeAsync(() => Task.CompletedTask);

        await cut.InvokeAsync(() => cut.Find("button.btn.btn-primary.mt-3").Click());
        cut.WaitForAssertion(() => Assert.Contains("Payslip Preview", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("button.btn.btn-success").Click());
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Payslip generated successfully", cut.Markup);
            Assert.Matches(new Regex(@"Wallet charged: R\s*5[\.,]00"), cut.Markup);
        });
    }

    [Fact]
    public async Task GeneratePayslip_KeepsGenerateButtonEnabled_WhenCachedWalletShowsInsufficientFunds()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        var userId = Guid.NewGuid();
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var employeeId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var mockPayslipService = new Mock<IPayslipService>();
        mockPayslipService.Setup(s => s.PreviewPayslipAsync(It.IsAny<PreviewPayslipQuery>()))
            .ReturnsAsync(new PayslipResult
            {
                Success = true,
                PayslipDto = new PayslipDto
                {
                    EmployeeId = employeeId,
                    GrossEarnings = 15000m,
                    UifDeduction = 150m,
                    TotalDeductions = 150m,
                    NetPay = 14850m
                }
            });
        Services.AddSingleton(mockPayslipService.Object);

        var mockWalletService = new Mock<IWalletService>();
        mockWalletService.Setup(s => s.GetWalletAsync(userId)).ReturnsAsync(new WalletDto
        {
            UserId = userId,
            CurrentBalance = 2m,
            CurrentPayslipPrice = 5m
        });
        Services.AddSingleton(mockWalletService.Object);

        var mockEmployeeService = new Mock<IEmployeeService>();
        mockEmployeeService.Setup(s => s.GetEmployeeByIdAsync(employeeId, userId))
            .ReturnsAsync(new EmployeeDto
            {
                Id = employeeId,
                FirstName = "John",
                LastName = "Doe",
                IdNumber = "123",
                EmployeeNumber = "E001",
                Occupation = "Dev",
                MonthlyGrossSalary = 15000,
                CompanyId = companyId
            });
        Services.AddSingleton(mockEmployeeService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Payslips.GeneratePayslip>(p => p
            .Add(c => c.CompanyId, companyId)
            .Add(c => c.EmployeeId, employeeId));
        await cut.InvokeAsync(() => Task.CompletedTask);

        await cut.InvokeAsync(() => cut.Find("button.btn.btn-primary.mt-3").Click());
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Insufficient funds", cut.Markup);
            Assert.False(cut.Find("button.btn.btn-success").HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task GeneratePayslip_DisablesWorkflow_WhenWalletLoadFails()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@test.com");
        authContext.SetRoles("CompanyOwner");
        var userId = Guid.NewGuid();
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var employeeId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        Services.AddSingleton(Mock.Of<IPayslipService>());

        var mockWalletService = new Mock<IWalletService>();
        mockWalletService.Setup(s => s.GetWalletAsync(userId)).ThrowsAsync(new InvalidOperationException("boom"));
        Services.AddSingleton(mockWalletService.Object);

        var mockEmployeeService = new Mock<IEmployeeService>();
        mockEmployeeService.Setup(s => s.GetEmployeeByIdAsync(employeeId, userId))
            .ReturnsAsync(new EmployeeDto
            {
                Id = employeeId,
                FirstName = "John",
                LastName = "Doe",
                IdNumber = "123",
                EmployeeNumber = "E001",
                Occupation = "Dev",
                MonthlyGrossSalary = 15000,
                CompanyId = companyId
            });
        Services.AddSingleton(mockEmployeeService.Object);

        var cut = RenderComponent<Payslip4All.Web.Pages.Payslips.GeneratePayslip>(p => p
            .Add(c => c.CompanyId, companyId)
            .Add(c => c.EmployeeId, employeeId));
        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.Contains("Wallet details are temporarily unavailable", cut.Markup);
        Assert.DoesNotContain("button.btn.btn-primary.mt-3", cut.Markup);
        Assert.DoesNotContain("boom", cut.Markup);
    }
}
