using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Globalization;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Domain.Services;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

public sealed class DynamoDbWalletTopUpAttemptRepository : IWalletTopUpAttemptRepository
{
    private const int QueryPageSize = 50;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private readonly string _walletsTableName;
    private readonly string _walletActivitiesTableName;

    public DynamoDbWalletTopUpAttemptRepository(IAmazonDynamoDB dynamoDb, DynamoDbTableNameProvider? tableNames = null)
    {
        _dynamoDb = dynamoDb;
        tableNames ??= DynamoDbTableNameProvider.CreateDefault();
        _tableName = tableNames.WalletTopUpAttempts;
        _walletsTableName = tableNames.Wallets;
        _walletActivitiesTableName = tableNames.WalletActivities;
    }

    public async Task AddAsync(WalletTopUpAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(attempt),
            ConditionExpression = "attribute_not_exists(id)"
        }, cancellationToken);
    }

    public async Task UpdateAsync(WalletTopUpAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(attempt),
            ConditionExpression = "attribute_exists(id) AND userId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":userId"] = new() { S = attempt.UserId.ToString() }
            }
        }, cancellationToken);
    }

    public async Task<WalletTopUpAttempt?> GetByIdAsync(Guid attemptId, Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = attemptId.ToString() }
            },
            ConsistentRead = true
        }, cancellationToken);

        if (!response.IsItemSet)
            return null;

        var attempt = Map(response.Item);
        return attempt.UserId == userId ? attempt : null;
    }

    public async Task<WalletTopUpAttempt?> GetAnyByIdAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = attemptId.ToString() }
            },
            ConsistentRead = true
        }, cancellationToken);

        return response.IsItemSet ? Map(response.Item) : null;
    }

    public async Task<IReadOnlyList<WalletTopUpAttempt>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null;

        do
        {
            var response = await _dynamoDb.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "userId-createdAt-index",
                KeyConditionExpression = "userId = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":userId"] = new() { S = userId.ToString() }
                },
                ScanIndexForward = false,
                Limit = QueryPageSize,
                ExclusiveStartKey = lastEvaluatedKey
            }, cancellationToken);

            items.AddRange(response.Items);
            lastEvaluatedKey = response.LastEvaluatedKey;
        }
        while (lastEvaluatedKey is { Count: > 0 });

        return items.Select(Map).ToList();
    }

    public async Task<WalletTopUpAttempt?> GetByCorrelationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.ScanAsync(new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "returnCorrelationToken = :token",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":token"] = new() { S = token }
            },
            Limit = 1
        }, cancellationToken);

        return response.Items.Count == 0 ? null : Map(response.Items[0]);
    }

    public async Task<IReadOnlyList<WalletTopUpAttempt>> GetByMerchantPaymentReferenceAsync(string merchantPaymentReference, CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.ScanAsync(new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "merchantPaymentReference = :merchantPaymentReference",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":merchantPaymentReference"] = new() { S = merchantPaymentReference }
            }
        }, cancellationToken);

        return response.Items.Select(Map).OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task<IReadOnlyList<WalletTopUpAttempt>> GetDueForReconciliationAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        var items = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null;

        do
        {
            var response = await _dynamoDb.ScanAsync(new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = "attribute_exists(nextReconciliationDueAt) AND nextReconciliationDueAt <= :cutoff AND (#status = :pending OR #status = :notConfirmed OR #status = :expired)",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#status"] = "status" },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":cutoff"] = new() { S = cutoff.ToString("O") },
                    [":pending"] = new() { N = ((int)WalletTopUpAttemptStatus.Pending).ToString(CultureInfo.InvariantCulture) },
                    [":notConfirmed"] = new() { N = ((int)WalletTopUpAttemptStatus.NotConfirmed).ToString(CultureInfo.InvariantCulture) },
                    [":expired"] = new() { N = ((int)WalletTopUpAttemptStatus.Expired).ToString(CultureInfo.InvariantCulture) }
                },
                Limit = QueryPageSize,
                ExclusiveStartKey = lastEvaluatedKey
            }, cancellationToken);

            items.AddRange(response.Items);
            lastEvaluatedKey = response.LastEvaluatedKey;
        }
        while (lastEvaluatedKey is { Count: > 0 });

        return items.Select(Map).OrderBy(a => a.NextReconciliationDueAt).ToList();
    }

    public async Task<IReadOnlyList<WalletTopUpAttempt>> GetForAdminReviewAsync(Guid? attemptId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, WalletTopUpAttemptStatus? status, CancellationToken cancellationToken = default)
    {
        if (attemptId.HasValue)
        {
            var attempt = await GetAnyByIdAsync(attemptId.Value, cancellationToken);
            if (attempt == null)
                return Array.Empty<WalletTopUpAttempt>();

            return MatchesFilters(attempt, fromUtc, toUtc, status)
                ? new[] { attempt }
                : Array.Empty<WalletTopUpAttempt>();
        }

        var response = await _dynamoDb.ScanAsync(new ScanRequest
        {
            TableName = _tableName
        }, cancellationToken);

        return response.Items
            .Select(Map)
            .Where(a => MatchesFilters(a, fromUtc, toUtc, status))
            .OrderByDescending(a => a.CreatedAt)
            .ToList();
    }

    public async Task<WalletTopUpSettlementResult> SettleSuccessfulAsync(WalletTopUpAttempt attempt, CancellationToken cancellationToken = default)
    {
        for (var retry = 0; retry < 3; retry++)
        {
            var persistedAttempt = await GetByIdAsync(attempt.Id, attempt.UserId, cancellationToken)
                                   ?? throw new InvalidOperationException("Top-up attempt could not be found.");

            if (persistedAttempt.CreditedWalletActivityId.HasValue)
            {
                var existingWallet = await LoadWalletAsync(attempt.UserId, cancellationToken);
                return new WalletTopUpSettlementResult
                {
                    WalletId = existingWallet?.Id ?? attempt.UserId,
                    WalletActivityId = persistedAttempt.CreditedWalletActivityId.Value,
                    WalletBalance = existingWallet?.CurrentBalance ?? 0m,
                    CreditedNow = false
                };
            }

            if (!attempt.ConfirmedChargedAmount.HasValue)
                throw new InvalidOperationException("A confirmed charged amount is required before settlement.");

            var wallet = await LoadWalletAsync(attempt.UserId, cancellationToken);
            var isNewWallet = wallet == null;
            wallet ??= Wallet.CreateForUser(attempt.UserId);
            var expectedUpdatedAt = wallet.UpdatedAt;
            wallet.CurrentBalance = WalletCalculator.CalculateBalanceAfterCredit(wallet.CurrentBalance, attempt.ConfirmedChargedAmount.Value);
            wallet.UpdatedAt = DateTimeOffset.UtcNow;

            var activity = new WalletActivity
            {
                WalletId = wallet.Id,
                ActivityType = WalletActivityType.Credit,
                Amount = attempt.ConfirmedChargedAmount.Value,
                PaymentReturnEvidenceId = attempt.AuthoritativeEvidenceId,
                Description = WalletActivity.HostedCardTopUpDescription,
                ReferenceType = WalletActivity.WalletTopUpReferenceType,
                ReferenceId = attempt.Id.ToString(),
                BalanceAfterActivity = wallet.CurrentBalance
            };

            try
            {
                await _dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
                {
                    TransactItems = new List<TransactWriteItem>
                    {
                        new()
                        {
                            Update = new Update
                            {
                                TableName = _tableName,
                                Key = new Dictionary<string, AttributeValue>
                                {
                                    ["id"] = new() { S = attempt.Id.ToString() }
                                },
                                ConditionExpression = "attribute_exists(id) AND userId = :userId AND attribute_not_exists(creditedWalletActivityId)",
                                UpdateExpression =
                                    "SET #status = :status, confirmedChargedAmount = :confirmedAmount, providerPaymentReference = :paymentReference, " +
                                    "creditedWalletActivityId = :creditedWalletActivityId, lastValidatedAt = :lastValidatedAt, lastEvaluatedAt = :lastEvaluatedAt, completedAt = :completedAt, " +
                                    "authoritativeOutcomeAcceptedAt = :acceptedAt, updatedAt = :updatedAt, authoritativeEvidenceId = :authoritativeEvidenceId, lastEvidenceReceivedAt = :lastEvidenceReceivedAt " +
                                    "REMOVE failureCode, failureMessage, outcomeReasonCode, outcomeMessage",
                                ExpressionAttributeNames = new Dictionary<string, string>
                                {
                                    ["#status"] = "status"
                                },
                                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                                {
                                    [":userId"] = new() { S = attempt.UserId.ToString() },
                                    [":status"] = new() { N = ((int)WalletTopUpAttemptStatus.Completed).ToString(CultureInfo.InvariantCulture) },
                                    [":confirmedAmount"] = new() { S = attempt.ConfirmedChargedAmount.Value.ToString("G", CultureInfo.InvariantCulture) },
                                    [":paymentReference"] = new() { S = attempt.ProviderPaymentReference ?? string.Empty },
                                    [":creditedWalletActivityId"] = new() { S = activity.Id.ToString() },
                                    [":lastValidatedAt"] = new() { S = (attempt.LastValidatedAt ?? DateTimeOffset.UtcNow).ToString("O") },
                                    [":lastEvaluatedAt"] = new() { S = (attempt.LastEvaluatedAt ?? attempt.LastValidatedAt ?? DateTimeOffset.UtcNow).ToString("O") },
                                    [":completedAt"] = new() { S = (attempt.AuthoritativeOutcomeAcceptedAt ?? attempt.LastValidatedAt ?? DateTimeOffset.UtcNow).ToString("O") },
                                    [":acceptedAt"] = new() { S = (attempt.AuthoritativeOutcomeAcceptedAt ?? attempt.LastValidatedAt ?? DateTimeOffset.UtcNow).ToString("O") },
                                    [":updatedAt"] = new() { S = DateTimeOffset.UtcNow.ToString("O") },
                                    [":authoritativeEvidenceId"] = new() { S = attempt.AuthoritativeEvidenceId?.ToString() ?? string.Empty },
                                    [":lastEvidenceReceivedAt"] = new() { S = (attempt.LastEvidenceReceivedAt ?? attempt.LastValidatedAt ?? DateTimeOffset.UtcNow).ToString("O") }
                                }
                            }
                        },
                        new()
                        {
                            Put = new Put
                            {
                                TableName = _walletsTableName,
                                Item = ToWalletItem(wallet),
                                ConditionExpression = isNewWallet
                                    ? "attribute_not_exists(id)"
                                    : "attribute_exists(id) AND userId = :walletUserId AND updatedAt = :expectedUpdatedAt",
                                ExpressionAttributeValues = isNewWallet
                                    ? null
                                    : new Dictionary<string, AttributeValue>
                                    {
                                        [":walletUserId"] = new() { S = wallet.UserId.ToString() },
                                        [":expectedUpdatedAt"] = new() { S = expectedUpdatedAt.ToString("O") }
                                    }
                            }
                        },
                        new()
                        {
                            Put = new Put
                            {
                                TableName = _walletActivitiesTableName,
                                Item = ToWalletActivityItem(activity),
                                ConditionExpression = "attribute_not_exists(id)"
                            }
                        }
                    }
                }, cancellationToken);

                return new WalletTopUpSettlementResult
                {
                    WalletId = wallet.Id,
                    WalletActivityId = activity.Id,
                    WalletBalance = wallet.CurrentBalance,
                    CreditedNow = true
                };
            }
            catch (TransactionCanceledException)
            {
            }
        }

        var settledAttempt = await GetByIdAsync(attempt.Id, attempt.UserId, cancellationToken)
                            ?? throw new InvalidOperationException("Top-up attempt could not be found.");
        var settledWallet = await LoadWalletAsync(attempt.UserId, cancellationToken);

        if (settledAttempt.CreditedWalletActivityId.HasValue)
        {
            return new WalletTopUpSettlementResult
            {
                WalletId = settledWallet?.Id ?? attempt.UserId,
                WalletActivityId = settledAttempt.CreditedWalletActivityId.Value,
                WalletBalance = settledWallet?.CurrentBalance ?? 0m,
                CreditedNow = false
            };
        }

        throw new InvalidOperationException("The wallet top-up could not be settled.");
    }

    private async Task<Wallet?> LoadWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _walletsTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = userId.ToString() }
            },
            ConsistentRead = true
        }, cancellationToken);

        return response.IsItemSet ? MapWallet(response.Item) : null;
    }

    private static Dictionary<string, AttributeValue> ToItem(WalletTopUpAttempt attempt)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = attempt.Id.ToString() },
            ["userId"] = new() { S = attempt.UserId.ToString() },
            ["requestedAmount"] = new() { S = attempt.RequestedAmount.ToString("G", CultureInfo.InvariantCulture) },
            ["currencyCode"] = new() { S = attempt.CurrencyCode },
            ["status"] = new() { N = ((int)attempt.Status).ToString(CultureInfo.InvariantCulture) },
            ["providerKey"] = new() { S = attempt.ProviderKey },
            ["merchantPaymentReference"] = new() { S = attempt.MerchantPaymentReference },
            ["createdAt"] = new() { S = attempt.CreatedAt.ToString("O") },
            ["updatedAt"] = new() { S = attempt.UpdatedAt.ToString("O") },
            ["abandonAfterUtc"] = new() { S = attempt.AbandonAfterUtc.ToString("O") }
        };

        SetOptional(item, "confirmedChargedAmount", attempt.ConfirmedChargedAmount?.ToString("G", CultureInfo.InvariantCulture));
        SetOptional(item, "providerSessionReference", attempt.ProviderSessionReference);
        SetOptional(item, "providerPaymentReference", attempt.ProviderPaymentReference);
        SetOptional(item, "returnCorrelationToken", attempt.ReturnCorrelationToken);
        SetOptional(item, "failureCode", attempt.FailureCode);
        SetOptional(item, "failureMessage", attempt.FailureMessage);
        SetOptional(item, "outcomeReasonCode", attempt.OutcomeReasonCode);
        SetOptional(item, "outcomeMessage", attempt.OutcomeMessage);
        SetOptional(item, "creditedWalletActivityId", attempt.CreditedWalletActivityId?.ToString());
        SetOptional(item, "authoritativeEvidenceId", attempt.AuthoritativeEvidenceId?.ToString());
        SetOptional(item, "redirectedAt", attempt.RedirectedAt?.ToString("O"));
        SetOptional(item, "lastValidatedAt", attempt.LastValidatedAt?.ToString("O"));
        SetOptional(item, "lastEvaluatedAt", attempt.LastEvaluatedAt?.ToString("O"));
        SetOptional(item, "lastEvidenceReceivedAt", attempt.LastEvidenceReceivedAt?.ToString("O"));
        SetOptional(item, "lastReconciledAt", attempt.LastReconciledAt?.ToString("O"));
        SetOptional(item, "cancelledAt", attempt.CancelledAt?.ToString("O"));
        SetOptional(item, "expiredAt", attempt.ExpiredAt?.ToString("O"));
        SetOptional(item, "abandonedAt", attempt.AbandonedAt?.ToString("O"));
        SetOptional(item, "completedAt", attempt.CompletedAt?.ToString("O"));
        SetOptional(item, "authoritativeOutcomeAcceptedAt", attempt.AuthoritativeOutcomeAcceptedAt?.ToString("O"));
        SetOptional(item, "hostedPageDeadline", attempt.HostedPageDeadline?.ToString("O"));
        SetOptional(item, "nextReconciliationDueAt", attempt.NextReconciliationDueAt?.ToString("O"));
        return item;
    }

    private static WalletTopUpAttempt Map(Dictionary<string, AttributeValue> item)
    {
        var attempt = new WalletTopUpAttempt();
        SetProperty(attempt, nameof(WalletTopUpAttempt.Id), Guid.Parse(item["id"].S));
        attempt.UserId = Guid.Parse(item["userId"].S);
        attempt.RequestedAmount = decimal.Parse(item["requestedAmount"].S, CultureInfo.InvariantCulture);
        attempt.CurrencyCode = item["currencyCode"].S;
        attempt.Status = (WalletTopUpAttemptStatus)int.Parse(item["status"].N, CultureInfo.InvariantCulture);
        attempt.ProviderKey = item["providerKey"].S;
        attempt.MerchantPaymentReference = item["merchantPaymentReference"].S;
        SetProperty(attempt, nameof(WalletTopUpAttempt.CreatedAt), DateTimeOffset.Parse(item["createdAt"].S, CultureInfo.InvariantCulture));
        attempt.UpdatedAt = DateTimeOffset.Parse(item["updatedAt"].S, CultureInfo.InvariantCulture);
        attempt.AbandonAfterUtc = DateTimeOffset.Parse(item["abandonAfterUtc"].S, CultureInfo.InvariantCulture);

        if (item.TryGetValue("confirmedChargedAmount", out var confirmedAmount))
            attempt.ConfirmedChargedAmount = decimal.Parse(confirmedAmount.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("providerSessionReference", out var providerSessionReference))
            attempt.ProviderSessionReference = providerSessionReference.S;
        if (item.TryGetValue("providerPaymentReference", out var providerPaymentReference))
            attempt.ProviderPaymentReference = providerPaymentReference.S;
        if (item.TryGetValue("returnCorrelationToken", out var returnCorrelationToken))
            attempt.ReturnCorrelationToken = returnCorrelationToken.S;
        if (item.TryGetValue("failureCode", out var failureCode))
            attempt.FailureCode = failureCode.S;
        if (item.TryGetValue("failureMessage", out var failureMessage))
            attempt.FailureMessage = failureMessage.S;
        if (item.TryGetValue("outcomeReasonCode", out var outcomeReasonCode))
            attempt.OutcomeReasonCode = outcomeReasonCode.S;
        if (item.TryGetValue("outcomeMessage", out var outcomeMessage))
            attempt.OutcomeMessage = outcomeMessage.S;
        if (item.TryGetValue("creditedWalletActivityId", out var creditedWalletActivityId))
            attempt.CreditedWalletActivityId = Guid.Parse(creditedWalletActivityId.S);
        if (item.TryGetValue("authoritativeEvidenceId", out var authoritativeEvidenceId) && !string.IsNullOrWhiteSpace(authoritativeEvidenceId.S))
            attempt.AuthoritativeEvidenceId = Guid.Parse(authoritativeEvidenceId.S);
        if (item.TryGetValue("redirectedAt", out var redirectedAt))
            attempt.RedirectedAt = DateTimeOffset.Parse(redirectedAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("lastValidatedAt", out var lastValidatedAt))
            attempt.LastValidatedAt = DateTimeOffset.Parse(lastValidatedAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("lastEvaluatedAt", out var lastEvaluatedAt))
            attempt.LastEvaluatedAt = DateTimeOffset.Parse(lastEvaluatedAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("lastEvidenceReceivedAt", out var lastEvidenceReceivedAt))
            attempt.LastEvidenceReceivedAt = DateTimeOffset.Parse(lastEvidenceReceivedAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("lastReconciledAt", out var lastReconciledAt))
            attempt.LastReconciledAt = DateTimeOffset.Parse(lastReconciledAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("cancelledAt", out var cancelledAt))
            attempt.CancelledAt = DateTimeOffset.Parse(cancelledAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("expiredAt", out var expiredAt))
            attempt.ExpiredAt = DateTimeOffset.Parse(expiredAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("abandonedAt", out var abandonedAt))
            attempt.AbandonedAt = DateTimeOffset.Parse(abandonedAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("completedAt", out var completedAt))
            attempt.CompletedAt = DateTimeOffset.Parse(completedAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("authoritativeOutcomeAcceptedAt", out var acceptedAt))
            attempt.AuthoritativeOutcomeAcceptedAt = DateTimeOffset.Parse(acceptedAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("hostedPageDeadline", out var expiresAt))
            attempt.HostedPageDeadline = DateTimeOffset.Parse(expiresAt.S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("nextReconciliationDueAt", out var nextReconciliationDueAt))
            attempt.NextReconciliationDueAt = DateTimeOffset.Parse(nextReconciliationDueAt.S, CultureInfo.InvariantCulture);

        return attempt;
    }

    private static Dictionary<string, AttributeValue> ToWalletItem(Wallet wallet)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = wallet.Id.ToString() },
            ["userId"] = new() { S = wallet.UserId.ToString() },
            ["currentBalance"] = new() { S = wallet.CurrentBalance.ToString("G", CultureInfo.InvariantCulture) },
            ["createdAt"] = new() { S = wallet.CreatedAt.ToString("O") },
            ["updatedAt"] = new() { S = wallet.UpdatedAt.ToString("O") }
        };
    }

    private static Wallet MapWallet(Dictionary<string, AttributeValue> item)
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

    private static Dictionary<string, AttributeValue> ToWalletActivityItem(WalletActivity activity)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = activity.Id.ToString() },
            ["walletId"] = new() { S = activity.WalletId.ToString() },
            ["activityType"] = new() { S = activity.ActivityType.ToString() },
            ["amount"] = new() { S = activity.Amount.ToString("G", CultureInfo.InvariantCulture) },
            ["balanceAfterActivity"] = new() { S = activity.BalanceAfterActivity.ToString("G", CultureInfo.InvariantCulture) },
            ["occurredAt"] = new() { S = activity.OccurredAt.ToString("O") },
            ["description"] = new() { S = activity.Description ?? WalletActivity.HostedCardTopUpDescription },
            ["referenceType"] = new() { S = activity.ReferenceType ?? WalletActivity.WalletTopUpReferenceType },
            ["referenceId"] = new() { S = activity.ReferenceId ?? string.Empty }
        };

        if (activity.PaymentReturnEvidenceId.HasValue)
            item["paymentReturnEvidenceId"] = new() { S = activity.PaymentReturnEvidenceId.Value.ToString() };

        return item;
    }

    private static void SetOptional(Dictionary<string, AttributeValue> item, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            item[key] = new AttributeValue { S = value };
    }

    private static bool MatchesFilters(WalletTopUpAttempt attempt, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, WalletTopUpAttemptStatus? status)
        => (!fromUtc.HasValue || attempt.CreatedAt >= fromUtc.Value)
            && (!toUtc.HasValue || attempt.CreatedAt <= toUtc.Value)
            && (!status.HasValue || attempt.Status == status.Value);

    private static void SetProperty<T>(T obj, string propertyName, object value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(obj, value);
    }
}
