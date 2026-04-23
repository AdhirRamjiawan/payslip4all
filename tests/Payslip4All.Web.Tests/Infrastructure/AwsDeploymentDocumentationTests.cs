using System.Text.RegularExpressions;

namespace Payslip4All.Web.Tests.Infrastructure;

public class AwsDeploymentDocumentationTests
{
    [Fact]
    public void DeploymentGuide_ListsLaunchPrerequisites_And_RuntimeEnvironmentVariables()
    {
        var readme = LoadDeploymentReadme();

        Assert.Contains("ArtifactSource", readme, StringComparison.Ordinal);
        Assert.Contains("PERSISTENCE_PROVIDER=dynamodb", readme, StringComparison.Ordinal);
        Assert.Contains("DYNAMODB_REGION", readme, StringComparison.Ordinal);
        Assert.Contains("DYNAMODB_TABLE_PREFIX", readme, StringComparison.Ordinal);
        Assert.Contains("AppConfigSecretArn", readme, StringComparison.Ordinal);
        Assert.Contains("/etc/payslip4all/app-config.secrets.json", readme, StringComparison.Ordinal);
        Assert.Contains("HostedPaymentsSecretArn", readme, StringComparison.Ordinal);
        Assert.Contains("TlsCertificateSecretArn", readme, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://127.0.0.1:8080", readme, StringComparison.Ordinal);
        Assert.Contains("payslip4all.co.za", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void DeploymentGuide_EnumeratesExactlyFiveManualPreLaunchActions()
    {
        var readme = LoadDeploymentReadme();
        var actions = ExtractOrderedListItems(readme, "## Five manual pre-launch actions");

        Assert.Equal(5, actions.Count);
        Assert.Contains(actions, action => action.Contains("Publish or upload the application artifact", StringComparison.Ordinal));
        Assert.Contains(actions, action => action.Contains("TLS certificate secret", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(actions, action => action.Contains("Elastic IP", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(actions, action => action.Contains("payslip4all.co.za", StringComparison.Ordinal));
        Assert.Contains(actions, action => action.Contains("external secret references", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(actions, action => action.Contains("Launch `infra/aws/cloudformation/payslip4all-web.yaml`", StringComparison.Ordinal));
    }

    [Fact]
    public void DeploymentGuide_DocumentsOperatorVisibleSignals_And_NginxSmokeChecks()
    {
        var readme = LoadDeploymentReadme();

        Assert.Contains("ApplicationUrl", readme, StringComparison.Ordinal);
        Assert.Contains("ElasticIpAddress", readme, StringComparison.Ordinal);
        Assert.Contains("InstanceId", readme, StringComparison.Ordinal);
        Assert.Contains("InstanceSecurityGroupId", readme, StringComparison.Ordinal);
        Assert.Contains("SsmStartSessionCommand", readme, StringComparison.Ordinal);
        Assert.Contains("AppConfigSecretReference", readme, StringComparison.Ordinal);
        Assert.Contains("AppConfigSecretsFilePath", readme, StringComparison.Ordinal);
        Assert.Contains("TlsCertificateSecretReference", readme, StringComparison.Ordinal);
        Assert.Contains("BackupProtectionMode", readme, StringComparison.Ordinal);
        Assert.Contains("/health", readme, StringComparison.Ordinal);
        Assert.Contains("http://payslip4all.co.za", readme, StringComparison.Ordinal);
        Assert.Contains("https://payslip4all.co.za", readme, StringComparison.Ordinal);
        Assert.Contains("nginx -t", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void DeploymentGuide_DocumentsAppConfigSecretCatalog_Precedence_And_FailureScenarios()
    {
        var readme = LoadDeploymentReadme();

        Assert.Contains("environment variables > rendered AWS-secret config > checked-in appsettings", readme, StringComparison.Ordinal);
        Assert.Contains("Auth:Cookie:ExpireDays", readme, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:DefaultConnection", readme, StringComparison.Ordinal);
        Assert.Contains("HostedPayments:PayFast:MerchantId", readme, StringComparison.Ordinal);
        Assert.Contains("mixed-source", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing or unreadable", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeploymentGuide_DocumentsElasticIpTlsConstraints_And_NoManagedEdgeAssumptions()
    {
        var readme = LoadDeploymentReadme();

        Assert.Contains("payslip4all.co.za", readme, StringComparison.Ordinal);
        Assert.Contains("Elastic IP", readme, StringComparison.Ordinal);
        Assert.Contains("nginx", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-ALB", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-Route53", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-ACM", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("free tier", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("replacing or recreating the EC2 instance", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public entry point remains the same", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeploymentGuide_DocumentsBackupRestoreRunbook_And_NonLiveTargetRestore()
    {
        var readme = LoadDeploymentReadme();

        Assert.Contains("point-in-time recovery", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("restore", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("restore to a new table", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/health", readme, StringComparison.Ordinal);
        Assert.Contains("infra/aws/cloudformation/payslip4all-web.yaml", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void RootReadme_ReferencesNginxAndCloudFormationDeploymentGuides()
    {
        var rootReadme = File.ReadAllText(Path.Combine(GetSolutionRoot(), "README.md"));

        Assert.Contains("AWS CloudFormation Deployment", rootReadme, StringComparison.Ordinal);
        Assert.Contains("infra/aws/cloudformation/README.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("infra/nginx/README.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("AWS Secrets Manager", rootReadme, StringComparison.Ordinal);
        Assert.Contains("/etc/payslip4all/app-config.secrets.json", rootReadme, StringComparison.Ordinal);
        Assert.Contains("payslip4all.co.za", rootReadme, StringComparison.Ordinal);
    }

    private static string LoadDeploymentReadme()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "aws", "cloudformation", "README.md"));
    }

    private static IReadOnlyList<string> ExtractOrderedListItems(string markdown, string heading)
    {
        var items = new List<string>();
        var inSection = false;

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            if (string.Equals(line.Trim(), heading, StringComparison.Ordinal))
            {
                inSection = true;
                continue;
            }

            if (inSection && line.StartsWith("## ", StringComparison.Ordinal))
                break;

            if (!inSection)
                continue;

            var match = Regex.Match(line.Trim(), @"^\d+\.\s+(.*)$");
            if (match.Success)
                items.Add(match.Groups[1].Value);
        }

        return items;
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
