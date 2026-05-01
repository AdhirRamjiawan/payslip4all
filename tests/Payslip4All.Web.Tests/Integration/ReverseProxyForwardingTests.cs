using System.Net;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Payslip4All.Web.Tests.Infrastructure;
using Payslip4All.Web.Tests.Startup;

namespace Payslip4All.Web.Tests.Integration;

[Collection("WebIntegration")]
public class ReverseProxyForwardingTests
{
    [Fact]
    public async Task GatewayMode_ForwardsPublicHttpsScheme_And_HostHeaders_ToTheUpstreamApp()
    {
        await using var upstream = await ReverseProxyTestSupport.UpstreamProbe.StartAsync(app =>
        {
            app.MapGet("/__proxy-test", (HttpContext context) => Results.Json(new
            {
                scheme = context.Request.Scheme,
                host = context.Request.Host.Value,
                forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString(),
                forwardedHost = context.Request.Headers["X-Forwarded-Host"].ToString()
            }));
        });

        using var certificate = TestTlsCertificate.Create();
        using var factory = BuildGatewayFactory(upstream.BaseUrl, certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.BaseAddress = new Uri("https://payslip4all.co.za");

        using var response = await client.GetAsync("/__proxy-test");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"scheme\":\"https\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"host\":\"payslip4all.co.za\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GatewayMode_RedirectsCorrectHostHttpToHttpsInASingleStep()
    {
        await using var upstream = await ReverseProxyTestSupport.UpstreamProbe.StartAsync(app =>
        {
            app.MapGet("/", () => Results.Ok("upstream-should-not-be-hit"));
        });

        using var certificate = TestTlsCertificate.Create();
        using var factory = BuildGatewayFactory(upstream.BaseUrl, certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.BaseAddress = new Uri("http://payslip4all.co.za");

        using var response = await client.GetAsync("/");

        Assert.Contains((int)response.StatusCode, new[] { 301, 302, 307, 308 });
        Assert.Equal("https://payslip4all.co.za/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GatewayMode_WhenConfiguredForHttpOnly_ForwardsHttpRequestsWithoutRedirect()
    {
        await using var upstream = await ReverseProxyTestSupport.UpstreamProbe.StartAsync(app =>
        {
            app.MapGet("/", (HttpContext context) => Results.Json(new
            {
                scheme = context.Request.Scheme,
                host = context.Request.Host.Value
            }));
        });

        using var factory = BuildGatewayFactory(upstream.BaseUrl, certificate: null, listenUrls: "http://0.0.0.0:80");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.BaseAddress = new Uri("http://payslip4all.co.za");

        using var response = await client.GetAsync("/");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"scheme\":\"http\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"host\":\"payslip4all.co.za\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GatewayMode_AllowsThreeConsecutiveHealthRequestsWithinFiveSecondsEach()
    {
        await using var upstream = await ReverseProxyTestSupport.UpstreamProbe.StartAsync(app =>
        {
            app.MapGet("/health", () => Results.Json(new { status = "Healthy" }));
        });

        using var certificate = TestTlsCertificate.Create();
        using var factory = BuildGatewayFactory(upstream.BaseUrl, certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.BaseAddress = new Uri("https://payslip4all.co.za");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            using var response = await client.GetAsync("/health");
            var body = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"status\":\"Healthy\"", body, StringComparison.Ordinal);
            ReverseProxyContractAssertions.AssertCompletedWithin(stopwatch, TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task GatewayMode_PreservesAbsoluteRedirectsOnThePublicHttpsHost()
    {
        await using var upstream = await ReverseProxyTestSupport.UpstreamProbe.StartAsync(app =>
        {
            app.MapGet("/redirect-me", (HttpContext context) =>
                Results.Redirect($"{context.Request.Scheme}://{context.Request.Host}/after-login"));
        });

        using var certificate = TestTlsCertificate.Create();
        using var factory = BuildGatewayFactory(upstream.BaseUrl, certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.BaseAddress = new Uri("https://payslip4all.co.za");

        using var response = await client.GetAsync("/redirect-me");

        Assert.Contains((int)response.StatusCode, new[] { 301, 302, 307, 308 });
        Assert.Equal("https://payslip4all.co.za/after-login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GatewayMode_PreservesSignalRNegotiationBindingOnThePublicHost()
    {
        await using var upstream = await ReverseProxyTestSupport.UpstreamProbe.StartAsync(app =>
        {
            app.MapPost("/_blazor/negotiate", (HttpContext context) => Results.Json(new
            {
                scheme = context.Request.Scheme,
                host = context.Request.Host.Value
            }));
        });

        using var certificate = TestTlsCertificate.Create();
        using var factory = BuildGatewayFactory(upstream.BaseUrl, certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.BaseAddress = new Uri("https://payslip4all.co.za");

        using var response = await client.PostAsync("/_blazor/negotiate?negotiateVersion=1", new StringContent(string.Empty));
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"scheme\":\"https\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"host\":\"payslip4all.co.za\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GatewayMode_PreservesFormSubmissionNavigationOnThePublicHost()
    {
        await using var upstream = await ReverseProxyTestSupport.UpstreamProbe.StartAsync(app =>
        {
            app.MapPost("/submit-form", async (HttpContext context) =>
            {
                _ = await context.Request.ReadFormAsync();
                return Results.Redirect($"{context.Request.Scheme}://{context.Request.Host}/after-form");
            });
        });

        using var certificate = TestTlsCertificate.Create();
        using var factory = BuildGatewayFactory(upstream.BaseUrl, certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.BaseAddress = new Uri("https://payslip4all.co.za");

        using var response = await client.PostAsync(
            "/submit-form",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["employeeId"] = "123" }));

        Assert.Contains((int)response.StatusCode, new[] { 301, 302, 307, 308 });
        Assert.Equal("https://payslip4all.co.za/after-form", response.Headers.Location?.ToString());
    }

    private static WebApplicationFactory<Program> BuildGatewayFactory(
        string upstreamBaseUrl,
        TestTlsCertificate? certificate,
        string? listenUrls = null)
    {
        return new TestWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("REVERSE_PROXY_ENABLED", "true");
            builder.UseSetting("REVERSE_PROXY_PUBLIC_HOST", "payslip4all.co.za");
            builder.UseSetting("REVERSE_PROXY_UPSTREAM_BASE_URL", upstreamBaseUrl);
            if (!string.IsNullOrWhiteSpace(listenUrls))
                builder.UseSetting("ASPNETCORE_URLS", listenUrls);

            if (certificate is not null)
            {
                builder.UseSetting("Kestrel:Certificates:Default:Path", certificate.CertificatePath);
                builder.UseSetting("Kestrel:Certificates:Default:Password", certificate.Password);
            }
        });
    }
}
