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
    public Guid? CreditedWalletActivityId { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? RedirectedAt { get; set; }
    public DateTimeOffset? LastValidatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? HostedPageDeadline { get; set; }

    public WalletTopUpAttempt()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
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

    public void RegisterHostedSession(string providerSessionReference, string returnCorrelationToken, DateTimeOffset? expiresAt)
    {
        EnsureNotFinal();

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
        EnsureNotFinal();
        LastValidatedAt = validatedAt;
        FailureCode = Normalize(failureCode);
        FailureMessage = Normalize(failureMessage);
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void RecordValidatedSuccess(decimal confirmedChargedAmount, string? providerPaymentReference, DateTimeOffset validatedAt)
    {
        EnsureNotFinal();
        WalletCalculator.ValidateAmount(confirmedChargedAmount);

        ConfirmedChargedAmount = confirmedChargedAmount;
        ProviderPaymentReference = Normalize(providerPaymentReference);
        LastValidatedAt = validatedAt;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void MarkCompleted(decimal confirmedChargedAmount, string? providerPaymentReference, DateTimeOffset validatedAt, Guid creditedWalletActivityId)
    {
        EnsureNotFinal();
        WalletCalculator.ValidateAmount(confirmedChargedAmount);

        if (creditedWalletActivityId == Guid.Empty)
            throw new ArgumentException("Credited wallet activity id is required.", nameof(creditedWalletActivityId));

        ConfirmedChargedAmount = confirmedChargedAmount;
        ProviderPaymentReference = Normalize(providerPaymentReference);
        CreditedWalletActivityId = creditedWalletActivityId;
        Status = WalletTopUpAttemptStatus.Completed;
        LastValidatedAt = validatedAt;
        CompletedAt = validatedAt;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    public void MarkFailed(string? failureCode, string? failureMessage, DateTimeOffset validatedAt)
        => MarkFinal(WalletTopUpAttemptStatus.Failed, failureCode, failureMessage, validatedAt);

    public void MarkCancelled(string? failureCode, string? failureMessage, DateTimeOffset validatedAt)
        => MarkFinal(WalletTopUpAttemptStatus.Cancelled, failureCode, failureMessage, validatedAt);

    public void MarkExpired(string? failureCode, string? failureMessage, DateTimeOffset validatedAt)
        => MarkFinal(WalletTopUpAttemptStatus.Expired, failureCode, failureMessage, validatedAt);

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
        EnsureNotFinal();
        Status = finalStatus;
        LastValidatedAt = validatedAt;
        CompletedAt = validatedAt;
        FailureCode = Normalize(failureCode);
        FailureMessage = Normalize(failureMessage);
        UpdatedAt = validatedAt;
        EnsureValid();
    }

    private void EnsureNotFinal()
    {
        if (Status != WalletTopUpAttemptStatus.Pending)
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
