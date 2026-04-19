namespace Payslip4All.Web.Tests.Infrastructure;

public class AwsCloudFormationTemplateTests
{
    [Fact]
    public void Template_DeclaresRequiredParameters_And_NginxCertificateInputs()
    {
        var template = LoadTemplate();

        Assert.Contains("Parameters:", template, StringComparison.Ordinal);
        Assert.Contains("EnvironmentName:", template, StringComparison.Ordinal);
        Assert.Contains("InstanceType:", template, StringComparison.Ordinal);
        Assert.Contains("ArtifactSource:", template, StringComparison.Ordinal);
        Assert.Contains("DynamoDbTablePrefix:", template, StringComparison.Ordinal);
        Assert.Contains("HostedPaymentsSecretArn:", template, StringComparison.Ordinal);
        Assert.Contains("TlsCertificateSecretArn:", template, StringComparison.Ordinal);
        Assert.Contains("payslip4all.co.za", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_ExposesCompleteOperatorSignalInventory()
    {
        var template = LoadTemplate();

        Assert.Contains("Outputs:", template, StringComparison.Ordinal);
        Assert.Contains("ApplicationUrl:", template, StringComparison.Ordinal);
        Assert.Contains("InstanceId:", template, StringComparison.Ordinal);
        Assert.Contains("ElasticIpAddress:", template, StringComparison.Ordinal);
        Assert.Contains("InstanceSecurityGroupId:", template, StringComparison.Ordinal);
        Assert.Contains("SsmStartSessionCommand:", template, StringComparison.Ordinal);
        Assert.Contains("HostedPaymentsSecretReference:", template, StringComparison.Ordinal);
        Assert.Contains("TlsCertificateSecretReference:", template, StringComparison.Ordinal);
        Assert.Contains("NginxConfigPath:", template, StringComparison.Ordinal);
        Assert.Contains("BackupProtectionMode:", template, StringComparison.Ordinal);
        Assert.Contains("RestoreRunbook:", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_ProvisionsSingleInstanceComputeNetwork_And_IamRuntimeResources()
    {
        var template = LoadTemplate();

        Assert.Contains("AWS::EC2::SecurityGroup", template, StringComparison.Ordinal);
        Assert.Contains("AWS::EC2::Instance", template, StringComparison.Ordinal);
        Assert.Contains("AWS::EC2::EIP", template, StringComparison.Ordinal);
        Assert.Contains("AWS::IAM::Role", template, StringComparison.Ordinal);
        Assert.Contains("AWS::IAM::InstanceProfile", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_ExposesOnlyNginxPublicPorts_And_KeepsTheAppPortPrivate()
    {
        var template = LoadTemplate();

        Assert.Contains("FromPort: 80", template, StringComparison.Ordinal);
        Assert.Contains("ToPort: 80", template, StringComparison.Ordinal);
        Assert.Contains("FromPort: 443", template, StringComparison.Ordinal);
        Assert.Contains("ToPort: 443", template, StringComparison.Ordinal);
        Assert.DoesNotContain("FromPort: 8080", template, StringComparison.Ordinal);
        Assert.DoesNotContain("ToPort: 8080", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_UsesSecretsBackedCertificateBootstrap_And_NginxUserDataWiring()
    {
        var template = LoadTemplate();

        Assert.Contains("secretsmanager:GetSecretValue", template, StringComparison.Ordinal);
        Assert.Contains("/etc/nginx/certs/fullchain.pem", template, StringComparison.Ordinal);
        Assert.Contains("/etc/nginx/certs/privkey.pem", template, StringComparison.Ordinal);
        Assert.Contains("/etc/nginx/conf.d/payslip4all.conf", template, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS=http://127.0.0.1:8080", template, StringComparison.Ordinal);
        Assert.Contains("nginx -t", template, StringComparison.Ordinal);
        Assert.Contains("systemctl restart nginx", template, StringComparison.Ordinal);
        Assert.Contains("systemctl restart payslip4all.service", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_GrantsBackupPermissions_RestorePermissions_And_RecoveryOutputs()
    {
        var template = LoadTemplate();

        Assert.Contains("dynamodb:DescribeContinuousBackups", template, StringComparison.Ordinal);
        Assert.Contains("dynamodb:RestoreTableToPointInTime", template, StringComparison.Ordinal);
        Assert.Contains("dynamodb:UpdateContinuousBackups", template, StringComparison.Ordinal);
        Assert.Contains("dynamodb:ListTables", template, StringComparison.Ordinal);
        Assert.Contains("recovery", template, StringComparison.OrdinalIgnoreCase);
    }

    private static string LoadTemplate()
    {
        return File.ReadAllText(Path.Combine(GetSolutionRoot(), "infra", "aws", "cloudformation", "payslip4all-web.yaml"));
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
