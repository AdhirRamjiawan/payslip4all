using System.Text.RegularExpressions;

namespace Payslip4All.Web.Tests.Infrastructure;

public class AwsCloudFormationTemplateTests
{
    [Fact]
    public void Template_DeclaresRequiredParameters_And_DynamoDbDefaults()
    {
        var template = LoadTemplate();

        Assert.Contains("Parameters:", template, StringComparison.Ordinal);
        Assert.Contains("EnvironmentName:", template, StringComparison.Ordinal);
        Assert.Contains("DomainName:", template, StringComparison.Ordinal);
        Assert.Contains("HostedZoneId:", template, StringComparison.Ordinal);
        Assert.Contains("CertificateArn:", template, StringComparison.Ordinal);
        Assert.Contains("InstanceType:", template, StringComparison.Ordinal);
        Assert.Contains("ArtifactSource:", template, StringComparison.Ordinal);
        Assert.Contains("AllowedSshCidr:", template, StringComparison.Ordinal);
        Assert.Contains("DynamoDbRegion:", template, StringComparison.Ordinal);
        Assert.Contains("DynamoDbTablePrefix:", template, StringComparison.Ordinal);
        Assert.Contains("HostedPaymentsSecretArn:", template, StringComparison.Ordinal);
        Assert.Contains("EnablePointInTimeRecovery:", template, StringComparison.Ordinal);
        Assert.Contains("Default: 'true'", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_ExposesCompleteOperatorSignalInventory()
    {
        var template = LoadTemplate();

        Assert.Contains("Outputs:", template, StringComparison.Ordinal);
        Assert.Contains("ApplicationUrl:", template, StringComparison.Ordinal);
        Assert.Contains("InstanceId:", template, StringComparison.Ordinal);
        Assert.Contains("LoadBalancerArn:", template, StringComparison.Ordinal);
        Assert.Contains("InstanceSecurityGroupId:", template, StringComparison.Ordinal);
        Assert.Contains("LoadBalancerSecurityGroupId:", template, StringComparison.Ordinal);
        Assert.Contains("HostedPaymentsSecretReference:", template, StringComparison.Ordinal);
        Assert.Contains("BackupProtectionMode:", template, StringComparison.Ordinal);
        Assert.Contains("RestoreRunbook:", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_ProvisionsSingleInstanceComputeNetwork_And_IamRuntimeResources()
    {
        var template = LoadTemplate();

        Assert.Contains("AWS::EC2::VPC", template, StringComparison.Ordinal);
        Assert.Contains("AWS::EC2::Subnet", template, StringComparison.Ordinal);
        Assert.Contains("AWS::EC2::InternetGateway", template, StringComparison.Ordinal);
        Assert.Contains("AWS::EC2::RouteTable", template, StringComparison.Ordinal);
        Assert.Contains("AWS::EC2::SecurityGroup", template, StringComparison.Ordinal);
        Assert.Contains("AWS::EC2::Instance", template, StringComparison.Ordinal);
        Assert.Contains("AWS::IAM::Role", template, StringComparison.Ordinal);
        Assert.Contains("AWS::IAM::InstanceProfile", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_PlacesTheApplicationBehindAnAlb_WithHealthBasedRouting()
    {
        var template = LoadTemplate();

        Assert.Contains("AWS::ElasticLoadBalancingV2::LoadBalancer", template, StringComparison.Ordinal);
        Assert.Contains("AWS::ElasticLoadBalancingV2::TargetGroup", template, StringComparison.Ordinal);
        Assert.Contains("AWS::ElasticLoadBalancingV2::Listener", template, StringComparison.Ordinal);
        Assert.Contains("HealthCheckPath: /health", template, StringComparison.Ordinal);
        Assert.Contains("Targets:", template, StringComparison.Ordinal);
        Assert.Contains("Id: !Ref WebInstance", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_UsesAllowedSshCidr_WithoutHardcodedOperatorAccess()
    {
        var template = LoadTemplate();

        Assert.Matches(@"AllowedSshCidr:\s*\n\s*Type:\s*String", template);
        Assert.Matches(@"FromPort:\s*22\s*\n\s*ToPort:\s*22\s*\n\s*CidrIp:\s*!Ref AllowedSshCidr", template);
        Assert.DoesNotContain("FromPort: 22\n          ToPort: 22\n          CidrIp: 0.0.0.0/0", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_KeepsSharedInfrastructure_IndependentOfEc2Replacement()
    {
        var template = LoadTemplate();

        Assert.Contains("ApplicationLoadBalancer:", template, StringComparison.Ordinal);
        Assert.Contains("ApplicationTargetGroup:", template, StringComparison.Ordinal);
        Assert.Contains("PublicDnsRecord:", template, StringComparison.Ordinal);
        Assert.Contains("PublicVpc:", template, StringComparison.Ordinal);
        Assert.DoesNotContain("DependsOn: WebInstance", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_ImplementsHttpsPublishing_And_HealthAwareDnsRouting()
    {
        var template = LoadTemplate();

        Assert.Contains("RedirectConfig", template, StringComparison.Ordinal);
        Assert.Contains("CertificateArn: !Ref CertificateArn", template, StringComparison.Ordinal);
        Assert.Contains("AWS::Route53::RecordSet", template, StringComparison.Ordinal);
        Assert.Contains("EvaluateTargetHealth: true", template, StringComparison.Ordinal);
        Assert.Contains("payslip4all-co-za-web-${EnvironmentName}", template, StringComparison.Ordinal);
        Assert.Contains("payslip4all-co-za-alb-${EnvironmentName}", template, StringComparison.Ordinal);
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
