using Payslip4All.Infrastructure.Configuration;

namespace Payslip4All.Infrastructure.Tests.Configuration;

/// <summary>
/// Tests for AWS Secrets scope validation logic (feature 015).
/// Validates that the refined scope catalog correctly identifies eligible and excluded keys.
/// </summary>
public sealed class AwsSecretsScopeValidationTests
{
    [Fact]
    public void Validate_WithOnlyEligibleKeys_ReturnsSuccess()
    {
        var keys = new[]
        {
            "PERSISTENCE_PROVIDER",
            "Auth:Cookie:ExpireDays",
            "HostedPayments:PayFast:MerchantId",
        };

        var result = AwsSecretsScopeValidator.Validate(keys);

        Assert.True(result.IsValid);
        Assert.Equal(3, result.EligibleKeysFound.Count);
        Assert.Empty(result.ExcludedKeysFound);
    }

    [Fact]
    public void Validate_WithExcludedDynamoDbKeys_ReturnsFailure()
    {
        var keys = new[]
        {
            "PERSISTENCE_PROVIDER",
            "DYNAMODB_REGION",
            "DYNAMODB_ENDPOINT",
        };

        var result = AwsSecretsScopeValidator.Validate(keys);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.ExcludedKeysFound.Count);
        Assert.Contains("DYNAMODB_REGION", result.ExcludedKeysFound);
        Assert.Contains("DYNAMODB_ENDPOINT", result.ExcludedKeysFound);
        Assert.NotNull(result.ValidationMessage);
        Assert.Contains("excluded keys", result.ValidationMessage);
        Assert.Contains("DYNAMODB_REGION", result.ValidationMessage);
    }

    [Fact]
    public void Validate_WithExcludedAwsCredentialKeys_ReturnsFailure()
    {
        var keys = new[]
        {
            "AWS_ACCESS_KEY_ID",
            "AWS_SECRET_ACCESS_KEY",
        };

        var result = AwsSecretsScopeValidator.Validate(keys);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.ExcludedKeysFound.Count);
        Assert.Contains("AWS_ACCESS_KEY_ID", result.ExcludedKeysFound);
        Assert.Contains("AWS_SECRET_ACCESS_KEY", result.ExcludedKeysFound);
        Assert.NotNull(result.ValidationMessage);
        Assert.Contains("AWS credentials", result.ValidationMessage);
    }

    [Fact]
    public void Validate_WithMixedEligibleAndExcluded_ReturnsFailure()
    {
        var keys = new[]
        {
            "Auth:Cookie:ExpireDays",
            "DYNAMODB_TABLE_PREFIX",
            "HostedPayments:PayFast:MerchantKey",
        };

        var result = AwsSecretsScopeValidator.Validate(keys);

        Assert.False(result.IsValid);
        Assert.Single(result.ExcludedKeysFound);
        Assert.Contains("DYNAMODB_TABLE_PREFIX", result.ExcludedKeysFound);
    }

    [Fact]
    public void Validate_IsCaseInsensitive()
    {
        var keys = new[]
        {
            "persistence_provider",
            "DYNAMODB_region",
        };

        var result = AwsSecretsScopeValidator.Validate(keys);

        Assert.False(result.IsValid);
        Assert.Single(result.ExcludedKeysFound);
    }

    [Fact]
    public void Validate_WithEmptyKeys_ReturnsSuccess()
    {
        var result = AwsSecretsScopeValidator.Validate(Array.Empty<string>());

        Assert.True(result.IsValid);
        Assert.Empty(result.EligibleKeysFound);
        Assert.Empty(result.ExcludedKeysFound);
    }

    [Fact]
    public void Catalog_ContainsAllPayFastKeys()
    {
        var payfastKeys = new[]
        {
            "HostedPayments:PayFast:ProviderKey",
            "HostedPayments:PayFast:UseSandbox",
            "HostedPayments:PayFast:MerchantId",
            "HostedPayments:PayFast:MerchantKey",
            "HostedPayments:PayFast:Passphrase",
            "HostedPayments:PayFast:PublicNotifyUrl",
            "HostedPayments:PayFast:SandboxBaseUrl",
            "HostedPayments:PayFast:LiveBaseUrl",
            "HostedPayments:PayFast:SandboxValidationUrl",
            "HostedPayments:PayFast:LiveValidationUrl",
            "HostedPayments:PayFast:ItemName",
        };

        foreach (var key in payfastKeys)
        {
            Assert.True(AwsSecretsScopeCatalog.IsEligible(key), $"Key '{key}' should be eligible");
            Assert.False(AwsSecretsScopeCatalog.IsExcluded(key), $"Key '{key}' should not be excluded");
        }
    }

    [Fact]
    public void Catalog_ContainsAllExcludedKeys()
    {
        var excludedKeys = new[]
        {
            "DYNAMODB_REGION",
            "DYNAMODB_ENDPOINT",
            "DYNAMODB_TABLE_PREFIX",
            "DYNAMODB_ENABLE_PITR",
            "AWS_ACCESS_KEY_ID",
            "AWS_SECRET_ACCESS_KEY",
        };

        foreach (var key in excludedKeys)
        {
            Assert.True(AwsSecretsScopeCatalog.IsExcluded(key), $"Key '{key}' should be excluded");
            Assert.False(AwsSecretsScopeCatalog.IsEligible(key), $"Key '{key}' should not be eligible");
        }
    }
}
