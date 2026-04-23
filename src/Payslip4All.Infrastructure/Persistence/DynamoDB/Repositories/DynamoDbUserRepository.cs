using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

/// <summary>
/// DynamoDB implementation of <see cref="IUserRepository"/>.
/// </summary>
public sealed class DynamoDbUserRepository : IUserRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoDbUserRepository(IAmazonDynamoDB dynamoDb, DynamoDbTableNameProvider? tableNames = null)
    {
        _dynamoDb = dynamoDb;
        tableNames ??= DynamoDbTableNameProvider.CreateDefault();
        _tableName = tableNames.Users;
    }

    public async Task AddAsync(User user)
    {
        var normalizedEmail = user.Email.ToLowerInvariant();
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = user.Id.ToString() },
            ["email"] = new AttributeValue { S = normalizedEmail },
            ["passwordHash"] = new AttributeValue { S = user.PasswordHash },
            ["createdAt"] = new AttributeValue { S = user.CreatedAt.ToString("O") },
        };

        var reservationItem = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = GetEmailReservationId(normalizedEmail) },
            ["markerType"] = new AttributeValue { S = "email-reservation" },
            ["createdAt"] = new AttributeValue { S = user.CreatedAt.ToString("O") },
        };

        try
        {
            await _dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = new List<TransactWriteItem>
                {
                    new()
                    {
                        Put = new Put
                        {
                            TableName = _tableName,
                            Item = reservationItem,
                            ConditionExpression = "attribute_not_exists(id)",
                        },
                    },
                    new()
                    {
                        Put = new Put
                        {
                            TableName = _tableName,
                            Item = item,
                            ConditionExpression = "attribute_not_exists(id)",
                        },
                    },
                },
            });
        }
        catch (TransactionCanceledException ex)
        {
            throw new InvalidOperationException($"A user with email '{normalizedEmail}' already exists.", ex);
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "email-index",
            KeyConditionExpression = "email = :email",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":email"] = new AttributeValue { S = email.ToLower() },
            },
            Limit = 1,
        });

        return response.Items.Count == 0 ? null : MapToUser(response.Items[0]);
    }

    public async Task<bool> ExistsAsync(string email)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "email-index",
            KeyConditionExpression = "email = :email",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":email"] = new AttributeValue { S = email.ToLower() },
            },
            Select = Select.COUNT,
        });

        return response.Count > 0;
    }

    private static string GetEmailReservationId(string normalizedEmail)
        => $"EMAIL#{normalizedEmail}";

    private static User MapToUser(Dictionary<string, AttributeValue> item)
    {
        var user = new User();
        SetPrivateId(user, Guid.Parse(item["id"].S));
        user.Email = item["email"].S;
        user.PasswordHash = item["passwordHash"].S;
        SetPrivateCreatedAt(user, DateTimeOffset.Parse(item["createdAt"].S));
        return user;
    }

    private static void SetPrivateId(User user, Guid id)
    {
        var prop = typeof(User).GetProperty("Id")!;
        prop.SetValue(user, id);
    }

    private static void SetPrivateCreatedAt(User user, DateTimeOffset createdAt)
    {
        var prop = typeof(User).GetProperty("CreatedAt")!;
        prop.SetValue(user, createdAt);
    }
}
