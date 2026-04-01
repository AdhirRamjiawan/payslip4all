using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Payslip4All.Application.DTOs.Pricing;
using Payslip4All.Application.Interfaces;
using Payslip4All.Infrastructure.Persistence;

namespace Payslip4All.Web.Tests.Integration;

public class PublicLandingPageTests
{
    [Fact]
    public async Task GetRoot_AsAnonymous_ShowsWalletMessagingPriceAndCallsToAction()
    {
        var pricingService = new Mock<IPayslipPricingService>();
        pricingService.Setup(s => s.GetCurrentPriceAsync()).ReturnsAsync(new PayslipPricingSettingDto
        {
            PricePerPayslip = 15m,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedByUserId = "admin-1"
        });

        var dbPath = Path.Combine(Path.GetTempPath(), $"p4a_public_{Guid.NewGuid():N}.db");

        try
        {
            using var factory = BuildFactory(dbPath, pricingService.Object);
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync("/");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("wallet credits", html, StringComparison.OrdinalIgnoreCase);
            Assert.Matches(new Regex(@"R\s*15[\.,]00"), html);
            Assert.Contains("/Portal/Auth/Register", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/Portal/Auth/Login", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Current Balance", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Recent Activity", html, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public async Task GetRoot_WhenPricingIsUnavailable_ShowsFallbackMessageWithoutPrivateWalletData()
    {
        var pricingService = new Mock<IPayslipPricingService>();
        pricingService.Setup(s => s.GetCurrentPriceAsync()).ThrowsAsync(new InvalidOperationException("pricing unavailable"));

        var dbPath = Path.Combine(Path.GetTempPath(), $"p4a_public_{Guid.NewGuid():N}.db");

        try
        {
            using var factory = BuildFactory(dbPath, pricingService.Object);
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync("/");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("temporarily unavailable", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("wallet credits", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Wallet Summary", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Top Up Wallet", html, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private static WebApplicationFactory<Program> BuildFactory(string dbPath, IPayslipPricingService pricingService)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("PERSISTENCE_PROVIDER", "sqlite");
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<PayslipDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<PayslipDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                services.RemoveAll<IPayslipPricingService>();
                services.AddSingleton(pricingService);
            });
        });
    }
}
