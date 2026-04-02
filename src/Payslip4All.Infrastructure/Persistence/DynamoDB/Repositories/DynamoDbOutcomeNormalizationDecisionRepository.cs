using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using System.Globalization;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

public sealed class DynamoDbOutcomeNormalizationDecisionRepository : IOutcomeNormalizationDecisionRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoDbOutcomeNormalizationDecisionRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_outcome_normalization_decisions";
    }

    public Task AddAsync(OutcomeNormalizationDecision decision, CancellationToken cancellationToken = default)
        => _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(decision),
            ConditionExpression = "attribute_not_exists(id)"
        }, cancellationToken);

    public async Task<IReadOnlyList<OutcomeNormalizationDecision>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.ScanAsync(new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "attemptId = :attemptId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":attemptId"] = new() { S = attemptId.ToString() }
            }
        }, cancellationToken);

        return response.Items.Select(Map).OrderBy(d => d.DecidedAt).ToList();
    }

    private static Dictionary<string, AttributeValue> ToItem(OutcomeNormalizationDecision decision)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = decision.Id.ToString() },
            ["decisionType"] = new() { S = decision.DecisionType },
            ["appliedPrecedence"] = new() { S = decision.AppliedPrecedence },
            ["normalizedOutcome"] = new() { S = decision.NormalizedOutcome },
            ["decisionReasonCode"] = new() { S = decision.DecisionReasonCode },
            ["decisionSummary"] = new() { S = decision.DecisionSummary },
            ["supersededAbandonment"] = new() { BOOL = decision.SupersededAbandonment },
            ["conflictWithAcceptedFinalOutcome"] = new() { BOOL = decision.ConflictWithAcceptedFinalOutcome },
            ["walletEffect"] = new() { S = decision.WalletEffect },
            ["decidedAt"] = new() { S = decision.DecidedAt.ToString("O") }
        };

        SetOptional(item, "attemptId", decision.AttemptId?.ToString());
        SetOptional(item, "paymentReturnEvidenceId", decision.PaymentReturnEvidenceId?.ToString());
        SetOptional(item, "unmatchedPaymentReturnRecordId", decision.UnmatchedPaymentReturnRecordId?.ToString());
        SetOptional(item, "authoritativeOutcomeBefore", decision.AuthoritativeOutcomeBefore);
        SetOptional(item, "authoritativeOutcomeAfter", decision.AuthoritativeOutcomeAfter);
        SetOptional(item, "walletActivityId", decision.WalletActivityId?.ToString());
        return item;
    }

    private static OutcomeNormalizationDecision Map(Dictionary<string, AttributeValue> item)
    {
        var decision = new OutcomeNormalizationDecision
        {
            DecisionType = item["decisionType"].S,
            AppliedPrecedence = item["appliedPrecedence"].S,
            NormalizedOutcome = item["normalizedOutcome"].S,
            DecisionReasonCode = item["decisionReasonCode"].S,
            DecisionSummary = item["decisionSummary"].S,
            SupersededAbandonment = item["supersededAbandonment"].BOOL,
            ConflictWithAcceptedFinalOutcome = item["conflictWithAcceptedFinalOutcome"].BOOL,
            WalletEffect = item["walletEffect"].S
        };

        typeof(OutcomeNormalizationDecision).GetProperty(nameof(OutcomeNormalizationDecision.Id))!.SetValue(decision, Guid.Parse(item["id"].S));
        typeof(OutcomeNormalizationDecision).GetProperty(nameof(OutcomeNormalizationDecision.DecidedAt))!.SetValue(decision, DateTimeOffset.Parse(item["decidedAt"].S, CultureInfo.InvariantCulture));
        if (item.TryGetValue("attemptId", out var attemptId)) decision.AttemptId = Guid.Parse(attemptId.S);
        if (item.TryGetValue("paymentReturnEvidenceId", out var evidenceId)) decision.PaymentReturnEvidenceId = Guid.Parse(evidenceId.S);
        if (item.TryGetValue("unmatchedPaymentReturnRecordId", out var unmatchedId)) decision.UnmatchedPaymentReturnRecordId = Guid.Parse(unmatchedId.S);
        if (item.TryGetValue("authoritativeOutcomeBefore", out var before)) decision.AuthoritativeOutcomeBefore = before.S;
        if (item.TryGetValue("authoritativeOutcomeAfter", out var after)) decision.AuthoritativeOutcomeAfter = after.S;
        if (item.TryGetValue("walletActivityId", out var walletActivityId)) decision.WalletActivityId = Guid.Parse(walletActivityId.S);
        return decision;
    }

    private static void SetOptional(Dictionary<string, AttributeValue> item, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            item[key] = new AttributeValue { S = value };
    }
}
