using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using System.Globalization;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

public sealed class DynamoDbWalletActivityRepository : IWalletActivityRepository
{
    private const int QueryPageSize = 50;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoDbWalletActivityRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_wallet_activities";
    }

    public async Task<IReadOnlyList<WalletActivity>> GetByWalletIdAsync(Guid walletId)
    {
        var items = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null;

        do
        {
            var response = await _dynamoDb.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "walletId-index",
                KeyConditionExpression = "walletId = :walletId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":walletId"] = new() { S = walletId.ToString() },
                },
                Limit = QueryPageSize,
                ScanIndexForward = false,
                ExclusiveStartKey = lastEvaluatedKey,
            });

            items.AddRange(response.Items);
            lastEvaluatedKey = response.LastEvaluatedKey;
        }
        while (lastEvaluatedKey is { Count: > 0 });

        return items.Select(Map).ToList();
    }

    public async Task AddAsync(WalletActivity activity)
    {
        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(activity),
        });
    }

    private static Dictionary<string, AttributeValue> ToItem(WalletActivity activity)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = activity.Id.ToString() },
            ["walletId"] = new() { S = activity.WalletId.ToString() },
            ["activityType"] = new() { S = activity.ActivityType.ToString() },
            ["amount"] = new() { S = activity.Amount.ToString("G", CultureInfo.InvariantCulture) },
            ["balanceAfterActivity"] = new() { S = activity.BalanceAfterActivity.ToString("G", CultureInfo.InvariantCulture) },
            ["occurredAt"] = new() { S = activity.OccurredAt.ToString("O") },
        };

        if (!string.IsNullOrWhiteSpace(activity.ReferenceType))
            item["referenceType"] = new() { S = activity.ReferenceType };
        if (!string.IsNullOrWhiteSpace(activity.ReferenceId))
            item["referenceId"] = new() { S = activity.ReferenceId };
        if (activity.PaymentReturnEvidenceId.HasValue)
            item["paymentReturnEvidenceId"] = new() { S = activity.PaymentReturnEvidenceId.Value.ToString() };
        if (!string.IsNullOrWhiteSpace(activity.Description))
            item["description"] = new() { S = activity.Description };

        return item;
    }

    private static WalletActivity Map(Dictionary<string, AttributeValue> item)
    {
        var activity = new WalletActivity();
        SetProperty(activity, nameof(WalletActivity.Id), Guid.Parse(item["id"].S));
        activity.WalletId = Guid.Parse(item["walletId"].S);
        activity.ActivityType = Enum.Parse<WalletActivityType>(item["activityType"].S);
        activity.Amount = decimal.Parse(item["amount"].S, CultureInfo.InvariantCulture);
        activity.BalanceAfterActivity = decimal.Parse(item["balanceAfterActivity"].S, CultureInfo.InvariantCulture);
        SetProperty(activity, nameof(WalletActivity.OccurredAt), DateTimeOffset.Parse(item["occurredAt"].S, CultureInfo.InvariantCulture));

        if (item.TryGetValue("referenceType", out var referenceType)) activity.ReferenceType = referenceType.S;
        if (item.TryGetValue("referenceId", out var referenceId)) activity.ReferenceId = referenceId.S;
        if (item.TryGetValue("paymentReturnEvidenceId", out var paymentReturnEvidenceId)) activity.PaymentReturnEvidenceId = Guid.Parse(paymentReturnEvidenceId.S);
        if (item.TryGetValue("description", out var description)) activity.Description = description.S;

        return activity;
    }

    private static void SetProperty<T>(T obj, string propertyName, object value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(obj, value);
    }
}
