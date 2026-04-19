using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Payslip4All.Web.Tests.Startup;

namespace Payslip4All.Web.Tests.Integration;

[Collection("WebIntegration")]
public class ReverseProxyForwardingTests
{
    [Fact]
    public async Task ForwardedHeaders_PreservePublicHttpsScheme_And_ForwardedHost()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/__proxy-test");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "payslip4all.co.za");
        request.Headers.Host = "127.0.0.1";

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"scheme\":\"https\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"host\":\"payslip4all.co.za\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthEndpoint_RemainsPublic_WhenForwardedThroughTheGatewayHost()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "payslip4all.co.za");
        request.Headers.Host = "127.0.0.1";

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"Healthy\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public void GatewayConfig_MapsUpstreamFailuresToGeneric503WithoutInternalAddressLeak()
    {
        var config = File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "nginx", "payslip4all.conf"));

        Assert.Contains("error_page 502 503 504 =503 /503.html;", config, StringComparison.Ordinal);
        Assert.Contains("return 503 \"Service temporarily unavailable.", config, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1:8080", ExtractUnavailableResponse(config), StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> BuildFactory()
    {
        return new TestWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter, ProxyEchoStartupFilter>();
            });
        });
    }

    private static string ExtractUnavailableResponse(string config)
    {
        const string marker = "return 503 \"";
        var start = config.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return string.Empty;

        start += marker.Length;
        var end = config.IndexOf("\";", start, StringComparison.Ordinal);
        return end < 0 ? config[start..] : config[start..end];
    }

    private static string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir is not null && !File.Exists(Path.Combine(currentDir, "Payslip4All.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        return currentDir ?? throw new InvalidOperationException("Could not find solution root.");
    }

    private sealed class ProxyEchoStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Path == "/__proxy-test")
                    {
                        var options = app.ApplicationServices
                            .GetRequiredService<IOptions<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>>()
                            .Value;

                        if (options.ForwardedHeaders.HasFlag(Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto))
                        {
                            var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString();
                            if (!string.IsNullOrWhiteSpace(forwardedProto))
                                context.Request.Scheme = forwardedProto;
                        }

                        if (options.ForwardedHeaders.HasFlag(Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost))
                        {
                            var forwardedHost = context.Request.Headers["X-Forwarded-Host"].ToString();
                            if (!string.IsNullOrWhiteSpace(forwardedHost))
                                context.Request.Host = HostString.FromUriComponent(forwardedHost);
                        }

                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            scheme = context.Request.Scheme,
                            host = context.Request.Host.Value
                        });
                        return;
                    }

                    await nextMiddleware();
                });

                next(app);
            };
        }
    }
}
