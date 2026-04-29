using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using Payslip4All.Web.Tests.Startup;

namespace Payslip4All.Web.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(DynamoDbStartupTestCollection.Name)]
public sealed class DynamoDbLocalStartupTests
{
    [Fact]
    public async Task CreateClient_WithLocalEndpoint_ProvisionsAllPrefixedTables()
    {
        using var harness = new LocalDynamoDbTestHarness();
        await using var factory = harness.CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var dynamoDb = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();

        try
        {
            var client = Assert.IsType<AmazonDynamoDBClient>(dynamoDb);
            Assert.Equal(harness.NormalizedServiceUrl, client.Config.ServiceURL);

            foreach (var tableName in harness.ExpectedTableNames)
            {
                var response = await dynamoDb.DescribeTableAsync(tableName);
                Assert.Equal(TableStatus.ACTIVE, response.Table.TableStatus);
            }
        }
        finally
        {
            await harness.DeleteProvisionedTablesAsync(dynamoDb);
        }
    }

    [Fact]
    public async Task CreateClient_WithLocalEndpoint_ExposesPayFastNotifyRoute()
    {
        using var harness = new LocalDynamoDbTestHarness();
        await using var factory = harness.CreateFactory();
        using var client = factory.CreateClient();

        try
        {
            using var response = await client.PostAsync("/api/payments/payfast/notify", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["m_payment_id"] = Guid.NewGuid().ToString("N"),
                ["signature"] = "invalid"
            }));

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var dynamoDb = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
            await harness.DeleteProvisionedTablesAsync(dynamoDb);
        }
    }
}
