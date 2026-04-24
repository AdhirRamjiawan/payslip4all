namespace Payslip4All.Infrastructure.Configuration;

/// <summary>
/// Result of validating an AWS Secrets-backed app-config artifact against the supported and excluded catalogs.
/// </summary>
public sealed class AwsSecretsScopeValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> ExcludedKeysFound { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EligibleKeysFound { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> UnrecognizedKeysFound { get; init; } = Array.Empty<string>();
    public string? ValidationMessage { get; init; }

    public static AwsSecretsScopeValidationResult Success(
        IEnumerable<string> eligibleKeys,
        IEnumerable<string>? unrecognizedKeys = null)
    {
        return new AwsSecretsScopeValidationResult
        {
            IsValid = true,
            EligibleKeysFound = eligibleKeys.ToList(),
            UnrecognizedKeysFound = unrecognizedKeys?.ToList() ?? new List<string>(),
        };
    }

    public static AwsSecretsScopeValidationResult Failure(
        IEnumerable<string> excludedKeys,
        string validationMessage)
    {
        return new AwsSecretsScopeValidationResult
        {
            IsValid = false,
            ExcludedKeysFound = excludedKeys.ToList(),
            ValidationMessage = validationMessage,
        };
    }
}
