using Microsoft.Extensions.Configuration;
using Payslip4All.Infrastructure.Configuration;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

public sealed class DynamoDbConfigurationOptions
{
    public string? Region { get; init; }
    public string? Endpoint { get; init; }
    public string TablePrefix { get; init; } = "payslip4all";
    public bool EnablePointInTimeRecovery { get; init; } = true;
    public string? AccessKeyId { get; init; }
    public string? SecretAccessKey { get; init; }

    public bool HasExplicitCredentials
        => !string.IsNullOrWhiteSpace(AccessKeyId)
           && !string.IsNullOrWhiteSpace(SecretAccessKey);

    public static DynamoDbConfigurationOptions FromConfiguration(IConfiguration configuration)
    {
        return new DynamoDbConfigurationOptions
        {
            Region = Normalize(configuration[Payslip4AllCustomConfigurationKeys.DynamoDb.Region]),
            Endpoint = Normalize(configuration[Payslip4AllCustomConfigurationKeys.DynamoDb.Endpoint]),
            TablePrefix = Normalize(configuration[Payslip4AllCustomConfigurationKeys.DynamoDb.TablePrefix]) ?? "payslip4all",
            EnablePointInTimeRecovery = configuration.GetValue<bool?>(Payslip4AllCustomConfigurationKeys.DynamoDb.EnablePointInTimeRecovery) ?? true,
            AccessKeyId = Normalize(configuration[Payslip4AllCustomConfigurationKeys.DynamoDb.AccessKeyId]),
            SecretAccessKey = Normalize(configuration[Payslip4AllCustomConfigurationKeys.DynamoDb.SecretAccessKey]),
        };
    }

    public void ValidateForStartup()
    {
        if (string.IsNullOrWhiteSpace(Region))
            throw new InvalidOperationException(
                $"PERSISTENCE_PROVIDER is set to 'dynamodb' but the required configuration value {Payslip4AllCustomConfigurationKeys.DynamoDb.Region} is not set.");

        var hasAccessKey = !string.IsNullOrWhiteSpace(AccessKeyId);
        var hasSecretKey = !string.IsNullOrWhiteSpace(SecretAccessKey);

        if (hasAccessKey != hasSecretKey)
            throw new InvalidOperationException(
                $"When using explicit DynamoDB credentials, both {Payslip4AllCustomConfigurationKeys.DynamoDb.AccessKeyId} and {Payslip4AllCustomConfigurationKeys.DynamoDb.SecretAccessKey} must be set.");
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
