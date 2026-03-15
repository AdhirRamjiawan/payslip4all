using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.Interfaces;
using Payslip4All.Infrastructure.Persistence;
using System.IO;
using System.Net;

namespace Payslip4All.Web.Tests.Security;

/// <summary>
/// T076 — HTTP cross-employer ownership security tests.
/// Verifies that the PDF download endpoint returns HTTP 404 (not 403) when a
/// CompanyOwner attempts to download a payslip belonging to another employer.
///
/// Per FR-008 and plan.md: ownership failures MUST return 404 to prevent data-
/// existence leakage. Returning 403 would confirm the resource exists.
/// </summary>
public class CrossEmployerHttpTests : IClassFixture<SecurityTestWebApplicationFactory>
{
    private readonly SecurityTestWebApplicationFactory _factory;

    public CrossEmployerHttpTests(SecurityTestWebApplicationFactory factory)
        => _factory = factory;

    [Fact]
    public async Task GetPayslipPdf_UnauthenticatedUser_Returns401Or302()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var payslipId = Guid.NewGuid();
        var response = await client.GetAsync($"/payslips/{payslipId}/download");

        // Cookie auth redirects to /login (302) for unauthenticated requests.
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 302 or 401 for unauthenticated request, got {response.StatusCode}");
    }

    [Fact]
    public async Task GetPayslipPdf_WrongOwner_Returns404_NotForbidden()
    {
        // Arrange — mock IPayslipService to return null (simulates ownership mismatch
        // where GetPdfAsync returns null when userId does not own the payslip).
        var mockPayslipSvc = new Mock<IPayslipService>();
        mockPayslipSvc
            .Setup(s => s.GetPdfAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((byte[]?)null);    // null = not found / not owner

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace IPayslipService with our mock.
                services.AddScoped(_ => mockPayslipSvc.Object);
            });
        })
        .CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Attach a valid CompanyOwner cookie so auth passes.
        client.DefaultRequestHeaders.Add("Cookie",
            SecurityTestWebApplicationFactory.BuildAuthCookie());

        var payslipId = Guid.NewGuid();
        var response = await client.GetAsync($"/payslips/{payslipId}/download");

        // Must be 404 — never 403. Returning 403 would confirm the resource exists.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPayslipPdf_CorrectOwner_Returns200WithPdfBytes()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF header

        var mockPayslipSvc = new Mock<IPayslipService>();
        mockPayslipSvc
            .Setup(s => s.GetPdfAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(pdfBytes);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockPayslipSvc.Object);
            });
        })
        .CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add("Cookie",
            SecurityTestWebApplicationFactory.BuildAuthCookie());

        var payslipId = Guid.NewGuid();
        var response = await client.GetAsync($"/payslips/{payslipId}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetPayslipPdf_MalformedGuid_DoesNotReturnPdfContent()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("Cookie",
            SecurityTestWebApplicationFactory.BuildAuthCookie());

        // A non-GUID path segment does NOT satisfy the {payslipId:guid} route constraint.
        // In Blazor Server, unmatched routes fall through to the MapFallbackToPage("/_Host")
        // which returns 200 with the Blazor shell HTML — not a PDF.
        var response = await client.GetAsync("/payslips/not-a-guid/download");

        // We don't assert status code here because Blazor fallback returns 200.
        // What matters is: the response is NOT a PDF (no PDF content-type).
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        Assert.NotEqual("application/pdf", contentType);
    }
}

/// <summary>
/// Custom WebApplicationFactory for security tests:
/// • Temp SQLite file DB (no external DB required)
/// • Fake cookie authentication scheme that authenticates any request bearing
///   the test cookie without requiring a real login round-trip
/// </summary>
public class SecurityTestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    internal const string TestScheme = "TestAuth";
    internal const string TestCookieName = "TestAuthCookie";
    internal const string TestUserId = "11111111-1111-1111-1111-111111111111";

    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"p4a_sec_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Replace real DB with temp SQLite file.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PayslipDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<PayslipDbContext>(opts =>
                opts.UseSqlite($"Data Source={_dbPath}"));
        });

        builder.ConfigureTestServices(services =>
        {
            // Add a test authentication handler that reads the test cookie header
            // and issues a CompanyOwner ClaimsPrincipal so the [Authorize] attribute passes.
            services.AddAuthentication(defaultScheme: TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestScheme, _ => { });
        });

        builder.UseSetting("DatabaseProvider", "sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
    }

    /// <summary>
    /// Builds a fake cookie header value that TestAuthHandler recognises as authenticated.
    /// </summary>
    internal static string BuildAuthCookie()
        => $"{TestCookieName}=valid-test-token";

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}

/// <summary>Minimal authentication handler for integration tests.</summary>
internal sealed class TestAuthHandler
    : Microsoft.AspNetCore.Authentication.AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Only authenticate requests that carry the test cookie.
        if (!Request.Headers.TryGetValue("Cookie", out var cookieHeader) ||
            !cookieHeader.ToString().Contains(SecurityTestWebApplicationFactory.TestCookieName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                SecurityTestWebApplicationFactory.TestUserId),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Role, "CompanyOwner"),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Email, "test@security.test")
        };

        var identity  = new System.Security.Claims.ClaimsIdentity(
            claims, SecurityTestWebApplicationFactory.TestScheme);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(
            principal, SecurityTestWebApplicationFactory.TestScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
