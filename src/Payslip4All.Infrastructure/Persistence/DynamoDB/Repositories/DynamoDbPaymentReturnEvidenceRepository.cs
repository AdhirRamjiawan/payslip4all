using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using System.Globalization;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

public sealed class DynamoDbPaymentReturnEvidenceRepository : IPaymentReturnEvidenceRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoDbPaymentReturnEvidenceRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_payment_return_evidences";
    }

    public Task AddAsync(PaymentReturnEvidence evidence, CancellationToken cancellationToken = default)
        => _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(evidence),
            ConditionExpression = "attribute_not_exists(id)"
        }, cancellationToken);

    public async Task<PaymentReturnEvidence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue> { ["id"] = new() { S = id.ToString() } },
            ConsistentRead = true
        }, cancellationToken);

        return response.IsItemSet ? Map(response.Item) : null;
    }

    public async Task<IReadOnlyList<PaymentReturnEvidence>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.ScanAsync(new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "matchedAttemptId = :attemptId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":attemptId"] = new() { S = attemptId.ToString() }
            }
        }, cancellationToken);

        return response.Items.Select(Map).OrderBy(e => e.ReceivedAt).ToList();
    }

    private static Dictionary<string, AttributeValue> ToItem(PaymentReturnEvidence evidence)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = evidence.Id.ToString() },
            ["providerKey"] = new() { S = evidence.ProviderKey },
            ["sourceChannel"] = new() { S = evidence.SourceChannel },
            ["correlationDisposition"] = new() { N = ((int)evidence.CorrelationDisposition).ToString(CultureInfo.InvariantCulture) },
            ["trustLevel"] = new() { N = ((int)evidence.TrustLevel).ToString(CultureInfo.InvariantCulture) },
            ["receivedAt"] = new() { S = evidence.ReceivedAt.ToString("O") },
            ["validatedAt"] = new() { S = evidence.ValidatedAt.ToString("O") },
            ["isAtOrAfterAbandonmentThreshold"] = new() { BOOL = evidence.IsAtOrAfterAbandonmentThreshold }
        };

        SetOptional(item, "providerSessionReference", evidence.ProviderSessionReference);
        SetOptional(item, "providerPaymentReference", evidence.ProviderPaymentReference);
        SetOptional(item, "returnCorrelationToken", evidence.ReturnCorrelationToken);
        SetOptional(item, "matchedAttemptId", evidence.MatchedAttemptId?.ToString());
        SetOptional(item, "claimedOutcome", evidence.ClaimedOutcome.HasValue ? ((int)evidence.ClaimedOutcome.Value).ToString(CultureInfo.InvariantCulture) : null, true);
        SetOptional(item, "confirmedChargedAmount", evidence.ConfirmedChargedAmount?.ToString("G", CultureInfo.InvariantCulture));
        SetOptional(item, "evidenceOccurredAt", evidence.EvidenceOccurredAt?.ToString("O"));
        SetOptional(item, "safePayloadSnapshot", evidence.SafePayloadSnapshot);
        SetOptional(item, "validationMessage", evidence.ValidationMessage);
        return item;
    }

    private static PaymentReturnEvidence Map(Dictionary<string, AttributeValue> item)
    {
        var evidence = new PaymentReturnEvidence
        {
            ProviderKey = item["providerKey"].S,
            SourceChannel = item["sourceChannel"].S,
            CorrelationDisposition = (PaymentReturnCorrelationDisposition)int.Parse(item["correlationDisposition"].N, CultureInfo.InvariantCulture),
            TrustLevel = (PaymentReturnTrustLevel)int.Parse(item["trustLevel"].N, CultureInfo.InvariantCulture),
            ValidatedAt = DateTimeOffset.Parse(item["validatedAt"].S, CultureInfo.InvariantCulture),
            IsAtOrAfterAbandonmentThreshold = item["isAtOrAfterAbandonmentThreshold"].BOOL
        };

        typeof(PaymentReturnEvidence).GetProperty(nameof(PaymentReturnEvidence.Id))!.SetValue(evidence, Guid.Parse(item["id"].S));
        typeof(PaymentReturnEvidence).GetProperty(nameof(PaymentReturnEvidence.ReceivedAt))!.SetValue(evidence, DateTimeOffset.Parse(item["receivedAt"].S, CultureInfo.InvariantCulture));
        if (item.TryGetValue("providerSessionReference", out var providerSessionReference)) evidence.ProviderSessionReference = providerSessionReference.S;
        if (item.TryGetValue("providerPaymentReference", out var providerPaymentReference)) evidence.ProviderPaymentReference = providerPaymentReference.S;
        if (item.TryGetValue("returnCorrelationToken", out var token)) evidence.ReturnCorrelationToken = token.S;
        if (item.TryGetValue("matchedAttemptId", out var matchedAttemptId)) evidence.MatchedAttemptId = Guid.Parse(matchedAttemptId.S);
        if (item.TryGetValue("claimedOutcome", out var claimedOutcome)) evidence.ClaimedOutcome = (PaymentReturnClaimedOutcome)int.Parse(claimedOutcome.N, CultureInfo.InvariantCulture);
        if (item.TryGetValue("confirmedChargedAmount", out var amount)) evidence.ConfirmedChargedAmount = decimal.Parse(amount.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("evidenceOccurredAt", out var evidenceOccurredAt)) evidence.EvidenceOccurredAt = DateTimeOffset.Parse(evidenceOccurredAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("safePayloadSnapshot", out var payload)) evidence.SafePayloadSnapshot = payload.S;
        if (item.TryGetValue("validationMessage", out var validationMessage)) evidence.ValidationMessage = validationMessage.S;
        return evidence;
    }

    private static void SetOptional(Dictionary<string, AttributeValue> item, string key, string? value, bool numeric = false)
    {
        if (!string.IsNullOrWhiteSpace(value))
            item[key] = numeric ? new AttributeValue { N = value } : new AttributeValue { S = value };
    }
}
