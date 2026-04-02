using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using System.Globalization;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

public sealed class DynamoDbUnmatchedPaymentReturnRecordRepository : IUnmatchedPaymentReturnRecordRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoDbUnmatchedPaymentReturnRecordRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_unmatched_payment_return_records";
    }

    public Task AddAsync(UnmatchedPaymentReturnRecord record, CancellationToken cancellationToken = default)
        => _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(record),
            ConditionExpression = "attribute_not_exists(id)"
        }, cancellationToken);

    public async Task<UnmatchedPaymentReturnRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue> { ["id"] = new() { S = id.ToString() } },
            ConsistentRead = true
        }, cancellationToken);

        return response.IsItemSet ? Map(response.Item) : null;
    }

    private static Dictionary<string, AttributeValue> ToItem(UnmatchedPaymentReturnRecord record)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = record.Id.ToString() },
            ["primaryEvidenceId"] = new() { S = record.PrimaryEvidenceId.ToString() },
            ["providerKey"] = new() { S = record.ProviderKey },
            ["correlationDisposition"] = new() { S = record.CorrelationDisposition },
            ["genericResultCode"] = new() { S = record.GenericResultCode },
            ["displayMessage"] = new() { S = record.DisplayMessage },
            ["receivedAt"] = new() { S = record.ReceivedAt.ToString("O") },
            ["createdAt"] = new() { S = record.CreatedAt.ToString("O") }
        };

        if (!string.IsNullOrWhiteSpace(record.SafePayloadSnapshot))
            item["safePayloadSnapshot"] = new() { S = record.SafePayloadSnapshot };

        return item;
    }

    private static UnmatchedPaymentReturnRecord Map(Dictionary<string, AttributeValue> item)
    {
        var record = new UnmatchedPaymentReturnRecord
        {
            PrimaryEvidenceId = Guid.Parse(item["primaryEvidenceId"].S),
            ProviderKey = item["providerKey"].S,
            CorrelationDisposition = item["correlationDisposition"].S,
            GenericResultCode = item["genericResultCode"].S,
            DisplayMessage = item["displayMessage"].S,
            ReceivedAt = DateTimeOffset.Parse(item["receivedAt"].S, CultureInfo.InvariantCulture),
            SafePayloadSnapshot = item.TryGetValue("safePayloadSnapshot", out var payload) ? payload.S : null
        };

        typeof(UnmatchedPaymentReturnRecord).GetProperty(nameof(UnmatchedPaymentReturnRecord.Id))!.SetValue(record, Guid.Parse(item["id"].S));
        typeof(UnmatchedPaymentReturnRecord).GetProperty(nameof(UnmatchedPaymentReturnRecord.CreatedAt))!.SetValue(record, DateTimeOffset.Parse(item["createdAt"].S, CultureInfo.InvariantCulture));
        return record;
    }
}
