using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Payslip4All.Web.Tests.Integration;

/// <summary>
/// Integration tests verifying that all wwwroot static assets are served correctly
/// in both Development and Production hosting environments.
///
/// Key scenario under test: Payslip4All.Web.styles.css is a build-time-generated
/// CSS isolation bundle that does NOT exist in the source wwwroot/ directory.
/// It is only available via the static web assets manifest (.staticwebassets.runtime.json).
/// Without an explicit UseStaticWebAssets() call, this file is inaccessible in
/// non-Development environments, causing styles to fail to load silently.
///
/// Placed in the "WebIntegration" collection alongside LoggingIntegrationTests
/// to prevent parallel factory startup from causing Serilog global-state races.
/// </summary>
[Collection("WebIntegration")]
public class StaticFilesIntegrationTests
{
    private static WebApplicationFactory<Program> BuildFactory(string dbPath, string? environment = null)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            if (environment is not null)
            {
                builder.UseEnvironment(environment);
            }

            builder.UseSetting("PERSISTENCE_PROVIDER", "sqlite");
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");
        });
    }

    [Fact]
    public async Task BootstrapCss_IsServed_InDevelopmentEnvironment()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"payslip4all-static-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = BuildFactory(dbPath);
            var client = factory.CreateClient();

            var response = await client.GetAsync("/css/bootstrap/bootstrap.min.css");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("text/css", response.Content.Headers.ContentType?.MediaType ?? "");
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
    public async Task SiteCss_IsServed_InDevelopmentEnvironment()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"payslip4all-static-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = BuildFactory(dbPath);
            var client = factory.CreateClient();

            var response = await client.GetAsync("/css/site.css");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("text/css", response.Content.Headers.ContentType?.MediaType ?? "");
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
    public async Task Favicon_IsServed_InDevelopmentEnvironment()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"payslip4all-static-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = BuildFactory(dbPath);
            var client = factory.CreateClient();

            var response = await client.GetAsync("/favicon.png");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("image/png", response.Content.Headers.ContentType?.MediaType ?? "");
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
    public async Task CssIsolationBundle_IsServed_InDevelopmentEnvironment()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"payslip4all-static-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = BuildFactory(dbPath);
            var client = factory.CreateClient();

            var response = await client.GetAsync("/Payslip4All.Web.styles.css");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("text/css", response.Content.Headers.ContentType?.MediaType ?? "");
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    /// <summary>
    /// This is the critical RED test: in Production environment the CSS isolation bundle
    /// MUST still be served. Without UseStaticWebAssets() this test fails because the
    /// static web assets manifest is not loaded outside Development mode.
    /// </summary>
    [Fact]
    public async Task CssIsolationBundle_IsServed_InProductionEnvironment()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"payslip4all-static-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = BuildFactory(dbPath, "Production");
            var client = factory.CreateClient();

            var response = await client.GetAsync("/Payslip4All.Web.styles.css");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("text/css", response.Content.Headers.ContentType?.MediaType ?? "");
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}
