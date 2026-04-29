namespace Payslip4All.Web.Tests.Infrastructure;

public sealed class LocalStackIntegrationConfigTests
{
    [Fact]
    public void LocalStackDockerfile_PinsTheDynamoDbRuntimeContract()
    {
        var dockerfile = File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "localstack", "Dockerfile"));

        Assert.Contains("FROM localstack/localstack:3.5", dockerfile, StringComparison.Ordinal);
        Assert.Contains("SERVICES=dynamodb", dockerfile, StringComparison.Ordinal);
        Assert.Contains("GATEWAY_LISTEN=0.0.0.0:8000", dockerfile, StringComparison.Ordinal);
        Assert.Contains("EDGE_PORT=8000", dockerfile, StringComparison.Ordinal);
        Assert.Contains("AWS_DEFAULT_REGION=us-east-1", dockerfile, StringComparison.Ordinal);
        Assert.Contains("EAGER_SERVICE_LOADING=1", dockerfile, StringComparison.Ordinal);
        Assert.Contains("PERSISTENCE=0", dockerfile, StringComparison.Ordinal);
        Assert.Contains("EXPOSE 8000", dockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void RepositoryReadme_PointsContributorsToTheLocalStackWorkflow()
    {
        var rootReadme = File.ReadAllText(Path.Combine(GetSolutionRoot(), "README.md"));

        Assert.Contains("LocalStack", rootReadme, StringComparison.Ordinal);
        Assert.Contains("infra/localstack/Dockerfile", rootReadme, StringComparison.Ordinal);
        Assert.Contains("infra/localstack/README.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("PERSISTENCE_PROVIDER=dynamodb", rootReadme, StringComparison.Ordinal);
        Assert.Contains("http://adhir-server:8000", rootReadme, StringComparison.Ordinal);
        Assert.Contains("dummy credentials", rootReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("development and smoke testing", rootReadme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DynamoDbInfrastructureCode_KeepsTheLocalEndpointInDocumentationInsteadOfSource()
    {
        var clientFactory = File.ReadAllText(Path.Combine(GetSolutionRoot(), "src", "Payslip4All.Infrastructure", "Persistence", "DynamoDB", "DynamoDbClientFactory.cs"));
        var serviceExtensions = File.ReadAllText(Path.Combine(GetSolutionRoot(), "src", "Payslip4All.Infrastructure", "Persistence", "DynamoDB", "DynamoDbServiceExtensions.cs"));
        var configurationOptions = File.ReadAllText(Path.Combine(GetSolutionRoot(), "src", "Payslip4All.Infrastructure", "Persistence", "DynamoDB", "DynamoDbConfigurationOptions.cs"));

        Assert.DoesNotContain("localhost:8000", clientFactory, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost:8000", serviceExtensions, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost:8000", configurationOptions, StringComparison.Ordinal);
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
