using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Infrastructure.Tests.DynamoDB;

[Collection(DynamoDbTestCollection.Name)]
public class DynamoDbTableProvisionerUnitTests
{
    [Fact]
    public async Task StartAsync_WhenExistingTableIsCreating_WaitsForActive()
    {
        var dynamoDb = new Mock<IAmazonDynamoDB>();
        var logger = new Mock<ILogger<DynamoDbTableProvisioner>>();

        await WithPrefix("unitprov", () =>
        {
            SetupDefaultDescribe(dynamoDb);
            SetupDescribeSequence(
                dynamoDb,
                "unitprov_users",
                new Queue<Func<Task<DescribeTableResponse>>>(new[]
                {
                    () => Task.FromResult(new DescribeTableResponse
                    {
                        Table = new TableDescription { TableStatus = TableStatus.CREATING },
                    }),
                    () => Task.FromResult(new DescribeTableResponse
                    {
                        Table = new TableDescription { TableStatus = TableStatus.ACTIVE },
                    }),
                }));

            var sut = new DynamoDbTableProvisioner(
                dynamoDb.Object,
                logger.Object,
                new DynamoDbTableNameProvider(new DynamoDbConfigurationOptions { TablePrefix = "unitprov" }),
                activationTimeout: TimeSpan.FromSeconds(1),
                pollInterval: TimeSpan.Zero);

            return sut.StartAsync(CancellationToken.None);
        });

        dynamoDb.Verify(x => x.DescribeTableAsync("unitprov_users", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task StartAsync_WhenCreateTableRaces_WaitsForActive()
    {
        var dynamoDb = new Mock<IAmazonDynamoDB>();
        var logger = new Mock<ILogger<DynamoDbTableProvisioner>>();

        await WithPrefix("unitprov", async () =>
        {
            SetupDefaultDescribe(dynamoDb);
            SetupDescribeSequence(
                dynamoDb,
                "unitprov_users",
                new Queue<Func<Task<DescribeTableResponse>>>(new[]
                {
                    () => Task.FromException<DescribeTableResponse>(new ResourceNotFoundException("missing")),
                    () => Task.FromResult(new DescribeTableResponse
                    {
                        Table = new TableDescription { TableStatus = TableStatus.ACTIVE },
                    }),
                }));

            dynamoDb
                .Setup(x => x.CreateTableAsync(
                    It.Is<CreateTableRequest>(r => r.TableName == "unitprov_users"),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceInUseException("creating"));

            var sut = new DynamoDbTableProvisioner(
                dynamoDb.Object,
                logger.Object,
                new DynamoDbTableNameProvider(new DynamoDbConfigurationOptions { TablePrefix = "unitprov" }),
                activationTimeout: TimeSpan.FromSeconds(1),
                pollInterval: TimeSpan.Zero);

            await sut.StartAsync(CancellationToken.None);
        });

        dynamoDb.Verify(x => x.CreateTableAsync(
            It.Is<CreateTableRequest>(r => r.TableName == "unitprov_users"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenTableNeverBecomesActive_ThrowsTimeoutException()
    {
        var dynamoDb = new Mock<IAmazonDynamoDB>();
        var logger = new Mock<ILogger<DynamoDbTableProvisioner>>();

        await WithPrefix("unitprov", async () =>
        {
            SetupDefaultDescribe(dynamoDb);
            SetupDescribeSequence(
                dynamoDb,
                "unitprov_users",
                new Queue<Func<Task<DescribeTableResponse>>>(new[]
                {
                    () => Task.FromException<DescribeTableResponse>(new ResourceNotFoundException("missing")),
                    () => Task.FromResult(new DescribeTableResponse
                    {
                        Table = new TableDescription { TableStatus = TableStatus.CREATING },
                    }),
                    () => Task.FromResult(new DescribeTableResponse
                    {
                        Table = new TableDescription { TableStatus = TableStatus.CREATING },
                    }),
                }));

            dynamoDb
                .Setup(x => x.CreateTableAsync(
                    It.Is<CreateTableRequest>(r => r.TableName == "unitprov_users"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateTableResponse());

            var sut = new DynamoDbTableProvisioner(
                dynamoDb.Object,
                logger.Object,
                new DynamoDbTableNameProvider(new DynamoDbConfigurationOptions { TablePrefix = "unitprov" }),
                activationTimeout: TimeSpan.Zero,
                pollInterval: TimeSpan.Zero);

            await Assert.ThrowsAsync<TimeoutException>(() => sut.StartAsync(CancellationToken.None));
        });
    }

    private static async Task WithPrefix(string prefix, Func<Task> action)
    {
        var savedPrefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX");
        Environment.SetEnvironmentVariable("DYNAMODB_TABLE_PREFIX", prefix);

        try
        {
            await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("DYNAMODB_TABLE_PREFIX", savedPrefix);
        }
    }

    private static void SetupDefaultDescribe(Mock<IAmazonDynamoDB> dynamoDb)
    {
        dynamoDb
            .Setup(x => x.DescribeTableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeTableResponse
            {
                Table = new TableDescription { TableStatus = TableStatus.ACTIVE },
            });
    }

    private static void SetupDescribeSequence(
        Mock<IAmazonDynamoDB> dynamoDb,
        string tableName,
        Queue<Func<Task<DescribeTableResponse>>> responses)
    {
        dynamoDb
            .Setup(x => x.DescribeTableAsync(tableName, It.IsAny<CancellationToken>()))
            .Returns(() => responses.Peek().Invoke())
            .Callback(() =>
            {
                if (responses.Count > 1)
                    responses.Dequeue();
            });
    }
}
