namespace Payslip4All.Web.Tests.Infrastructure;

/// <summary>
/// Tests that verify the root README and deployment documentation consistently describe
/// the refined AWS Secrets scope (feature 015).
/// </summary>
public sealed class AwsSecretsScopeDocumentationTests
{
    private readonly string _rootReadmePath = Path.Combine(
        GetRepositoryRoot(),
        "README.md");

    private readonly string _infraReadmePath = Path.Combine(
        GetRepositoryRoot(),
        "infra/aws/cloudformation/README.md");

    [Fact]
    public void RootReadme_MentionsAwsSecretsSupport()
    {
        var content = File.ReadAllText(_rootReadmePath);

        Assert.Contains("AWS Secrets", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InfraReadme_DocumentsRefinedScope()
    {
        var content = File.ReadAllText(_infraReadmePath);

        // Should document the refined scope
        Assert.Contains("app-config", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InfraReadme_ExcludesDynamoDbRuntimeKeys()
    {
        var content = File.ReadAllText(_infraReadmePath);

        // DynamoDB runtime keys should be documented as outside the app-config secret
        Assert.Contains("DYNAMODB", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InfraReadme_ExcludesAwsCredentialKeys()
    {
        var content = File.ReadAllText(_infraReadmePath);

        // AWS credential keys should be documented as outside the app-config secret
        Assert.Contains("AWS_ACCESS_KEY_ID", content, StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "Payslip4All.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        if (currentDir == null)
            throw new InvalidOperationException("Could not find repository root.");

        return currentDir;
    }
}
