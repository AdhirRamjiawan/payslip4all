namespace Payslip4All.Web.Tests.Startup;

public class PayFastSetupDocumentationTests
{
    private static string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir is not null && !File.Exists(Path.Combine(currentDir, "Payslip4All.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        return currentDir ?? throw new InvalidOperationException("Could not find solution root.");
    }

    [Fact]
    public void PayFastNotifyRoute_IsRegistered_InProgramSource()
    {
        var solutionRoot = GetSolutionRoot();
        var programSource = File.ReadAllText(Path.Combine(solutionRoot, "src", "Payslip4All.Web", "Program.cs"));

        Assert.Contains("app.MapPost(\"/api/payments/payfast/notify\"", programSource, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedConfiguration_DeclaresPayFastHostedPaymentKeys()
    {
        var solutionRoot = GetSolutionRoot();
        var configuration = File.ReadAllText(Path.Combine(solutionRoot, "src", "Payslip4All.Web", "appsettings.json"));

        Assert.Contains("\"HostedPayments\"", configuration, StringComparison.Ordinal);
        Assert.Contains("\"PayFast\"", configuration, StringComparison.Ordinal);
        Assert.Contains("\"ProviderKey\": \"payfast\"", configuration, StringComparison.Ordinal);
        Assert.Contains("\"UseSandbox\": false", configuration, StringComparison.Ordinal);
        Assert.Contains("\"MerchantId\": \"\"", configuration, StringComparison.Ordinal);
        Assert.Contains("\"MerchantKey\": \"\"", configuration, StringComparison.Ordinal);
        Assert.Contains("\"Passphrase\": \"\"", configuration, StringComparison.Ordinal);
        Assert.Contains("\"PublicNotifyUrl\": \"\"", configuration, StringComparison.Ordinal);
    }

    [Fact]
    public void DevelopmentConfiguration_UsesSandboxMode_ForPayFast()
    {
        var solutionRoot = GetSolutionRoot();
        var configuration = File.ReadAllText(Path.Combine(solutionRoot, "src", "Payslip4All.Web", "appsettings.Development.json"));

        Assert.Contains("\"HostedPayments\"", configuration, StringComparison.Ordinal);
        Assert.Contains("\"PayFast\"", configuration, StringComparison.Ordinal);
        Assert.Contains("\"UseSandbox\": true", configuration, StringComparison.Ordinal);
    }
}
