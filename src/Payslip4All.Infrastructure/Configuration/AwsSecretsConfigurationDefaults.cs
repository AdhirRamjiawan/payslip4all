using Microsoft.Extensions.Configuration;

namespace Payslip4All.Infrastructure.Configuration;

public static class AwsSecretsConfigurationDefaults
{
    public const string PathOverrideEnvironmentVariable = "PAYSLIP4ALL_AWS_SECRETS_CONFIG_PATH";
    public const string DefaultSecretsFilePath = "/etc/payslip4all/app-config.secrets.json";

    public static string ResolveSecretsFilePath(IConfiguration configuration)
    {
        var overridePath = configuration[PathOverrideEnvironmentVariable]?.Trim();
        return string.IsNullOrWhiteSpace(overridePath)
            ? DefaultSecretsFilePath
            : overridePath;
    }
}
