using Payslip4All.Domain.Enums;
using Payslip4All.Domain.Services;

namespace Payslip4All.Domain.Entities;

public class WalletTopUpAttempt
{
    public Guid Id { get; private set; }
    public Guid UserId { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal? ConfirmedChargedAmount { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";
    public WalletTopUpAttemptStatus Status { get; set; }
    public string ProviderKey { get; set; } = string.Empty;
    public string? ProviderSessionReference { get; set; }
    public string? ProviderPaymentReference { get; set; }
    public string MerchantPaymentReference { get; set; } = string.Empty;
    public string? ReturnCorrelationToken { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string? OutcomeReasonCode { get; set; }
    public string? OutcomeMessage { get; set; }
    public Guid? CreditedWalletActivityId { get; set; }
    public Guid? AuthoritativeEvidenceId { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? RedirectedAt { get; set; }
    public DateTimeOffset? LastValidatedAt { get; set; }
    public DateTimeOffset? LastEvaluatedAt { get; set; }
    public DateTimeOffset? LastEvidenceReceivedAt { get; set; }
    public DateTimeOffset? LastReconciledAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
    public DateTimeOffset? AbandonedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? AuthoritativeOutcomeAcceptedAt { get; set; }
    public DateTimeOffset? HostedPageDeadline { get; set; }
    public DateTimeOffset? NextReconciliationDueAt { get; set; }
    /// <summary>Legacy compatibility field retained for existing persistence/tests.</summary>
    public DateTimeOffset AbandonAfterUtc { get; set; }

    public WalletTopUpAttempt()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        MerchantPaymentReference = Id.ToString("N");
        AbandonAfterUtc = CreatedAt.AddHours(1);
        Status = WalletTopUpAttemptStatus.Pending;
    }

    public static WalletTopUpAttempt CreatePending(Guid userId, decimal requestedAmount, string providerKey)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var attempt = new WalletTopUpAttempt
        {
            UserId = userId,
            RequestedAmount = requestedAmount,
            ProviderKey = providerKey,
            CurrencyCode = "ZAR",
            Status = WalletTopUpAttemptStatus.Pending
        };

        attempt.EnsureValid();
        return attempt;
    }

    public bool IsFinalForSettlement
        => Status is WalletTopUpAttemptStatus.Completed
            or WalletTopUpAttemptStatus.Cancelled
            or WalletTopUpAttemptStatus.Expired
            or WalletTopUpAttemptStatus.Abandoned;

    public bool IsUnresolved
        => Status is WalletTopUpAttemptStatus.Pending or WalletTopUpAttemptStatus.NotConfirmed or WalletTopUpAttemptStatus.Expired;

    public void RegisterHostedSession(string providerSessionReference, string returnCorrelationToken, DateTimeOffset? expiresAt)
    {
        if (IsFinalForSettlement || Status == WalletTopUpAttemptStatus.NotConfirmed)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        if (string.IsNullOrWhiteSpace(providerSessionReference))
            throw new ArgumentException("Provider session reference is required.", nameof(providerSessionReference));

        if (string.IsNullOrWhiteSpace(returnCorrelationToken))
            throw new ArgumentException("Return correlation token is required.", nameof(returnCorrelationToken));

        ProviderSessionReference = providerSessionReference.Trim();
        ReturnCorrelationToken = returnCorrelationToken.Trim();
        RedirectedAt = DateTimeOffset.UtcNow;
        HostedPageDeadline = expiresAt;
        NextReconciliationDueAt = expiresAt;
        if (expiresAt.HasValue)
            AbandonAfterUtc = expiresAt.Value.AddMinutes(1);
        UpdatedAt = DateTimeOffset.UtcNow;
        EnsureValid();
    }

    public void MarkPendingValidation(DateTimeOffset validatedAt, string? failureCode, string? failureMessage)
    {
        EnsurePendingLike();
        LastValidatedAt = validatedAt;
        LastEvaluatedAt = validatedAt;
        OutcomeReasonCode = Normalize(failureCode);
        OutcomeMessage = Normalize(failureMessage);
        FailureCode = OutcomeReasonCode;
        FailureMessage = OutcomeMessage;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void MarkNotConfirmed(string? reasonCode, string? message, DateTimeOffset at)
    {
        if (IsFinalForSettlement)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        Status = WalletTopUpAttemptStatus.NotConfirmed;
        LastEvaluatedAt = at;
        LastValidatedAt ??= at;
        OutcomeReasonCode = Normalize(reasonCode);
        OutcomeMessage = Normalize(message);
        FailureCode = OutcomeReasonCode;
        FailureMessage = OutcomeMessage;
        UpdatedAt = at;
        EnsureValid();
    }

    public void MarkUnverified(string? reasonCode, string? message, DateTimeOffset at)
        => MarkNotConfirmed(reasonCode, message, at);

    public void RecordValidatedSuccess(decimal confirmedChargedAmount, string? providerPaymentReference, DateTimeOffset validatedAt)
    {
        if (IsFinalForSettlement)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        WalletCalculator.ValidateAmount(confirmedChargedAmount);
        ConfirmedChargedAmount = confirmedChargedAmount;
        ProviderPaymentReference = Normalize(providerPaymentReference);
        LastValidatedAt = validatedAt;
        LastEvaluatedAt = validatedAt;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void MarkCompleted(decimal confirmedChargedAmount, string? providerPaymentReference, DateTimeOffset validatedAt, Guid creditedWalletActivityId)
    {
        if (creditedWalletActivityId == Guid.Empty)
            throw new ArgumentException("Credited wallet activity id is required.", nameof(creditedWalletActivityId));

        RecordValidatedSuccess(confirmedChargedAmount, providerPaymentReference, validatedAt);
        CreditedWalletActivityId = creditedWalletActivityId;
        Status = WalletTopUpAttemptStatus.Completed;
        CompletedAt = validatedAt;
        AuthoritativeOutcomeAcceptedAt = validatedAt;
        NextReconciliationDueAt = null;
        OutcomeReasonCode = null;
        OutcomeMessage = null;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void MarkCancelled(string? failureCode, string? failureMessage, DateTimeOffset validatedAt)
    {
        if (IsFinalForSettlement)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        Status = WalletTopUpAttemptStatus.Cancelled;
        CancelledAt = validatedAt;
        LastValidatedAt = validatedAt;
        LastEvaluatedAt = validatedAt;
        AuthoritativeOutcomeAcceptedAt = validatedAt;
        NextReconciliationDueAt = null;
        OutcomeReasonCode = Normalize(failureCode);
        OutcomeMessage = Normalize(failureMessage);
        FailureCode = OutcomeReasonCode;
        FailureMessage = OutcomeMessage;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void MarkExpired(string? failureCode, string? failureMessage, DateTimeOffset validatedAt, DateTimeOffset? nextReconciliationDueAt = null)
    {
        if (Status is WalletTopUpAttemptStatus.Completed or WalletTopUpAttemptStatus.Cancelled or WalletTopUpAttemptStatus.Abandoned)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        Status = WalletTopUpAttemptStatus.Expired;
        ExpiredAt = validatedAt;
        LastValidatedAt = validatedAt;
        LastEvaluatedAt = validatedAt;
        NextReconciliationDueAt = nextReconciliationDueAt;
        OutcomeReasonCode = Normalize(failureCode);
        OutcomeMessage = Normalize(failureMessage);
        FailureCode = OutcomeReasonCode;
        FailureMessage = OutcomeMessage;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void MarkAbandoned(DateTimeOffset at)
    {
        if (Status is WalletTopUpAttemptStatus.Completed or WalletTopUpAttemptStatus.Cancelled or WalletTopUpAttemptStatus.Abandoned)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        Status = WalletTopUpAttemptStatus.Abandoned;
        AbandonedAt = at;
        LastEvaluatedAt = at;
        NextReconciliationDueAt = null;
        OutcomeReasonCode = "abandoned";
        OutcomeMessage = "Top-up not confirmed";
        FailureCode = OutcomeReasonCode;
        FailureMessage = OutcomeMessage;
        UpdatedAt = at;
        EnsureValid();
    }

    public void RecordReconciled(DateTimeOffset at)
    {
        LastReconciledAt = at;
        UpdatedAt = at;
    }

    public void AcceptTrustworthyEvidence(
        Guid evidenceId,
        PaymentReturnClaimedOutcome claimedOutcome,
        decimal? confirmedAmount,
        string? paymentRef,
        DateTimeOffset at,
        Guid? creditedActivityId)
    {
        if (evidenceId == Guid.Empty)
            throw new ArgumentException("Evidence id is required.", nameof(evidenceId));

        if (IsFinalForSettlement)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        AuthoritativeEvidenceId = evidenceId;
        AuthoritativeOutcomeAcceptedAt = at;
        ProviderPaymentReference = Normalize(paymentRef) ?? ProviderPaymentReference;
        ConfirmedChargedAmount = confirmedAmount ?? ConfirmedChargedAmount;
        CreditedWalletActivityId = creditedActivityId ?? CreditedWalletActivityId;

        switch (claimedOutcome)
        {
            case PaymentReturnClaimedOutcome.Completed:
                if (!ConfirmedChargedAmount.HasValue)
                    throw new ArgumentException("Confirmed amount is required for completed outcomes.", nameof(confirmedAmount));
                Status = WalletTopUpAttemptStatus.Completed;
                CompletedAt = at;
                NextReconciliationDueAt = null;
                break;
            case PaymentReturnClaimedOutcome.Cancelled:
                Status = WalletTopUpAttemptStatus.Cancelled;
                CancelledAt = at;
                NextReconciliationDueAt = null;
                break;
            case PaymentReturnClaimedOutcome.Expired:
                Status = WalletTopUpAttemptStatus.Expired;
                ExpiredAt = at;
                NextReconciliationDueAt = at.AddMinutes(1);
                break;
            default:
                Status = WalletTopUpAttemptStatus.NotConfirmed;
                break;
        }

        LastValidatedAt = at;
        LastEvaluatedAt = at;
        OutcomeReasonCode = null;
        OutcomeMessage = null;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAt = at;
        EnsureValid();
    }

    public void EnsureValid()
    {
        if (UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(UserId));

        if (string.IsNullOrWhiteSpace(ProviderKey))
            throw new ArgumentException("ProviderKey is required.", nameof(ProviderKey));

        if (string.IsNullOrWhiteSpace(MerchantPaymentReference))
            throw new ArgumentException("MerchantPaymentReference is required.", nameof(MerchantPaymentReference));

        WalletCalculator.ValidateAmount(RequestedAmount);
        EnsureTwoDecimalPlaces(RequestedAmount, nameof(RequestedAmount));

        if (ConfirmedChargedAmount.HasValue)
        {
            WalletCalculator.ValidateAmount(ConfirmedChargedAmount.Value);
            EnsureTwoDecimalPlaces(ConfirmedChargedAmount.Value, nameof(ConfirmedChargedAmount));
        }

        if (!string.Equals(CurrencyCode, "ZAR", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("CurrencyCode must be ZAR.", nameof(CurrencyCode));

        if (RedirectedAt.HasValue)
        {
            if (string.IsNullOrWhiteSpace(ProviderSessionReference))
                throw new ArgumentException("ProviderSessionReference is required after redirect initiation.", nameof(ProviderSessionReference));

            if (string.IsNullOrWhiteSpace(ReturnCorrelationToken))
                throw new ArgumentException("ReturnCorrelationToken is required after redirect initiation.", nameof(ReturnCorrelationToken));
        }

        if (Status == WalletTopUpAttemptStatus.Completed)
        {
            if (!ConfirmedChargedAmount.HasValue)
                throw new ArgumentException("ConfirmedChargedAmount is required for completed attempts.", nameof(ConfirmedChargedAmount));

            if (!CreditedWalletActivityId.HasValue)
                throw new ArgumentException("CreditedWalletActivityId is required for completed attempts.", nameof(CreditedWalletActivityId));
        }
    }

    private void EnsurePendingLike()
    {
        if (IsFinalForSettlement)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");
    }

    private static void EnsureTwoDecimalPlaces(decimal amount, string propertyName)
    {
        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Amount must use no more than two decimal places.", propertyName);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
