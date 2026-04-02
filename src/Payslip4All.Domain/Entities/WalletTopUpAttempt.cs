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
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? AuthoritativeOutcomeAcceptedAt { get; set; }
    public DateTimeOffset? HostedPageDeadline { get; set; }
    public DateTimeOffset AbandonAfterUtc { get; set; }

    public WalletTopUpAttempt()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
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

        attempt.AbandonAfterUtc = attempt.CreatedAt.AddHours(1);
        attempt.EnsureValid();
        return attempt;
    }

    public void RegisterHostedSession(string providerSessionReference, string returnCorrelationToken, DateTimeOffset? expiresAt)
    {
        EnsureMutable();

        if (string.IsNullOrWhiteSpace(providerSessionReference))
            throw new ArgumentException("Provider session reference is required.", nameof(providerSessionReference));

        if (string.IsNullOrWhiteSpace(returnCorrelationToken))
            throw new ArgumentException("Return correlation token is required.", nameof(returnCorrelationToken));

        ProviderSessionReference = providerSessionReference.Trim();
        ReturnCorrelationToken = returnCorrelationToken.Trim();
        RedirectedAt = DateTimeOffset.UtcNow;
        HostedPageDeadline = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
        EnsureValid();
    }

    public void MarkPendingValidation(DateTimeOffset validatedAt, string? failureCode, string? failureMessage)
    {
        EnsureMutable();
        LastValidatedAt = validatedAt;
        LastEvaluatedAt = validatedAt;
        OutcomeReasonCode = Normalize(failureCode);
        OutcomeMessage = Normalize(failureMessage);
        FailureCode = OutcomeReasonCode;
        FailureMessage = OutcomeMessage;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void RecordValidatedSuccess(decimal confirmedChargedAmount, string? providerPaymentReference, DateTimeOffset validatedAt)
    {
        EnsureMutable();
        WalletCalculator.ValidateAmount(confirmedChargedAmount);

        ConfirmedChargedAmount = confirmedChargedAmount;
        ProviderPaymentReference = Normalize(providerPaymentReference);
        LastValidatedAt = validatedAt;
        LastEvaluatedAt = validatedAt;
        OutcomeReasonCode = null;
        OutcomeMessage = null;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void MarkCompleted(decimal confirmedChargedAmount, string? providerPaymentReference, DateTimeOffset validatedAt, Guid creditedWalletActivityId)
    {
        EnsureMutable();
        WalletCalculator.ValidateAmount(confirmedChargedAmount);

        if (creditedWalletActivityId == Guid.Empty)
            throw new ArgumentException("Credited wallet activity id is required.", nameof(creditedWalletActivityId));

        ConfirmedChargedAmount = confirmedChargedAmount;
        ProviderPaymentReference = Normalize(providerPaymentReference);
        CreditedWalletActivityId = creditedWalletActivityId;
        Status = WalletTopUpAttemptStatus.Completed;
        LastValidatedAt = validatedAt;
        LastEvaluatedAt = validatedAt;
        CompletedAt = validatedAt;
        AuthoritativeOutcomeAcceptedAt = validatedAt;
        OutcomeReasonCode = null;
        OutcomeMessage = null;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    /// <summary>Legacy method kept for backward compatibility with pre-008 data. Do not use in new code — use MarkUnverified instead.</summary>
    [Obsolete("Use MarkUnverified instead. Failed status is no longer used in new top-up flows.")]
    public void MarkFailed(string? failureCode, string? failureMessage, DateTimeOffset validatedAt)
        => MarkFinal(WalletTopUpAttemptStatus.Failed, failureCode, failureMessage, validatedAt);

    public void MarkCancelled(string? failureCode, string? failureMessage, DateTimeOffset validatedAt)
        => MarkFinal(WalletTopUpAttemptStatus.Cancelled, failureCode, failureMessage, validatedAt);

    public void MarkExpired(string? failureCode, string? failureMessage, DateTimeOffset validatedAt)
        => MarkFinal(WalletTopUpAttemptStatus.Expired, failureCode, failureMessage, validatedAt);

    public void MarkAbandoned(DateTimeOffset at)
    {
        if (Status is WalletTopUpAttemptStatus.Completed or WalletTopUpAttemptStatus.Cancelled or WalletTopUpAttemptStatus.Expired)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        Status = WalletTopUpAttemptStatus.Abandoned;
        LastEvaluatedAt = at;
        OutcomeReasonCode = "abandoned";
        OutcomeMessage = "The hosted payment session was abandoned.";
        FailureCode = OutcomeReasonCode;
        FailureMessage = OutcomeMessage;
        UpdatedAt = at;
        EnsureValid();
    }

    public void MarkUnverified(string? reasonCode, string? message, DateTimeOffset at)
    {
        if (Status is WalletTopUpAttemptStatus.Completed or WalletTopUpAttemptStatus.Cancelled or WalletTopUpAttemptStatus.Expired)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        Status = WalletTopUpAttemptStatus.Unverified;
        LastEvaluatedAt = at;
        OutcomeReasonCode = Normalize(reasonCode);
        OutcomeMessage = Normalize(message);
        FailureCode = OutcomeReasonCode;
        FailureMessage = OutcomeMessage;
        UpdatedAt = at;
        EnsureValid();
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

        if (Status is WalletTopUpAttemptStatus.Completed or WalletTopUpAttemptStatus.Cancelled or WalletTopUpAttemptStatus.Expired)
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");

        AuthoritativeEvidenceId = evidenceId;
        AuthoritativeOutcomeAcceptedAt = at;
        LastEvaluatedAt = at;
        ProviderPaymentReference = Normalize(paymentRef) ?? ProviderPaymentReference;
        ConfirmedChargedAmount = confirmedAmount ?? ConfirmedChargedAmount;
        CreditedWalletActivityId = creditedActivityId ?? CreditedWalletActivityId;
        OutcomeReasonCode = null;
        OutcomeMessage = null;
        FailureCode = null;
        FailureMessage = null;

        Status = claimedOutcome switch
        {
            PaymentReturnClaimedOutcome.Completed => WalletTopUpAttemptStatus.Completed,
            PaymentReturnClaimedOutcome.Cancelled => WalletTopUpAttemptStatus.Cancelled,
            PaymentReturnClaimedOutcome.Expired => WalletTopUpAttemptStatus.Expired,
            _ => WalletTopUpAttemptStatus.Unverified
        };

        if (Status == WalletTopUpAttemptStatus.Completed)
        {
            if (!ConfirmedChargedAmount.HasValue)
                throw new ArgumentException("Confirmed amount is required for completed outcomes.", nameof(confirmedAmount));

            CompletedAt = at;
        }
        else if (Status is WalletTopUpAttemptStatus.Cancelled or WalletTopUpAttemptStatus.Expired)
        {
            CompletedAt = at;
        }

        UpdatedAt = at;
        EnsureValid();
    }

    public void EnsureValid()
    {
        if (UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(UserId));

        if (string.IsNullOrWhiteSpace(ProviderKey))
            throw new ArgumentException("ProviderKey is required.", nameof(ProviderKey));

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

        if (AbandonAfterUtc != CreatedAt.AddHours(1))
            throw new ArgumentException("AbandonAfterUtc must be exactly one hour after CreatedAt.", nameof(AbandonAfterUtc));

        if (Status == WalletTopUpAttemptStatus.Completed)
        {
            if (!ConfirmedChargedAmount.HasValue)
                throw new ArgumentException("ConfirmedChargedAmount is required for completed attempts.", nameof(ConfirmedChargedAmount));

            if (!CreditedWalletActivityId.HasValue)
                throw new ArgumentException("CreditedWalletActivityId is required for completed attempts.", nameof(CreditedWalletActivityId));
        }
    }

    private void MarkFinal(WalletTopUpAttemptStatus finalStatus, string? failureCode, string? failureMessage, DateTimeOffset validatedAt)
    {
        EnsureMutable();
        Status = finalStatus;
        LastValidatedAt = validatedAt;
        LastEvaluatedAt = validatedAt;
        CompletedAt = validatedAt;
        AuthoritativeOutcomeAcceptedAt = validatedAt;
        OutcomeReasonCode = Normalize(failureCode);
        OutcomeMessage = Normalize(failureMessage);
        FailureCode = OutcomeReasonCode;
        FailureMessage = OutcomeMessage;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    private void EnsureMutable()
    {
#pragma warning disable CS0618 // Failed is legacy but still guards the entity against mutation
        if (Status is WalletTopUpAttemptStatus.Completed
            or WalletTopUpAttemptStatus.Failed
            or WalletTopUpAttemptStatus.Cancelled
            or WalletTopUpAttemptStatus.Expired
            or WalletTopUpAttemptStatus.Abandoned
            or WalletTopUpAttemptStatus.Unverified)
#pragma warning restore CS0618
        {
            throw new InvalidOperationException("Wallet top-up attempt is already in a final state.");
        }
    }

    private static void EnsureTwoDecimalPlaces(decimal amount, string propertyName)
    {
        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Amount must use no more than two decimal places.", propertyName);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
