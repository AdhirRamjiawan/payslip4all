using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

/// <summary>
/// Factory for creating a configured <see cref="AmazonDynamoDBClient"/> from resolved configuration.
/// </summary>
public static class DynamoDbClientFactory
{
    public static IAmazonDynamoDB Create()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        return Create(DynamoDbConfigurationOptions.FromConfiguration(configuration));
    }

    /// <summary>
    /// Creates an <see cref="AmazonDynamoDBClient"/> configured from resolved configuration.
    /// </summary>
    public static IAmazonDynamoDB Create(DynamoDbConfigurationOptions options)
    {
        options.ValidateForStartup();
        var endpointUri = options.GetValidatedEndpointUriOrNull();

        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region),
        };

        if (endpointUri is not null)
            config.ServiceURL = endpointUri.AbsoluteUri;

        if (options.HasExplicitCredentials)
        {
            var credentials = new BasicAWSCredentials(options.AccessKeyId!, options.SecretAccessKey!);
            return new AmazonDynamoDBClient(credentials, config);
        }

        if (endpointUri is not null)
        {
            // Local emulators commonly require syntactically valid credentials even though
            // they do not authenticate them, so use standard dummy values when no explicit
            // credentials were supplied.
            return new AmazonDynamoDBClient(new BasicAWSCredentials("dummy", "dummy"), config);
        }

        return new AmazonDynamoDBClient(config);
    }
}
