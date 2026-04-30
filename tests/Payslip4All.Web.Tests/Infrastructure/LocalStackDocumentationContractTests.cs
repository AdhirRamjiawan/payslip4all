namespace Payslip4All.Web.Tests.Infrastructure;

public sealed class LocalStackDocumentationContractTests
{
    [Fact]
    public void RepositoryAndLocalStackReadmes_StayAlignedOnTheSharedWorkflow()
    {
        var rootReadme = File.ReadAllText(Path.Combine(GetSolutionRoot(), "README.md"));
        var localStackReadme = File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "localstack", "README.md"));

        const string buildCommand = "docker -H ssh://adhir-server build -f infra/localstack/LocalStackDockerfile -t payslip4all-localstack .";
        const string runCommand = "docker -H ssh://adhir-server run --rm --name payslip4all-localstack -p 8000:8000 payslip4all-localstack";
        const string integrationCommand = "dotnet test tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj --filter Category=Integration";

        Assert.Contains(buildCommand, rootReadme, StringComparison.Ordinal);
        Assert.Contains(buildCommand, localStackReadme, StringComparison.Ordinal);
        Assert.Contains(runCommand, rootReadme, StringComparison.Ordinal);
        Assert.Contains(runCommand, localStackReadme, StringComparison.Ordinal);
        Assert.Contains(integrationCommand, rootReadme, StringComparison.Ordinal);
        Assert.Contains(integrationCommand, localStackReadme, StringComparison.Ordinal);
    }

    [Fact]
    public void RepositoryReadme_DocumentsDiscoveryConfigurabilityAndTroubleshooting()
    {
        var rootReadme = File.ReadAllText(Path.Combine(GetSolutionRoot(), "README.md"));

        Assert.Contains("infra/localstack/README.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("development and smoke testing", rootReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DYNAMODB_ENDPOINT=http://adhir-server:8000", rootReadme, StringComparison.Ordinal);
        Assert.Contains("DYNAMODB_TABLE_PREFIX", rootReadme, StringComparison.Ordinal);
        Assert.Contains("port `8000` is already in use", rootReadme, StringComparison.Ordinal);
        Assert.Contains("dummy credentials", rootReadme, StringComparison.OrdinalIgnoreCase);
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
