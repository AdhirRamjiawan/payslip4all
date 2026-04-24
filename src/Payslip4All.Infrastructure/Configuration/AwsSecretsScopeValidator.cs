namespace Payslip4All.Infrastructure.Configuration;

/// <summary>
/// Validates AWS Secrets-backed app-config artifacts against the refined feature 015 scope.
/// Rejects excluded DynamoDB runtime and credential keys while accepting eligible repo-owned settings.
/// </summary>
public static class AwsSecretsScopeValidator
{
    /// <summary>
    /// Validates the keys present in an AWS Secrets-backed app-config artifact.
    /// Returns a validation result indicating whether the artifact is compliant.
    /// </summary>
    public static AwsSecretsScopeValidationResult Validate(IEnumerable<string> keys)
    {
        var keyList = keys.ToList();
        var excludedFound = new List<string>();
        var eligibleFound = new List<string>();
        var unrecognizedFound = new List<string>();

        foreach (var key in keyList)
        {
            if (AwsSecretsScopeCatalog.IsExcluded(key))
            {
                excludedFound.Add(key);
            }
            else if (AwsSecretsScopeCatalog.IsEligible(key))
            {
                eligibleFound.Add(key);
            }
            else
            {
                unrecognizedFound.Add(key);
            }
        }

        if (excludedFound.Count > 0)
        {
            var message = BuildExcludedKeysMessage(excludedFound);
            return AwsSecretsScopeValidationResult.Failure(excludedFound, message);
        }

        return AwsSecretsScopeValidationResult.Success(eligibleFound, unrecognizedFound);
    }

    private static string BuildExcludedKeysMessage(IReadOnlyList<string> excludedKeys)
    {
        var keyList = string.Join(", ", excludedKeys.Select(k => $"'{k}'"));
        return $"The AWS Secrets-backed app-config artifact contains excluded keys: {keyList}. " +
               "These keys must not be present in the AWS app-config secret. " +
               "DynamoDB runtime settings (DYNAMODB_*) and AWS credentials (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY) " +
               "must be supplied through environment variables, IAM instance profiles, or other non-app-config deployment sources. " +
               "Remove these keys from the AWS Secrets-backed payload and supply them via the appropriate alternate source.";
    }
}
