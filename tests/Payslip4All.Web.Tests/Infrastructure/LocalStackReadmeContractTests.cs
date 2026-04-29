namespace Payslip4All.Web.Tests.Infrastructure;

public sealed class LocalStackReadmeContractTests
{
    [Fact]
    public void LocalStackReadme_DocumentsBuildRunVerifyAndStopFlow()
    {
        var readme = File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "localstack", "README.md"));

        Assert.Contains("docker -H ssh://adhir-server build -f infra/localstack/Dockerfile -t payslip4all-localstack .", readme, StringComparison.Ordinal);
        Assert.Contains("docker -H ssh://adhir-server run --rm --name payslip4all-localstack -p 8000:8000 payslip4all-localstack", readme, StringComparison.Ordinal);
        Assert.Contains("The container must remain running", readme, StringComparison.Ordinal);
        Assert.Contains("dotnet test tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj --filter Category=Integration", readme, StringComparison.Ordinal);
        Assert.Contains("docker -H ssh://adhir-server stop payslip4all-localstack", readme, StringComparison.Ordinal);

        Assert.True(
            readme.IndexOf("dotnet test tests/Payslip4All.Web.Tests/Payslip4All.Web.Tests.csproj --filter Category=Integration", StringComparison.Ordinal)
            < readme.IndexOf("docker -H ssh://adhir-server stop payslip4all-localstack", StringComparison.Ordinal),
            "The README should document running the integration test before stopping the LocalStack container.");
    }

    [Fact]
    public void LocalStackReadme_DocumentsRuntimeConfigurationAndCredentialExpectations()
    {
        var readme = File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "localstack", "README.md"));

        Assert.Contains("export PERSISTENCE_PROVIDER=dynamodb", readme, StringComparison.Ordinal);
        Assert.Contains("export DYNAMODB_REGION=us-east-1", readme, StringComparison.Ordinal);
        Assert.Contains("export DYNAMODB_ENDPOINT=http://adhir-server:8000", readme, StringComparison.Ordinal);
        Assert.Contains("export DYNAMODB_TABLE_PREFIX=payslip4all", readme, StringComparison.Ordinal);
        Assert.Contains("dummy credentials", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not require a live AWS account", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocalStackReadme_DocumentsConfigurableValuesAndTroubleshooting()
    {
        var readme = File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "localstack", "README.md"));

        Assert.Contains("port `8000` is already in use", readme, StringComparison.Ordinal);
        Assert.Contains("choose a different host port", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DYNAMODB_ENDPOINT", readme, StringComparison.Ordinal);
        Assert.Contains("DYNAMODB_TABLE_PREFIX", readme, StringComparison.Ordinal);
        Assert.Contains("check that the container is still running", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("paired explicit credentials", readme, StringComparison.OrdinalIgnoreCase);
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
