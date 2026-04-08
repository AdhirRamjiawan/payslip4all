using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

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
        Assert.Contains("AWS::ElasticLoadBalancingV2::TargetGroupAttachment", template, StringComparison.Ordinal);
        Assert.Contains("AWS::ElasticLoadBalancingV2::Listener", template, StringComparison.Ordinal);
        Assert.Contains("HealthCheckPath: /health", template, StringComparison.Ordinal);
        Assert.Contains("TargetId: !Ref WebInstance", template, StringComparison.Ordinal);
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
        var resources = LoadResources();

        Assert.False(ResourceReferencesNode(resources, "ApplicationLoadBalancer", "WebInstance"));
        Assert.False(ResourceReferencesNode(resources, "ApplicationTargetGroup", "WebInstance"));
        Assert.False(ResourceReferencesNode(resources, "PublicDnsRecord", "WebInstance"));
        Assert.False(ResourceReferencesNode(resources, "PublicVpc", "WebInstance"));
        Assert.True(ResourceReferencesNode(resources, "WebInstanceTargetAttachment", "WebInstance"));
    }

    [Fact]
    public void Template_ImplementsHttpsPublishing_And_HealthAwareDnsRouting()
    {
        var template = LoadTemplate();

        Assert.Contains("RedirectConfig", template, StringComparison.Ordinal);
        Assert.Contains("CertificateArn: !Ref CertificateArn", template, StringComparison.Ordinal);
        Assert.Contains("SslPolicy: ELBSecurityPolicy-TLS13-1-2-2021-06", template, StringComparison.Ordinal);
        Assert.Contains("AWS::Route53::RecordSet", template, StringComparison.Ordinal);
        Assert.Contains("EvaluateTargetHealth: true", template, StringComparison.Ordinal);
        Assert.Contains("payslip4all-co-za-web-${EnvironmentName}", template, StringComparison.Ordinal);
        Assert.Contains("payslip4all-co-za-alb-${EnvironmentName}", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_WaitsForBootstrapHealth_And_SecuresEnvironmentFileBeforeWritingSecrets()
    {
        var template = LoadTemplate();

        Assert.Contains("CreationPolicy:", template, StringComparison.Ordinal);
        Assert.Contains("ResourceSignal:", template, StringComparison.Ordinal);
        Assert.Contains("Timeout: PT15M", template, StringComparison.Ordinal);
        Assert.Contains("install -m 0600 /dev/null \"${ENV_FILE}\"", template, StringComparison.Ordinal);
        Assert.Contains("chmod 600 \"${ENV_FILE}\"", template, StringComparison.Ordinal);
        Assert.Contains("aws cloudformation signal-resource", template, StringComparison.Ordinal);
        Assert.Contains("systemctl is-active --quiet payslip4all.service", template, StringComparison.Ordinal);
        Assert.Contains("http://127.0.0.1/health", template, StringComparison.Ordinal);
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

    private static YamlMappingNode LoadResources()
    {
        using var reader = new StringReader(LoadTemplate());
        var stream = new YamlStream();
        stream.Load(reader);

        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        return (YamlMappingNode)root.Children[new YamlScalarNode("Resources")];
    }

    private static bool ResourceReferencesNode(YamlMappingNode resources, string resourceName, string targetName)
    {
        var resource = (YamlMappingNode)resources.Children[new YamlScalarNode(resourceName)];
        return NodeContainsReference(resource, targetName);
    }

    private static bool NodeContainsReference(YamlNode node, string targetName)
    {
        switch (node)
        {
            case YamlScalarNode scalar:
                return string.Equals(scalar.Value, targetName, StringComparison.Ordinal);

            case YamlSequenceNode sequence:
                return sequence.Children.Any(child => NodeContainsReference(child, targetName));

            case YamlMappingNode mapping:
                foreach (var (key, value) in mapping.Children)
                {
                    if (key is YamlScalarNode scalarKey
                        && string.Equals(scalarKey.Value, "DependsOn", StringComparison.Ordinal)
                        && NodeContainsReference(value, targetName))
                    {
                        return true;
                    }

                    if (NodeContainsReference(value, targetName))
                    {
                        return true;
                    }
                }

                return false;

            default:
                return false;
        }
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
