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

    public DynamoDbUserRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_users";
    }

    public async Task AddAsync(User user)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = user.Id.ToString() },
            ["email"] = new AttributeValue { S = user.Email.ToLower() },
            ["passwordHash"] = new AttributeValue { S = user.PasswordHash },
            ["createdAt"] = new AttributeValue { S = user.CreatedAt.ToString("O") },
        };

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item,
        });
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
