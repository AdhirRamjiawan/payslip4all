namespace Payslip4All.Web.Tests.Infrastructure;

public class YarpGatewayConfigTests
{
    [Fact]
    public void WebApp_ReferencesYarpGatewayMode_And_HostedProxySettings()
    {
        var program = LoadProgram();
        var project = LoadWebProject();

        Assert.Contains("Yarp.ReverseProxy", project, StringComparison.Ordinal);
        Assert.Contains("AddReverseProxy()", program, StringComparison.Ordinal);
        Assert.Contains("MapReverseProxy()", program, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_ENABLED", program, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_PUBLIC_HOST", program, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_UPSTREAM_BASE_URL", program, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status421MisdirectedRequest", program, StringComparison.Ordinal);
        Assert.Contains("Service temporarily unavailable.", program, StringComparison.Ordinal);
    }

    [Fact]
    public void AwsBootstrap_ConfiguresBackendAndGatewayServices_WithoutNginx()
    {
        var bootstrap = LoadBootstrap();
        var template = LoadTemplate();

        Assert.Contains("payslip4all-gateway.service", bootstrap, StringComparison.Ordinal);
        Assert.Contains("payslip4all-gateway.service", template, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_ENABLED=true", bootstrap, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_ENABLED=true", template, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080", bootstrap, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080", template, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://127.0.0.1:8080", bootstrap, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://127.0.0.1:8080", template, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://0.0.0.0:80;https://0.0.0.0:443", bootstrap, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://0.0.0.0:80;https://0.0.0.0:443", template, StringComparison.Ordinal);
        Assert.Contains("Kestrel__Certificates__Default__Path", bootstrap, StringComparison.Ordinal);
        Assert.Contains("Kestrel__Certificates__Default__Path", template, StringComparison.Ordinal);
        Assert.DoesNotContain("nginx", bootstrap, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/etc/nginx", bootstrap, StringComparison.Ordinal);
    }

    [Fact]
    public void GatewayReadme_ExplainsPrerequisites_ActivationInputs_And_Discoverability()
    {
        var readme = LoadYarpReadme();

        Assert.Contains("infra/yarp/README.md", readme, StringComparison.Ordinal);
        Assert.Contains("YARP", readme, StringComparison.Ordinal);
        Assert.Contains("payslip4all.co.za", readme, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_ENABLED=true", readme, StringComparison.Ordinal);
        Assert.Contains("REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080", readme, StringComparison.Ordinal);
        Assert.Contains("Kestrel__Certificates__Default__Path", readme, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1:8080", readme, StringComparison.Ordinal);
        Assert.Contains("bootstrap-payslip4all.sh", readme, StringComparison.Ordinal);
        Assert.Contains("payslip4all-web.yaml", readme, StringComparison.Ordinal);
        Assert.Contains("421", readme, StringComparison.Ordinal);
        Assert.Contains("503", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void RepositoryGovernance_RemainsYarpFirst_And_PointsOperatorsToCanonicalDocs()
    {
        var constitution = LoadConstitution();
        var contract = LoadContract();
        var quickstart = LoadQuickstart();

        Assert.Contains("1.4.0", constitution, StringComparison.Ordinal);
        Assert.Contains("Yarp.ReverseProxy", constitution, StringComparison.Ordinal);
        Assert.Contains("canonical", contract, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/health", contract, StringComparison.Ordinal);
        Assert.Contains("421 Misdirected Request", contract, StringComparison.Ordinal);
        Assert.Contains("within 10 seconds", contract, StringComparison.Ordinal);
        Assert.Contains("single operator-facing entrypoint", quickstart, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("contracts/yarp-gateway-contract.md", quickstart, StringComparison.Ordinal);
    }

    [Fact]
    public void SupportingDocs_AreReferenceOnly_And_DoNotIntroduceNewNginxRequirements()
    {
        var yarpReadme = LoadYarpReadme();
        var cloudFormationReadme = LoadCloudFormationReadme();

        Assert.Contains("reference-only", yarpReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quickstart", yarpReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reference-only", cloudFormationReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quickstart", cloudFormationReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REVERSE_PROXY_UPSTREAM_BASE_URL=http://127.0.0.1:8080", yarpReadme, StringComparison.Ordinal);
        Assert.Contains("within 10 seconds", cloudFormationReadme, StringComparison.Ordinal);
        Assert.DoesNotContain("nginx requirement", yarpReadme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nginx requirement", cloudFormationReadme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppSettings_DeclareReverseProxyDefaults_AndDevelopmentTlsPlaceholders()
    {
        var sharedConfiguration = LoadWebAppSettings("appsettings.json");
        var developmentConfiguration = LoadWebAppSettings("appsettings.Development.json");
        var developmentPrivateConfiguration = LoadWebAppSettings("appsettings.Development.Private.json");

        Assert.Contains("\"REVERSE_PROXY_ENABLED\": false", sharedConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"REVERSE_PROXY_PUBLIC_HOST\": \"payslip4all.co.za\"", sharedConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"REVERSE_PROXY_UPSTREAM_BASE_URL\": \"http://127.0.0.1:8080\"", sharedConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"REVERSE_PROXY_ACTIVITY_TIMEOUT_SECONDS\": 10", sharedConfiguration, StringComparison.Ordinal);

        Assert.Contains("\"REVERSE_PROXY_ENABLED\": false", developmentConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"REVERSE_PROXY_PUBLIC_HOST\": \"payslip4all.co.za\"", developmentConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"REVERSE_PROXY_UPSTREAM_BASE_URL\": \"http://127.0.0.1:8080\"", developmentConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"REVERSE_PROXY_ACTIVITY_TIMEOUT_SECONDS\": 10", developmentConfiguration, StringComparison.Ordinal);

        Assert.Contains("\"Kestrel\"", developmentPrivateConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"Certificates\"", developmentPrivateConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"Default\"", developmentPrivateConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"Path\": \"\"", developmentPrivateConfiguration, StringComparison.Ordinal);
        Assert.Contains("\"Password\": \"\"", developmentPrivateConfiguration, StringComparison.Ordinal);
    }

    private static string LoadProgram()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "src", "Payslip4All.Web", "Program.cs"));
    }

    private static string LoadWebProject()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "src", "Payslip4All.Web", "Payslip4All.Web.csproj"));
    }

    private static string LoadWebAppSettings(string fileName)
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "src", "Payslip4All.Web", fileName));
    }

    private static string LoadBootstrap()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "aws", "cloudformation", "user-data", "bootstrap-payslip4all.sh"));
    }

    private static string LoadTemplate()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "aws", "cloudformation", "payslip4all-web.yaml"));
    }

    private static string LoadYarpReadme()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "yarp", "README.md"));
    }

    private static string LoadCloudFormationReadme()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "aws", "cloudformation", "README.md"));
    }

    private static string LoadConstitution()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), ".specify", "memory", "constitution.md"));
    }

    private static string LoadContract()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "specs", "017-yarp-https-proxy", "contracts", "yarp-gateway-contract.md"));
    }

    private static string LoadQuickstart()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "specs", "017-yarp-https-proxy", "quickstart.md"));
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
