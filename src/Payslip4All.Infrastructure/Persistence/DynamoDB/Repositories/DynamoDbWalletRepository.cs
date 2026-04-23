using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using System.Globalization;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

public sealed class DynamoDbWalletRepository : IWalletRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoDbWalletRepository(IAmazonDynamoDB dynamoDb, DynamoDbTableNameProvider? tableNames = null)
    {
        _dynamoDb = dynamoDb;
        tableNames ??= DynamoDbTableNameProvider.CreateDefault();
        _tableName = tableNames.Wallets;
    }

    public async Task<Wallet?> GetByUserIdAsync(Guid userId)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = userId.ToString() },
            },
            ConsistentRead = true,
        });

        return response.IsItemSet ? Map(response.Item) : null;
    }

    public async Task<Wallet?> GetByIdAsync(Guid id, Guid userId)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = id.ToString() },
            },
            ConsistentRead = true,
        });

        if (!response.IsItemSet)
            return null;

        var wallet = Map(response.Item);
        return wallet.UserId == userId ? wallet : null;
    }

    public async Task AddAsync(Wallet wallet)
    {
        wallet.EnsureCanonicalId();

        try
        {
            await _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = ToItem(wallet),
                ConditionExpression = "attribute_not_exists(id)",
            });
            wallet.CapturePersistedState();
        }
        catch (ConditionalCheckFailedException ex)
        {
            throw new InvalidOperationException("Wallet already exists for this user.", ex);
        }
    }

    public async Task UpdateAsync(Wallet wallet)
    {
        try
        {
            await _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = ToItem(wallet),
                ConditionExpression = "attribute_exists(id) AND userId = :userId AND updatedAt = :expectedUpdatedAt",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":userId"] = new() { S = wallet.UserId.ToString() },
                    [":expectedUpdatedAt"] = new() { S = wallet.GetPersistedUpdatedAt().ToString("O") },
                },
            });
            wallet.CapturePersistedState();
        }
        catch (ConditionalCheckFailedException ex)
        {
            throw new InvalidOperationException("Wallet was modified by another process.", ex);
        }
    }

    private static Dictionary<string, AttributeValue> ToItem(Wallet wallet)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = wallet.Id.ToString() },
            ["userId"] = new() { S = wallet.UserId.ToString() },
            ["currentBalance"] = new() { S = wallet.CurrentBalance.ToString("G", CultureInfo.InvariantCulture) },
            ["createdAt"] = new() { S = wallet.CreatedAt.ToString("O") },
            ["updatedAt"] = new() { S = wallet.UpdatedAt.ToString("O") },
        };
    }

    private static Wallet Map(Dictionary<string, AttributeValue> item)
    {
        var wallet = new Wallet();
        SetProperty(wallet, nameof(Wallet.Id), Guid.Parse(item["id"].S));
        wallet.UserId = Guid.Parse(item["userId"].S);
        wallet.CurrentBalance = decimal.Parse(item["currentBalance"].S, CultureInfo.InvariantCulture);
        SetProperty(wallet, nameof(Wallet.CreatedAt), DateTimeOffset.Parse(item["createdAt"].S, CultureInfo.InvariantCulture));
        wallet.UpdatedAt = DateTimeOffset.Parse(item["updatedAt"].S, CultureInfo.InvariantCulture);
        wallet.CapturePersistedState();
        return wallet;
    }

    private static void SetProperty<T>(T obj, string propertyName, object value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(obj, value);
    }
}
