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
        Assert.Contains("AllowedSshCidr", readme, StringComparison.Ordinal);
        Assert.Contains("HostedPaymentsSecretArn", readme, StringComparison.Ordinal);
        Assert.Contains("HostedZoneId", readme, StringComparison.Ordinal);
        Assert.Contains("CertificateArn", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void DeploymentGuide_EnumeratesExactlyFiveManualPreLaunchActions()
    {
        var readme = LoadDeploymentReadme();
        var actions = ExtractOrderedListItems(readme, "## Five manual pre-launch actions");

        Assert.Equal(5, actions.Count);
        Assert.Contains(actions, action => action.Contains("Publish or upload the application artifact", StringComparison.Ordinal));
        Assert.Contains(actions, action => action.Contains("ACM certificate", StringComparison.Ordinal));
        Assert.Contains(actions, action => action.Contains("Route 53 hosted zone", StringComparison.Ordinal));
        Assert.Contains(actions, action => action.Contains("external secret references", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(actions, action => action.Contains("Launch `infra/aws/cloudformation/payslip4all-web.yaml`", StringComparison.Ordinal));
    }

    [Fact]
    public void DeploymentGuide_DocumentsOperatorVisibleSignals_AndBehindAlbHealthChecks()
    {
        var readme = LoadDeploymentReadme();

        Assert.Contains("ApplicationUrl", readme, StringComparison.Ordinal);
        Assert.Contains("InstanceId", readme, StringComparison.Ordinal);
        Assert.Contains("LoadBalancerArn", readme, StringComparison.Ordinal);
        Assert.Contains("InstanceSecurityGroupId", readme, StringComparison.Ordinal);
        Assert.Contains("LoadBalancerSecurityGroupId", readme, StringComparison.Ordinal);
        Assert.Contains("BackupProtectionMode", readme, StringComparison.Ordinal);
        Assert.Contains("ALB target health", readme, StringComparison.Ordinal);
        Assert.Contains("/health", readme, StringComparison.Ordinal);
        Assert.Contains("forwarded headers", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeploymentGuide_DocumentsDnsTls_CostTradeoffs_And_ReplaceableCompute()
    {
        var readme = LoadDeploymentReadme();

        Assert.Contains("payslip4all.co.za", readme, StringComparison.Ordinal);
        Assert.Contains("Application Load Balancer", readme, StringComparison.Ordinal);
        Assert.Contains("Route 53", readme, StringComparison.Ordinal);
        Assert.Contains("ACM", readme, StringComparison.Ordinal);
        Assert.Contains("free tier", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ALB", readme, StringComparison.Ordinal);
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
        Assert.Contains("60-minute", readme, StringComparison.Ordinal);
        Assert.Contains("/health", readme, StringComparison.Ordinal);
        Assert.Contains("infra/aws/cloudformation/payslip4all-web.yaml", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void RootReadme_ReferencesCloudFormationDeploymentGuide()
    {
        var rootReadme = File.ReadAllText(Path.Combine(GetSolutionRoot(), "README.md"));

        Assert.Contains("AWS CloudFormation Deployment", rootReadme, StringComparison.Ordinal);
        Assert.Contains("infra/aws/cloudformation/README.md", rootReadme, StringComparison.Ordinal);
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
