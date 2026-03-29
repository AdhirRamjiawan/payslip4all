using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

/// <summary>
/// Factory for creating a configured <see cref="AmazonDynamoDBClient"/> from environment variables.
/// </summary>
public static class DynamoDbClientFactory
{
    /// <summary>
    /// Creates an <see cref="AmazonDynamoDBClient"/> configured from environment variables.
    /// </summary>
    /// <remarks>
    /// Required env vars: DYNAMODB_REGION
    /// Optional env vars: DYNAMODB_ENDPOINT, AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY
    /// </remarks>
    public static IAmazonDynamoDB Create()
    {
        var region = Environment.GetEnvironmentVariable("DYNAMODB_REGION")?.Trim();
        if (string.IsNullOrWhiteSpace(region))
            throw new InvalidOperationException(
                "PERSISTENCE_PROVIDER is set to 'dynamodb' but the required environment variable DYNAMODB_REGION is not set.");

        var endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT")?.Trim();
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")?.Trim();
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")?.Trim();

        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region),
        };

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            config.ServiceURL = endpoint;
        }

        if (HasExplicitCredentials(accessKey, secretKey))
        {
            var credentials = new BasicAWSCredentials(accessKey!, secretKey!);
            return new AmazonDynamoDBClient(credentials, config);
        }

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            // Local emulators commonly require syntactically valid credentials even though
            // they do not authenticate them, so use standard dummy values when no explicit
            // credentials were supplied.
            return new AmazonDynamoDBClient(new BasicAWSCredentials("dummy", "dummy"), config);
        }

        return new AmazonDynamoDBClient(config);
    }

    private static bool HasExplicitCredentials(string? accessKey, string? secretKey)
    {
        var hasAccessKey = !string.IsNullOrWhiteSpace(accessKey);
        var hasSecretKey = !string.IsNullOrWhiteSpace(secretKey);

        if (hasAccessKey != hasSecretKey)
            throw new InvalidOperationException(
                "When using explicit DynamoDB credentials, both AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY must be set.");

        return hasAccessKey;
    }
}
