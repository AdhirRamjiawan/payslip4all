using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Payslip4All.Web.Tests.Infrastructure;
using Payslip4All.Web.Tests.Startup;

namespace Payslip4All.Web.Tests.Integration;

[Collection("WebIntegration")]
public class ReverseProxyFailureTests
{
    [Fact]
    public async Task GatewayMode_RejectsRequests_ForUnexpectedHosts()
    {
        using var certificate = TestTlsCertificate.Create();
        using var factory = BuildGatewayFactory("http://127.0.0.1:8080", certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.BaseAddress = new Uri("http://unexpected.example.com");

        using var response = await client.GetAsync("/");

        ReverseProxyContractAssertions.AssertWrongHost(response.StatusCode);
    }

    [Fact]
    public async Task GatewayMode_MapsUnreachableUpstreamToExactGeneric503WithinTenSeconds()
    {
        using var certificate = TestTlsCertificate.Create();
        var unreachablePort = ReverseProxyTestSupport.GetUnusedLoopbackPort();
        using var factory = BuildGatewayFactory($"http://127.0.0.1:{unreachablePort}", certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.BaseAddress = new Uri("https://payslip4all.co.za");

        var stopwatch = Stopwatch.StartNew();
        using var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        stopwatch.Stop();

        ReverseProxyContractAssertions.AssertServiceUnavailable(response.StatusCode, body);
        ReverseProxyContractAssertions.AssertCompletedWithin(stopwatch, TimeSpan.FromSeconds(10));
    }

    private static WebApplicationFactory<Program> BuildGatewayFactory(string upstreamBaseUrl, TestTlsCertificate certificate)
    {
        return new TestWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("REVERSE_PROXY_ENABLED", "true");
            builder.UseSetting("REVERSE_PROXY_PUBLIC_HOST", "payslip4all.co.za");
            builder.UseSetting("REVERSE_PROXY_UPSTREAM_BASE_URL", upstreamBaseUrl);
            builder.UseSetting("Kestrel:Certificates:Default:Path", certificate.CertificatePath);
            builder.UseSetting("Kestrel:Certificates:Default:Password", certificate.Password);
        });
    }
}
