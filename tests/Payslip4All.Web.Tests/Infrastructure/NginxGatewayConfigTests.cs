namespace Payslip4All.Web.Tests.Infrastructure;

public class NginxGatewayConfigTests
{
    [Fact]
    public void GatewayConfig_DefinesHttpsSiteForProductionDomain_And_HttpRedirect()
    {
        var config = LoadNginxConfig();

        Assert.Contains("server_name payslip4all.co.za;", config, StringComparison.Ordinal);
        Assert.Contains("listen 80;", config, StringComparison.Ordinal);
        Assert.Contains("listen 443 ssl http2;", config, StringComparison.Ordinal);
        Assert.Contains("return 301 https://payslip4all.co.za$request_uri;", config, StringComparison.Ordinal);
    }

    [Fact]
    public void GatewayConfig_UsesExternalCertificateFiles_And_LoopbackUpstream()
    {
        var config = LoadNginxConfig();

        Assert.Contains("/etc/nginx/certs/fullchain.pem", config, StringComparison.Ordinal);
        Assert.Contains("/etc/nginx/certs/privkey.pem", config, StringComparison.Ordinal);
        Assert.Contains("server 127.0.0.1:8080;", config, StringComparison.Ordinal);
    }

    [Fact]
    public void GatewayConfig_ForwardsProxyHeaders_WebSockets_And_Generic503Fallback()
    {
        var config = LoadNginxConfig();

        Assert.Contains("proxy_set_header Host $host;", config, StringComparison.Ordinal);
        Assert.Contains("proxy_set_header X-Real-IP $remote_addr;", config, StringComparison.Ordinal);
        Assert.Contains("proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;", config, StringComparison.Ordinal);
        Assert.Contains("proxy_set_header X-Forwarded-Proto https;", config, StringComparison.Ordinal);
        Assert.Contains("proxy_set_header X-Forwarded-Host $host;", config, StringComparison.Ordinal);
        Assert.Contains("proxy_set_header Upgrade $http_upgrade;", config, StringComparison.Ordinal);
        Assert.Contains("proxy_set_header Connection $connection_upgrade;", config, StringComparison.Ordinal);
        Assert.Contains("error_page 502 503 504 =503 /503.html;", config, StringComparison.Ordinal);
        Assert.Contains("return 503 \"Service temporarily unavailable.", config, StringComparison.Ordinal);
    }

    [Fact]
    public void GatewayReadme_ExplainsPrerequisites_ActivationInputs_And_Discoverability()
    {
        var readme = LoadNginxReadme();

        Assert.Contains("infra/nginx/payslip4all.conf", readme, StringComparison.Ordinal);
        Assert.Contains("payslip4all.co.za", readme, StringComparison.Ordinal);
        Assert.Contains("fullchain.pem", readme, StringComparison.Ordinal);
        Assert.Contains("privkey.pem", readme, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1:8080", readme, StringComparison.Ordinal);
        Assert.Contains("bootstrap-payslip4all.sh", readme, StringComparison.Ordinal);
        Assert.Contains("payslip4all-web.yaml", readme, StringComparison.Ordinal);
        Assert.Contains("wrong-host", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nginx -t", readme, StringComparison.Ordinal);
    }

    private static string LoadNginxConfig()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "nginx", "payslip4all.conf"));
    }

    private static string LoadNginxReadme()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "nginx", "README.md"));
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
}
