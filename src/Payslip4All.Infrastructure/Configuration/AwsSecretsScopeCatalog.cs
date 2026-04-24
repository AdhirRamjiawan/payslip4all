using Payslip4All.Infrastructure.HostedPayments;

namespace Payslip4All.Infrastructure.Configuration;

/// <summary>
/// Defines the supported and excluded AWS Secrets-backed configuration catalogs.
/// Feature 015 narrows the feature 014 scope to preserve only eligible repo-owned app settings.
/// </summary>
public static class AwsSecretsScopeCatalog
{
    /// <summary>
    /// Eligible keys that MAY be supplied through the AWS Secrets-backed app-config artifact.
    /// These are repo-owned application settings consumed by startup or feature code.
    /// </summary>
    public static readonly IReadOnlySet<string> EligibleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Persistence selection and relational settings
        Payslip4AllCustomConfigurationKeys.PersistenceProvider,
        Payslip4AllCustomConfigurationKeys.DefaultConnectionString,
        Payslip4AllCustomConfigurationKeys.MySqlConnectionString,

        // Authentication
        Payslip4AllCustomConfigurationKeys.AuthCookieExpireDays,

        // Hosted payments - PayFast
        $"{PayFastHostedPaymentOptions.SectionKey}:ProviderKey",
        $"{PayFastHostedPaymentOptions.SectionKey}:UseSandbox",
        $"{PayFastHostedPaymentOptions.SectionKey}:MerchantId",
        $"{PayFastHostedPaymentOptions.SectionKey}:MerchantKey",
        $"{PayFastHostedPaymentOptions.SectionKey}:Passphrase",
        $"{PayFastHostedPaymentOptions.SectionKey}:PublicNotifyUrl",
        $"{PayFastHostedPaymentOptions.SectionKey}:SandboxBaseUrl",
        $"{PayFastHostedPaymentOptions.SectionKey}:LiveBaseUrl",
        $"{PayFastHostedPaymentOptions.SectionKey}:SandboxValidationUrl",
        $"{PayFastHostedPaymentOptions.SectionKey}:LiveValidationUrl",
        $"{PayFastHostedPaymentOptions.SectionKey}:ItemName",
    };

    /// <summary>
    /// Excluded keys that MUST NOT be present in the AWS Secrets-backed app-config artifact.
    /// These are DynamoDB runtime and AWS credential keys that violate the constitution when secret-backed.
    /// </summary>
    public static readonly IReadOnlySet<string> ExcludedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // DynamoDB runtime settings
        Payslip4AllCustomConfigurationKeys.DynamoDb.Region,
        Payslip4AllCustomConfigurationKeys.DynamoDb.Endpoint,
        Payslip4AllCustomConfigurationKeys.DynamoDb.TablePrefix,
        Payslip4AllCustomConfigurationKeys.DynamoDb.EnablePointInTimeRecovery,

        // AWS credentials
        Payslip4AllCustomConfigurationKeys.DynamoDb.AccessKeyId,
        Payslip4AllCustomConfigurationKeys.DynamoDb.SecretAccessKey,
    };

    /// <summary>
    /// Returns true if the specified key is explicitly excluded from the AWS Secrets scope.
    /// </summary>
    public static bool IsExcluded(string key)
        => ExcludedKeys.Contains(key);

    /// <summary>
    /// Returns true if the specified key is eligible for the AWS Secrets scope.
    /// </summary>
    public static bool IsEligible(string key)
        => EligibleKeys.Contains(key);
}
